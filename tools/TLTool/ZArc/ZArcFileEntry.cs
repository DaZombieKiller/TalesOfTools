namespace TLTool;

public sealed class ZArcFileEntry(IDataSource source, ulong nameHash)
{
    /// <summary>The hash of the entry's file name.</summary>
    public ulong NameHash { get; } = nameHash;

    /// <summary>The source for the entry's data.</summary>
    public IDataSource DataSource { get; } = source;

    /// <inheritdoc cref="IDataSource.OpenRead()" />
    public Stream OpenRead()
    {
        return DataSource.OpenRead();
    }
}
