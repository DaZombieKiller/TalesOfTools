using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Globalization;

namespace TLTool;

public sealed class QueryFileCommand
{
    public Command Command { get; } = new("query-file");

    public Argument<string> HeaderPath { get; } = new("header-path", "Path to FILEHEADER.TOFHDB file");

    public Argument<string> DataOffset { get; } = new("data-offset", "Hexadecimal offset to data in TLFILE");

    public Option<bool> Is32Bit { get; } = new("--bit32", "File is 32-bit (Xillia, Zestiria)");

    public Option<bool> IsBigEndian { get; } = new("--big-endian", "File is big-endian");

    public Option<string> Dictionary { get; } = new("--dictionary", "Path to name dictionary file");

    public QueryFileCommand()
    {
        Command.AddArgument(HeaderPath);
        Command.AddArgument(DataOffset);
        Command.AddOption(Is32Bit);
        Command.AddOption(IsBigEndian);
        Command.AddOption(Dictionary);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var data = new DataHeader();
        var mapper = new NameDictionary();
        var is32Bit = context.ParseResult.GetValueForOption(Is32Bit);
        var bigEndian = context.ParseResult.GetValueForOption(IsBigEndian);
        var offset = ulong.Parse(context.ParseResult.GetValueForArgument(DataOffset), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        using (var stream = File.OpenRead(context.ParseResult.GetValueForArgument(HeaderPath)))
        using (var reader = bigEndian ? new BigEndianBinaryReader(stream) : new BinaryReader(stream))
            data.ReadFrom(reader, is32Bit);

        if (context.ParseResult.HasOption(Dictionary))
            mapper.AddNamesFromFile(context.ParseResult.GetValueForOption(Dictionary)!);

        foreach (var entry in data.Entries)
        {
            var source = (TLFileDataSource)entry.DataSource;

            if (offset < (ulong)source.Offset || offset >= (ulong)source.Offset + (ulong)source.CompressedLength)
                continue;

            Console.WriteLine(mapper.GetNameOrFallback(entry.NameHash, entry.Extension));
            return;
        }
    }
}
