using System.CommandLine;
using System.CommandLine.Invocation;

namespace TLTool;

public sealed class ScriptUnpackCommand
{
    public Command Command { get; } = new("scunpack");

    public Argument<string> InputPath { get; } = new("input-path", "Path to input SCPACKR file");

    public Argument<string> OutputPath { get; } = new("output-path", "Folder to unpack .LUAC files into");

    public ScriptUnpackCommand()
    {
        Command.AddArgument(InputPath);
        Command.AddArgument(OutputPath);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var scpack = new ScriptPackage();
        var output = context.ParseResult.GetValueForArgument(OutputPath);
        Directory.CreateDirectory(output);

        using (var stream = File.OpenRead(context.ParseResult.GetValueForArgument(InputPath)))
            scpack.Read(stream);

        foreach (var (name, data) in scpack.Entries)
        {
            File.WriteAllBytes(Path.Combine(output, name + ".LUAC"), data);
        }
    }
}
