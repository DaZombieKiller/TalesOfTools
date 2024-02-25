using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;

namespace TLTool;

public sealed class UpdateNamesCommand
{
    public Command Command { get; } = new("update-names");

    public Argument<string> FilesPath { get; } = new("files-path", "Folder containing unpacked files");

    public Argument<string> FileDictionaryPath { get; } = new("dictionary", "Path to name dictionary file");

    public UpdateNamesCommand()
    {
        Command.AddArgument(FilesPath);
        Command.AddArgument(FileDictionaryPath);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var mapper = new Dictionary<uint, string>();

        using (var reader = new StreamReader(context.ParseResult.GetValueForArgument(FileDictionaryPath)))
        {
            for (string? line; (line = reader.ReadLine()) is { };)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                mapper.TryAdd(NameHash.Compute(line), line);
            }
        }

        foreach (string path in Directory.EnumerateFiles(context.ParseResult.GetValueForArgument(FilesPath), "$*.*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(path);

            if (!name.StartsWith('$'))
                continue;

            if (!uint.TryParse(name.AsSpan(1), NumberStyles.HexNumber, null, out uint hash))
                continue;

            if (mapper.TryGetValue(hash, out name))
            {
                try
                {
                    File.Move(path, Path.Combine(Path.GetDirectoryName(path) ?? "", name));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to rename {path}:");
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
