using System.Text;
using System.Buffers.Binary;

namespace TLTool;

public sealed class BigEndianBinaryReader : BinaryReader
{
    public BigEndianBinaryReader(Stream input)
        : base(input)
    {
    }

    public BigEndianBinaryReader(Stream input, Encoding encoding)
        : base(input, encoding)
    {
    }

    public BigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen)
        : base(input, encoding, leaveOpen)
    {
    }

    public override double ReadDouble()
    {
        return BitConverter.UInt64BitsToDouble(ReadUInt64());
    }

    public override Half ReadHalf()
    {
        return BitConverter.UInt16BitsToHalf(ReadUInt16());
    }

    public override short ReadInt16()
    {
        return BinaryPrimitives.ReverseEndianness(base.ReadInt16());
    }

    public override int ReadInt32()
    {
        return BinaryPrimitives.ReverseEndianness(base.ReadInt32());
    }

    public override long ReadInt64()
    {
        return BinaryPrimitives.ReverseEndianness(base.ReadInt64());
    }

    public override float ReadSingle()
    {
        return BitConverter.UInt32BitsToSingle(ReadUInt32());
    }

    public override ushort ReadUInt16()
    {
        return BinaryPrimitives.ReverseEndianness(base.ReadUInt16());
    }

    public override uint ReadUInt32()
    {
        return BinaryPrimitives.ReverseEndianness(base.ReadUInt32());
    }

    public override ulong ReadUInt64()
    {
        return BinaryPrimitives.ReverseEndianness(base.ReadUInt64());
    }
}
