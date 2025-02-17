namespace TLTool;

/// <summary>An <see cref="IDataSource"/> referencing a file inside an existing data file.</summary>
/// <remarks>Initializes a new <see cref="TLFileDataSource"/> instance.</remarks>
public sealed class TLFileDataSource(FileInfo? file, uint index, long offset, long length, long compressedLength) : IDataSource
{
    /// <summary>The data file containing the referenced data.</summary>
    public FileInfo? File { get; set; } = file;

    /// <summary>The index into the file header for this data.</summary>
    public uint Index { get; } = index;

    /// <summary>The offset into <see cref="File"/> where the referenced data is located.</summary>
    public long Offset { get; } = offset;

    /// <inheritdoc/>
    public long Length { get; } = length;

    /// <summary>The compressed length of the data, in bytes.</summary>
    public long CompressedLength { get; } = compressedLength;

    /// <summary>Whether or not the data has been compressed.</summary>
    public bool IsCompressed => Length != CompressedLength;

    /// <inheritdoc/>
    public Stream OpenRead()
    {
        var stream = OpenReadRaw();

        if (IsCompressed)
            stream = CompressionUtility.GetTlzcDecompressionStream(stream, leaveOpen: false);

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
        return new SubReadStream(file.OpenRead(), Offset, CompressedLength);
    }
}
