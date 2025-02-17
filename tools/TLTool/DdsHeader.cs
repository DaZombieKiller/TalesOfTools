using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TLTool;

public unsafe struct DdsHeader
{
    public uint Flags;
    public uint Height;
    public uint Width;
    public uint PitchOrLinearSize;
    public uint Depth;
    public uint MipMapCount;
    public Reserved1Buffer Reserved1;
    public DdsPixelFormat PixelFormat;
    public uint Caps;
    public uint Caps2;
    public uint Caps3;
    public uint Caps4;
    public uint Reserved2;

    public readonly void Write(BinaryStream writer)
    {
        writer.Write("DDS "u8);
        writer.WriteUInt32(0x7C);
        writer.WriteUInt32(Flags);
        writer.WriteUInt32(Height);
        writer.WriteUInt32(Width);
        writer.WriteUInt32(PitchOrLinearSize);
        writer.WriteUInt32(Depth);
        writer.WriteUInt32(MipMapCount);
        writer.Write(MemoryMarshal.AsBytes((ReadOnlySpan<uint>)Reserved1));
        PixelFormat.Write(writer);
        writer.WriteUInt32(Caps);
        writer.WriteUInt32(Caps2);
        writer.WriteUInt32(Caps3);
        writer.WriteUInt32(Caps4);
        writer.WriteUInt32(Reserved2);
    }

    [InlineArray(11)]
    public struct Reserved1Buffer
    {
        private uint _e0;
    }
}
