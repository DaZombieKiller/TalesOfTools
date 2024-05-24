using System.CommandLine;
using System.CommandLine.Invocation;

namespace TLTool;

public sealed class TextureDdsConvertCommand
{
    public Command Command { get; } = new("tex2dds");

    public Argument<string> MetaPath { get; } = new("meta-path", "Path to the TOTEXB/TOTEXB_D file");

    public Argument<string> DataPath { get; } = new("data-path", "Path to the TOTEXP/TOTEXP_P file");

    public Argument<string> OutputPath { get; } = new("output-path", "Path of resulting DDS file");

    public Option<string> Format { get; } = new("--platform", "Platform that the texture comes from [pc (default), ps3, ps4]");

    // Xillia
    private const uint MTEX = 'M' | ('T' << 8) | ('E' << 16) | ('X' << 24);

    // Zestiria, Berseria
    private const uint DPDF = 'D' | ('P' << 8) | ('D' << 16) | ('F' << 24);

    public TextureDdsConvertCommand()
    {
        Command.AddArgument(MetaPath);
        Command.AddArgument(DataPath);
        Command.AddArgument(OutputPath);
        Command.AddOption(Format);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var output = context.ParseResult.GetValueForArgument(OutputPath);
        var meta = context.ParseResult.GetValueForArgument(MetaPath);
        var data = File.ReadAllBytes(context.ParseResult.GetValueForArgument(DataPath));

        var platform = (context.ParseResult.GetValueForOption(Format) ?? "pc") switch
        {
            "pc" => Platform.PC,
            "ps3" => Platform.PS3,
            "ps4" => Platform.PS4,
            _ => Platform.None
        };

        if (platform == Platform.PC)
        {
            // On PC, the textures are already in DDS format.
            // They just have some kind of 4 byte value before it.
            File.WriteAllBytes(output, data[4..]);
        }
        else if (platform == Platform.PS3)
        {
            using var stream = File.OpenRead(meta);
            using var reader = new BigEndianBinaryReader(stream);

            // Determine format
            var isMTex = stream.ReadUInt32LittleEndian() == MTEX;

            // The format, width and height are located at offset 0x0D in TOTEXB.
            stream.Position = isMTex ? 0x0D : 0x1D;
            var format = (PS3TextureFormat)reader.ReadByte();
            var width = reader.ReadUInt16();
            var height = reader.ReadUInt16();

            using var writer = new BinaryWriter(File.Create(output));
            var header = new DdsHeader
            {
                Width = width,
                Height = height,
                Flags = 0x1007, // DDS_HEADER_FLAGS_TEXTURE
                MipMapCount = 1,
            };

            if (format == PS3TextureFormat.DXT1)
            {
                header.PitchOrLinearSize = (uint)width * height;
                header.PixelFormat = new DdsPixelFormat
                {
                    Flags = 0x4, // DDPF_FOURCC
                    FourCC = GetFourCC(TextureFormat.DXT1)
                };
            }
            else if (format == PS3TextureFormat.ARGB)
            {
                for (int i = 0; i < data.Length; i += 4)
                    data.AsSpan(i, 4).Reverse();

                header.PitchOrLinearSize = (uint)width * 4;
                header.PixelFormat = new DdsPixelFormat
                {
                    Flags = 0x1 | 0x40, // DDPF_ALPHAPIXELS | DDPF_RGB
                    RGBBitCount = 32,
                    ABitMask = 0xFF000000,
                    RBitMask = 0x00FF0000,
                    GBitMask = 0x0000FF00,
                    BBitMask = 0x000000FF,
                };
            }

            header.Write(writer);
            writer.Write(data);
        }
        else if (platform == Platform.PS4)
        {
            var buffer = new byte[data.Length];
            using var stream = File.OpenRead(meta);
            using var reader = new BinaryReader(stream);

            // The format, width and height are located at offset 0x1D in TOTEXB_D.
            stream.Position = 0x1D;
            var format = (TextureFormat)reader.ReadByte();
            var width = reader.ReadUInt16();
            var height = reader.ReadUInt16();

            // Un-swizzle texture data.
            OrbisUnSwizzle(data, buffer, width, height, blockSize: GetBlockSize(format));
            
            using var writer = new BinaryWriter(File.Create(output));
            var header       = new DdsHeader
            {
                Width = width,
                Height = height,
                Flags = 0x1007, // DDS_HEADER_FLAGS_TEXTURE
                PitchOrLinearSize = (uint)width * height,
                MipMapCount = 1,
                PixelFormat =
                {
                    Flags = 0x4, // DDPF_FOURCC
                    FourCC = GetFourCC(format)
                },
            };

            header.Write(writer);
            writer.Write(buffer);
        }
        else
        {
            throw new ArgumentException($"Unknown platform '{platform}'.");
        }
    }

    private enum Platform
    {
        None,
        PC,
        PS3,
        PS4,
    }

    private enum TextureFormat
    {
        DXT5 = 1,
        DXT1 = 3
    }

    private enum PS3TextureFormat
    {
        ARGB = 1,
        DXT1 = 3,
    }

    private static int GetBlockSize(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.DXT5 => 16,
            TextureFormat.DXT1 => 8,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    private static uint GetFourCC(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.DXT5 => 'D' | ('X' << 8) | ('T' << 16) | ('5' << 24),
            TextureFormat.DXT1 => 'D' | ('X' << 8) | ('T' << 16) | ('1' << 24),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    // Swizzle code from PDTools https://github.com/Nenkai/PDTools/blob/master/PDTools.Files/Textures/PS4/OrbisTexture.cs#L101
    private static void OrbisUnSwizzle(ReadOnlySpan<byte> input, Span<byte> output, int width, int height, int blockSize)
    {
        var heightTexels = height / 4;
        var heightTexelsAligned = (heightTexels + 7) / 8;
        int widthTexels = width / 4;
        var widthTexelsAligned = (widthTexels + 7) / 8;
        var dataIndex = 0;

        for (int y = 0; y < heightTexelsAligned; y++)
        {
            for (int x = 0; x < widthTexelsAligned; x++)
            {
                for (int t = 0; t < 64; t++)
                {
                    int pixelIndex = MortonReorder(t, 8, 8);
                    int cPixel = pixelIndex / 8;
                    int remPixel = pixelIndex % 8;
                    var yOffset = y * 8 + cPixel;
                    var xOffset = x * 8 + remPixel;

                    if (xOffset < widthTexels && yOffset < heightTexels)
                    {
                        var destPixelIndex = yOffset * widthTexels + xOffset;
                        int destIndex = blockSize * destPixelIndex;
                        input.Slice(dataIndex, blockSize).CopyTo(output.Slice(destIndex, blockSize));
                    }

                    dataIndex += blockSize;
                }
            }
        }
    }

    private static int MortonReorder(int i, int width, int height)
    {
        int x = 1;
        int y = 1;

        int w = width;
        int h = height;

        int index = 0;
        int index2 = 0;

        while (w > 1 || h > 1)
        {
            if (w > 1)
            {
                index += x * (i & 1);
                i >>= 1;
                x *= 2;
                w >>= 1;
            }
            if (h > 1)
            {
                index2 += y * (i & 1);
                i >>= 1;
                y *= 2;
                h >>= 1;
            }
        }

        return index2 * width + index;
    }
}
