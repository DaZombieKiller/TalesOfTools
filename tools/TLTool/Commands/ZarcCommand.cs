using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;

namespace TLTool;

public sealed class ZarcCommand
{
    public Command Command { get; } = new("zarc");

    public Argument<string> InputPath { get; } = new("input-path", "Folder containing unpacked files");

    public Argument<string> OutputPath { get; } = new("output-path", "Path to write ZARC to");

    public ZarcCommand()
    {
        Command.AddArgument(InputPath);
        Command.AddArgument(OutputPath);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var zarc = new ZArcFile();
        var inputs = context.ParseResult.GetValueForArgument(InputPath);
        var output = context.ParseResult.GetValueForArgument(OutputPath);

        foreach (string file in Directory.EnumerateFiles(inputs, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            var path = Path.GetRelativePath(inputs, file).Replace("\\", "/").ToLowerInvariant();
            var hash = ZArcHash.HashToUInt64(path);
            Console.WriteLine(path);

            if (name.StartsWith('$') && !ulong.TryParse(name.AsSpan(1), NumberStyles.HexNumber, null, out hash))
            {
                Console.WriteLine($"Error: could not parse hash from '{file}'. Skipping...");
                continue;
            }

            if (zarc.TryGetEntry(hash, out var entry))
            {
                Console.WriteLine($"Error: cannot import '{file}' because it conflicts with '{((FileDataSource)entry.DataSource).File.FullName}'.");
                continue;
            }

            zarc.AddEntry(new ZArcFileEntry(new FileInfo(file), hash));
        }

        if (Path.GetDirectoryName(output) is { } directory && !string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        using var stream = File.Create(output);
        zarc.Write(new BinaryStream(stream, bigEndian: true));
    }
}
