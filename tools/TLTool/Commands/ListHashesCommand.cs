using System.CommandLine;
using System.CommandLine.Invocation;

namespace TLTool;

public sealed class ListHashesCommand
{
    public Command Command { get; } = new("list-hashes");

    public Argument<string[]> Names { get; } = new("names") { Arity = ArgumentArity.OneOrMore };

    public Option<bool> CaseSensitive { get; } = new("--case-sensitive", "Whether to use case-sensitive hashes (Zestiria SCPACK)");

    public Option<bool> Zarc { get; } = new("--zarc", "Whether to use ZARC lowercase hashes");

    public ListHashesCommand()
    {
        Command.AddArgument(Names);
        Command.AddOption(CaseSensitive);
        Command.AddOption(Zarc);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        bool isZarc = context.ParseResult.GetValueForOption(Zarc);
        bool ignoreCase = !context.ParseResult.GetValueForOption(CaseSensitive);
        var flags = ignoreCase ? TLHashOptions.IgnoreCase : TLHashOptions.None;

        if (isZarc)
        {
            foreach (string name in context.ParseResult.GetValueForArgument(Names))
            {
                Console.WriteLine($"{ZArcHash.HashToUInt64(name.ToLowerInvariant()):X16}: {name}");
            }
        }
        else
        {
            foreach (string name in context.ParseResult.GetValueForArgument(Names))
            {
                Console.WriteLine($"{TLHash.HashToUInt32(name, flags):X8}: {name}");
            }
        }
    }
}
