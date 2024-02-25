using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace TLTool;

public unsafe sealed class ScriptPackage
{
    private const uint Alignment = 4;

    private const uint LittleEndianMagic = 'K' | ('P' << 8) | ('C' << 16) | ('S' << 24);

    private const uint BigEndianMagic = 'S' | ('C' << 8) | ('P' << 16) | ('K' << 24);

    public List<KeyValuePair<string, byte[]>> Entries = [];

    public void Read(Stream stream)
    {
        Header header;
        bool isLittleEndian;
        stream.ReadExactly(new(&header, sizeof(Header)));

        if (header.Magic == LittleEndianMagic)
            isLittleEndian = true;
        else if (header.Magic == BigEndianMagic)
            isLittleEndian = false;
        else
            throw new ArgumentException("Stream does not contain a valid SCPK file.");

        if (isLittleEndian != BitConverter.IsLittleEndian)
            header.ReverseEndianness();

        if (header.Version != 100)
            Console.WriteLine("[Warning] File version is not equal to 100.");

        Entries.Clear();
        var builder = new StringBuilder();
        var entries = new Entry[header.NumberOfEntries];
        var scripts = new Script[header.NumberOfEntries];
        stream.Seek(header.OffsetOfEntries, SeekOrigin.Begin);
        stream.ReadExactly(MemoryMarshal.AsBytes(entries.AsSpan()));

        if (isLittleEndian != BitConverter.IsLittleEndian)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i].ReverseEndianness();
            }
        }

        for (int i = 0; i < entries.Length; i++)
        {
            var baseOffset = sizeof(Header) + sizeof(Entry) * i;
            stream.Seek(baseOffset + entries[i].Offset, SeekOrigin.Begin);
            stream.ReadExactly(MemoryMarshal.AsBytes(new Span<Script>(ref scripts[i])));

            if (isLittleEndian != BitConverter.IsLittleEndian)
                scripts[i].ReverseEndianness();

            for (int c; (c = stream.ReadByte()) > 0; builder.Append((char)c))
                continue;

            var code = new byte[scripts[i].Size - 0x10];
            stream.Seek(baseOffset + entries[i].Offset + scripts[i].Offset, SeekOrigin.Begin);
            stream.ReadExactly(code);
            Entries.Add(new(builder.ToString(), code));
            builder.Clear();
        }
    }

    public void Write(Stream stream, bool bigEndian)
    {
        var header = new Header
        {
            Magic           = BitConverter.IsLittleEndian ? LittleEndianMagic : BigEndianMagic,
            Version         = 100,
            OffsetOfEntries = (uint)sizeof(Header),
            NumberOfEntries = (uint)Entries.Count
        };

        if (BitConverter.IsLittleEndian == bigEndian)
            header.ReverseEndianness();

        stream.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<Header>(ref header)));
        var offset  = stream.Position;
        var entries = new Entry[Entries.Count];
        stream.Write(MemoryMarshal.AsBytes(entries.AsSpan()));

        // Entries must be sorted by name hash.
        Entries.Sort(CompareNameHashes);

        for (int i = 0; i < Entries.Count; i++)
        {
            byte[] name = Encoding.ASCII.GetBytes(Entries[i].Key);
            var script  = new Script
            {
                Unknown1 = 0,
                Unknown2 = 0x10,
                Size     = (uint)Entries[i].Value.Length + 0x10
            };

            // Update entry.
            stream.WriteAlign(Alignment);
            entries[i].Offset = (uint)(stream.Position - (sizeof(Header) + sizeof(Entry) * i));
            entries[i].Hash = NameHash.Compute(name);

            if (BitConverter.IsLittleEndian == bigEndian)
                entries[i].ReverseEndianness();

            long position = stream.Position;

            using (stream.TemporarySeek())
            {
                stream.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<Script>(ref script)));
                stream.Write(name);
                stream.WriteByte(0);
                stream.WriteAlign(Alignment);
                script.Offset = (uint)(stream.Position - position);
                stream.Write(Entries[i].Value);
                position = stream.Position;
            }

            if (BitConverter.IsLittleEndian == bigEndian)
                script.ReverseEndianness();

            stream.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<Script>(ref script)));
            stream.Position = position;
            stream.WriteAlign(Alignment);
        }

        stream.WriteAlign(Alignment);
        stream.Position = offset;
        stream.Write(MemoryMarshal.AsBytes(entries.AsSpan()));
    }

    private static int CompareNameHashes(KeyValuePair<string, byte[]> a, KeyValuePair<string, byte[]> b)
    {
        var hash1 = NameHash.Compute(a.Key);
        var hash2 = NameHash.Compute(b.Key);
        return hash1.CompareTo(hash2);
    }

    private struct Header
    {
        /// <summary>The magic number that identifies the file and its endianness.</summary>
        public uint Magic;

        /// <summary>The version number of the file.</summary>
        public uint Version;

        /// <summary>The offset to the table of entries, relative to the <see cref="Header"/>.</summary>
        public uint OffsetOfEntries;

        /// <summary>The number of entries in the file.</summary>
        public uint NumberOfEntries;

        /// <summary>Reverses the endianness of the structure.</summary>
        public void ReverseEndianness()
        {
            Magic = BinaryPrimitives.ReverseEndianness(Magic);
            Version = BinaryPrimitives.ReverseEndianness(Version);
            OffsetOfEntries = BinaryPrimitives.ReverseEndianness(OffsetOfEntries);
            NumberOfEntries = BinaryPrimitives.ReverseEndianness(NumberOfEntries);
        }
    }

    private struct Entry
    {
        /// <summary>The hash of the entry's name, computed by <see cref="NameHash.Compute"/>.</summary>
        public uint Hash;

        /// <summary>The offset of the entry's data, relative to the <see cref="Entry"/>.</summary>
        public uint Offset;

        /// <summary>Reverses the endianness of the structure.</summary>
        public void ReverseEndianness()
        {
            Hash = BinaryPrimitives.ReverseEndianness(Hash);
            Offset = BinaryPrimitives.ReverseEndianness(Offset);
        }
    }

    private struct Script
    {
        /// <summary>An unknown value. It is always equal to zero.</summary>
        public uint Unknown1;

        /// <summary>An unknown value. It is always equal to 16.</summary>
        public uint Unknown2;

        /// <summary>The offset to the Lua code, relative to the <see cref="Script"/>.</summary>
        public uint Offset;

        /// <summary>The size of the Lua code in bytes.</summary>
        public uint Size;

        /// <summary>Reverses the endianness of the structure.</summary>
        public void ReverseEndianness()
        {
            Unknown1 = BinaryPrimitives.ReverseEndianness(Unknown1);
            Unknown2 = BinaryPrimitives.ReverseEndianness(Unknown2);
            Offset = BinaryPrimitives.ReverseEndianness(Offset);
            Size = BinaryPrimitives.ReverseEndianness(Size);
        }
    }
}
