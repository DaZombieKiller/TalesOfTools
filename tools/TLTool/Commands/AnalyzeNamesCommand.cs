using System.CommandLine;
using System.CommandLine.Invocation;

namespace TLTool;

public sealed class AnalyzeNamesCommand
{
    public Command Command { get; } = new("analyze-names");

    public Argument<string> HeaderPath { get; } = new("header-path", "Path to FILEHEADER.TOFHDB file");

    public Argument<string> FileDictionaryPath { get; } = new("dictionary", "Path to name dictionary file");

    public Option<bool> Is32Bit { get; } = new("--bit32", "File is 32-bit (Xillia, Zestiria)");

    public Option<bool> IsBigEndian { get; } = new("--big-endian", "File is big-endian");

    public AnalyzeNamesCommand()
    {
        Command.AddArgument(FileDictionaryPath);
        Command.AddArgument(HeaderPath);
        Command.AddOption(Is32Bit);
        Command.AddOption(IsBigEndian);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var data = new TLDataHeader();
        var mapper = new TLDataNameDictionary();
        var is32Bit = context.ParseResult.GetValueForOption(Is32Bit);
        var bigEndian = context.ParseResult.GetValueForOption(IsBigEndian);

        using (var stream = File.OpenRead(context.ParseResult.GetValueForArgument(HeaderPath)))
            data.ReadFrom(new BinaryStream(stream, bigEndian), is32Bit);

        mapper.AddNamesFromFile(context.ParseResult.GetValueForArgument(FileDictionaryPath));
        var entriesNamed = 0;
        var longestExtension = "Type".Length;
        int longestCount = int.Max(data.Entries.Count.ToString().Length, "Count".Length);
        var filesByExtension = new Dictionary<string, int>();
        var namedByExtension = new Dictionary<string, int>();

        foreach (var entry in data.Entries)
        {
            longestExtension = int.Max(longestExtension, entry.Extension.Length);
            filesByExtension[entry.Extension] = filesByExtension.GetValueOrDefault(entry.Extension) + 1;

            if (mapper.TryGetValue(entry.NameHash, entry.Extension, out _))
            {
                entriesNamed++;
                namedByExtension[entry.Extension] = namedByExtension.GetValueOrDefault(entry.Extension) + 1;
            }
        }

        var format = $"{{0,{longestExtension}}} | {{1,{longestCount}}} | {{2,{longestCount}}} | {{3,7}}";
        var header = string.Format(format, "Type", "Count", "Named", "Percentage");
        Console.WriteLine(header);
        Console.WriteLine(new string('-', header.Length));

        foreach (var (extension, fileCount) in filesByExtension.OrderBy(pair => pair.Key))
        {
            var named = namedByExtension.GetValueOrDefault(extension);
            Console.WriteLine(format, extension, fileCount, named, $"{named / (float)fileCount * 100:F2}%");
        }

        Console.WriteLine(new string('-', header.Length));
        Console.WriteLine(format, "Total", data.Entries.Count, entriesNamed, $"{entriesNamed / (float)data.Entries.Count * 100:F2}%");
    }
}
