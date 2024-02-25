namespace TLTool;

/// <summary>A data header entry referencing an external file.</summary>
public sealed class DataHeaderEntry : IDataHeaderEntry
{
    /// <summary>The file used as the source for the entry's data.</summary>
    public FileInfo FileInfo { get; }

    /// <inheritdoc/>
    public long Length => FileInfo.Length;

    /// <inheritdoc/>
    public string Extension => FileInfo.Extension.TrimStart('.');

    /// <summary>Initializes a new <see cref="DataHeaderEntry"/> instance.</summary>
    public DataHeaderEntry(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);
        FileInfo = file;
    }

    /// <inheritdoc/>
    public Stream OpenRead()
    {
        return FileInfo.OpenRead();
    }
}
