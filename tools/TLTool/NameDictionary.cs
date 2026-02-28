using System.Xml.Linq;

namespace TLTool;

public abstract class NameDictionary
{
    public abstract bool TryAdd(FileMap name);

    public virtual bool TryAdd(string name) => TryAdd(new FileMap { Name = name });

    public abstract void WriteNameList(TextWriter writer);

    public abstract void WriteFileDatabase(TextWriter writer);

    public void AddNamesFromList(TextReader reader)
    {
        for (string? line; (line = reader.ReadLine()) is { };)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            TryAdd(line);
        }
    }

    public void AddNamesFromList(string path)
    {
        using var reader = new StreamReader(path);
        AddNamesFromList(reader);
    }

    public void AddNamesFromXml(string path)
    {
        var document = XDocument.Parse(File.ReadAllText(path));

        foreach (var map in document.Root?.Element("FileMapArray")?.Elements("FileMap") ?? [])
        {
            var value = map.Element("Value");

            if (value == null || value.Element("Name") is not { } name)
                continue;

            var entry = new FileMap
            {
                Name = name.Value,
                FullPath = value.Element("FullPath")?.Value ?? string.Empty,
                SourcePath = value.Element("SourcePath")?.Value ?? string.Empty,
            };

            if (value.Element("Size") is { } sizeElement && long.TryParse(sizeElement.Value, out long size))
                entry.Size = size;

            if (value.Element("TimeStamp") is { } timeElement && long.TryParse(timeElement.Value, out long timeStamp))
                entry.TimeStamp = timeStamp;

            TryAdd(entry);
        }
    }
}

public abstract class NameDictionary<THash> : NameDictionary
    where THash : notnull
{
    public readonly Dictionary<THash, FileMap> Names = [];

    protected abstract ulong GetHash(THash hash);

    public override void WriteNameList(TextWriter writer)
    {
        foreach (var entry in Names.Values.Order())
        {
            writer.WriteLine(entry.Name);
        }
    }

    public override void WriteFileDatabase(TextWriter writer)
    {
        var document = new XDocument(
            new XDeclaration("1.0", "Shift_JIS", null),
            new XElement("BODY",
                new XElement("FileMapArray",
                    new XAttribute("size", Names.Count),
                    Names.Select(Name => Name.Value.ToXElement(GetHash(Name.Key))))));

        writer.Write(document.ToString(SaveOptions.None));
    }
}
