namespace TLTool;

/// <summary>An entry in a data file header.</summary>
/// <remarks>Initializes a new <see cref="DataHeaderEntry"/> instance.</remarks>
public sealed class DataHeaderEntry(IDataSource source, uint nameHash, string extension)
{
    /// <summary>The hash of the entry's file name, including the extension.</summary>
    public uint NameHash { get; } = nameHash;

    /// <summary>The file extension for the entry, excluding a leading period.</summary>
    public string Extension { get; } = extension;

    /// <summary>The source for the entry's data.</summary>
    public IDataSource DataSource { get; } = source;

    /// <summary>Initializes a new <see cref="DataHeaderEntry"/> instance.</summary>
    public DataHeaderEntry(FileInfo file, uint hash)
        : this(new FileDataSource(file), hash, GetExtension(file))
    {
    }

    /// <summary>Initializes a new <see cref="DataHeaderEntry"/> instance.</summary>
    public DataHeaderEntry(FileInfo file)
        : this(file, TLHash.ComputeIgnoreCase(file.Name))
    {
    }

    /// <summary>Gets the file extension without a leading period.</summary>
    private static string GetExtension(FileInfo file)
    {
        string extension = file.Extension;

        if (string.IsNullOrEmpty(extension))
            return extension;

        return extension[1..];
    }
}
