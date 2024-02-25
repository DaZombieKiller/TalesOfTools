using System.Text;

namespace TLTool;

/// <summary>Provides methods for computing hashes of names.</summary>
public static class NameHash
{
    /// <summary>Computes the hash of the provided span of bytes.</summary>
    public static uint Compute(ReadOnlySpan<byte> name)
    {
        uint hash = 0;

        foreach (byte b in name)
            hash = Append(hash, ToUpper(b));

        return hash;
    }

    /// <summary>Computes the hash of the ASCII representation of the provided string.</summary>
    public static uint Compute(string name)
    {
        return Compute(Encoding.ASCII.GetBytes(name));
    }

    /// <summary>Appends the provided byte to the hash.</summary>
    private static uint Append(uint hash, byte b)
    {
        return hash ^ (b + (hash << 6) + (hash >> 2) - 0x61C88647);
    }

    /// <summary>Converts the provided ASCII value to uppercase.</summary>
    private static byte ToUpper(byte b)
    {
        return (byte)(b - 'a') < 0x1A ? (byte)(b - ' ') : b;
    }
}
