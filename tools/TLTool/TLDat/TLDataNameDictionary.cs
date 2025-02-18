using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace TLTool;

public sealed class TLDataNameDictionary : NameDictionary<(uint Hash, string Extension)>
{
    public override bool TryAdd(string name)
    {
        name = name.ToUpperInvariant();

        // Don't add placeholder hash names
        if (name.StartsWith('$'))
            return false;

        // We need a file extension
        var extension = Path.GetExtension(name);

        if (extension is not { Length: > 1 })
            return false;

        var hash = TLHash.HashToUInt32(name, TLHashOptions.IgnoreCase);
        return Names.TryAdd((hash, extension[1..]), name);
    }

    public bool TryGetValue(uint hash, string extension, [NotNullWhen(true)] out string? name)
    {
        return Names.TryGetValue((hash, extension.ToUpperInvariant()), out name);
    }

    public string GetNameOrFallback(uint hash, string extension)
    {
        extension = extension.ToUpperInvariant();

        if (Names.TryGetValue((hash, extension), out string? name))
            return name;

        return $"${hash:X8}.{extension}";
    }
}
