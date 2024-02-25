using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace TLTool;

public sealed class ScriptPackCommand
{
    public Command Command { get; } = new("scpack");

    public Argument<string> InputPath { get; } = new("input-path", "Folder containing .LUAC files");

    public Argument<string> OutputPath { get; } = new("output-path", "Path to write resulting SCPACKR file to");

    public Option<bool> BigEndian { get; } = new("--big-endian", "Whether to write a big-endian file for PS3");

    public ScriptPackCommand()
    {
        Command.AddArgument(InputPath);
        Command.AddArgument(OutputPath);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var scpack = new ScriptPackage();
        var inputs = context.ParseResult.GetValueForArgument(InputPath);
        var output = context.ParseResult.GetValueForArgument(OutputPath);

        foreach (string file in Directory.EnumerateFiles(inputs, "*.LUAC", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            scpack.Entries.Add(new KeyValuePair<string, byte[]>(name, File.ReadAllBytes(file)));
        }

        using var stream = File.Create(output);

        if (context.ParseResult.HasOption(BigEndian))
            scpack.Write(stream, bigEndian: context.ParseResult.GetValueForOption(BigEndian));
        else
            scpack.Write(stream, bigEndian: false);
    }
}
