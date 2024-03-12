namespace TLTool;

public sealed class NameDictionary : Dictionary<uint, string>
{
    public bool TryAdd(string name)
    {
        return TryAdd(NameHash.Compute(name), name);
    }

    public string GetNameOrFallback(uint hash, string extension)
    {
        if (TryGetValue(hash, out string? name))
            return name;

        return $"${hash:X8}.{extension}";
    }

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
