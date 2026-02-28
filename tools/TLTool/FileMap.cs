using System.Xml.Linq;

namespace TLTool;

public sealed class FileMap
{
    /// <summary>Name of the file, including the extension.</summary>
    public required string Name { get; set; }

    /// <summary>Full path of the file.</summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>Path to the source asset that the file was created from.</summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>Size of the file in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Timestamp of the file.</summary>
    public long TimeStamp { get; set; }

    public XElement ToXElement(ulong key)
    {
        return new XElement("FileMap",
            new XElement("Key", key),
            new XElement("Value",
                new XElement("Name", Name),
                new XElement("FullPath", FullPath),
                new XElement("SourcePath", SourcePath),
                new XElement("Size", Size),
                new XElement("TimeStamp", TimeStamp)));
    }
}
