using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TLTool;

/// <summary>A data file's header.</summary>
public sealed partial class TLDataHeader
{
    /// <summary>The creation time of the header file.</summary>
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;

    /// <summary>The entries contained in the <see cref="TLDataHeader"/>.</summary>
    public ReadOnlyCollection<TLDataHeaderEntry> Entries { get; }

    /// <summary>The entries contained in the <see cref="TLDataHeader"/>.</summary>
    private readonly List<TLDataHeaderEntry> _entries = [];

    /// <summary>Maps a file hash to a file index.</summary>
    private readonly Dictionary<uint, int> _hashToIndex = [];

    /// <summary>Initializes a new <see cref="TLDataHeader"/> instance.</summary>
    public TLDataHeader()
    {
        Entries = _entries.AsReadOnly();
    }

    /// <summary>Reads the entries from the specified stream into the <see cref="TLDataHeader"/>.</summary>
    public void ReadFrom(BinaryStream stream, bool is32Bit)
    {
        ReadFrom(stream, data: null, is32Bit);
    }

    /// <summary>Reads the entries from the specified stream into the <see cref="TLDataHeader"/>.</summary>
    /// <param name="data">The data file containing the data for the entries described in the stream.</param>
    public void ReadFrom(BinaryStream stream, FileInfo? data, bool is32Bit)
    {
        var header = new RawHeader(stream, is32Bit);
        CreationTime = DateTime.FromFileTimeUtc((long)header.CreationTime);

        stream.BaseStream.Position = RawHeader.GetBaseFileOffset(is32Bit) + (long)header.FileArrayOffset;
        var files = new RawFile[header.FileArrayLength];

        for (var i = 0ul; i < header.FileArrayLength; i++)
            files[i] = new RawFile(stream);

        stream.BaseStream.Position = RawHeader.GetBaseEntryOffset(is32Bit) + (long)header.FileHashArrayOffset;
        for (var i = 0ul; i < header.FileHashArrayLength; i++)
        {
            var hash = stream.ReadUInt32();
            var index = stream.ReadUInt32();
            var file = files[index];
            var source = new TLFileDataSource(data, index, (long)file.Offset, (long)file.Length, (long)file.CompressedLength);
            var entry = new TLDataHeaderEntry(source, hash, Encoding.ASCII.GetString(file.Extension));
            AddEntry(entry);
        }
    }

    /// <summary>Adds the specified file to the <see cref="TLDataHeader"/>.</summary>
    public void AddEntry(TLDataHeaderEntry entry)
    {
        if (_hashToIndex.ContainsKey(entry.NameHash))
            throw new ArgumentException("A file with the same hash already exists.");

        _hashToIndex.Add(entry.NameHash, _entries.Count);
        _entries.Add(entry);
    }

    /// <summary>Adds the specified file to the <see cref="TLDataHeader"/>.</summary>
    public void AddOrUpdateEntry(TLDataHeaderEntry entry)
    {
        if (_hashToIndex.TryGetValue(entry.NameHash, out int index))
            _entries[index] = entry;
        else
            AddEntry(entry);
    }

    /// <summary>Gets the file with the specified name hash.</summary>
    public bool TryGetEntry(uint hash, [NotNullWhen(true)] out TLDataHeaderEntry? entry)
    {
        if (_hashToIndex.TryGetValue(hash, out int index))
        {
            entry = _entries[index];
            return true;
        }

        entry = null;
        return false;
    }

    /// <summary>Gets the file with the specified name hash and extension.</summary>
    public bool TryGetEntry(uint hash, string extension, [NotNullWhen(true)] out TLDataHeaderEntry? entry)
    {
        if (_hashToIndex.TryGetValue(hash, out int index))
        {
            entry = _entries[index];

            if (entry.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        entry = null;
        return false;
    }

    /// <summary>Gets the file with the specified name.</summary>
    public bool TryGetEntry(string name, [NotNullWhen(true)] out TLDataHeaderEntry? entry)
    {
        return TryGetEntry(TLHash.HashToUInt32(name), GetExtension(name), out entry);
    }

    /// <summary>Gets the file with the specified name.</summary>
    public bool TryGetEntry(ReadOnlySpan<byte> name, [NotNullWhen(true)] out TLDataHeaderEntry? entry)
    {
        return TryGetEntry(TLHash.HashToUInt32(name, TLHashOptions.IgnoreCase), GetExtension(Encoding.ASCII.GetString(name)), out entry);
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

    /// <summary>Removes all entries from the <see cref="TLDataHeader"/>.</summary>
    public void Clear()
    {
        _hashToIndex.Clear();
        _entries.Clear();
    }

    /// <summary>Writes the header and all of its entries to the specified header and data streams.</summary>
    public unsafe void Write(BinaryStream writer, Stream data, bool is32Bit)
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
            writer.WriteUInt32(entries[i].NameHash);
            writer.WriteInt32(i);
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

                bytes[..length].CopyTo(file.ExtensionBuffer);
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

    /// <summary>Gets the file extension without a leading period.</summary>
    private static string GetExtension(string name)
    {
        string extension = Path.GetExtension(name);

        if (string.IsNullOrEmpty(extension))
            return extension;

        return extension[1..];
    }
}
