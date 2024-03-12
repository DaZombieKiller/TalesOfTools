namespace TLTool;

/// <summary>An entry in a data file header.</summary>
public sealed class DataHeaderEntry
{
    /// <summary>The source for the entry's data.</summary>
    public IDataSource DataSource { get; }

    /// <summary>The file extension for the entry, excluding a leading period.</summary>
    public string Extension { get; }

    /// <summary>Initializes a new <see cref="DataHeaderEntry"/> instance.</summary>
    public DataHeaderEntry(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);
        DataSource = new FileDataSource(file);
        Extension  = GetExtension(file.Extension);

        static string GetExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return extension;

            return extension[1..];
        }
    }

    /// <summary>Initializes a new <see cref="DataHeaderEntry"/> instance.</summary>
    public DataHeaderEntry(IDataSource source, string extension)
    {
        DataSource = source;
        Extension  = extension;
    }
}
