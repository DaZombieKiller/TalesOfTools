using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace TLTool;

public sealed class UnpackCommand
{
    public Command Command { get; } = new("unpack");

    public Argument<string> HeaderPath { get; } = new("header-path", "Path to FILEHEADER.TOFHDB");

    public Argument<string> TLFilePath { get; } = new("tlfile-path", "Path to TLFILE.TLDAT");

    public Argument<string> OutputPath { get; } = new("output-path", "Folder to unpack files into");

    public Option<string> FileDictionaryPath { get; } = new("--dictionary", "Path to name dictionary file");

    public Option<bool> Is32Bit { get; } = new("--bit32", "File is 32-bit (Xillia, Zestiria)");

    public Option<bool> IsBigEndian { get; } = new("--big-endian", "File is big-endian");

    public UnpackCommand()
    {
        Command.AddArgument(HeaderPath);
        Command.AddArgument(TLFilePath);
        Command.AddArgument(OutputPath);
        Command.AddOption(FileDictionaryPath);
        Command.AddOption(Is32Bit);
        Command.AddOption(IsBigEndian);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var header = new DataHeader();
        var mapper = new NameDictionary();
        var output = context.ParseResult.GetValueForArgument(OutputPath);
        var is32Bit = context.ParseResult.GetValueForOption(Is32Bit);
        var bigEndian = context.ParseResult.GetValueForOption(IsBigEndian);
        
        using (var stream = File.OpenRead(context.ParseResult.GetValueForArgument(HeaderPath)))
        using (var reader = bigEndian ? new BigEndianBinaryReader(stream) : new BinaryReader(stream))
            header.ReadFrom(reader, new FileInfo(context.ParseResult.GetValueForArgument(TLFilePath)), is32Bit);

        if (context.ParseResult.HasOption(FileDictionaryPath))
            mapper.AddNamesFromFile(context.ParseResult.GetValueForOption(FileDictionaryPath)!);

        foreach (var (hash, entry) in header.Entries)
        {
            var name = mapper.GetNameOrFallback(hash, entry.Extension);
            Directory.CreateDirectory(Path.Combine(output, entry.Extension));
            using var stream = File.Create(Path.Combine(output, entry.Extension, name));
            using var source = entry.DataSource.OpenRead();
            source.CopyTo(stream);
        }
    }
}
