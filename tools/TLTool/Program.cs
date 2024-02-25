using TLTool;
using System.CommandLine;

var root = new RootCommand("TLTool");
root.AddCommand(new UnpackCommand().Command);
root.AddCommand(new PackCommand().Command);
root.AddCommand(new UpdateNamesCommand().Command);
root.AddCommand(new ScriptUnpackCommand().Command);
root.AddCommand(new ScriptPackCommand().Command);
await root.InvokeAsync(args);
