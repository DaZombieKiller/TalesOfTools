using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace TLTool;

/// <summary>Provides extension methods for <see cref="Stream"/>.</summary>
public static class StreamExtensions
{
    /// <summary>Writes 0x00 bytes to the <see cref="Stream"/> until its position is aligned to the specified value.</summary>
    public static void WriteAlign(this Stream stream, long alignment)
    {
        var aligned = (stream.Position + (alignment - 1)) & ~(alignment - 1);

        while (stream.Position < aligned)
        {
            stream.WriteByte(0);
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

    /// <summary>Reads a 4-byte unsigned little-endian integer from the stream.</summary>
    public static unsafe uint ReadUInt32LittleEndian(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        uint value;
        stream.ReadExactly(new Span<byte>(&value, sizeof(uint)));

        if (!BitConverter.IsLittleEndian)
            value = BinaryPrimitives.ReverseEndianness(value);

        return value;
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
}
