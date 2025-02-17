using System.Buffers.Binary;
using System.IO.Hashing;
using System.Numerics;

namespace TLTool;

/// <summary>Defines options for <see cref="TLHash"/>.</summary>
[Flags]
public enum TLHashOptions
{
    /// <summary>No options.</summary>
    None = 0,

    /// <summary>The input is transformed into uppercase before hashing.</summary>
    // TODO: Consider removing this
    IgnoreCase = 1 << 0,

    /// <summary>The XOR operation is not performed while hashing.</summary>
    NoXor = 1 << 1,
}

/// <summary>Provides methods for computing hashes of names.</summary>
public sealed class TLHash(TLHashOptions options) : NonCryptographicHashAlgorithm(sizeof(uint))
{
    private uint _hash;

    /// <inheritdoc/>
    public override void Append(ReadOnlySpan<byte> source)
    {
        _hash = Update(source, _hash, options);
    }

    /// <summary>Appends the contents of <paramref name="source"/> to the data already processed for the current hash computation.</summary>
    public void Append(ReadOnlySpan<char> source)
    {
        _hash = Update(source, _hash, options);
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        _hash = 0;
    }

    /// <inheritdoc/>
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(destination, _hash);
    }

    /// <inheritdoc/>
    protected override void GetHashAndResetCore(Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(destination, _hash);
        _hash = 0;
    }

    /// <summary>Appends the provided span of bytes to the hash.</summary>
    private static uint Update<T>(ReadOnlySpan<T> source, uint hash, TLHashOptions options)
        where T : IBinaryInteger<T>
    {
        foreach (T value in source)
        {
            // The hash is performed on bytes, but we allow char for convenience.
            byte b = byte.CreateTruncating(value);

            // If IgnoreCase is specified, we need to convert to ASCII uppercase.
            if ((options & TLHashOptions.IgnoreCase) != 0) b = ToUpper(b);

            // Compute the next hash value.
            uint next = b + (hash << 6) + (hash >> 2) - 0x61C88647;

            // For some reason, DLC headers don't use the XOR operation for their hashes.
            hash = (options & TLHashOptions.NoXor) != 0 ? next : hash ^ next;
        }

        return hash;
    }

    /// <summary>Computes the hash of the provided data.</summary>
    public static uint HashToUInt32(ReadOnlySpan<byte> source)
    {
        return Update(source, 0, TLHashOptions.None);
    }

    /// <summary>Computes the hash of the provided data.</summary>
    public static uint HashToUInt32(ReadOnlySpan<char> source)
    {
        return Update(source, 0, TLHashOptions.None);
    }

    /// <summary>Computes the hash of the provided data.</summary>
    public static uint HashToUInt32(ReadOnlySpan<byte> source, TLHashOptions options)
    {
        return Update(source, 0, options);
    }

    /// <summary>Computes the hash of the provided data.</summary>
    public static uint HashToUInt32(ReadOnlySpan<char> source, TLHashOptions options)
    {
        return Update(source, 0, options);
    }

    /// <summary>Converts the provided ASCII value to uppercase.</summary>
    private static byte ToUpper(byte b)
    {
        return (byte)(b - 'a') < 0x1A ? (byte)(b - ' ') : b;
    }
}
