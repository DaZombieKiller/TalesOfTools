using System.CommandLine;
using System.CommandLine.Invocation;

namespace TLTool;

public sealed class ListNamesCommand
{
    public Command Command { get; } = new("list-names");

    public Argument<string> HeaderPath { get; } = new("header-path", "Path to FILEHEADER.TOFHDB");

    public Argument<string> FileDictionaryPath { get; } = new("dictionary", "Path to name dictionary file");

    public Option<bool> Is32Bit { get; } = new("--bit32", "File is 32-bit (Xillia, Zestiria)");

    public Option<bool> IsBigEndian { get; } = new("--big-endian", "File is big-endian");

    public ListNamesCommand()
    {
        Command.AddArgument(HeaderPath);
        Command.AddArgument(FileDictionaryPath);
        Command.AddOption(Is32Bit);
        Command.AddOption(IsBigEndian);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var header = new DataHeader();
        var mapper = new NameDictionary();
        var is32Bit = context.ParseResult.GetValueForOption(Is32Bit);
        var bigEndian = context.ParseResult.GetValueForOption(IsBigEndian);
        mapper.AddNamesFromFile(context.ParseResult.GetValueForArgument(FileDictionaryPath)!);

        using (var stream = File.OpenRead(context.ParseResult.GetValueForArgument(HeaderPath)))
        using (var reader = bigEndian ? new BigEndianBinaryReader(stream) : new BinaryReader(stream))
            header.ReadFrom(reader, is32Bit);

        // Sort the entries by data offset, which can reveal the original filesystem folder groupings.
        var entries = header.Entries.Where(entry => entry.DataSource is TLFileDataSource).ToArray();
        Array.Sort(entries, CompareDataSourceOffsets);

        foreach (var entry in entries)
        {
            Console.WriteLine(mapper.GetNameOrFallback(entry.NameHash, entry.Extension));
        }
    }

    private static int CompareDataSourceOffsets(DataHeaderEntry a, DataHeaderEntry b)
    {
        var offset1 = ((TLFileDataSource)a.DataSource).Offset;
        var offset2 = ((TLFileDataSource)b.DataSource).Offset;
        return offset1.CompareTo(offset2);
    }
}
