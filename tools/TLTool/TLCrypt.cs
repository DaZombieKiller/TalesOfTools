using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace TLTool;

/// <summary>Provides methods for working with the TLDAT encryption algorithm.</summary>
public static class TLCrypt
{
    /// <summary>Values used to generate an initial decryption key.</summary>
    public static ReadOnlySpan<ulong> DummyHash =>
    [
        0x551B8A2ECD6197EF, 0x186A57F394700E34, 0x3E9F2E302712B938, 0xE14C2303CCC551F2,
        0xCCF38CA1F5C17133, 0x2353622F23B1C9DB, 0x34AFADAC84AE7417, 0x0A5DCACA1D9365EB,
        0xF262BECF99CD3C0F, 0x03125B4B2F481962, 0xCD5EC4039782A7AA, 0x7E33B2FC317E77F3,
        0xBEFB5409BB40D4FA, 0x3368C49E410E24EF, 0x1E9693617E3E6BBF, 0x9C35278E358912B1,
    ];

    /// <summary>Gets a decryption key from two 4-bit key indices.</summary>
    public static ulong GetKey(byte key1, byte key2)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(key1, DummyHash.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(key2, DummyHash.Length);

        // If key1 and key2 are equal, the game will increment key2 and use the next
        // key in the sequence for it, wrapping the value if necessary. Should that
        // be replicated here? Or should it be up to the discretion of the call site?
        return DummyHash[key1] ^ DummyHash[key2];
    }

    /// <summary>Decrypts the provided buffer with the provided key.</summary>
    public static void Decrypt(Span<byte> data, ulong key)
    {
        Process(data, key, encrypt: false);
    }

    /// <summary>Encrypts the provided buffer with the provided key.</summary>
    public static void Encrypt(Span<byte> data, ulong key)
    {
        Process(data, key, encrypt: true);
    }

    /// <summary>Decrypts or encrypts the provided buffer with the provided key.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void Process(Span<byte> data, ulong key, bool encrypt)
    {
        int i;
        int n = data.Length - (data.Length % 16);

        for (i = 0; i + sizeof(ulong) <= n; i += sizeof(ulong))
        {
            ulong temp = BinaryPrimitives.ReadUInt64LittleEndian(data[i..]);
            BinaryPrimitives.WriteUInt64LittleEndian(data[i..], temp ^ key);

            if (!encrypt)
                temp ^= key;

            key ^= 0x4E3362BF7A4C7C26;
            key ^= key << 13;
            key ^= key >>> 7;
            key ^= (key << 17) ^ (ulong)((int)temp | (int)(temp >>> 32));
        }

        for (; i < data.Length; i++)
        {
            data[i] ^= (byte)(key >>> (8 * (i & (sizeof(ulong) - 1))));
        }
    }
}
