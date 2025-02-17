using System.Diagnostics.CodeAnalysis;

namespace TLTool;

public sealed class ZArcNameDictionary(ZArcStringCaseType caseConversion)
{
    private readonly Dictionary<ulong, string> _names = [];

    public bool TryAdd(string name)
    {
        switch (caseConversion)
        {
        case ZArcStringCaseType.Lower:
            name = name.ToLowerInvariant();
            break;
        case ZArcStringCaseType.Upper:
            name = name.ToUpperInvariant();
            break;
        }

        // Don't add placeholder hash names
        if (name.StartsWith('$'))
            return false;

        var hash = ZArcHash.HashToUInt64(name);
        return _names.TryAdd(hash, name);
    }

    public bool TryGetValue(ulong hash, [NotNullWhen(true)] out string? name)
    {
        return _names.TryGetValue(hash, out name);
    }

    public string GetNameOrFallback(ulong hash)
    {
        return GetNameOrFallback(hash, "bin");
    }

    public string GetNameOrFallback(ulong hash, string fallbackExtension)
    {
        if (_names.TryGetValue(hash, out string? name))
            return name;

        return $"${hash:X16}.{fallbackExtension}";
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

    public void Write(TextWriter writer)
    {
        foreach (string name in _names.Values.Order())
        {
            writer.WriteLine(name);
        }
    }
}
