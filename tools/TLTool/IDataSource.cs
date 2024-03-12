namespace TLTool;

/// <summary>A source of data.</summary>
public interface IDataSource
{
    /// <summary>The uncompressed length of the data, in bytes.</summary>
    long Length { get; }

    /// <summary>Opens the data for reading.</summary>
    Stream OpenRead();
}
