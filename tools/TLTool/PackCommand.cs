using System.CommandLine;
using System.CommandLine.Invocation;

namespace TLTool;

public sealed class PackCommand
{
    public Command Command { get; } = new("pack");

    public Argument<string> InputPath { get; } = new("input-path", "Folder containing unpacked files");

    public Argument<string> OutputPath { get; } = new("output-path", "Folder to write FILEHEADER.TOFHDB and TLFILE.TLDAT to");

    public PackCommand()
    {
        Command.AddArgument(InputPath);
        Command.AddArgument(OutputPath);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var header = new DataHeader();
        var inputs = context.ParseResult.GetValueForArgument(InputPath);
        var output = context.ParseResult.GetValueForArgument(OutputPath);

        foreach (string file in Directory.EnumerateFiles(inputs, "*", SearchOption.AllDirectories))
        {
            var entry = new DataHeaderEntry(new FileInfo(file));
            header.AddFile(Path.GetFileNameWithoutExtension(file), entry);
        }

        using var head = File.Create(Path.Combine(output, "FILEHEADER.TOFHDB"));
        using var data = File.Create(Path.Combine(output, "TLFILE.TLDAT"));
        header.Write(head, data);
    }
}
