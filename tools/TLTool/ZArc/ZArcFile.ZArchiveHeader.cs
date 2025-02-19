
namespace TLTool;

public partial class ZArcFile
{
    /// <summary>Data structure describing the raw header of a <see cref="ZArcFile"/>.</summary>
    private struct ZArchiveHeader(BinaryStream reader)
    {
        /// <summary>The size of a <see cref="ZArchiveHeader"/> within a ZARC.</summary>
        public const int Size = 40;

        /// <summary>The ZARC magic value.</summary>
        public uint Magic = reader.ReadUInt32();

        /// <summary>Format version of the file?</summary>
        public uint Version = reader.ReadUInt32();

        /// <summary>Offset to the end of the header.</summary>
        public uint EndOfHeader = reader.ReadUInt32();

        /// <summary>Size of a content entry.</summary>
        public uint ContentSize = reader.ReadUInt32();

        /// <summary>Number of content entries in the header.</summary>
        public uint ContentCount = reader.ReadUInt32();

        /// <summary>Unknown.</summary>
        public uint Unknown = reader.ReadUInt32();

        /// <summary>The encoding of file path strings before they were hashed.</summary>
        public ZArcStringEncodeType PathEncoding = (ZArcStringEncodeType)reader.ReadUInt32();

        /// <summary>The alignment of compressed blocks.</summary>
        public uint BlockAlignment = reader.ReadUInt32();

        /// <summary>The alignment used for file data.</summary>
        public uint FileAlignment = reader.ReadUInt32();

        /// <summary>The case conversion (if any) to perform on file paths when hashing.</summary>
        public ZArcStringCaseType PathCaseConversion = (ZArcStringCaseType)reader.ReadUInt32();

        /// <summary>Writes this <see cref="ZArchiveHeader"/> to the given <see cref="BinaryStream"/>.</summary>
        public readonly void Write(BinaryStream writer)
        {
            writer.WriteUInt32(Magic);
            writer.WriteUInt32(Version);
            writer.WriteUInt32(EndOfHeader);
            writer.WriteUInt32(ContentSize);
            writer.WriteUInt32(ContentCount);
            writer.WriteUInt32(Unknown);
            writer.WriteUInt32((uint)PathEncoding);
            writer.WriteUInt32(BlockAlignment);
            writer.WriteUInt32(FileAlignment);
            writer.WriteUInt32((uint)PathCaseConversion);
        }
    }
}
