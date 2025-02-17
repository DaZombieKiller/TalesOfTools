using System.Buffers.Binary;

namespace TLTool;

/// <summary>A data file's encryption header.</summary>
public sealed class TLDataEncryptHeader(byte[] data)
{
    /// <summary>Gets the encryption key for the file with the provided index.</summary>
    public bool GetFileKey(uint index, out ulong key)
    {
        index = (index & ~0xFu) | ((index >> 2) & 3) | (4 * (3 - (index & 3)));
        key = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(sizeof(ulong) * (int)index));
        return (key & 0x4000) != 0;
    }

    /// <summary>Gets the encryption key for the data header.</summary>
    public ulong GetHeaderKey()
    {
        var a = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(96));
        var b = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(32));
        return TLCrypt.GetKey((byte)(a & 0xF), (byte)((b >>> 4) & 0xF));
    }
}
