using System.Diagnostics.CodeAnalysis;

namespace TLTool;

public sealed class TLDataNameDictionary : NameDictionary<(uint Hash, string Extension)>
{
    public override bool TryAdd(FileMap map)
    {
        var extension = Path.GetExtension(map.Name);

        if (extension is not { Length: > 1 })
            return false;

        var hash = TLHash.HashToUInt32(map.Name, TLHashOptions.IgnoreCase);
        return Names.TryAdd((hash, extension[1..].ToUpperInvariant()), map);
    }

    public override bool TryAdd(string name)
    {
        name = name.ToUpperInvariant();

        // Don't add placeholder hash names
        if (name.StartsWith('$'))
            return false;

        return TryAdd(new FileMap { Name = name });
    }

    public bool TryGetValue(uint hash, string extension, [NotNullWhen(true)] out string? name)
    {
        if (Names.TryGetValue((hash, extension.ToUpperInvariant()), out var entry))
        {
            name = entry.Name;
            return true;
        }

        name = null;
        return false;
    }

    public string GetNameOrFallback(uint hash, string extension)
    {
        extension = extension.ToUpperInvariant();

        if (Names.TryGetValue((hash, extension), out FileMap? entry))
            return entry.Name;

        return $"${hash:X8}.{extension}";
    }

    public bool TryGetFileMap(uint hash, string extension, [NotNullWhen(true)] out FileMap? entry)
    {
        return Names.TryGetValue((hash, extension.ToUpperInvariant()), out entry);
    }

    protected override ulong GetHash((uint Hash, string Extension) hash)
    {
        return hash.Hash;
    }
}
