using TLTool;
using System.CommandLine;

var root = new RootCommand("TLTool");
root.AddCommand(new UnpackCommand().Command);
root.AddCommand(new PackCommand().Command);
root.AddCommand(new UpdateNamesCommand().Command);
root.AddCommand(new ScriptUnpackCommand().Command);
root.AddCommand(new ScriptPackCommand().Command);
root.AddCommand(new TextureDdsConvertCommand().Command);
root.AddCommand(new InsertCommand().Command);
root.AddCommand(new ListNamesCommand().Command);
root.AddCommand(new ListHashesCommand().Command);
root.AddCommand(new AnalyzeNamesCommand().Command);
root.AddCommand(new DecryptDlcHeaderCommand().Command);
root.AddCommand(new DecryptCommand().Command);
root.AddCommand(new QueryNamesCommand().Command);
await root.InvokeAsync(Environment.CommandLine);
