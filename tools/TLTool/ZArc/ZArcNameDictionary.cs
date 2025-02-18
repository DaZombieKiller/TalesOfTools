using System.Diagnostics.CodeAnalysis;

namespace TLTool;

public sealed class ZArcNameDictionary(ZArcStringCaseType caseConversion) : NameDictionary<ulong>
{
    public override bool TryAdd(string name)
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
        return Names.TryAdd(hash, name);
    }

    public bool TryGetValue(ulong hash, [NotNullWhen(true)] out string? name)
    {
        return Names.TryGetValue(hash, out name);
    }

    public string GetNameOrFallback(ulong hash)
    {
        return GetNameOrFallback(hash, "bin");
    }

    public string GetNameOrFallback(ulong hash, string fallbackExtension)
    {
        if (Names.TryGetValue(hash, out string? name))
            return name;

        return $"${hash:X16}.{fallbackExtension}";
    }
}
