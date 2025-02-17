using System.Diagnostics.CodeAnalysis;

namespace TLTool;

/// <summary>Wraps a <see cref="Stream"/> to provide endianness tracking.</summary>
public struct BinaryStream
{
    /// <summary>Gets the stream being wrapped by this <see cref="BinaryStream"/>.</summary>
    public Stream BaseStream { get; }

    /// <summary><see langword="true"/> if this <see cref="BinaryStream"/> is operating in big endian mode.</summary>
    public bool IsBigEndian { get; set; }

    /// <inheritdoc cref="Stream.CanRead"/>
    public readonly bool CanRead => BaseStream.CanRead;

    /// <inheritdoc cref="Stream.CanWrite"/>
    public readonly bool CanWrite => BaseStream.CanWrite;

    /// <inheritdoc cref="Stream.CanSeek"/>
    public readonly bool CanSeek => BaseStream.CanSeek;

    /// <inheritdoc cref="Stream.Length"/>
    public readonly long Length => BaseStream.Length;

    /// <inheritdoc cref="Stream.Position"/>
    public readonly long Position
    {
        get => BaseStream.Position;
        set => BaseStream.Position = value;
    }

    /// <summary>Initializes a new <see cref="BinaryStream"/> instance.</summary>
    public BinaryStream(Stream stream, bool bigEndian)
    {
        ArgumentNullException.ThrowIfNull(stream);
        BaseStream = stream;
        IsBigEndian = bigEndian;
    }

    /// <summary>Reads a <see cref="byte"/> from the stream.</summary>
    public readonly byte ReadByte()
    {
        int value = BaseStream.ReadByte();

        if (value == -1)
            ThrowEndOfStreamException();

        return (byte)value;
    }

    /// <inheritdoc cref="Stream.Write(ReadOnlySpan{byte})"/>
    public readonly void Write(ReadOnlySpan<byte> buffer) => BaseStream.Write(buffer);

    /// <inheritdoc cref="Stream.ReadExactly(Span{byte})"/>
    public readonly ReadOnlySpan<byte> ReadExactly(Span<byte> buffer)
    {
        BaseStream.ReadExactly(buffer);
        return buffer;
    }

    /// <inheritdoc cref="Stream.ReadAtLeast(Span{byte}, int, bool)"/>
    public readonly int ReadAtLeast(Span<byte> buffer, int minimumBytes, bool throwOnEndOfStream = true) => BaseStream.ReadAtLeast(buffer, minimumBytes, throwOnEndOfStream);

    /// <summary>Reads an <see cref="sbyte"/> from the stream.</summary>
    public readonly sbyte ReadSByte() => unchecked((sbyte)ReadByte());

    /// <summary>Reads a <see cref="short"/> from the stream.</summary>
    public readonly short ReadInt16() => IsBigEndian ? BaseStream.ReadInt16BigEndian() : BaseStream.ReadInt16LittleEndian();

    /// <summary>Reads a <see cref="ushort"/> from the stream.</summary>
    public readonly ushort ReadUInt16() => IsBigEndian ? BaseStream.ReadUInt16BigEndian() : BaseStream.ReadUInt16LittleEndian();

    /// <summary>Reads a 24-bit <see cref="int"/> from the stream.</summary>
    public readonly int ReadInt24() => IsBigEndian ? BaseStream.ReadInt24BigEndian() : BaseStream.ReadInt24LittleEndian();

    /// <summary>Reads a 24-bit <see cref="uint"/> from the stream.</summary>
    public readonly uint ReadUInt24() => IsBigEndian ? BaseStream.ReadUInt24BigEndian() : BaseStream.ReadUInt24LittleEndian();

    /// <summary>Reads an <see cref="int"/> from the stream.</summary>
    public readonly int ReadInt32() => IsBigEndian ? BaseStream.ReadInt32BigEndian() : BaseStream.ReadInt32LittleEndian();

    /// <summary>Reads a <see cref="uint"/> from the stream.</summary>
    public readonly uint ReadUInt32() => IsBigEndian ? BaseStream.ReadUInt32BigEndian() : BaseStream.ReadUInt32LittleEndian();

    /// <summary>Reads a 40-bit <see cref="long"/> from the stream.</summary>
    public readonly long ReadInt40() => IsBigEndian ? BaseStream.ReadInt40BigEndian() : BaseStream.ReadInt40LittleEndian();

    /// <summary>Reads a 40-bit <see cref="ulong"/> from the stream.</summary>
    public readonly ulong ReadUInt40() => IsBigEndian ? BaseStream.ReadUInt40BigEndian() : BaseStream.ReadUInt40LittleEndian();

    /// <summary>Reads an <see cref="long"/> from the stream.</summary>
    public readonly long ReadInt64() => IsBigEndian ? BaseStream.ReadInt64BigEndian() : BaseStream.ReadInt64LittleEndian();

    /// <summary>Reads a <see cref="ulong"/> from the stream.</summary>
    public readonly ulong ReadUInt64() => IsBigEndian ? BaseStream.ReadUInt64BigEndian() : BaseStream.ReadUInt64LittleEndian();

    /// <summary>Reads an <see cref="Int128"/> from the stream.</summary>
    public readonly Int128 ReadInt128() => IsBigEndian ? BaseStream.ReadInt128BigEndian() : BaseStream.ReadInt128LittleEndian();

    /// <summary>Reads a <see cref="UInt128"/> from the stream.</summary>
    public readonly UInt128 ReadUInt128() => IsBigEndian ? BaseStream.ReadUInt128BigEndian() : BaseStream.ReadUInt128LittleEndian();

    /// <summary>Reads a <see cref="nint"/> from the stream.</summary>
    public readonly nint ReadIntPtr() => IsBigEndian ? BaseStream.ReadIntPtrBigEndian() : BaseStream.ReadIntPtrLittleEndian();

    /// <summary>Reads a <see cref="nuint"/> from the stream.</summary>
    public readonly nuint ReadUIntPtr() => IsBigEndian ? BaseStream.ReadUIntPtrBigEndian() : BaseStream.ReadUIntPtrLittleEndian();

    /// <summary>Reads a <see cref="Half"/> from the stream.</summary>
    public readonly Half ReadHalf() => IsBigEndian ? BaseStream.ReadHalfBigEndian() : BaseStream.ReadHalfLittleEndian();

    /// <summary>Reads a <see cref="float"/> from the stream.</summary>
    public readonly float ReadSingle() => IsBigEndian ? BaseStream.ReadSingleBigEndian() : BaseStream.ReadSingleLittleEndian();

    /// <summary>Reads a <see cref="double"/> from the stream.</summary>
    public readonly double ReadDouble() => IsBigEndian ? BaseStream.ReadDoubleBigEndian() : BaseStream.ReadDoubleLittleEndian();

    /// <inheritdoc cref="Stream.WriteByte"/>
    public readonly void WriteByte(byte value) => BaseStream.WriteByte(value);

    /// <summary>Writes an <see cref="sbyte"/> to the stream.</summary>
    public readonly void WriteSByte(sbyte value) => BaseStream.WriteByte(unchecked((byte)value));

    /// <summary>Writes a <see cref="short"/> to the stream.</summary>
    public readonly void WriteInt16(short value) { if (IsBigEndian) BaseStream.WriteInt16BigEndian(value); else BaseStream.WriteInt16LittleEndian(value); }

    /// <summary>Writes a <see cref="ushort"/> to the stream.</summary>
    public readonly void WriteUInt16(ushort value) { if (IsBigEndian) BaseStream.WriteUInt16BigEndian(value); else BaseStream.WriteUInt16LittleEndian(value); }

    /// <summary>Writes a 24-bit <see cref="int"/> to the stream.</summary>
    public readonly void WriteInt24(int value) { if (IsBigEndian) BaseStream.WriteInt24BigEndian(value); else BaseStream.WriteInt24LittleEndian(value); }

    /// <summary>Writes a 24-bit <see cref="uint"/> to the stream.</summary>
    public readonly void WriteUInt24(uint value) { if (IsBigEndian) BaseStream.WriteUInt24BigEndian(value); else BaseStream.WriteUInt24LittleEndian(value); }

    /// <summary>Writes a <see cref="short"/> to the stream.</summary>
    public readonly void WriteInt32(int value) { if (IsBigEndian) BaseStream.WriteInt32BigEndian(value); else BaseStream.WriteInt32LittleEndian(value); }

    /// <summary>Writes a <see cref="ushort"/> to the stream.</summary>
    public readonly void WriteUInt32(uint value) { if (IsBigEndian) BaseStream.WriteUInt32BigEndian(value); else BaseStream.WriteUInt32LittleEndian(value); }

    /// <summary>Writes a 40-bit <see cref="long"/> to the stream.</summary>
    public readonly void WriteInt40(long value) { if (IsBigEndian) BaseStream.WriteInt40BigEndian(value); else BaseStream.WriteInt40LittleEndian(value); }

    /// <summary>Writes a 40-bit <see cref="ulong"/> to the stream.</summary>
    public readonly void WriteUInt40(ulong value) { if (IsBigEndian) BaseStream.WriteUInt40BigEndian(value); else BaseStream.WriteUInt40LittleEndian(value); }

    /// <summary>Writes a <see cref="long"/> to the stream.</summary>
    public readonly void WriteInt64(long value) { if (IsBigEndian) BaseStream.WriteInt64BigEndian(value); else BaseStream.WriteInt64LittleEndian(value); }

    /// <summary>Writes a <see cref="ulong"/> to the stream.</summary>
    public readonly void WriteUInt64(ulong value) { if (IsBigEndian) BaseStream.WriteUInt64BigEndian(value); else BaseStream.WriteUInt64LittleEndian(value); }

    /// <summary>Writes a <see cref="Int128"/> to the stream.</summary>
    public readonly void WriteInt128(Int128 value) { if (IsBigEndian) BaseStream.WriteInt128BigEndian(value); else BaseStream.WriteInt128LittleEndian(value); }

    /// <summary>Writes a <see cref="UInt128"/> to the stream.</summary>
    public readonly void WriteUInt128(UInt128 value) { if (IsBigEndian) BaseStream.WriteUInt128BigEndian(value); else BaseStream.WriteUInt128LittleEndian(value); }

    /// <summary>Writes a <see cref="nint"/> to the stream.</summary>
    public readonly void WriteIntPtr(nint value) { if (IsBigEndian) BaseStream.WriteIntPtrBigEndian(value); else BaseStream.WriteIntPtrLittleEndian(value); }

    /// <summary>Writes a <see cref="nuint"/> to the stream.</summary>
    public readonly void WriteUIntPtr(nuint value) { if (IsBigEndian) BaseStream.WriteUIntPtrBigEndian(value); else BaseStream.WriteUIntPtrLittleEndian(value); }

    /// <summary>Writes a <see cref="Half"/> to the stream.</summary>
    public readonly void WriteHalf(Half value) { if (IsBigEndian) BaseStream.WriteHalfBigEndian(value); else BaseStream.WriteHalfLittleEndian(value); }

    /// <summary>Writes a <see cref="float"/> to the stream.</summary>
    public readonly void WriteSingle(float value) { if (IsBigEndian) BaseStream.WriteSingleBigEndian(value); else BaseStream.WriteSingleLittleEndian(value); }

    /// <summary>Writes a <see cref="double"/> to the stream.</summary>
    public readonly void WriteDouble(double value) { if (IsBigEndian) BaseStream.WriteDoubleBigEndian(value); else BaseStream.WriteDoubleLittleEndian(value); }

    /// <inheritdoc cref="Stream.Seek"/>
    public readonly long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);

    /// <summary>Throws <see cref="EndOfStreamException"/>.</summary>
    [DoesNotReturn]
    private static void ThrowEndOfStreamException() => throw new EndOfStreamException();
}
