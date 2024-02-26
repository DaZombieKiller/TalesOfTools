using System.Text;
using System.IO.Compression;

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

                    if (version == 0x0201)
                    {
                        reader.ReadUInt32(); // compressed size
                        reader.ReadUInt32(); // uncompressed size
                        reader.ReadUInt32(); // unknown
                        reader.ReadUInt32(); // unknown
                        return new DeflateStream(stream, CompressionMode.Decompress);
                    }
                }

                stream.Position = 0;
            }

            return stream;
        }
    }
}
