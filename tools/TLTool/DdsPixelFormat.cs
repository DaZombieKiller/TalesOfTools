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

    public readonly void Write(BinaryWriter writer)
    {
        writer.Write(0x20);
        writer.Write(Flags);
        writer.Write(FourCC);
        writer.Write(RGBBitCount);
        writer.Write(RBitMask);
        writer.Write(GBitMask);
        writer.Write(BBitMask);
        writer.Write(ABitMask);
    }
}
