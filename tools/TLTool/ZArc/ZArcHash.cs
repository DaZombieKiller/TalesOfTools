using System.Buffers.Binary;
using System.IO.Hashing;
using System.Numerics;

namespace TLTool;

/// <summary>Implements the hash algorithm used by <see cref="ZArcFile"/>.</summary>
public sealed class ZArcHash() : NonCryptographicHashAlgorithm(sizeof(ulong))
{
    private const uint InitialState = 0xFFFFFFFF;

    private uint _lower = InitialState;

    private uint _upper = InitialState;

    /// <inheritdoc/>
    public override void Append(ReadOnlySpan<byte> source)
    {
        (_lower, _upper) = Update(source, _lower, _upper);
    }

    /// <summary>Appends the contents of <paramref name="source"/> to the data already processed for the current hash computation.</summary>
    public void Append(ReadOnlySpan<char> source)
    {
        (_lower, _upper) = Update(source, _lower, _upper);
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        _lower = InitialState;
        _upper = InitialState;
    }

    /// <inheritdoc/>
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(destination, ~(_lower | ((ulong)_upper << 32)));
    }

    /// <inheritdoc/>
    protected override void GetHashAndResetCore(Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(destination, ~(_lower | ((ulong)_upper << 32)));
        _lower = InitialState;
        _upper = InitialState;
    }

    /// <summary>Computes the hash of the provided data.</summary>
    public static ulong HashToUInt64(ReadOnlySpan<byte> source)
    {
        (uint lower, uint upper) = Update(source, InitialState, InitialState);
        return ~(lower | ((ulong)upper << 32));
    }

    /// <summary>Computes the hash of the provided data.</summary>
    public static ulong HashToUInt64(ReadOnlySpan<char> source)
    {
        (uint lower, uint upper) = Update(source, InitialState, InitialState);
        return ~(lower | ((ulong)upper << 32));
    }

    /// <summary>Updates the hash using the provided data.</summary>
    private static (uint, uint) Update<T>(ReadOnlySpan<T> source, uint lower, uint upper)
        where T : IBinaryInteger<T>
    {
        // Magic value
        const uint UpperMask = 0x10215681;

        // Derived from UpperMask
        const uint LowerMask = (uint)(((UpperMask | ((ulong)UpperMask << 32)) >> 16) & 0xFFFFFFFF);

        foreach (T value in source)
        {
            lower ^= byte.CreateTruncating(value);
            upper ^= byte.CreateTruncating(value);

            for (int i = 0; i < 8; i++)
            {
                // Need to cast to int to perform signed right shift.
                lower = ((uint)((int)(lower << 31) >> 31) & LowerMask) ^ (lower >>> 1);
                upper = ((uint)((int)(upper << 31) >> 31) & UpperMask) ^ (upper >>> 1);
            }
        }

        return (lower, upper);
    }
}
