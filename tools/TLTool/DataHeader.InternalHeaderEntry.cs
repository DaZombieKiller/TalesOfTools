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
                stream.Position += 4 * 6;
                stream = new DeflateStream(stream, CompressionMode.Decompress);
            }

            return stream;
        }
    }
}
