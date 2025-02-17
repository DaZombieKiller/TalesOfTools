using System.CommandLine;
using System.CommandLine.Invocation;

namespace TLTool;

public sealed class ScriptPackCommand
{
    public Command Command { get; } = new("scpack");

    public Argument<string> InputPath { get; } = new("input-path", "Folder containing .LUAC files");

    public Argument<string> OutputPath { get; } = new("output-path", "Path to write resulting SCPACKR file to");

    public Option<bool> BigEndian { get; } = new("--big-endian", "Whether to write a big-endian file for PS3");

    public Option<bool> CaseSensitive { get; } = new("--case-sensitive", "Whether to use case-sensitive hashes (Zestiria)");

    public ScriptPackCommand()
    {
        Command.AddArgument(InputPath);
        Command.AddArgument(OutputPath);
        Command.AddOption(BigEndian);
        Command.AddOption(CaseSensitive);
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
        scpack.Write(stream, context.ParseResult.GetValueForOption(BigEndian), !context.ParseResult.GetValueForOption(CaseSensitive));
    }
}
