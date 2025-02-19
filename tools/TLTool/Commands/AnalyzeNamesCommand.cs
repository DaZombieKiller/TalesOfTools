using System.CommandLine;
using System.CommandLine.Invocation;

namespace TLTool;

public sealed class AnalyzeNamesCommand
{
    public Command Command { get; } = new("analyze-names");

    public Argument<string> InputPath { get; } = new("input-path", "Path to input file");

    public Argument<string> FileDictionaryPath { get; } = new("dictionary", "Path to name dictionary file");

    public Option<bool> Is32Bit { get; } = new("--bit32", "File is 32-bit (Xillia, Zestiria)");

    public Option<bool> IsBigEndian { get; } = new("--big-endian", "File is big-endian");

    public Option<bool> IsZarc { get; } = new("--zarc", "Input file is a ZARC");

    public AnalyzeNamesCommand()
    {
        Command.AddArgument(FileDictionaryPath);
        Command.AddArgument(InputPath);
        Command.AddOption(Is32Bit);
        Command.AddOption(IsBigEndian);
        Command.AddOption(IsZarc);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var data = new TLDataHeader();
        var zarc = new ZArcFile();
        var is32Bit = context.ParseResult.GetValueForOption(Is32Bit);
        var bigEndian = context.ParseResult.GetValueForOption(IsBigEndian);
        var isZarc = context.ParseResult.GetValueForOption(IsZarc);
        NameDictionary mapper;

        if (isZarc)
        {
            zarc.ReadFrom(new FileInfo(context.ParseResult.GetValueForArgument(InputPath)));
            mapper = new ZArcNameDictionary(zarc.PathCaseConversion);
        }
        else
        {
            using (var stream = File.OpenRead(context.ParseResult.GetValueForArgument(InputPath)))
                data.ReadFrom(new BinaryStream(stream, bigEndian), is32Bit);

            mapper = new TLDataNameDictionary();
        }

        mapper.AddNamesFromFile(context.ParseResult.GetValueForArgument(FileDictionaryPath));
        var entriesNamed = 0;
        var longestExtension = "Type".Length;
        int longestCount = int.Max(data.Entries.Count.ToString().Length, "Count".Length);
        var filesByExtension = new Dictionary<string, int>();
        var namedByExtension = new Dictionary<string, int>();

        if (mapper is TLDataNameDictionary tldatNames)
        {
            foreach (var entry in data.Entries)
            {
                longestExtension = int.Max(longestExtension, entry.Extension.Length);
                filesByExtension[entry.Extension] = filesByExtension.GetValueOrDefault(entry.Extension) + 1;

                if (tldatNames.TryGetValue(entry.NameHash, entry.Extension, out _))
                {
                    entriesNamed++;
                    namedByExtension[entry.Extension] = namedByExtension.GetValueOrDefault(entry.Extension) + 1;
                }
            }
        }
        else if (mapper is ZArcNameDictionary zarcNames)
        {
            foreach (var entry in zarc.Entries)
            {
                if (zarcNames.TryGetValue(entry.NameHash, out _))
                {
                    entriesNamed++;
                }
            }
        }

        if (isZarc)
            Console.WriteLine("{0} | {1} | {2} | {3}", "Total", zarc.Entries.Count, entriesNamed, $"{entriesNamed / (float)zarc.Entries.Count * 100:F2}%");
        else
        {
            var format = $"{{0,{longestExtension}}} | {{1,{longestCount}}} | {{2,{longestCount}}} | {{3,7}}";
            var header = string.Format(format, "Type", "Count", "Named", "Percentage");

            Console.WriteLine(header);
            Console.WriteLine(new string('-', header.Length));

            if (!isZarc)
            {
                foreach (var (extension, fileCount) in filesByExtension.OrderBy(pair => pair.Key))
                {
                    var named = namedByExtension.GetValueOrDefault(extension);
                    Console.WriteLine(format, extension, fileCount, named, $"{named / (float)fileCount * 100:F2}%");
                }
            }

            Console.WriteLine(new string('-', header.Length));
            Console.WriteLine(format, "Total", data.Entries.Count, entriesNamed, $"{entriesNamed / (float)data.Entries.Count * 100:F2}%");
        }
    }
}
