namespace TLTool;

public sealed class ZArcFileDataSource(FileInfo? file, uint alignment, long offset, long length, ReadOnlySpan<uint> blockSizes) : IDataSource
{
    /// <summary>The data file containing the referenced data.</summary>
    public FileInfo? File { get; set; } = file;

    /// <summary>Alignment of the compressed blocks.</summary>
    public uint Alignment { get; } = alignment;

    /// <summary>Offset of the data within the archive.</summary>
    public long Offset { get; } = offset;

    /// <inheritdoc/>
    public long Length { get; } = length;

    /// <inheritdoc/>
    public long CompressedLength { get; } = GetCompressedLength(blockSizes, alignment);

    /// <summary>Index into the compression block table for this file.</summary>
    public uint[] BlockSizes { get; } = blockSizes.ToArray();

    /// <inheritdoc/>
    public Stream OpenRead()
    {
        var stream = CompressionUtility.GetZArcDecompressionStream(OpenReadRaw(), BlockSizes, Alignment, Length, leaveOpen: false);

        if (stream.Length > sizeof(int))
        {
            int magic = stream.ReadInt32LittleEndian();
            stream.Position -= sizeof(int);

            if (magic == CompressionUtility.TlzcMagic)
            {
                return CompressionUtility.GetTlzcDecompressionStream(stream, leaveOpen: false);
            }
        }

        return stream;
    }

    /// <inheritdoc cref="OpenRead()"/>
    public Stream OpenReadRaw()
    {
        if (File is null)
            throw new InvalidOperationException();

        return OpenReadRaw(File);
    }

    /// <inheritdoc cref="OpenReadRaw()"/>
    public Stream OpenReadRaw(FileInfo file)
    {
        var length = CompressedLength > 0 ? CompressedLength : Length;
        return new SubReadStream(file.OpenRead(), Offset, length);
    }

    /// <summary>Gets the compressed length of the ZARC entry.</summary>
    private static long GetCompressedLength(ReadOnlySpan<uint> blockSizes, uint alignment)
    {
        long length = 0;

        for (int i = 0; i < blockSizes.Length; i++)
        {
            if (blockSizes[i] == 0)
                length += alignment;
            else
            {
                // LZMA decoder settings
                length += 5;

                // output length
                length += sizeof(ulong);

                // ...block data
                length += blockSizes[i];
            }
        }

        return length;
    }
}
