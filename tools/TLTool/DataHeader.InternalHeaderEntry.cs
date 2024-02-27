using System.Text;
using System.Buffers;
using System.IO.Compression;
using LzmaDecoder = SevenZip.Compression.LZMA.Decoder;

namespace TLTool;

public sealed partial class DataHeader
{
    /// <summary>A header entry describing data that already exists in a data file.</summary>
    private sealed class InternalHeaderEntry : IDataHeaderEntry
    {
        /// <inheritdoc/>
        public string Extension { get; }

        /// <summary>The data file containing this entry's data.</summary>
        public FileInfo DataFile { get; }

        /// <summary>The offset into <see cref="DataFile"/> where this entry's data is located.</summary>
        public long Offset { get; init; }

        /// <inheritdoc/>
        public long Length { get; init; }

        /// <summary>The compressed length of the entry, in bytes.</summary>
        public long CompressedLength { get; init; }

        /// <summary>Initializes a new <see cref="InternalHeaderEntry"/> instance.</summary>
        public InternalHeaderEntry(FileInfo data, string extension)
        {
            DataFile  = data;
            Extension = extension;
        }

        /// <inheritdoc/>
        public Stream OpenRead()
        {
            Stream stream = DataFile.OpenRead();
            stream = new SubReadStream(stream, Offset, CompressedLength);

            if (Length != CompressedLength)
            {
                using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

                if (reader.ReadUInt32() == 0x435A4C54) // TLZC
                {
                    var version = reader.ReadUInt32();
                    var compressedSize = reader.ReadUInt32();
                    var uncompressedSize = reader.ReadUInt32();
                    reader.ReadUInt32(); // unknown
                    reader.ReadUInt32(); // unknown

                    if (version == 0x0201)
                        return new DeflateStream(stream, CompressionMode.Decompress);

                    // https://blog.lse.epita.fr/2012/04/07/static-analysis-of-an-unknown-compression-format.html
                    // TODO: Clean this up a bit, make it actually stream the data instead of decompressing all at once.
                    if (version == 0x0401)
                    {
                        var decoded    = new MemoryStream();
                        var decoder    = new LzmaDecoder();
                        var blockCount = (uncompressedSize + 0xFFFF) / 0x10000;
                        var blockSizes = new ushort[blockCount];
                        var remaining  = uncompressedSize;
                        decoder.SetDecoderProperties(reader.ReadBytes(5));
                        
                        for (int i = 0; i < blockSizes.Length; i++)
                            blockSizes[i] = reader.ReadUInt16();

                        for (int i = 0; i < blockSizes.Length; i++)
                        {
                            if (blockSizes[i] == 0)
                            {
                                // If the block size is zero, then the rest of the file is uncompressed.
                                var buffer = ArrayPool<byte>.Shared.Rent((int)remaining);
                                stream.ReadExactly(buffer, 0, (int)remaining);
                                decoded.Write(buffer, 0, (int)remaining);
                                ArrayPool<byte>.Shared.Return(buffer);
                                break;
                            }

                            decoder.Code(stream, decoded, blockSizes[i], uint.Min(remaining, 0x10000), null);
                            remaining -= 0x10000;
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
}
