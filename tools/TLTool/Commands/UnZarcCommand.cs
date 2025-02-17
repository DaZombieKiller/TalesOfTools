using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace TLTool;

public sealed class UnZarcCommand
{
    public Command Command { get; } = new("unzarc");

    public Argument<string> InputPath { get; } = new("file-path", "Path to the ZARC file");

    public Argument<string> OutputPath { get; } = new("output-path", "Folder to unpack files into");

    public Option<string> FileDictionaryPath { get; } = new("--dictionary", "Path to name dictionary file");

    public UnZarcCommand()
    {
        Command.AddArgument(InputPath);
        Command.AddArgument(OutputPath);
        Command.AddOption(FileDictionaryPath);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var archive = new ZArcFile();
        var output = context.ParseResult.GetValueForArgument(OutputPath);
        var input = new FileInfo(context.ParseResult.GetValueForArgument(InputPath));
        archive.ReadFrom(input);
        var mapper = new ZArcNameDictionary(archive.PathCaseConversion);

        if (context.ParseResult.HasOption(FileDictionaryPath))
            mapper.AddNamesFromFile(context.ParseResult.GetValueForOption(FileDictionaryPath)!);

        Parallel.ForEach(archive.Entries, entry =>
        {
            var name = mapper.GetNameOrFallback(entry.NameHash);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(output, name))!);
            using var source = entry.OpenRead();
            using var stream = File.Create(Path.Combine(output, name));
            source.CopyTo(stream);
        });
    }
}
