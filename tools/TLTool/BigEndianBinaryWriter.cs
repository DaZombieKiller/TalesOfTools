using System.Buffers.Binary;
using System.Text;

namespace TLTool;

public sealed class BigEndianBinaryWriter : BinaryWriter
{
    public BigEndianBinaryWriter(Stream output)
        : base(output)
    {
    }

    public BigEndianBinaryWriter(Stream output, Encoding encoding)
        : base(output, encoding)
    {
    }

    public BigEndianBinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
        : base(output, encoding, leaveOpen)
    {
    }

    public override void Write(double value)
    {
        Write(BitConverter.DoubleToUInt64Bits(value));
    }

    public override void Write(Half value)
    {
        Write(BitConverter.HalfToUInt16Bits(value));
    }

    public override void Write(short value)
    {
        base.Write(BinaryPrimitives.ReverseEndianness(value));
    }

    public override void Write(int value)
    {
        base.Write(BinaryPrimitives.ReverseEndianness(value));
    }

    public override void Write(long value)
    {
        base.Write(BinaryPrimitives.ReverseEndianness(value));
    }

    public override void Write(float value)
    {
        Write(BitConverter.SingleToUInt32Bits(value));
    }

    public override void Write(ushort value)
    {
        base.Write(BinaryPrimitives.ReverseEndianness(value));
    }

    public override void Write(uint value)
    {
        base.Write(BinaryPrimitives.ReverseEndianness(value));
    }

    public override void Write(ulong value)
    {
        base.Write(BinaryPrimitives.ReverseEndianness(value));
    }
}
