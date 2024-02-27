namespace TLTool;

public sealed partial class DataHeader
{
    /// <summary>Data structure describing the raw header of a <see cref="DataHeader"/>.</summary>
    private struct RawHeader
    {
        /// <summary>The creation time of the file.</summary>
        public ulong CreationTime;

        /// <summary>Offset to an array of uint pairs. Relative to itself.</summary>
        public ulong FileHashArrayOffset;

        /// <summary>Number of entries in the header.</summary>
        public ulong FileHashArrayLength;

        /// <summary>Unknown purpose, usually zero.</summary>
        public ulong VirtualHashArrayOffset;

        /// <summary>Unknown purpose, usually zero.</summary>
        public ulong VirtualHashArrayLength;

        /// <summary>Offset to array of <see cref="RawFile"/> entries. Relative to itself.</summary>
        public ulong FileArrayOffset;

        /// <summary>Number of files in the header.</summary>
        public ulong FileArrayLength;

        /// <summary>Unknown purpose, usually zero.</summary>
        public ulong VirtualPackArrayOffset;

        /// <summary>Unknown purpose, usually zero.</summary>
        public ulong VirtualPackArrayLength;

        /// <summary>Gets the offset of <see cref="FileHashArrayOffset"/> in the header.</summary>
        public static long GetBaseEntryOffset(bool is32Bit)
        {
            return 0x8;
        }

        /// <summary>Gets the offset of <see cref="FileArrayOffset"/> in the header.</summary>
        public static long GetBaseFileOffset(bool is32Bit)
        {
            return is32Bit ? 0x18 : 0x28;
        }

        /// <summary>Initializes a new <see cref="RawHeader"/> instance.</summary>
        public RawHeader(BinaryReader reader, bool is32Bit)
        {
            CreationTime = reader.ReadUInt64();
            FileHashArrayOffset = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            FileHashArrayLength = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            VirtualHashArrayOffset = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            VirtualHashArrayLength = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            FileArrayOffset = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            FileArrayLength = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            VirtualPackArrayOffset = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
            VirtualPackArrayLength = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
        }

        /// <summary>Writes this <see cref="RawHeader"/> to the given <see cref="BinaryWriter"/>.</summary>
        public readonly void Write(BinaryWriter writer, bool is32Bit)
        {
            writer.Write(CreationTime);

            if (is32Bit)
            {
                writer.Write((uint)FileHashArrayOffset);
                writer.Write((uint)FileHashArrayLength);
                writer.Write((uint)VirtualHashArrayOffset);
                writer.Write((uint)VirtualHashArrayLength);
                writer.Write((uint)FileArrayOffset);
                writer.Write((uint)FileArrayLength);
                writer.Write((uint)VirtualPackArrayOffset);
                writer.Write((uint)VirtualPackArrayLength);
            }
            else
            {
                writer.Write(FileHashArrayOffset);
                writer.Write(FileHashArrayLength);
                writer.Write(VirtualHashArrayOffset);
                writer.Write(VirtualHashArrayLength);
                writer.Write(FileArrayOffset);
                writer.Write(FileArrayLength);
                writer.Write(VirtualPackArrayOffset);
                writer.Write(VirtualPackArrayLength);
            }
        }
    }
}
