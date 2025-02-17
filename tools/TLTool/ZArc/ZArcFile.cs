using System.Collections.ObjectModel;

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

    /// <summary>The stride of the block size table (0, 1, 2 or 4).</summary>
    private uint[] _blockSizes = [];

    /// <summary>The compression block alignment for the archive.</summary>
    public int BlockAlignment { get; private set; } = 65536;

    /// <summary>The text encoding that the file names were in prior to being hashed.</summary>
    public ZArcStringEncodeType PathEncoding { get; set; } = ZArcStringEncodeType.Ascii;

    /// <summary>String case conversion to perform when hashing file names.</summary>
    public ZArcStringCaseType PathCaseConversion { get; set; } = ZArcStringCaseType.None;

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
                (long)header.BlockStride * contents[i].BlockOffset,
                (long)contents[i].UncompressedLength,
                _blockSizes.AsSpan((int)contents[i].BlockIndex, (int)blocks));

            var entry = new ZArcFileEntry(source, contents[i].HashId);
            AddEntry(entry);
        }
    }

    /// <summary>Adds the specified file to the <see cref="ZArcFile"/>.</summary>
    private void AddEntry(ZArcFileEntry entry)
    {
        if (_hashToIndex.ContainsKey(entry.NameHash))
            throw new ArgumentException("A file with the same hash already exists.");

        _hashToIndex.Add(entry.NameHash, _entries.Count);
        _entries.Add(entry);
    }

    /// <summary>Calculates the stride of the block size table.</summary>
    private static uint GetBlockSizeTableStride(uint alignment)
    {
        // The game checks != 1, which means '0' is apparently valid, and will produce a stride of 4.
        // I have no idea what happens if alignment is '1' and therefore stride is '0'.
        return alignment != 1 ? 1 + uint.Log2(alignment - 1) / 8 : 0;
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
