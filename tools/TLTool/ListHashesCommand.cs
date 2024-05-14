using System.CommandLine;
using System.CommandLine.Invocation;

namespace TLTool;

public sealed class ListHashesCommand
{
    public Command Command { get; } = new("list-hashes");

    public Argument<string[]> Names { get; } = new("names") { Arity = ArgumentArity.OneOrMore };

    public ListHashesCommand()
    {
        Command.AddArgument(Names);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        foreach (string name in context.ParseResult.GetValueForArgument(Names))
        {
            Console.WriteLine($"{NameHash.Compute(name):X8}: {name}");
        }
    }
}
