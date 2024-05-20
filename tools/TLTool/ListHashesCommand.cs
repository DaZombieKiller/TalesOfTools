﻿using System.CommandLine;
using System.CommandLine.Invocation;

namespace TLTool;

public sealed class ListHashesCommand
{
    public Command Command { get; } = new("list-hashes");

    public Argument<string[]> Names { get; } = new("names") { Arity = ArgumentArity.OneOrMore };

    public Option<bool> CaseSensitive { get; } = new("--case-sensitive", "Whether to use case-sensitive hashes (Zestiria SCPACK)");

    public ListHashesCommand()
    {
        Command.AddArgument(Names);
        Command.AddOption(CaseSensitive);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        bool ignoreCase = !context.ParseResult.GetValueForOption(CaseSensitive);

        foreach (string name in context.ParseResult.GetValueForArgument(Names))
        {
            Console.WriteLine($"{NameHash.Compute(name, ignoreCase):X8}: {name}");
        }
    }
}
