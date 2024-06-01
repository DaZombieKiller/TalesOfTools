using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;

namespace TLTool;

public sealed class PackCommand
{
    public Command Command { get; } = new("pack");

    public Argument<string> InputPath { get; } = new("input-path", "Folder containing unpacked files");

    public Argument<string> OutputPath { get; } = new("output-path", "Folder to write FILEHEADER.TOFHDB and TLFILE.TLDAT to");

    public Option<bool> Is32Bit { get; } = new("--bit32", "Write a 32-bit package");

    public Option<bool> IsBigEndian { get; } = new("--big-endian", "Write a big-endian package");

    public PackCommand()
    {
        Command.AddArgument(InputPath);
        Command.AddArgument(OutputPath);
        Command.AddOption(Is32Bit);
        Command.AddOption(IsBigEndian);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var header = new DataHeader();
        var inputs = context.ParseResult.GetValueForArgument(InputPath);
        var output = context.ParseResult.GetValueForArgument(OutputPath);
        var is32Bit = context.ParseResult.GetValueForOption(Is32Bit);
        var bigEndian = context.ParseResult.GetValueForOption(IsBigEndian);

        foreach (string file in Directory.EnumerateFiles(inputs, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var hash = TLHash.ComputeIgnoreCase(Path.GetFileName(file));

            if (name.StartsWith('$') && !uint.TryParse(name.AsSpan(1), NumberStyles.HexNumber, null, out hash))
            {
                Console.WriteLine($"Error: could not parse hash from '{file}'. Skipping...");
                continue;
            }

            if (header.Entries.TryGetValue(hash, out var entry))
            {
                Console.WriteLine($"Error: cannot import '{file}' because it conflicts with '{((FileDataSource)entry.DataSource).File.FullName}'.");
                continue;
            }

            header.AddFile(hash, new DataHeaderEntry(new FileInfo(file)));
        }

        using var stream = File.Create(Path.Combine(output, "FILEHEADER.TOFHDB"));
        using var writer = bigEndian ? new BigEndianBinaryWriter(stream) : new BinaryWriter(stream);
        using var data = File.Create(Path.Combine(output, "TLFILE.TLDAT"));
        header.Write(writer, data, is32Bit);
    }
}
