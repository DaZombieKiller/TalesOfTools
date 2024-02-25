﻿using System.Text;

namespace TLTool;

/// <summary>A data file's header.</summary>
public sealed partial class DataHeader
{
    private readonly Dictionary<uint, IDataHeaderEntry> _entries;

    /// <summary>The entries contained in the <see cref="DataHeader"/>.</summary>
    public IReadOnlyDictionary<uint, IDataHeaderEntry> Entries { get; }

    /// <summary>Initializes a new <see cref="DataHeader"/> instance.</summary>
    public DataHeader()
    {
        _entries = new Dictionary<uint, IDataHeaderEntry>();
        Entries  = _entries.AsReadOnly();
    }

    /// <summary>Initializes a new <see cref="DataHeader"/> instance.</summary>
    public DataHeader(Stream stream, FileInfo data)
        : this()
    {
        ReadFrom(stream, data);
    }

    /// <summary>Reads the entries from the specified stream into the <see cref="DataHeader"/>.</summary>
    /// <param name="data">The data file containing the data for the entries described in the stream.</param>
    public void ReadFrom(Stream stream, FileInfo data)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        var header       = new RawHeader(reader);

        // We could read the entries array here, but it's merely a sorted lookup table
        // into the files array, so assuming we have a well-formed file, we don't really
        // need to bother reading it and can just read the files array directly.
        stream.Position = RawHeader.BaseFileOffset + (long)header.FilesOffset;
        var files = new RawFile[header.FilesCount];

        for (var i = 0ul; i < header.FilesCount; i++)
        {
            files[i] = new RawFile(reader);
        }

        stream.Position = RawHeader.BaseEntryOffset + (long)header.EntryOffset;
        for (var i = 0ul; i < header.EntryCount; i++)
        {
            reader.ReadUInt32();
            var file = files[reader.ReadUInt32()];
            _entries.Add(file.Hash, new InternalHeaderEntry(data, Encoding.ASCII.GetString(file.Extension))
            {
                Offset = (long)file.Offset,
                Length = (long)file.Length,
                CompressedLength = (long)file.CompressedLength,
            });
        }
    }

    /// <summary>Adds the specified file to the <see cref="DataHeader"/>.</summary>
    public void AddFile(string name, IDataHeaderEntry entry)
    {
        AddFile(NameHash.Compute(name), entry);
    }

    /// <summary>Adds the specified file to the <see cref="DataHeader"/>.</summary>
    public void AddFile(uint hash, IDataHeaderEntry entry)
    {
        _entries.Add(hash, entry);
    }

    /// <summary>Writes the header and all of its entries to the specified header and data streams.</summary>
    public unsafe void Write(Stream destination, Stream data)
    {
        var header       = new RawHeader();
        using var writer = new BinaryWriter(destination, Encoding.ASCII, leaveOpen: true);
        header.Write(writer);

        // The entries array is sorted by hash so that the engine can perform a binary search on it.
        var entries = _entries.ToArray();
        Array.Sort(entries, new KeyComparer<uint, IDataHeaderEntry>());

        header.EntryOffset = (ulong)destination.Position - RawHeader.BaseEntryOffset;
        header.EntryCount  = (uint)entries.Length;

        for (int i = 0; i < entries.Length; i++)
        {
            writer.Write(entries[i].Key);
            writer.Write(i);
        }

        header.FilesOffset = (ulong)destination.Position - RawHeader.BaseFileOffset;
        header.FilesCount  = (uint)entries.Length;

        foreach (var (hash, entry) in entries)
        {
            var file = new RawFile
            {
                Hash = hash,
                Offset = (ulong)data.Position,
                Length = (ulong)entry.Length,
                CompressedLength = (ulong)entry.Length,
            };

            fixed (char* extension = entry.Extension)
            {
                var bytes = Encoding.ASCII.GetBytes(entry.Extension);
                int length = int.Min(RawFile.MaxExtensionLength, bytes.Length);

                if (bytes.Length > RawFile.MaxExtensionLength)
                    Console.WriteLine($"Warning: '{entry.Extension}' extension is too long and will be truncated.");

                bytes[..length].CopyTo(new Span<byte>(file.ExtensionBuffer, RawFile.MaxExtensionLength));
                file.ExtensionLength = (ushort)length;
            }

            using (var stream = entry.OpenRead())
                stream.CopyTo(data);

            file.Write(writer);
        }

        destination.Position = 0;
        header.Write(writer);
    }
}
