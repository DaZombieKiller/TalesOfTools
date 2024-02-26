namespace TLTool;

public unsafe struct DdsHeader
{
    public uint Flags;
    public uint Height;
    public uint Width;
    public uint PitchOrLinearSize;
    public uint Depth;
    public uint MipMapCount;
    public fixed uint Reserved1[11];
    public DdsPixelFormat PixelFormat;
    public uint Caps;
    public uint Caps2;
    public uint Caps3;
    public uint Caps4;
    public uint Reserved2;

    public readonly void Write(BinaryWriter writer)
    {
        writer.Write("DDS "u8);
        writer.Write(0x7C);
        writer.Write(Flags);
        writer.Write(Height);
        writer.Write(Width);
        writer.Write(PitchOrLinearSize);
        writer.Write(Depth);
        writer.Write(MipMapCount);

        fixed (uint* pReserved = Reserved1)
            writer.Write(new ReadOnlySpan<byte>(pReserved, sizeof(uint) * 11));

        PixelFormat.Write(writer);
        writer.Write(Caps);
        writer.Write(Caps2);
        writer.Write(Caps3);
        writer.Write(Caps4);
        writer.Write(Reserved2);
    }
}
