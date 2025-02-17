namespace TLTool;

public struct DdsPixelFormat
{
    public uint Flags;
    public uint FourCC;
    public uint RGBBitCount;
    public uint RBitMask;
    public uint GBitMask;
    public uint BBitMask;
    public uint ABitMask;

    public readonly void Write(BinaryStream writer)
    {
        writer.WriteUInt32(0x20);
        writer.WriteUInt32(Flags);
        writer.WriteUInt32(FourCC);
        writer.WriteUInt32(RGBBitCount);
        writer.WriteUInt32(RBitMask);
        writer.WriteUInt32(GBitMask);
        writer.WriteUInt32(BBitMask);
        writer.WriteUInt32(ABitMask);
    }
}
