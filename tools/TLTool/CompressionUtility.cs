using System.Buffers;
using System.IO.Compression;
using Microsoft.IO;
using LzmaDecoder = SevenZip.Compression.LZMA.Decoder;

namespace TLTool;

public static class CompressionUtility
{
    private static readonly RecyclableMemoryStreamManager s_StreamManager = new();

    public const int TlzcMagic = 0x435A4C54; // TLZC

    /// <summary>Gets a decompression stream for TLZC file data.</summary>
    public static Stream GetTlzcDecompressionStream(Stream stream, bool leaveOpen)
    {
        var reader = new BinaryStream(stream, bigEndian: false);

        if (reader.ReadUInt32() != TlzcMagic)
            throw new InvalidOperationException();

        reader.ReadByte(); // unknown
        var compressionType = reader.ReadByte();
        reader.ReadUInt16(); // unknown
        reader.ReadUInt32(); // compressed size
        var uncompressedSize = reader.ReadUInt32();
        reader.ReadUInt32(); // unknown
        reader.ReadUInt32(); // unknown

        return compressionType switch
        {
            2 => new DeflateStream(stream, CompressionMode.Decompress, leaveOpen),
            4 => GetTlzcLzmaBlockStream(reader, alignment: 65536, uncompressedSize, leaveOpen),
            _ => throw new InvalidOperationException("Unknown compression type"),
        };
    }

    /// <summary>Gets a decompression stream for ZARC file data.</summary>
    // TODO: Clean this up a bit, make it actually stream the data instead of decompressing all at once.
    public static Stream GetZArcDecompressionStream(Stream stream, ReadOnlySpan<uint> blockSizes, uint alignment, long uncompressedSize, bool leaveOpen)
    {
        var decoded = s_StreamManager.GetStream();
        var decoder = new LzmaDecoder();
        var remaining = uncompressedSize;
        var properties = (stackalloc byte[5]);

        for (int i = 0; i < blockSizes.Length; i++)
        {
            var length = long.Min(remaining, alignment);

            if (blockSizes[i] != 0)
            {
                stream.ReadExactly(properties);
                decoder.SetDecoderProperties(properties);
                length = stream.ReadInt64LittleEndian();
                decoder.Code(stream, decoded, blockSizes[i], length, null);
            }
            else
            {
                var buffer = ArrayPool<byte>.Shared.Rent((int)length);
                stream.ReadExactly(buffer, 0, (int)length);
                decoded.Write(buffer, 0, (int)length);
                ArrayPool<byte>.Shared.Return(buffer);
            }

            remaining -= length;
        }

        if (!leaveOpen)
            stream.Dispose();

        decoded.Position = 0;
        return decoded;
    }

    // https://blog.lse.epita.fr/2012/04/07/static-analysis-of-an-unknown-compression-format.html
    // TODO: Clean this up a bit, make it actually stream the data instead of decompressing all at once.
    private static Stream GetTlzcLzmaBlockStream(BinaryStream reader, uint alignment, uint uncompressedSize, bool leaveOpen)
    {
        var decoded = s_StreamManager.GetStream();
        var decoder = new LzmaDecoder();
        var remaining = uncompressedSize;
        var blockCount = (uncompressedSize + alignment - 1) / alignment;
        var blockSizes = new ushort[blockCount];
        decoder.SetDecoderProperties(reader.ReadExactly(stackalloc byte[5]));

        for (int i = 0; i < blockSizes.Length; i++)
            blockSizes[i] = reader.ReadUInt16();

        for (int i = 0; i < blockSizes.Length; i++)
        {
            var length = int.Min((int)remaining, (int)alignment);

            if (blockSizes[i] != 0)
                decoder.Code(reader.BaseStream, decoded, blockSizes[i], (uint)length, null);
            else
            {
                var buffer = ArrayPool<byte>.Shared.Rent(length);
                reader.BaseStream.ReadExactly(buffer, 0, length);
                decoded.Write(buffer, 0, length);
                ArrayPool<byte>.Shared.Return(buffer);
            }

            remaining -= (uint)length;
        }

        if (!leaveOpen)
            reader.BaseStream.Dispose();

        decoded.Position = 0;
        return decoded;
    }
}
