namespace TLTool;

public partial class ZArcFile
{
    /// <summary>Data structure describing the raw layout of a file entry in a <see cref="ZArcFile"/>.</summary>
    private struct ZArchiveContent(BinaryStream reader)
    {
        /// <summary>The size of a <see cref="ZArchiveContent"/> within a ZARC.</summary>
        public const uint Size = 24;

        /// <summary>The hash of the file's name.</summary>
        public ulong HashId = reader.ReadUInt64();

        /// <summary>The uncompressed length of the file.</summary>
        public ulong UncompressedLength = reader.ReadUInt40();

        /// <summary>Unknown.</summary>
        public uint Unknown = reader.ReadUInt32();

        /// <summary>The block index.</summary>
        public uint BlockIndex = reader.ReadUInt24();

        /// <summary>The offset of the file data in blocks.</summary>
        public uint BlockOffset = reader.ReadUInt32();

        /// <summary>Writes this <see cref="ZArchiveContent"/> to the specified writer.</summary>
        public readonly void Write(BinaryStream writer)
        {
            writer.WriteUInt64(HashId);
            writer.WriteUInt40(UncompressedLength);
            writer.WriteUInt32(Unknown);
            writer.WriteUInt24(BlockIndex);
            writer.WriteUInt32(BlockOffset);
        }
    }
}
