namespace TLTool;

/// <summary>An entry in a data file header.</summary>
public interface IDataHeaderEntry
{
    /// <summary>The uncompressed length of the entry, in bytes.</summary>
    long Length { get; }

    /// <summary>The file extension for the entry, excluding a leading period.</summary>
    string Extension { get; }

    /// <summary>Opens the entry for reading.</summary>
    Stream OpenRead();
}
