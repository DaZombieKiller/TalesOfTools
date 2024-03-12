namespace TLTool;

/// <summary>A data source referencing a file on disk.</summary>
public sealed class FileDataSource : IDataSource
{
    /// <summary>The file referenced by this data source.</summary>
    public FileInfo File { get; }

    /// <inheritdoc/>
    public long Length => File.Length;

    /// <summary>Initializes a new <see cref="FileDataSource"/> instance.</summary>
    public FileDataSource(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);
        File = file;
    }

    /// <inheritdoc/>
    public Stream OpenRead()
    {
        return File.OpenRead();
    }
}
