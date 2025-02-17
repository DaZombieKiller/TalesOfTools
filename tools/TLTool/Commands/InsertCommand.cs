using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;

namespace TLTool;

public sealed class InsertCommand
{
    public Command Command { get; } = new("insert");

    public Argument<string> HeaderPath { get; } = new("header-path", "Path to FILEHEADER.TOFHDB");

    public Argument<string> TLFilePath { get; } = new("tlfile-path", "Path to TLFILE.TLDAT");

    public Argument<string> InputPath { get; } = new("input-path", "Folder containing unpacked files to insert");

    public Option<bool> Is32Bit { get; } = new("--bit32", "File is 32-bit (Xillia, Zestiria)");

    public Option<bool> IsBigEndian { get; } = new("--big-endian", "File is big-endian");

    public InsertCommand()
    {
        Command.AddArgument(HeaderPath);
        Command.AddArgument(TLFilePath);
        Command.AddArgument(InputPath);
        Command.AddOption(Is32Bit);
        Command.AddOption(IsBigEndian);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var header = new TLDataHeader();
        var inputs = context.ParseResult.GetValueForArgument(InputPath);
        var is32Bit = context.ParseResult.GetValueForOption(Is32Bit);
        var bigEndian = context.ParseResult.GetValueForOption(IsBigEndian);

        using (var stream = File.OpenRead(context.ParseResult.GetValueForArgument(HeaderPath)))
            header.ReadFrom(new BinaryStream(stream, bigEndian), new FileInfo(context.ParseResult.GetValueForArgument(TLFilePath)), is32Bit);

        foreach (string file in Directory.EnumerateFiles(inputs, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var hash = TLHash.HashToUInt32(Path.GetFileName(file), TLHashOptions.IgnoreCase);

            if (name.StartsWith('$') && !uint.TryParse(name.AsSpan(1), NumberStyles.HexNumber, null, out hash))
            {
                Console.WriteLine($"Error: could not parse hash from '{file}'. Skipping...");
                continue;
            }

            header.AddOrUpdateEntry(new TLDataHeaderEntry(new FileInfo(file), hash));
        }

        using (var data = File.Open(context.ParseResult.GetValueForArgument(TLFilePath), FileMode.Append))
        {
            using var stream = File.Create(context.ParseResult.GetValueForArgument(HeaderPath));
            header.Write(new BinaryStream(stream, bigEndian), data, is32Bit);
        }
    }
}
