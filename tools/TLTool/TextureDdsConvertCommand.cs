using System.CommandLine;
using System.CommandLine.Invocation;

namespace TLTool;

public sealed class TextureDdsConvertCommand
{
    public Command Command { get; } = new("tex2dds");

    public Argument<string> MetaPath { get; } = new("meta-path", "Path to the TOTEXB_D file");

    public Argument<string> DataPath { get; } = new("data-path", "Path to the TOTEXP_P file");

    public Argument<string> OutputPath { get; } = new("output-path", "Path of resulting DDS file");

    public Option<string> Format { get; } = new("--platform", "Platform that the texture comes from [pc (default), ps3, ps4]");

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
        var platform = context.ParseResult.GetValueForOption(Format) ?? "pc";
        var output = context.ParseResult.GetValueForArgument(OutputPath);
        var meta = context.ParseResult.GetValueForArgument(MetaPath);
        var data = File.ReadAllBytes(context.ParseResult.GetValueForArgument(DataPath));

        if (platform == "pc")
        {
            // On PC, the textures are already in DDS format.
            // They just have some kind of 4 byte value before it.
            File.WriteAllBytes(output, data[4..]);
        }
        else if (platform == "ps3")
        {
            throw new NotSupportedException("PS3 textures are not yet supported.");
        }
        else if (platform == "ps4")
        {
            var buffer = new byte[data.Length];
            using var reader = new BinaryReader(File.OpenRead(meta));

            // The format, width and height are located at offset 0x1D.
            reader.BaseStream.Position = 0x1D;
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

    private enum TextureFormat
    {
        DXT5 = 1,
        DXT1 = 3
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
            TextureFormat.DXT5 => 0x35545844,
            TextureFormat.DXT1 => 0x31545844,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    // Swizzle code from PDTools https://github.com/Nenkai/PDTools/blob/master/PDTools.Files/Textures/PS4/OrbisTexture.cs#L101
    private static void OrbisUnSwizzle(Span<byte> input, Span<byte> output, int width, int height, int blockSize)
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
