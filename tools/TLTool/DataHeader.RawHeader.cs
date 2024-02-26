namespace TLTool;

public sealed partial class DataHeader
{
    /// <summary>Data structure describing the raw header of a <see cref="DataHeader"/>.</summary>
    private struct RawHeader
    {
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

        /// <summary>Gets the offset of <see cref="EntryOffset"/> in the header.</summary>
        public static long GetBaseEntryOffset(bool is32Bit)
        {
            return 0x8;
        }

        /// <summary>Gets the offset of <see cref="FilesOffset"/> in the header.</summary>
        public static long GetBaseFileOffset(bool is32Bit)
        {
            return is32Bit ? 0x18 : 0x28;
        }

        /// <summary>Initializes a new <see cref="RawHeader"/> instance.</summary>
        public RawHeader(BinaryReader reader, bool is32Bit)
        {
            Unknown1 = reader.ReadUInt64();
            EntryOffset = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            EntryCount = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            Unknown2 = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            Unknown3 = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            FilesOffset = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            FilesCount = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            Unknown4 = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            Unknown5 = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
        }

        /// <summary>Writes this <see cref="RawHeader"/> to the given <see cref="BinaryWriter"/>.</summary>
        public readonly void Write(BinaryWriter writer, bool is32Bit)
        {
            writer.Write(Unknown1);

            if (is32Bit)
            {
                writer.Write((uint)EntryOffset);
                writer.Write((uint)EntryCount);
                writer.Write((uint)Unknown2);
                writer.Write((uint)Unknown3);
                writer.Write((uint)FilesOffset);
                writer.Write((uint)FilesCount);
                writer.Write((uint)Unknown4);
                writer.Write((uint)Unknown5);
            }
            else
            {
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
}
