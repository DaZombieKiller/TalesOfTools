using System.CommandLine;
using System.CommandLine.Invocation;

namespace TLTool;

public sealed class MergeFileMapsCommand
{
    public Command Command { get; } = new("merge-tlfdbx");

    public Argument<string> OutputPath { get; } = new("output", "Path to output TLFDBX");

    public Argument<string[]> Paths { get; } = new("paths", "Path to each TLFDBX file");

    public MergeFileMapsCommand()
    {
        Command.AddArgument(OutputPath);
        Command.AddArgument(Paths);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var mapper = new TLDataNameDictionary();
        var inputs = context.ParseResult.GetValueForArgument(Paths)!;
        var output = context.ParseResult.GetValueForArgument(OutputPath)!;

        foreach (var input in inputs)
            mapper.AddNamesFromXml(input);

        using var writer = new StreamWriter(output);
        mapper.WriteFileDatabase(writer);
    }
}
