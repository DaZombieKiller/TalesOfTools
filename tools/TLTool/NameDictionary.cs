namespace TLTool;

public abstract class NameDictionary
{
    public abstract bool TryAdd(string name);

    public abstract void Write(TextWriter writer);

    public void AddNames(TextReader reader)
    {
        for (string? line; (line = reader.ReadLine()) is { };)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            TryAdd(line);
        }
    }

    public void AddNamesFromFile(string path)
    {
        using var reader = new StreamReader(path);
        AddNames(reader);
    }
}

public abstract class NameDictionary<THash> : NameDictionary
    where THash : notnull
{
    protected readonly Dictionary<THash, string> Names = [];

    public override void Write(TextWriter writer)
    {
        foreach (string name in Names.Values.Order())
        {
            writer.WriteLine(name);
        }
    }
}
