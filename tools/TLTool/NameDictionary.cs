using System.Diagnostics.CodeAnalysis;

namespace TLTool;

public sealed class NameDictionary
{
    private readonly Dictionary<(uint Hash, string Extension), string> _names = [];

    public bool TryAdd(string name)
    {
        name = name.ToUpperInvariant();

        // Don't add placeholder hash names
        if (name.StartsWith('$'))
            return false;

        // We need a file extension
        var extension = Path.GetExtension(name);

        if (extension is not { Length: > 1 })
            return false;

        var hash = TLHash.ComputeIgnoreCase(name);
        return _names.TryAdd((hash, extension[1..]), name);
    }

    public bool TryGetValue(uint hash, string extension, [NotNullWhen(true)] out string? name)
    {
        return _names.TryGetValue((hash, extension.ToUpperInvariant()), out name);
    }

    public string GetNameOrFallback(uint hash, string extension)
    {
        extension = extension.ToUpperInvariant();

        if (_names.TryGetValue((hash, extension), out string? name))
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
