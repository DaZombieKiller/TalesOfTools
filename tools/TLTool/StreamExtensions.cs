using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace TLTool;

/// <summary>Provides extension methods for <see cref="Stream"/>.</summary>
public static class StreamExtensions
{
    /// <summary>Writes padding bytes to the <see cref="Stream"/> until its position is aligned to the specified value.</summary>
    public static void WriteAlign(this Stream stream, long alignment, byte pad = 0x00)
    {
        var aligned = (stream.Position + (alignment - 1)) & ~(alignment - 1);

        while (stream.Position < aligned)
        {
            stream.WriteByte(pad);
        }
    }

    /// <summary>Tracks the current position of the <see cref="Stream"/> and restores it on dispose.</summary>
    public static TemporarySeek TemporarySeek(this Stream stream)
    {
        return new TemporarySeek(stream, stream.Position);
    }

    /// <summary>Seeks the <see cref="Stream"/> to the provided position, tracking its previous position and restoring it on dispose.</summary>
    public static TemporarySeek TemporarySeek(this Stream stream, long position)
    {
        return new TemporarySeek(stream, position);
    }

    /// <summary>Reads a null-terminated string from the stream.</summary>
    public static string ReadTerminatedString(this Stream stream, Encoding encoding)
    {
        if (stream is MemoryStream ms && ms.TryGetBuffer(out var segment))
            return GetTerminatedString(stream, encoding, segment.AsSpan((int)stream.Position));

        static string GetTerminatedString(Stream stream, Encoding encoding, ReadOnlySpan<byte> buffer)
        {
            var size = buffer.IndexOf((byte)0);

            if (size == -1)
                throw new EndOfStreamException();

            stream.Position += size + 1;
            return encoding.GetString(buffer[..size]);
        }

        var writer = new ArrayBufferWriter<byte>();
        var buffer = writer.GetSpan();
        var offset = 0;

        for (int v = stream.ReadByte(); v != 0; v = stream.ReadByte())
        {
            if (v == -1)
                throw new EndOfStreamException();

            buffer[offset++] = (byte)v;

            if (offset == buffer.Length)
            {
                writer.Advance(offset);
                buffer = writer.GetSpan();
                offset = 0;
            }
        }

        writer.Advance(offset);
        return encoding.GetString(writer.WrittenSpan);
    }

    /// <summary>Reads a <see cref="ushort"/> from a stream, as little endian.</summary>
    public static ushort ReadUInt16LittleEndian(this Stream stream) => BinaryPrimitives.ReadUInt16LittleEndian(stream.FillBuffer(stackalloc byte[sizeof(ushort)]));

    /// <summary>Reads a <see cref="short"/> from a stream, as little endian.</summary>
    public static short ReadInt16LittleEndian(this Stream stream) => BinaryPrimitives.ReadInt16LittleEndian(stream.FillBuffer(stackalloc byte[sizeof(short)]));

    /// <summary>Reads a 24-bit <see cref="uint"/> from a stream, as little endian.</summary>
    public static uint ReadUInt24LittleEndian(this Stream stream) => BinaryPrimitivesEx.ReadUInt24LittleEndian(stream.FillBuffer(stackalloc byte[BinaryPrimitivesEx.Int24Size]));

    /// <summary>Reads a 24-bit <see cref="int"/> from a stream, as little endian.</summary>
    public static int ReadInt24LittleEndian(this Stream stream) => BinaryPrimitivesEx.ReadInt24LittleEndian(stream.FillBuffer(stackalloc byte[BinaryPrimitivesEx.Int24Size]));

    /// <summary>Reads a <see cref="uint"/> from a stream, as little endian.</summary>
    public static uint ReadUInt32LittleEndian(this Stream stream) => BinaryPrimitives.ReadUInt32LittleEndian(stream.FillBuffer(stackalloc byte[sizeof(uint)]));

    /// <summary>Reads an <see cref="int"/> from a stream, as little endian.</summary>
    public static int ReadInt32LittleEndian(this Stream stream) => BinaryPrimitives.ReadInt32LittleEndian(stream.FillBuffer(stackalloc byte[sizeof(int)]));

    /// <summary>Reads a 40-bit <see cref="ulong"/> from a stream, as little endian.</summary>
    public static ulong ReadUInt40LittleEndian(this Stream stream) => BinaryPrimitivesEx.ReadUInt40LittleEndian(stream.FillBuffer(stackalloc byte[BinaryPrimitivesEx.Int40Size]));

    /// <summary>Reads a 40-bit <see cref="long"/> from a stream, as little endian.</summary>
    public static long ReadInt40LittleEndian(this Stream stream) => BinaryPrimitivesEx.ReadInt40LittleEndian(stream.FillBuffer(stackalloc byte[BinaryPrimitivesEx.Int40Size]));

    /// <summary>Reads a <see cref="ulong"/> from a stream, as little endian.</summary>
    public static ulong ReadUInt64LittleEndian(this Stream stream) => BinaryPrimitives.ReadUInt64LittleEndian(stream.FillBuffer(stackalloc byte[sizeof(ulong)]));

    /// <summary>Reads a <see cref="long"/> from a stream, as little endian.</summary>
    public static long ReadInt64LittleEndian(this Stream stream) => BinaryPrimitives.ReadInt64LittleEndian(stream.FillBuffer(stackalloc byte[sizeof(long)]));

    /// <summary>Reads a <see cref="UInt128"/> from a stream, as little endian.</summary>
    public static unsafe UInt128 ReadUInt128LittleEndian(this Stream stream) => BinaryPrimitives.ReadUInt128LittleEndian(stream.FillBuffer(stackalloc byte[sizeof(UInt128)]));

    /// <summary>Reads an <see cref="Int128"/> from a stream, as little endian.</summary>
    public static unsafe Int128 ReadInt128LittleEndian(this Stream stream) => BinaryPrimitives.ReadInt128LittleEndian(stream.FillBuffer(stackalloc byte[sizeof(Int128)]));

    /// <summary>Reads a <see cref="nuint"/> from a stream, as little endian.</summary>
    public static unsafe nuint ReadUIntPtrLittleEndian(this Stream stream) => BinaryPrimitives.ReadUIntPtrLittleEndian(stream.FillBuffer(stackalloc byte[sizeof(nuint)]));

    /// <summary>Reads a <see cref="nint"/> from a stream, as little endian.</summary>
    public static unsafe nint ReadIntPtrLittleEndian(this Stream stream) => BinaryPrimitives.ReadIntPtrLittleEndian(stream.FillBuffer(stackalloc byte[sizeof(nint)]));

    /// <summary>Reads a <see cref="Half"/> from a stream, as little endian.</summary>
    public static unsafe Half ReadHalfLittleEndian(this Stream stream) => BinaryPrimitives.ReadHalfLittleEndian(stream.FillBuffer(stackalloc byte[sizeof(Half)]));

    /// <summary>Reads a <see cref="float"/> from a stream, as little endian.</summary>
    public static float ReadSingleLittleEndian(this Stream stream) => BinaryPrimitives.ReadSingleLittleEndian(stream.FillBuffer(stackalloc byte[sizeof(float)]));

    /// <summary>Reads a <see cref="double"/> from a stream, as little endian.</summary>
    public static double ReadDoubleLittleEndian(this Stream stream) => BinaryPrimitives.ReadDoubleLittleEndian(stream.FillBuffer(stackalloc byte[sizeof(double)]));

    /// <summary>Reads a <see cref="ushort"/> from a stream, as big endian.</summary>
    public static ushort ReadUInt16BigEndian(this Stream stream) => BinaryPrimitives.ReadUInt16BigEndian(stream.FillBuffer(stackalloc byte[sizeof(ushort)]));

    /// <summary>Reads a <see cref="short"/> from a stream, as big endian.</summary>
    public static short ReadInt16BigEndian(this Stream stream) => BinaryPrimitives.ReadInt16BigEndian(stream.FillBuffer(stackalloc byte[sizeof(short)]));

    /// <summary>Reads a 24-bit <see cref="uint"/> from a stream, as big endian.</summary>
    public static uint ReadUInt24BigEndian(this Stream stream) => BinaryPrimitivesEx.ReadUInt24BigEndian(stream.FillBuffer(stackalloc byte[BinaryPrimitivesEx.Int24Size]));

    /// <summary>Reads a 24-bit <see cref="int"/> from a stream, as big endian.</summary>
    public static int ReadInt24BigEndian(this Stream stream) => BinaryPrimitivesEx.ReadInt24BigEndian(stream.FillBuffer(stackalloc byte[BinaryPrimitivesEx.Int24Size]));

    /// <summary>Reads a <see cref="uint"/> from a stream, as big endian.</summary>
    public static uint ReadUInt32BigEndian(this Stream stream) => BinaryPrimitives.ReadUInt32BigEndian(stream.FillBuffer(stackalloc byte[sizeof(uint)]));

    /// <summary>Reads an <see cref="int"/> from a stream, as big endian.</summary>
    public static int ReadInt32BigEndian(this Stream stream) => BinaryPrimitives.ReadInt32BigEndian(stream.FillBuffer(stackalloc byte[sizeof(int)]));

    /// <summary>Reads a 40-bit <see cref="ulong"/> from a stream, as big endian.</summary>
    public static ulong ReadUInt40BigEndian(this Stream stream) => BinaryPrimitivesEx.ReadUInt40BigEndian(stream.FillBuffer(stackalloc byte[BinaryPrimitivesEx.Int40Size]));

    /// <summary>Reads a 40-bit <see cref="long"/> from a stream, as big endian.</summary>
    public static long ReadInt40BigEndian(this Stream stream) => BinaryPrimitivesEx.ReadInt40BigEndian(stream.FillBuffer(stackalloc byte[BinaryPrimitivesEx.Int40Size]));

    /// <summary>Reads a <see cref="ulong"/> from a stream, as big endian.</summary>
    public static ulong ReadUInt64BigEndian(this Stream stream) => BinaryPrimitives.ReadUInt64BigEndian(stream.FillBuffer(stackalloc byte[sizeof(ulong)]));

    /// <summary>Reads a <see cref="long"/> from a stream, as big endian.</summary>
    public static long ReadInt64BigEndian(this Stream stream) => BinaryPrimitives.ReadInt64BigEndian(stream.FillBuffer(stackalloc byte[sizeof(long)]));

    /// <summary>Reads a <see cref="UInt128"/> from a stream, as big endian.</summary>
    public static unsafe UInt128 ReadUInt128BigEndian(this Stream stream) => BinaryPrimitives.ReadUInt128BigEndian(stream.FillBuffer(stackalloc byte[sizeof(UInt128)]));

    /// <summary>Reads an <see cref="Int128"/> from a stream, as big endian.</summary>
    public static unsafe Int128 ReadInt128BigEndian(this Stream stream) => BinaryPrimitives.ReadInt128BigEndian(stream.FillBuffer(stackalloc byte[sizeof(Int128)]));

    /// <summary>Reads a <see cref="nuint"/> from a stream, as big endian.</summary>
    public static unsafe nuint ReadUIntPtrBigEndian(this Stream stream) => BinaryPrimitives.ReadUIntPtrBigEndian(stream.FillBuffer(stackalloc byte[sizeof(nuint)]));

    /// <summary>Reads a <see cref="nint"/> from a stream, as big endian.</summary>
    public static unsafe nint ReadIntPtrBigEndian(this Stream stream) => BinaryPrimitives.ReadIntPtrBigEndian(stream.FillBuffer(stackalloc byte[sizeof(nint)]));

    /// <summary>Reads a <see cref="Half"/> from a stream, as big endian.</summary>
    public static unsafe Half ReadHalfBigEndian(this Stream stream) => BinaryPrimitives.ReadHalfBigEndian(stream.FillBuffer(stackalloc byte[sizeof(Half)]));

    /// <summary>Reads a <see cref="float"/> from a stream, as big endian.</summary>
    public static float ReadSingleBigEndian(this Stream stream) => BinaryPrimitives.ReadSingleBigEndian(stream.FillBuffer(stackalloc byte[sizeof(float)]));

    /// <summary>Reads a <see cref="double"/> from a stream, as big endian.</summary>
    public static double ReadDoubleBigEndian(this Stream stream) => BinaryPrimitives.ReadDoubleBigEndian(stream.FillBuffer(stackalloc byte[sizeof(double)]));

    /// <summary>Writes a <see cref="short"/> into a stream, as little endian.</summary>
    public static void WriteInt16LittleEndian(this Stream stream, short value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="ushort"/> into a stream, as little endian.</summary>
    public static void WriteUInt16LittleEndian(this Stream stream, ushort value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a 24-bit <see cref="int"/> into a stream, as little endian.</summary>
    public static void WriteInt24LittleEndian(this Stream stream, int value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[BinaryPrimitivesEx.Int24Size];
        BinaryPrimitivesEx.WriteInt24LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a 24-bit <see cref="uint"/> into a stream, as little endian.</summary>
    public static void WriteUInt24LittleEndian(this Stream stream, uint value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[BinaryPrimitivesEx.Int24Size];
        BinaryPrimitivesEx.WriteUInt24LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes an <see cref="int"/> into a stream, as little endian.</summary>
    public static void WriteInt32LittleEndian(this Stream stream, int value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="uint"/> into a stream, as little endian.</summary>
    public static void WriteUInt32LittleEndian(this Stream stream, uint value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a 40-bit <see cref="long"/> into a stream, as little endian.</summary>
    public static void WriteInt40LittleEndian(this Stream stream, long value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[BinaryPrimitivesEx.Int40Size];
        BinaryPrimitivesEx.WriteInt40LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a 40-bit <see cref="ulong"/> into a stream, as little endian.</summary>
    public static void WriteUInt40LittleEndian(this Stream stream, ulong value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[BinaryPrimitivesEx.Int40Size];
        BinaryPrimitivesEx.WriteUInt40LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="long"/> into a stream, as little endian.</summary>
    public static void WriteInt64LittleEndian(this Stream stream, long value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="ulong"/> into a stream, as little endian.</summary>
    public static void WriteUInt64LittleEndian(this Stream stream, ulong value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes an <see cref="Int128"/> into a stream, as little endian.</summary>
    public static unsafe void WriteInt128LittleEndian(this Stream stream, Int128 value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(Int128)];
        BinaryPrimitives.WriteInt128LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="UInt128"/> into a stream, as little endian.</summary>
    public static unsafe void WriteUInt128LittleEndian(this Stream stream, UInt128 value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(UInt128)];
        BinaryPrimitives.WriteUInt128LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="nint"/> into a stream, as little endian.</summary>
    public static unsafe void WriteIntPtrLittleEndian(this Stream stream, nint value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(nint)];
        BinaryPrimitives.WriteIntPtrLittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="nuint"/> into a stream, as little endian.</summary>
    public static unsafe void WriteUIntPtrLittleEndian(this Stream stream, nuint value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(nuint)];
        BinaryPrimitives.WriteUIntPtrLittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="Half"/> into a stream, as little endian.</summary>
    public static unsafe void WriteHalfLittleEndian(this Stream stream, Half value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(Half)];
        BinaryPrimitives.WriteHalfLittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="float"/> into a stream, as little endian.</summary>
    public static void WriteSingleLittleEndian(this Stream stream, float value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(float)];
        BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="double"/> into a stream, as little endian.</summary>
    public static void WriteDoubleLittleEndian(this Stream stream, double value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(double)];
        BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="short"/> into a stream, as big endian.</summary>
    public static void WriteInt16BigEndian(this Stream stream, short value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        BinaryPrimitives.WriteInt16BigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="ushort"/> into a stream, as big endian.</summary>
    public static void WriteUInt16BigEndian(this Stream stream, ushort value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a 24-bit <see cref="int"/> into a stream, as big endian.</summary>
    public static void WriteInt24BigEndian(this Stream stream, int value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[BinaryPrimitivesEx.Int24Size];
        BinaryPrimitivesEx.WriteInt24BigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a 24-bit <see cref="uint"/> into a stream, as big endian.</summary>
    public static void WriteUInt24BigEndian(this Stream stream, uint value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[BinaryPrimitivesEx.Int24Size];
        BinaryPrimitivesEx.WriteUInt24BigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes an <see cref="int"/> into a stream, as big endian.</summary>
    public static void WriteInt32BigEndian(this Stream stream, int value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="uint"/> into a stream, as big endian.</summary>
    public static void WriteUInt32BigEndian(this Stream stream, uint value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a 40-bit <see cref="long"/> into a stream, as big endian.</summary>
    public static void WriteInt40BigEndian(this Stream stream, long value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[BinaryPrimitivesEx.Int40Size];
        BinaryPrimitivesEx.WriteInt40BigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a 40-bit <see cref="ulong"/> into a stream, as big endian.</summary>
    public static void WriteUInt40BigEndian(this Stream stream, ulong value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[BinaryPrimitivesEx.Int40Size];
        BinaryPrimitivesEx.WriteUInt40BigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="long"/> into a stream, as big endian.</summary>
    public static void WriteInt64BigEndian(this Stream stream, long value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="ulong"/> into a stream, as big endian.</summary>
    public static void WriteUInt64BigEndian(this Stream stream, ulong value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes an <see cref="Int128"/> into a stream, as big endian.</summary>
    public static unsafe void WriteInt128BigEndian(this Stream stream, Int128 value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(Int128)];
        BinaryPrimitives.WriteInt128BigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="UInt128"/> into a stream, as big endian.</summary>
    public static unsafe void WriteUInt128BigEndian(this Stream stream, UInt128 value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(UInt128)];
        BinaryPrimitives.WriteUInt128BigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="nint"/> into a stream, as big endian.</summary>
    public static unsafe void WriteIntPtrBigEndian(this Stream stream, nint value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(nint)];
        BinaryPrimitives.WriteIntPtrBigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="nuint"/> into a stream, as big endian.</summary>
    public static unsafe void WriteUIntPtrBigEndian(this Stream stream, nuint value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(nuint)];
        BinaryPrimitives.WriteUIntPtrBigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="Half"/> into a stream, as big endian.</summary>
    public static unsafe void WriteHalfBigEndian(this Stream stream, Half value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(Half)];
        BinaryPrimitives.WriteHalfBigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="float"/> into a stream, as big endian.</summary>
    public static void WriteSingleBigEndian(this Stream stream, float value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(float)];
        BinaryPrimitives.WriteSingleBigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Writes a <see cref="double"/> into a stream, as big endian.</summary>
    public static void WriteDoubleBigEndian(this Stream stream, double value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[sizeof(double)];
        BinaryPrimitives.WriteDoubleBigEndian(buffer, value);
        stream.Write(buffer);
    }

    /// <summary>Fills the provided span with bytes from the stream.</summary>
    public static ReadOnlySpan<byte> FillBuffer(this Stream stream, Span<byte> buffer)
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.ReadExactly(buffer);
        return buffer;
    }
}
