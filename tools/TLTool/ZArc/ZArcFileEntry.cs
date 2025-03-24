namespace TLTool;

public sealed class ZArcFileEntry(IDataSource source, ulong nameHash)
{
    /// <summary>The hash of the entry's file name.</summary>
    public ulong NameHash { get; } = nameHash;

    /// <summary>The source for the entry's data.</summary>
    public IDataSource DataSource { get; } = source;

    /// <summary>Initializes a new <see cref="ZArcFileEntry"/> instance.</summary>
    public ZArcFileEntry(FileInfo file, ulong hash)
        : this(new FileDataSource(file), hash)
    {
    }

    /// <summary>Initializes a new <see cref="ZArcFileEntry"/> instance.</summary>
    public ZArcFileEntry(FileInfo file)
        : this(file, ZArcHash.HashToUInt64(file.Name))
    {
    }

    /// <summary>Gets the number of blocks used for this file in a ZARC.</summary>
    public uint GetBlockCount(uint alignment)
    {
        return (uint)((DataSource.Length + alignment - 1) / alignment);
    }

    /// <inheritdoc cref="IDataSource.OpenRead()" />
    public Stream OpenRead()
    {
        return DataSource.OpenRead();
    }

    /// <inheritdoc cref="ZArcFileDataSource.OpenRead"/>
    public Stream OpenRead(bool recurse)
    {
        return DataSource is ZArcFileDataSource source ? source.OpenRead(recurse) : OpenRead();
    }
}
