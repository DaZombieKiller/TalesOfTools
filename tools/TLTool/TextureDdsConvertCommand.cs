using System.CommandLine;
using System.CommandLine.Invocation;
using System.Numerics;
using System.Runtime.InteropServices;

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
            return;
        }

        var buffer = data;
        var header = new DdsHeader
        {
            Flags = 0x1007, // DDS_HEADER_FLAGS_TEXTURE
            MipMapCount = 1,
            Caps = 0x1000, // DDSCAPS_TEXTURE
        };

        using var stream = File.OpenRead(meta);
        using var reader = platform == Platform.PS3 ? new BigEndianBinaryReader(stream) : new BinaryReader(stream);

        // Determine file format
        var isMTex = stream.ReadUInt32LittleEndian() == MTEX;

        // Read texture usage, width and height
        stream.Position = isMTex ? 0x0D : 0x1D;
        var usage = (TextureUsage)reader.ReadByte();
        header.Width = reader.ReadUInt16();
        header.Height = reader.ReadUInt16();
        header.Depth = reader.ReadUInt16();
        var ddsFormat = TextureFormat.Unknown;

        if (header.Depth > 0)
        {
            header.Flags |= 0x800000; // DDSD_DEPTH
            header.Caps |= 0x8; // DDSCAPS_COMPLEX;
            header.Caps2 |= 0x200000; // DDSCAPS2_VOLUME
        }

        if (platform == Platform.PS3)
        {
            var format = new CellTextureFormat(reader.ReadByte());
            ddsFormat = format.ColorType switch
            {
                CellColorType.DXT1 => TextureFormat.DXT1,
                CellColorType.DXT45 => TextureFormat.DXT5,
                CellColorType.A8B8G8R8 => TextureFormat.B8G8R8A8,
                CellColorType.A4B4G4R4 => TextureFormat.B4G4R4A4,
                _ => throw new NotSupportedException(format.ColorType.ToString())
            };

            // Compressed textures are seemingly always linear, even if not specified as such.
            if (format.IsSwizzled && !format.IsCompressed)
            {
                buffer = new byte[data.Length];
                CellUnSwizzle(data, buffer, (int)header.Width, (int)header.Height, blockSize: GetBlockSize(ddsFormat));
            }
        }
        else if (platform == Platform.PS4)
        {
            // TODO: Get format properly
            ddsFormat = usage switch
            {
                TextureUsage.World => TextureFormat.DXT1,
                TextureUsage.Interface => TextureFormat.DXT5,
                _ => TextureFormat.Unknown
            };

            // Un-swizzle texture data.
            buffer = new byte[data.Length];
            OrbisUnSwizzle(data, buffer, (int)header.Width, (int)header.Height, blockSize: GetBlockSize(ddsFormat));
        }
        else
        {
            throw new ArgumentException($"Unknown platform '{platform}'.");
        }

        if (ddsFormat is TextureFormat.DXT1 or TextureFormat.DXT5)
        {
            header.Flags |= 0x80000; // DDSD_LINEARSIZE
            header.PitchOrLinearSize = header.Height * uint.Max(1, (header.Width + 3) / 4) * (uint)GetBlockSize(ddsFormat);
            header.PixelFormat = new DdsPixelFormat
            {
                Flags = 0x4, // DDPF_FOURCC
                FourCC = GetFourCC(ddsFormat)
            };
        }
        else
        {
            var blockSize = GetBlockSize(ddsFormat);
            header.Flags |= 0x8; // DDSD_PITCH
            header.PitchOrLinearSize = header.Width * ((uint)blockSize * 8 + 7) / 8;
            header.PixelFormat = new DdsPixelFormat
            {
                Flags = 0x1 | 0x40, // DDPF_ALPHAPIXELS | DDPF_RGB
                RGBBitCount = (uint)blockSize * 8,
            };

            if (ddsFormat == TextureFormat.B8G8R8A8)
            {
                header.PixelFormat.BBitMask = 0x000000FF;
                header.PixelFormat.GBitMask = 0x0000FF00;
                header.PixelFormat.RBitMask = 0x00FF0000;
                header.PixelFormat.ABitMask = 0xFF000000;

                SwizzleFormat<uint>(buffer, header.PixelFormat, fromFormat: header.PixelFormat with
                {
                    ABitMask = 0x000000FF,
                    RBitMask = 0x0000FF00,
                    GBitMask = 0x00FF0000,
                    BBitMask = 0xFF000000,
                });
            }
            else if (ddsFormat == TextureFormat.B4G4R4A4)
            {
                header.PixelFormat.BBitMask = 0x000F;
                header.PixelFormat.GBitMask = 0x00F0;
                header.PixelFormat.RBitMask = 0x0F00;
                header.PixelFormat.ABitMask = 0xF000;

                SwizzleFormat<ushort>(buffer, header.PixelFormat, fromFormat: header.PixelFormat with
                {
                    ABitMask = 0x000F,
                    BBitMask = 0x00F0,
                    GBitMask = 0x0F00,
                    RBitMask = 0xF000,
                });
            }
        }

        using var writer = new BinaryWriter(File.Create(output));
        header.Write(writer);
        writer.Write(buffer);
    }

    private static unsafe void SwizzleFormat<T>(Span<byte> buffer, in DdsPixelFormat toFormat, in DdsPixelFormat fromFormat)
        where T : unmanaged, IBinaryInteger<T>
    {
        if (toFormat.RGBBitCount != sizeof(T) * 8)
            throw new ArgumentException(null, nameof(toFormat));

        if (fromFormat.RGBBitCount != sizeof(T) * 8)
            throw new ArgumentException(null, nameof(fromFormat));

        for (int i = 0; i < buffer.Length; i += sizeof(T))
        {
            var span = buffer[i..];
            var chan = MemoryMarshal.Read<T>(span);

            // Read channels
            T r = GetChannel(chan, fromFormat.RBitMask);
            T g = GetChannel(chan, fromFormat.GBitMask);
            T b = GetChannel(chan, fromFormat.BBitMask);
            T a = GetChannel(chan, fromFormat.ABitMask);

            // Write channels
            MemoryMarshal.Write(span,
                CreateChannel(r, toFormat.RBitMask) |
                CreateChannel(g, toFormat.GBitMask) |
                CreateChannel(b, toFormat.BBitMask) |
                CreateChannel(a, toFormat.ABitMask)
            );
        }

        static T GetChannel(T channels, uint mask)
        {
            return (channels & T.CreateTruncating(mask)) >>> BitOperations.TrailingZeroCount(mask);
        }

        static T CreateChannel(T value, uint mask)
        {
            return (value << BitOperations.TrailingZeroCount(mask)) & T.CreateTruncating(mask);
        }
    }

    private enum Platform
    {
        None,
        PC,
        PS3,
        PS4,
    }

    private enum TextureUsage
    {
        Interface = 1,
        World = 3
    }

    private enum TextureFormat
    {
        Unknown,
        DXT1,
        DXT5,
        B8G8R8A8,
        B4G4R4A4,
    }

    private readonly struct CellTextureFormat
    {
        private readonly byte _value;

        public readonly bool IsSwizzled => !Flags.HasFlag(CellTextureFlags.Linear);

        public readonly bool IsCompressed => ColorType switch
        {
            CellColorType.DXT1 => true,
            CellColorType.DXT45 => true,
            _ => false
        };

        public readonly CellColorType ColorType => (CellColorType)(_value & ~(0x60));

        public readonly CellTextureFlags Flags => (CellTextureFlags)(_value & (0x60));

        public CellTextureFormat(byte format)
        {
            _value = format;
        }

        public CellTextureFormat(CellColorType color, CellTextureFlags flags)
        {
            _value = (byte)((byte)color | (byte)flags);
        }
    }

    private enum CellColorType
    {
        A4B4G4R4 = 0x83,
        A8B8G8R8 = 0x85,
        DXT1 = 0x86,
        DXT45 = 0x88,
    }

    [Flags]
    private enum CellTextureFlags
    {
        None = 0,
        Linear = 0x20,
    }

    private static int GetBlockSize(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.DXT1 => 8,
            TextureFormat.DXT5 => 16,
            TextureFormat.B8G8R8A8 => 4,
            TextureFormat.B4G4R4A4 => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    private static uint GetFourCC(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.DXT1 => 'D' | ('X' << 8) | ('T' << 16) | ('1' << 24),
            TextureFormat.DXT5 => 'D' | ('X' << 8) | ('T' << 16) | ('5' << 24),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    private static void CellUnSwizzle(ReadOnlySpan<byte> input, Span<byte> output, int width, int height, int blockSize)
    {
        for (int i = 0; i < width * height; i++)
        {
            int pixel = MortonReorder(i, width, height);
            var source = input.Slice(i * blockSize, blockSize);
            var destination = output.Slice(pixel * blockSize, blockSize);
            source.CopyTo(destination);
        }
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
