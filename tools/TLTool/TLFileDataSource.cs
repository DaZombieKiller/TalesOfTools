using System.Buffers;
using System.IO.Compression;
using System.Text;
using LzmaDecoder = SevenZip.Compression.LZMA.Decoder;

namespace TLTool;

/// <summary>An <see cref="IDataSource"/> referencing a file inside an existing data file.</summary>
public sealed class TLFileDataSource : IDataSource
{
    /// <summary>The data file containing the referenced data.</summary>
    public FileInfo File { get; }

    /// <summary>The offset into <see cref="File"/> where the referenced data is located.</summary>
    public long Offset { get; }

    /// <inheritdoc/>
    public long Length { get; }

    /// <summary>The compressed length of the data, in bytes.</summary>
    public long CompressedLength { get; }

    /// <summary>Initializes a new <see cref="TLFileDataSource"/> instance.</summary>
    public TLFileDataSource(FileInfo file, long offset, long length, long compressedLength)
    {
        ArgumentNullException.ThrowIfNull(file);
        File = file;
        Offset = offset;
        Length = length;
        CompressedLength = compressedLength;
    }

    /// <inheritdoc/>
    public Stream OpenRead()
    {
        Stream stream = File.OpenRead();
        stream = new SubReadStream(stream, Offset, CompressedLength);

        if (Length != CompressedLength)
        {
            using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

            if (reader.ReadUInt32() == 0x435A4C54) // TLZC
            {
                reader.ReadByte(); // unknown
                var compressionType = reader.ReadByte();
                reader.ReadUInt16(); // unknown
                var compressedSize = reader.ReadUInt32();
                var uncompressedSize = reader.ReadUInt32();
                reader.ReadUInt32(); // unknown
                reader.ReadUInt32(); // unknown

                if (compressionType == 2)
                    return new DeflateStream(stream, CompressionMode.Decompress);

                // https://blog.lse.epita.fr/2012/04/07/static-analysis-of-an-unknown-compression-format.html
                // TODO: Clean this up a bit, make it actually stream the data instead of decompressing all at once.
                if (compressionType == 4)
                {
                    var decoded = new MemoryStream();
                    var decoder = new LzmaDecoder();
                    var blockCount = (uncompressedSize + 0xFFFF) / 0x10000;
                    var blockSizes = new ushort[blockCount];
                    var remaining = uncompressedSize;
                    decoder.SetDecoderProperties(reader.ReadBytes(5));

                    for (int i = 0; i < blockSizes.Length; i++)
                        blockSizes[i] = reader.ReadUInt16();

                    for (int i = 0; i < blockSizes.Length; i++)
                    {
                        var length = int.Min((int)remaining, 0x10000);

                        if (blockSizes[i] != 0)
                            decoder.Code(stream, decoded, blockSizes[i], (uint)length, null);
                        else
                        {
                            var buffer = ArrayPool<byte>.Shared.Rent(length);
                            stream.ReadExactly(buffer, 0, length);
                            decoded.Write(buffer, 0, length);
                            ArrayPool<byte>.Shared.Return(buffer);
                        }

                        remaining -= (uint)length;
                    }

                    stream.Dispose();
                    decoded.Position = 0;
                    return decoded;
                }
            }

            stream.Position = 0;
        }

        return stream;
    }
}
