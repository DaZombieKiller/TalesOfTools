using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;

namespace TLTool;

public sealed class DumpNamesCommand
{
    public Command Command { get; } = new("dump-names");

    public Argument<string> HeaderPath { get; } = new("header-path", "Path to FILEHEADER.TOFHDB");

    public Argument<string> TLFilePath { get; } = new("tlfile-path", "Path to TLFILE.TLDAT");

    public Option<bool> Is32Bit { get; } = new("--bit32", "File is 32-bit (Xillia, Zestiria)");

    public Option<bool> IsBigEndian { get; } = new("--big-endian", "File is big-endian");

    private readonly Dictionary<string, List<TLDataHeaderEntry>> _lookup = [];

    public DumpNamesCommand()
    {
        Command.AddArgument(HeaderPath);
        Command.AddArgument(TLFilePath);
        Command.AddOption(Is32Bit);
        Command.AddOption(IsBigEndian);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var header = new TLDataHeader();
        var mapper = new TLDataNameDictionary();
        var buffer = File.ReadAllBytes(context.ParseResult.GetValueForArgument(HeaderPath));
        var is32Bit = context.ParseResult.GetValueForOption(Is32Bit);
        var bigEndian = context.ParseResult.GetValueForOption(IsBigEndian);

        using (var stream = new MemoryStream(buffer))
            header.ReadFrom(new BinaryStream(stream, bigEndian), new FileInfo(context.ParseResult.GetValueForArgument(TLFilePath)), is32Bit);

        InitializeExtensionLookup(header);
        AddNamesFromDependFiles(header, mapper, bigEndian, is32Bit);
        BruteForceDependFromPhysical(header, mapper, "TOTEXB_D", "TOTEXP_P");
        BruteForceDependFromPhysical(header, mapper, "TOMDLB_D", "TOMDLP_P");
        mapper.Write(Console.Out);
    }

    private static void AddNamesFromDependFiles(TLDataHeader header, TLDataNameDictionary mapper, bool bigEndian, bool is32Bit)
    {
        foreach (TLDataHeaderEntry entry in header.Entries)
        {
            if (entry.Extension.EndsWith("_D", StringComparison.OrdinalIgnoreCase))
            {
                AddNamesFromDependFile(entry, mapper, bigEndian, is32Bit);
            }
        }
    }

    private void BruteForceDependFromPhysical(TLDataHeader header, TLDataNameDictionary mapper, string dependExtension, string physicalExtension)
    {
        foreach (TLDataHeaderEntry texture in GetFilesWithExtension(physicalExtension))
        {
            if (!mapper.TryGetValue(texture.NameHash, texture.Extension, out string? name))
                continue;

            // Change the extension and see if we have a match.
            name = Path.ChangeExtension(name, dependExtension);

            if (!header.TryGetEntry(name, out _))
                continue;

            mapper.TryAdd(name);
        }
    }

    private static void AddNamesFromDependFile(TLDataHeaderEntry entry, TLDataNameDictionary mapper, bool bigEndian, bool is32Bit)
    {
        using var stream = new MemoryStream((int)entry.DataSource.Length);
        var reader = new BinaryStream(stream, bigEndian);

        using (var source = entry.OpenRead())
        {
            source.CopyTo(stream);
            stream.Position = 0;
        }

        // DPDF
        if (is32Bit)
            reader.ReadInt32();
        else
            reader.ReadInt64();

        long endPosition = stream.Position + (is32Bit ? reader.ReadInt32() : reader.ReadInt64());
        long dependCount = is32Bit ? reader.ReadInt32() : reader.ReadInt64();

        // Seek to the dependency name array
        stream.Position = endPosition;
        stream.Position += is32Bit ? reader.ReadInt32() : reader.ReadInt64();

        for (int i = 0; i < dependCount; i++)
        {
            mapper.TryAdd(stream.ReadTerminatedString(Encoding.ASCII));
        }
    }

    private IEnumerable<TLDataHeaderEntry> GetFilesWithExtension(string extension)
    {
        if (_lookup.TryGetValue(extension, out var entries))
            return entries;

        return Enumerable.Empty<TLDataHeaderEntry>();
    }

    private void InitializeExtensionLookup(TLDataHeader header)
    {
        _lookup.Clear();

        foreach (var entry in header.Entries)
        {
            if (!_lookup.TryGetValue(entry.Extension, out var entries))
            {
                entries = [];
                _lookup.Add(entry.Extension, entries);
            }

            entries.Add(entry);
        }
    }
}
