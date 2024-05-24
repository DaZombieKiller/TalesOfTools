using System.Buffers.Binary;

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
}
