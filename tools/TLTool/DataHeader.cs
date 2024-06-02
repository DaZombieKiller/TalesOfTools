using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TLTool;

/// <summary>A data file's header.</summary>
public sealed partial class DataHeader
{
    /// <summary>The creation time of the header file.</summary>
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;

    /// <summary>The entries contained in the <see cref="DataHeader"/>.</summary>
    public ReadOnlyCollection<DataHeaderEntry> Entries { get; }

    /// <summary>The entries contained in the <see cref="DataHeader"/>.</summary>
    private readonly List<DataHeaderEntry> _entries = [];

    /// <summary>Maps a file hash to a file index.</summary>
    private readonly Dictionary<uint, int> _hashToIndex = [];

    /// <summary>Initializes a new <see cref="DataHeader"/> instance.</summary>
    public DataHeader()
    {
        Entries = _entries.AsReadOnly();
    }

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

        reader.BaseStream.Position = RawHeader.GetBaseFileOffset(is32Bit) + (long)header.FileArrayOffset;
        var files = new RawFile[header.FileArrayLength];

        for (var i = 0ul; i < header.FileArrayLength; i++)
            files[i] = new RawFile(reader);

        reader.BaseStream.Position = RawHeader.GetBaseEntryOffset(is32Bit) + (long)header.FileHashArrayOffset;
        for (var i = 0ul; i < header.FileHashArrayLength; i++)
        {
            var hash = reader.ReadUInt32();
            var index = reader.ReadUInt32();
            var file = files[index];
            var source = new TLFileDataSource(data, index, (long)file.Offset, (long)file.Length, (long)file.CompressedLength);
            var entry = new DataHeaderEntry(source, hash, Encoding.ASCII.GetString(file.Extension));
            AddEntry(entry);
        }
    }

    /// <summary>Adds the specified file to the <see cref="DataHeader"/>.</summary>
    public void AddEntry(DataHeaderEntry entry)
    {
        if (_hashToIndex.ContainsKey(entry.NameHash))
            throw new ArgumentException("A file with the same hash already exists.");

        _hashToIndex.Add(entry.NameHash, _entries.Count);
        _entries.Add(entry);
    }

    /// <summary>Adds the specified file to the <see cref="DataHeader"/>.</summary>
    public void AddOrUpdateEntry(DataHeaderEntry entry)
    {
        if (_hashToIndex.TryGetValue(entry.NameHash, out int index))
            _entries[index] = entry;
        else
            AddEntry(entry);
    }

    /// <summary>Gets the file with the specified name hash.</summary>
    public bool TryGetEntry(uint hash, [NotNullWhen(true)] out DataHeaderEntry? entry)
    {
        if (_hashToIndex.TryGetValue(hash, out int index))
        {
            entry = _entries[index];
            return true;
        }

        entry = null;
        return false;
    }

    /// <summary>Sorts all entries by name hash.</summary>
    public void SortEntriesByNameHash()
    {
        _hashToIndex.Clear();
        _entries.Sort((a, b) => a.NameHash.CompareTo(b.NameHash));

        for (int i = 0; i < _entries.Count; i++)
        {
            _hashToIndex.Add(_entries[i].NameHash, i);
        }
    }

    /// <summary>Removes all entries from the <see cref="DataHeader"/>.</summary>
    public void Clear()
    {
        _hashToIndex.Clear();
        _entries.Clear();
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
        Array.Sort(entries, (a, b) => a.NameHash.CompareTo(b.NameHash));

        header.FileHashArrayOffset = (ulong)writer.BaseStream.Position - (ulong)RawHeader.GetBaseEntryOffset(is32Bit);
        header.FileHashArrayLength = (uint)entries.Length;

        for (int i = 0; i < entries.Length; i++)
        {
            writer.Write(entries[i].NameHash);
            writer.Write(i);
        }

        header.FileArrayOffset = (ulong)writer.BaseStream.Position - (ulong)RawHeader.GetBaseFileOffset(is32Bit);
        header.FileArrayLength = (uint)entries.Length;

        foreach (var entry in entries)
        {
            var file = new RawFile
            {
                Hash = entry.NameHash,
                Offset = (ulong)data.Length,
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

            data.Position = (long)file.Offset;

            using (var stream = entry.DataSource.OpenRead())
                stream.CopyTo(data);

            file.Write(writer);
        }

        writer.BaseStream.Position = 0;
        header.Write(writer, is32Bit);
    }
}
