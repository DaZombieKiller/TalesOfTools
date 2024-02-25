namespace TLTool;

public sealed partial class DataHeader
{
    /// <summary>Data structure describing the raw header of a <see cref="DataHeader"/>.</summary>
    private struct RawHeader
    {
        /// <summary>The offset of <see cref="EntryOffset"/> in the header.</summary>
        public const long BaseEntryOffset = 0x8;

        /// <summary>The offset of <see cref="FilesOffset"/> in the header.</summary>
        public const long BaseFileOffset = 0x28;

        /// <summary>Unknown purpose. Related to FILEDATABASE.TLFDBX file.</summary>
        public ulong Unknown1;

        /// <summary>Offset to an array of uint pairs. Relative to itself.</summary>
        public ulong EntryOffset;

        /// <summary>Number of entries in the header.</summary>
        public ulong EntryCount;

        /// <summary>Unknown purpose, usually zero.</summary>
        public ulong Unknown2;

        /// <summary>Unknown purpose, usually zero.</summary>
        public ulong Unknown3;

        /// <summary>Offset to array of <see cref="RawFile"/> entries. Relative to itself.</summary>
        public ulong FilesOffset;

        /// <summary>Number of files in the header.</summary>
        public ulong FilesCount;

        /// <summary>Unknown purpose, usually zero.</summary>
        public ulong Unknown4;

        /// <summary>Unknown purpose, usually zero.</summary>
        public ulong Unknown5;

        /// <summary>Initializes a new <see cref="RawHeader"/> instance.</summary>
        public RawHeader(BinaryReader reader)
        {
            Unknown1 = reader.ReadUInt64();
            EntryOffset = reader.ReadUInt64();
            EntryCount = reader.ReadUInt64();
            Unknown2 = reader.ReadUInt64();
            Unknown3 = reader.ReadUInt64();
            FilesOffset = reader.ReadUInt64();
            FilesCount = reader.ReadUInt64();
            Unknown4 = reader.ReadUInt64();
            Unknown5 = reader.ReadUInt64();
        }

        /// <summary>Writes this <see cref="RawHeader"/> to the given <see cref="BinaryWriter"/>.</summary>
        public readonly void Write(BinaryWriter writer)
        {
            writer.Write(Unknown1);
            writer.Write(EntryOffset);
            writer.Write(EntryCount);
            writer.Write(Unknown2);
            writer.Write(Unknown3);
            writer.Write(FilesOffset);
            writer.Write(FilesCount);
            writer.Write(Unknown4);
            writer.Write(Unknown5);
        }
    }
}
