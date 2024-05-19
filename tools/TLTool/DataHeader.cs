using System.Text;

namespace TLTool;

/// <summary>A data file's header.</summary>
public sealed partial class DataHeader
{
    /// <summary>The creation time of the header file.</summary>
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;

    /// <summary>The entries contained in the <see cref="DataHeader"/>.</summary>
    public Dictionary<uint, DataHeaderEntry> Entries { get; } = [];

    /// <summary>Reads the entries from the specified stream into the <see cref="DataHeader"/>.</summary>
    public void ReadFrom(BinaryReader reader, bool is32Bit)
    {
        ReadFrom(reader, data: null, is32Bit);
    }

    /// <summary>Reads the entries from the specified stream into the <see cref="DataHeader"/>.</summary>
    /// <param name="data">The data file containing the data for the entries described in the stream.</param>
    public void ReadFrom(BinaryReader reader, FileInfo? data, bool is32Bit)
    {
        var header = new RawHeader(reader, is32Bit);
        CreationTime = DateTime.FromFileTimeUtc((long)header.CreationTime);

        // We could read the entries array here, but it's merely a sorted lookup table
        // into the files array, so assuming we have a well-formed file, we don't really
        // need to bother reading it and can just read the files array directly.
        reader.BaseStream.Position = RawHeader.GetBaseFileOffset(is32Bit) + (long)header.FileArrayOffset;
        var files = new RawFile[header.FileArrayLength];

        for (var i = 0ul; i < header.FileArrayLength; i++)
        {
            files[i] = new RawFile(reader);
        }

        reader.BaseStream.Position = RawHeader.GetBaseEntryOffset(is32Bit) + (long)header.FileHashArrayOffset;
        for (var i = 0ul; i < header.FileHashArrayLength; i++)
        {
            reader.ReadUInt32();
            var file = files[reader.ReadUInt32()];
            var source = new TLFileDataSource(data, (long)file.Offset, (long)file.Length, (long)file.CompressedLength);
            var entry = new DataHeaderEntry(source, Encoding.ASCII.GetString(file.Extension));
            Entries.Add(file.Hash, entry);
        }
    }

    /// <summary>Adds the specified file to the <see cref="DataHeader"/>.</summary>
    public void AddFile(string name, DataHeaderEntry entry)
    {
        AddFile(NameHash.Compute(name), entry);
    }

    /// <summary>Adds the specified file to the <see cref="DataHeader"/>.</summary>
    public void AddFile(uint hash, DataHeaderEntry entry)
    {
        Entries.Add(hash, entry);
    }

    /// <summary>Adds the specified file to the <see cref="DataHeader"/>.</summary>
    public void AddOrUpdateFile(string name, DataHeaderEntry entry)
    {
        AddOrUpdateFile(NameHash.Compute(name), entry);
    }

    /// <summary>Adds the specified file to the <see cref="DataHeader"/>.</summary>
    public void AddOrUpdateFile(uint hash, DataHeaderEntry entry)
    {
        Entries[hash] = entry;
    }

    /// <summary>Writes the header and all of its entries to the specified header and data streams.</summary>
    public unsafe void Write(BinaryWriter writer, Stream data, bool is32Bit)
    {
        var header = new RawHeader();
        header.Write(writer, is32Bit);

        // Update the creation time
        header.CreationTime = (ulong)CreationTime.ToFileTimeUtc();

        // The entries array is sorted by hash so that the engine can perform a binary search on it.
        var entries = Entries.ToArray();
        Array.Sort(entries, new KeyComparer<uint, DataHeaderEntry>());

        header.FileHashArrayOffset = (ulong)writer.BaseStream.Position - (ulong)RawHeader.GetBaseEntryOffset(is32Bit);
        header.FileHashArrayLength  = (uint)entries.Length;

        for (int i = 0; i < entries.Length; i++)
        {
            writer.Write(entries[i].Key);
            writer.Write(i);
        }

        header.FileArrayOffset = (ulong)writer.BaseStream.Position - (ulong)RawHeader.GetBaseFileOffset(is32Bit);
        header.FileArrayLength  = (uint)entries.Length;

        foreach (var (hash, entry) in entries)
        {
            var file = new RawFile
            {
                Hash = hash,
                Offset = (ulong)data.Position,
                Length = (ulong)entry.DataSource.Length,
                CompressedLength = (ulong)entry.DataSource.Length,
            };

            fixed (char* extension = entry.Extension)
            {
                var bytes = Encoding.ASCII.GetBytes(entry.Extension);
                int length = int.Min(RawFile.MaxExtensionLength, bytes.Length);

                if (bytes.Length > RawFile.MaxExtensionLength)
                    Console.WriteLine($"Warning: '{entry.Extension}' extension is too long and will be truncated.");

                bytes[..length].CopyTo(new Span<byte>(file.ExtensionBuffer, RawFile.MaxExtensionLength));
                file.ExtensionLength = (byte)length;
            }

            if (entry.DataSource is TLFileDataSource internalEntry)
            {
                file.CompressedLength = (ulong)internalEntry.CompressedLength;
                file.Offset = (ulong)internalEntry.Offset;
                file.Write(writer);
                continue;
            }

            using (var stream = entry.DataSource.OpenRead())
                stream.CopyTo(data);

            file.Write(writer);
        }

        writer.BaseStream.Position = 0;
        header.Write(writer, is32Bit);
    }
}
