using System.Buffers.Binary;

namespace TLTool;

/// <summary>Provides additional methods to complement the functionality of <see cref="BinaryPrimitives"/>.</summary>
internal static partial class BinaryPrimitivesEx
{
    /// <summary>Size in bytes of a 40-bit integer.</summary>
    public const int Int40Size = 5;

    /// <summary>Minimum value of a signed 40-bit integer.</summary>
    public const long Int40Min = -0x8000000000;

    /// <summary>Maximum value of a signed 40-bit integer.</summary>
    public const long Int40Max = 0x7FFFFFFFFF;

    /// <summary>Maximum value of an unsigned 40-bit integer.</summary>
    public const ulong UInt40Max = 0xFFFFFFFFFF;

    /// <summary>Reads a 40-bit <see cref="long"/> from the beginning of a read-only span of bytes, as little endian.</summary>
    /// <returns>The little endian value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static long ReadInt40LittleEndian(ReadOnlySpan<byte> source)
    {
        return unchecked((long)ReadUInt40LittleEndian(source) << 8) >> 8;
    }

    /// <summary>Reads a 40-bit <see cref="ulong"/> from the beginning of a read-only span of bytes, as little endian.</summary>
    /// <returns>The little endian value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static ulong ReadUInt40LittleEndian(ReadOnlySpan<byte> source)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Int40Size, source.Length);
        return ((ulong)source[4] << 32) | ((ulong)source[3] << 24) | ((ulong)source[2] << 16) | ((ulong)source[1] << 8) | source[0];
    }

    /// <summary>Reads a 40-bit <see cref="long"/> from the beginning of a read-only span of bytes, as little endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 40-bit <see cref="long"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryReadInt40LittleEndian(ReadOnlySpan<byte> source, out long value)
    {
        if (TryReadUInt40LittleEndian(source, out ulong unsignedValue))
        {
            value = unchecked((long)unsignedValue << 8) >> 8;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Reads a 40-bit <see cref="ulong"/> from the beginning of a read-only span of bytes, as little endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 40-bit <see cref="ulong"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryReadUInt40LittleEndian(ReadOnlySpan<byte> source, out ulong value)
    {
        if (Int40Size > source.Length)
        {
            value = default;
            return false;
        }

        value = ((ulong)source[2] << 16) | ((ulong)source[1] << 8) | source[0];
        return true;
    }

    /// <summary>Writes a 40-bit <see cref="long"/> into a span of bytes, as little endian.</summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void WriteInt40LittleEndian(Span<byte> destination, long value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Int40Size, destination.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(value, Int40Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Int40Max);
        destination[4] = unchecked((byte)(value >> 32));
        destination[3] = unchecked((byte)(value >> 24));
        destination[2] = unchecked((byte)(value >> 16));
        destination[1] = unchecked((byte)(value >> 8));
        destination[0] = unchecked((byte)value);
    }

    /// <summary>Writes a 40-bit <see cref="ulong"/> into a span of bytes, as little endian.</summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void WriteUInt40LittleEndian(Span<byte> destination, ulong value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Int40Size, destination.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, UInt40Max);
        destination[4] = unchecked((byte)(value >> 32));
        destination[3] = unchecked((byte)(value >> 24));
        destination[2] = unchecked((byte)(value >> 16));
        destination[1] = unchecked((byte)(value >> 8));
        destination[0] = unchecked((byte)value);
    }

    /// <summary>Writes a 40-bit <see cref="long"/> into a span of bytes, as little endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 40-bit <see cref="long"/> and <paramref name="value"/> is in range; otherwise, <see langword="false"/>.</returns>
    public static bool TryWriteInt40LittleEndian(Span<byte> destination, long value)
    {
        if (Int40Size > destination.Length)
            return false;

        if (value < Int40Min || value > Int40Max)
            return false;

        destination[4] = unchecked((byte)(value >> 32));
        destination[3] = unchecked((byte)(value >> 24));
        destination[2] = unchecked((byte)(value >> 16));
        destination[1] = unchecked((byte)(value >> 8));
        destination[0] = unchecked((byte)value);
        return true;
    }

    /// <summary>Writes a 40-bit <see cref="ulong"/> into a span of bytes, as little endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 40-bit <see cref="ulong"/> and <paramref name="value"/> is in range; otherwise, <see langword="false"/>.</returns>
    public static bool TryWriteUInt40LittleEndian(Span<byte> destination, ulong value)
    {
        if (Int40Size > destination.Length)
            return false;

        if (value > UInt40Max)
            return false;

        destination[4] = unchecked((byte)(value >> 32));
        destination[3] = unchecked((byte)(value >> 24));
        destination[2] = unchecked((byte)(value >> 16));
        destination[1] = unchecked((byte)(value >> 8));
        destination[0] = unchecked((byte)value);
        return true;
    }

    /// <summary>Reads a 40-bit <see cref="long"/> from the beginning of a read-only span of bytes, as big endian.</summary>
    /// <returns>The big endian value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static long ReadInt40BigEndian(ReadOnlySpan<byte> source)
    {
        return unchecked((long)ReadUInt40BigEndian(source) << 8) >> 8;
    }

    /// <summary>Reads a 40-bit <see cref="ulong"/> from the beginning of a read-only span of bytes, as big endian.</summary>
    /// <returns>The big endian value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static ulong ReadUInt40BigEndian(ReadOnlySpan<byte> source)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Int40Size, source.Length);
        return source[4] | ((ulong)source[3] << 8) | ((ulong)source[2] << 16) | ((ulong)source[1] << 24) | ((ulong)source[0] << 32);
    }

    /// <summary>Reads a 40-bit <see cref="long"/> from the beginning of a read-only span of bytes, as big endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 40-bit <see cref="long"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryReadInt40BigEndian(ReadOnlySpan<byte> source, out long value)
    {
        if (TryReadUInt40BigEndian(source, out ulong unsignedValue))
        {
            value = ((long)unsignedValue << 8) >> 8;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Reads a 40-bit <see cref="ulong"/> from the beginning of a read-only span of bytes, as big endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 40-bit <see cref="ulong"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryReadUInt40BigEndian(ReadOnlySpan<byte> source, out ulong value)
    {
        if (Int40Size > source.Length)
        {
            value = default;
            return false;
        }

        value = source[4] | ((ulong)source[3] << 8) | ((ulong)source[2] << 16) | ((ulong)source[1] << 24) | ((ulong)source[0] << 32);
        return true;
    }

    /// <summary>Writes a 40-bit <see cref="long"/> into a span of bytes, as big endian.</summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void WriteInt40BigEndian(Span<byte> destination, long value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Int40Size, destination.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(value, Int40Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Int40Max);
        destination[4] = unchecked((byte)value);
        destination[3] = unchecked((byte)(value >> 8));
        destination[2] = unchecked((byte)(value >> 16));
        destination[1] = unchecked((byte)(value >> 24));
        destination[0] = unchecked((byte)(value >> 32));
    }

    /// <summary>Writes a 40-bit <see cref="ulong"/> into a span of bytes, as big endian.</summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void WriteUInt40BigEndian(Span<byte> destination, ulong value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Int40Size, destination.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, UInt40Max);
        destination[4] = unchecked((byte)value);
        destination[3] = unchecked((byte)(value >> 8));
        destination[2] = unchecked((byte)(value >> 16));
        destination[1] = unchecked((byte)(value >> 24));
        destination[0] = unchecked((byte)(value >> 32));
    }

    /// <summary>Writes a 40-bit <see cref="long"/> into a span of bytes, as big endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 40-bit <see cref="long"/> and <paramref name="value"/> is in range; otherwise, <see langword="false"/>.</returns>
    public static bool TryWriteInt40BigEndian(Span<byte> destination, long value)
    {
        if (Int40Size > destination.Length)
            return false;

        if (value < Int40Min || value > Int40Max)
            return false;

        destination[4] = unchecked((byte)value);
        destination[3] = unchecked((byte)(value >> 8));
        destination[2] = unchecked((byte)(value >> 16));
        destination[1] = unchecked((byte)(value >> 24));
        destination[0] = unchecked((byte)(value >> 32));
        return true;
    }

    /// <summary>Writes a 40-bit <see cref="ulong"/> into a span of bytes, as big endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 40-bit <see cref="ulong"/> and <paramref name="value"/> is in range; otherwise, <see langword="false"/>.</returns>
    public static bool TryWriteUInt40BigEndian(Span<byte> destination, ulong value)
    {
        if (Int40Size > destination.Length)
            return false;

        if (value > UInt40Max)
            return false;

        destination[4] = unchecked((byte)value);
        destination[3] = unchecked((byte)(value >> 8));
        destination[2] = unchecked((byte)(value >> 16));
        destination[1] = unchecked((byte)(value >> 24));
        destination[0] = unchecked((byte)(value >> 32));
        return true;
    }
}
