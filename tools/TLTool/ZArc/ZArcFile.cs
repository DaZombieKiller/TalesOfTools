using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace TLTool;

public partial class ZArcFile
{
    /// <summary>Expected value of <see cref="ZArchiveHeader.Magic"/>.</summary>
    private const uint ZarcMagic = ('Z' << 24) | ('A' << 16) | ('R' << 8) | 'C';

    /// <summary>Expected value of <see cref="ZArchiveHeader.Version"/>.</summary>
    private const uint ZarcVersion = 2;

    /// <summary>The entries contained in the <see cref="TLDataHeader"/>.</summary>
    public ReadOnlyCollection<ZArcFileEntry> Entries { get; }

    /// <summary>The entries contained in the <see cref="TLDataHeader"/>.</summary>
    private readonly List<ZArcFileEntry> _entries = [];

    /// <summary>Maps a file hash to a file index.</summary>
    private readonly Dictionary<ulong, int> _hashToIndex = [];

    /// <summary>The byte size of each LZMA block.</summary>
    private uint[] _blockSizes = [];

    /// <summary>The compression block alignment for the archive.</summary>
    public int BlockAlignment { get; private set; } = 65536;

    /// <summary>The text encoding that the file names were in prior to being hashed.</summary>
    public ZArcStringEncodeType PathEncoding { get; set; } = ZArcStringEncodeType.Ascii;

    /// <summary>String case conversion to perform when hashing file names.</summary>
    public ZArcStringCaseType PathCaseConversion { get; set; } = ZArcStringCaseType.Lower;

    /// <summary>Initializes a new <see cref="ZArcFile"/> instance.</summary>
    public ZArcFile()
    {
        Entries = _entries.AsReadOnly();
    }

    /// <summary>Reads the entries from the specified stream into the <see cref="ZArcFile"/>.</summary>
    public void ReadFrom(FileInfo file)
    {
        using var stream = file.OpenRead();

        // ZARC files are big endian.
        var reader = new BinaryStream(stream, bigEndian: true);
        var header = new ZArchiveHeader(reader);

        if (header.Magic != ZarcMagic)
            throw new ArgumentException("Stream does not contain a valid ZARC file.");

        if (header.Version != ZarcVersion)
            throw new ArgumentException($"Stream does not contain a version {ZarcVersion} ZARC file.");

        if (header.ContentSize != ZArchiveContent.Size)
            throw new ArgumentException("Stream does not contain a valid ZARC file.");

        // Copy info from the header.
        BlockAlignment = (int)header.BlockAlignment;
        PathCaseConversion = header.PathCaseConversion;
        PathEncoding = header.PathEncoding;

        // The content entries are stored directly following the header.
        var contents = new ZArchiveContent[header.ContentCount];

        for (int i = 0; i < contents.Length; i++)
            contents[i] = new ZArchiveContent(reader);

        // Following the content entries is the block size table.
        _blockSizes = ReadBlockSizes(in header, reader);

        for (int i = 0; i < contents.Length; i++)
        {
            var blocks = (contents[i].UncompressedLength + header.BlockAlignment - 1) / header.BlockAlignment;
            var source = new ZArcFileDataSource(
                file,
                header.BlockAlignment,
                (long)header.FileAlignment * contents[i].BlockOffset,
                (long)contents[i].UncompressedLength,
                _blockSizes.AsSpan((int)contents[i].BlockIndex, (int)blocks));

            var entry = new ZArcFileEntry(source, contents[i].HashId);
            AddEntry(entry);
        }
    }

    public void Write(BinaryStream stream)
    {
        SortEntriesByNameHash();

        var header = new ZArchiveHeader
        {
            Magic = ZarcMagic,
            Version = ZarcVersion,
            EndOfHeader = ZArchiveHeader.Size,
            ContentSize = ZArchiveContent.Size,
            ContentCount = (uint)_entries.Count,
            Unknown = 0,
            PathEncoding = PathEncoding,
            BlockAlignment = (uint)BlockAlignment,
            FileAlignment = 128,
            PathCaseConversion = PathCaseConversion,
        };

        // Compute the offset to the end of the header.
        var blockCount = GetTotalBlockCount(header.BlockAlignment);
        header.EndOfHeader += ZArchiveContent.Size * (uint)_entries.Count;
        header.EndOfHeader += GetBlockSizeTableStride(header.BlockAlignment) * blockCount;
        header.Write(stream);
        var entries = new ZArchiveContent[_entries.Count];
        
        for ((int i, uint blockIndex, long fileOffset) = (0, 0, header.EndOfHeader); i < entries.Length; i++)
        {
            fileOffset = (fileOffset + header.FileAlignment - 1) & ~((long)header.FileAlignment - 1);
            var length = _entries[i].DataSource.Length;
            entries[i] = new ZArchiveContent
            {
                HashId = _entries[i].NameHash,
                UncompressedLength = (ulong)length,
                Unknown = 0,
                BlockIndex = blockIndex,
                BlockOffset = (uint)(fileOffset / header.FileAlignment),
            };

            entries[i].Write(stream);
            blockIndex += _entries[i].GetBlockCount(header.BlockAlignment);
            fileOffset += length;
        }

        // Write a bunch of zero block sizes (no compression support yet...)
        var blockSizes = new uint[blockCount];
        WriteBlockSizes(stream, header.BlockAlignment, blockSizes);

        // Write the file data
        for (int i = 0; i < entries.Length; i++)
        {
            // Need to align to FileAlignment so BlockOffset is valid.
            stream.BaseStream.WriteAlign(header.FileAlignment, 0xEE);
            using var data = _entries[i].OpenRead();
            data.CopyTo(stream.BaseStream);
        }
    }

    /// <summary>Gets the file with the specified name hash.</summary>
    public bool TryGetEntry(ulong hash, [NotNullWhen(true)] out ZArcFileEntry? entry)
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

    /// <summary>Adds the specified file to the <see cref="ZArcFile"/>.</summary>
    public void AddEntry(ZArcFileEntry entry)
    {
        if (_hashToIndex.ContainsKey(entry.NameHash))
            throw new ArgumentException("A file with the same hash already exists.");

        _hashToIndex.Add(entry.NameHash, _entries.Count);
        _entries.Add(entry);
    }

    private uint GetTotalBlockCount(uint alignment)
    {
        uint count = 0;

        foreach (var entry in _entries)
            count += entry.GetBlockCount(alignment);

        return count;
    }

    /// <summary>Calculates the stride of the block size table.</summary>
    private static uint GetBlockSizeTableStride(uint alignment)
    {
        // The game checks != 1, which means '0' is apparently valid, and will produce a stride of 4.
        // I have no idea what happens if alignment is '1' and therefore stride is '0'.
        return alignment != 1 ? 1 + uint.Log2(alignment - 1) / 8 : 0;
    }

    /// <summary>Writes the block size table.</summary>
    private void WriteBlockSizes(BinaryStream writer, uint alignment, ReadOnlySpan<uint> blocks)
    {
        var stride = GetBlockSizeTableStride(alignment);

        if (stride == sizeof(byte))
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                writer.WriteByte((byte)blocks[i]);
            }
        }
        else if (stride == sizeof(ushort))
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                writer.WriteUInt16((ushort)blocks[i]);
            }
        }
        else if (stride == BinaryPrimitivesEx.Int24Size)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                writer.WriteUInt24(blocks[i]);
            }
        }
        else if (stride == sizeof(uint))
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                writer.WriteUInt32(blocks[i]);
            }
        }
    }

    /// <summary>Reads the block size table.</summary>
    private uint[] ReadBlockSizes(in ZArchiveHeader header, BinaryStream reader)
    {
        // The alignment of the LZMA block size table determines the bitness of the
        // individual elements contained within it. For example, when the alignment
        // is 65536, no value will ever be greater than 65535, so there is no point
        // in using 32 bits to store each value, so 16 bits are used instead.
        var stride = GetBlockSizeTableStride(header.BlockAlignment);
        var blocks = new uint[(header.EndOfHeader - reader.Position) / stride];

        if (stride == sizeof(byte))
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = reader.ReadByte();
            }
        }
        else if (stride == sizeof(ushort))
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = reader.ReadUInt16();
            }
        }
        else if (stride == BinaryPrimitivesEx.Int24Size)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = reader.ReadUInt24();
            }
        }
        else if (stride == sizeof(uint))
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = reader.ReadUInt32();
            }
        }

        return blocks;
    }
}
