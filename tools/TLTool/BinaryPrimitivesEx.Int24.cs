using System.Buffers.Binary;

namespace TLTool;

/// <summary>Provides additional methods to complement the functionality of <see cref="BinaryPrimitives"/>.</summary>
internal static partial class BinaryPrimitivesEx
{
    /// <summary>Size in bytes of a 24-bit integer.</summary>
    public const int Int24Size = 3;

    /// <summary>Minimum value of a signed 24-bit integer.</summary>
    public const int Int24Min = -0x800000;

    /// <summary>Maximum value of a signed 24-bit integer.</summary>
    public const int Int24Max = 0x7FFFFF;

    /// <summary>Maximum value of an unsigned 24-bit integer.</summary>
    public const uint UInt24Max = 0xFFFFFF;

    /// <summary>Reads a 24-bit <see cref="int"/> from the beginning of a read-only span of bytes, as little endian.</summary>
    /// <returns>The little endian value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static int ReadInt24LittleEndian(ReadOnlySpan<byte> source)
    {
        return unchecked((int)ReadUInt24LittleEndian(source) << 8) >> 8;
    }

    /// <summary>Reads a 24-bit <see cref="uint"/> from the beginning of a read-only span of bytes, as little endian.</summary>
    /// <returns>The little endian value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static uint ReadUInt24LittleEndian(ReadOnlySpan<byte> source)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Int24Size, source.Length);
        return ((uint)source[2] << 16) | ((uint)source[1] << 8) | source[0];
    }

    /// <summary>Reads a 24-bit <see cref="int"/> from the beginning of a read-only span of bytes, as little endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 24-bit <see cref="int"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryReadInt24LittleEndian(ReadOnlySpan<byte> source, out int value)
    {
        if (TryReadUInt24LittleEndian(source, out uint unsignedValue))
        {
            value = unchecked((int)unsignedValue << 8) >> 8;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Reads a 24-bit <see cref="uint"/> from the beginning of a read-only span of bytes, as little endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 24-bit <see cref="uint"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryReadUInt24LittleEndian(ReadOnlySpan<byte> source, out uint value)
    {
        if (Int24Size > source.Length)
        {
            value = default;
            return false;
        }

        value = ((uint)source[2] << 16) | ((uint)source[1] << 8) | source[0];
        return true;
    }

    /// <summary>Writes a 24-bit <see cref="int"/> into a span of bytes, as little endian.</summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void WriteInt24LittleEndian(Span<byte> destination, int value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Int24Size, destination.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(value, Int24Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Int24Max);
        destination[2] = unchecked((byte)(value >> 16));
        destination[1] = unchecked((byte)(value >> 8));
        destination[0] = unchecked((byte)value);
    }

    /// <summary>Writes a 24-bit <see cref="uint"/> into a span of bytes, as little endian.</summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void WriteUInt24LittleEndian(Span<byte> destination, uint value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Int24Size, destination.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, UInt24Max);
        destination[2] = unchecked((byte)(value >> 16));
        destination[1] = unchecked((byte)(value >> 8));
        destination[0] = unchecked((byte)value);
    }

    /// <summary>Writes a 24-bit <see cref="int"/> into a span of bytes, as little endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 24-bit <see cref="int"/> and <paramref name="value"/> is in range; otherwise, <see langword="false"/>.</returns>
    public static bool TryWriteInt24LittleEndian(Span<byte> destination, int value)
    {
        if (Int24Size > destination.Length)
            return false;

        if (value < Int24Min || value > Int24Max)
            return false;

        destination[2] = unchecked((byte)(value >> 16));
        destination[1] = unchecked((byte)(value >> 8));
        destination[0] = unchecked((byte)value);
        return true;
    }

    /// <summary>Writes a 24-bit <see cref="uint"/> into a span of bytes, as little endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 24-bit <see cref="uint"/> and <paramref name="value"/> is in range; otherwise, <see langword="false"/>.</returns>
    public static bool TryWriteUInt24LittleEndian(Span<byte> destination, uint value)
    {
        if (Int24Size > destination.Length)
            return false;

        if (value > UInt24Max)
            return false;

        destination[2] = unchecked((byte)(value >> 16));
        destination[1] = unchecked((byte)(value >> 8));
        destination[0] = unchecked((byte)value);
        return true;
    }

    /// <summary>Reads a 24-bit <see cref="int"/> from the beginning of a read-only span of bytes, as big endian.</summary>
    /// <returns>The big endian value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static int ReadInt24BigEndian(ReadOnlySpan<byte> source)
    {
        return unchecked((int)ReadUInt24BigEndian(source) << 8) >> 8;
    }

    /// <summary>Reads a 24-bit <see cref="uint"/> from the beginning of a read-only span of bytes, as big endian.</summary>
    /// <returns>The big endian value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static uint ReadUInt24BigEndian(ReadOnlySpan<byte> source)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Int24Size, source.Length);
        return source[2] | ((uint)source[1] << 8) | ((uint)source[0] << 16);
    }

    /// <summary>Reads a 24-bit <see cref="int"/> from the beginning of a read-only span of bytes, as big endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 24-bit <see cref="int"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryReadInt24BigEndian(ReadOnlySpan<byte> source, out int value)
    {
        if (TryReadUInt24BigEndian(source, out uint unsignedValue))
        {
            value = ((int)unsignedValue << 8) >> 8;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Reads a 24-bit <see cref="uint"/> from the beginning of a read-only span of bytes, as big endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 24-bit <see cref="uint"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryReadUInt24BigEndian(ReadOnlySpan<byte> source, out uint value)
    {
        if (Int24Size > source.Length)
        {
            value = default;
            return false;
        }

        value = source[2] | ((uint)source[1] << 8) | ((uint)source[0] << 16);
        return true;
    }

    /// <summary>Writes a 24-bit <see cref="int"/> into a span of bytes, as big endian.</summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void WriteInt24BigEndian(Span<byte> destination, int value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Int24Size, destination.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(value, Int24Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Int24Max);
        destination[2] = unchecked((byte)value);
        destination[1] = unchecked((byte)(value >> 8));
        destination[0] = unchecked((byte)(value >> 16));
    }

    /// <summary>Writes a 24-bit <see cref="uint"/> into a span of bytes, as big endian.</summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void WriteUInt24BigEndian(Span<byte> destination, uint value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Int24Size, destination.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, UInt24Max);
        destination[2] = unchecked((byte)value);
        destination[1] = unchecked((byte)(value >> 8));
        destination[0] = unchecked((byte)(value >> 16));
    }

    /// <summary>Writes a 24-bit <see cref="int"/> into a span of bytes, as big endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 24-bit <see cref="int"/> and <paramref name="value"/> is in range; otherwise, <see langword="false"/>.</returns>
    public static bool TryWriteInt24BigEndian(Span<byte> destination, int value)
    {
        if (Int24Size > destination.Length)
            return false;

        if (value < Int24Min || value > Int24Max)
            return false;

        destination[2] = unchecked((byte)value);
        destination[1] = unchecked((byte)(value >> 8));
        destination[0] = unchecked((byte)(value >> 16));
        return true;
    }

    /// <summary>Writes a 24-bit <see cref="uint"/> into a span of bytes, as big endian.</summary>
    /// <returns><see langword="true"/> if the span is large enough to contain a 24-bit <see cref="uint"/> and <paramref name="value"/> is in range; otherwise, <see langword="false"/>.</returns>
    public static bool TryWriteUInt24BigEndian(Span<byte> destination, uint value)
    {
        if (Int24Size > destination.Length)
            return false;

        if (value > UInt24Max)
            return false;

        destination[2] = unchecked((byte)value);
        destination[1] = unchecked((byte)(value >> 8));
        destination[0] = unchecked((byte)(value >> 16));
        return true;
    }
}
