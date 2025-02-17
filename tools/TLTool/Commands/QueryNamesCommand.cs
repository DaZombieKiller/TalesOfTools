using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;

namespace TLTool;

public sealed class QueryNamesCommand
{
    public Command Command { get; } = new("query-names");

    public Argument<string> Dictionary { get; } = new("dictionary", "Path to name dictionary file");

    public Argument<string[]> Hashes { get; } = new("hashes") { Arity = ArgumentArity.OneOrMore };

    public QueryNamesCommand()
    {
        Command.AddArgument(Dictionary);
        Command.AddArgument(Hashes);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var mapper = new TLDataNameDictionary();
        mapper.AddNamesFromFile(context.ParseResult.GetValueForArgument(Dictionary));

        foreach (string query in context.ParseResult.GetValueForArgument(Hashes))
        {
            var hashValue = Path.GetFileNameWithoutExtension(query);
            var extension = Path.GetExtension(query);

            if (extension is null || extension.Length < 2)
                goto NotFound;

            if (hashValue.StartsWith('$'))
                hashValue = hashValue[1..];

            if (!uint.TryParse(hashValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hash))
                goto NotFound;

            if (!mapper.TryGetValue(hash, Path.GetExtension(query)[1..], out var name))
                goto NotFound;

            Console.WriteLine($"{query}: {name}");
            continue;
        NotFound:
            Console.WriteLine($"{query}: <unknown>");
        }
    }
}
