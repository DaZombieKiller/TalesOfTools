using System.Diagnostics.CodeAnalysis;

namespace TLTool;

public sealed class NameDictionary
{
    private readonly Dictionary<uint, string> _names = [];

    public bool TryAdd(string name)
    {
        return _names.TryAdd(NameHash.Compute(name), name);
    }

    public bool TryGetValue(uint hash, [NotNullWhen(true)] out string? name)
    {
        return _names.TryGetValue(hash, out name);
    }

    public string GetNameOrFallback(uint hash, string extension)
    {
        if (_names.TryGetValue(hash, out string? name))
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
