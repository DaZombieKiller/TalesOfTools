using System.Diagnostics.CodeAnalysis;

namespace TLTool;

public sealed class ZArcNameDictionary(ZArcStringCaseType caseConversion) : NameDictionary<ulong>
{
    public override bool TryAdd(FileMap map)
    {
        string hashName = map.Name;

        switch (caseConversion)
        {
        case ZArcStringCaseType.Lower:
            hashName = hashName.ToLowerInvariant();
            break;
        case ZArcStringCaseType.Upper:
            hashName = hashName.ToUpperInvariant();
            break;
        }

        var hash = ZArcHash.HashToUInt64(hashName);
        return Names.TryAdd(hash, map);
    }

    public override bool TryAdd(string name)
    {
        // Don't add placeholder hash names
        if (name.StartsWith('$'))
            return false;

        return TryAdd(new FileMap { Name = name });
    }

    public bool TryGetValue(ulong hash, [NotNullWhen(true)] out string? name)
    {
        if (Names.TryGetValue(hash, out var entry))
        {
            name = entry.Name;
            return true;
        }

        name = null;
        return false;
    }

    public string GetNameOrFallback(ulong hash)
    {
        return GetNameOrFallback(hash, "bin");
    }

    public string GetNameOrFallback(ulong hash, string fallbackExtension)
    {
        if (Names.TryGetValue(hash, out var entry))
            return entry.Name;

        return $"${hash:X16}.{fallbackExtension}";
    }

    protected override ulong GetHash(ulong hash)
    {
        return hash;
    }
}
