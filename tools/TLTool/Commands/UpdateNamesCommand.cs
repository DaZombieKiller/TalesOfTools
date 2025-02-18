using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;

namespace TLTool;

public sealed class UpdateNamesCommand
{
    public Command Command { get; } = new("update-names");

    public Argument<string> FilesPath { get; } = new("files-path", "Folder containing unpacked files");

    public Argument<string> FileDictionaryPath { get; } = new("dictionary", "Path to name dictionary file");

    public Option<string> HashTypeString { get; } = new("--hash-type", "Hash type (tlhash (default), zarc, zarc-lower, zarc-upper)");

    public UpdateNamesCommand()
    {
        Command.AddArgument(FilesPath);
        Command.AddArgument(FileDictionaryPath);
        Command.AddOption(HashTypeString);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var filesPath = context.ParseResult.GetValueForArgument(FilesPath);
        NameDictionary mapper = GetHashType(context.ParseResult.GetValueForOption(HashTypeString)) switch
        {
            HashType.ZArc => new ZArcNameDictionary(ZArcStringCaseType.None),
            HashType.ZArcLower => new ZArcNameDictionary(ZArcStringCaseType.Lower),
            HashType.ZArcUpper => new ZArcNameDictionary(ZArcStringCaseType.Upper),
            _ => new TLDataNameDictionary()
        };

        mapper.AddNamesFromFile(context.ParseResult.GetValueForArgument(FileDictionaryPath));

        foreach (string path in Directory.EnumerateFiles(filesPath, "$*.*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(path);

            if (!name.StartsWith('$'))
                continue;

            if (Path.GetExtension(path) is not { Length: > 1 } extension)
                continue;

            if (!ulong.TryParse(name.AsSpan(1), NumberStyles.HexNumber, null, out ulong hash))
                continue;

            if (mapper is ZArcNameDictionary zarc && !zarc.TryGetValue(hash, out name))
                continue;
            else if (mapper is TLDataNameDictionary tl && !tl.TryGetValue(checked((uint)hash), extension[1..], out name))
                continue;

            try
            {
                if (Path.GetDirectoryName(name) is { } subDirectory)
                    Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(path)!, subDirectory));

                File.Move(path, Path.Combine(Path.GetDirectoryName(path)!, name));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to rename {path}:");
                Console.WriteLine(ex);
            }
        }
    }

    private static HashType GetHashType(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "zarc" => HashType.ZArc,
            "zarc-lower" => HashType.ZArcLower,
            "zarc-upper" => HashType.ZArcUpper,
            _ => HashType.TLHash
        };
    }

    private enum HashType
    {
        TLHash,
        ZArc,
        ZArcLower,
        ZArcUpper,
    }
}
