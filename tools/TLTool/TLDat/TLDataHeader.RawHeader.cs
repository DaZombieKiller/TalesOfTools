namespace TLTool;

public sealed partial class TLDataHeader
{
    /// <summary>Data structure describing the raw header of a <see cref="TLDataHeader"/>.</summary>
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
        public RawHeader(BinaryStream reader, bool is32Bit)
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

        /// <summary>Writes this <see cref="RawHeader"/> to the given <see cref="BinaryStream"/>.</summary>
        public readonly void Write(BinaryStream writer, bool is32Bit)
        {
            writer.WriteUInt64(CreationTime);

            if (is32Bit)
            {
                writer.WriteUInt32((uint)FileHashArrayOffset);
                writer.WriteUInt32((uint)FileHashArrayLength);
                writer.WriteUInt32((uint)VirtualHashArrayOffset);
                writer.WriteUInt32((uint)VirtualHashArrayLength);
                writer.WriteUInt32((uint)FileArrayOffset);
                writer.WriteUInt32((uint)FileArrayLength);
                writer.WriteUInt32((uint)VirtualPackArrayOffset);
                writer.WriteUInt32((uint)VirtualPackArrayLength);
            }
            else
            {
                writer.WriteUInt64(FileHashArrayOffset);
                writer.WriteUInt64(FileHashArrayLength);
                writer.WriteUInt64(VirtualHashArrayOffset);
                writer.WriteUInt64(VirtualHashArrayLength);
                writer.WriteUInt64(FileArrayOffset);
                writer.WriteUInt64(FileArrayLength);
                writer.WriteUInt64(VirtualPackArrayOffset);
                writer.WriteUInt64(VirtualPackArrayLength);
            }
        }
    }
}
