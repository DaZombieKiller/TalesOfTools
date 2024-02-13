using ScpkTool;

if (args.Length < 3)
{
    PrintUsage();
    return;
}

var package = new ScriptPackage();

if (args[0] == "unpack")
{
    Directory.CreateDirectory(args[2]);

    using (var fs = File.OpenRead(args[1]))
        package.Read(fs);

    foreach (var entry in package.Entries)
    {
        File.WriteAllBytes(Path.Combine(args[2], entry.Key + ".LUA"), entry.Value);
    }
}
else if (args[0] == "pack")
{
    bool bigEndian = false;

    foreach (var file in Directory.EnumerateFiles(args[1], "*.lua"))
    {
        var name = Path.GetFileNameWithoutExtension(file).ToUpperInvariant();
        package.Entries.Add(new KeyValuePair<string, byte[]>(name, File.ReadAllBytes(file)));
    }

    if (args.Length > 3 && args[3] == "big-endian")
        bigEndian = true;

    using var fs = new FileStream(args[2], FileMode.Create, FileAccess.Write);
    package.Write(fs, bigEndian);
}
else
{
    PrintUsage();
    return;
}

static void PrintUsage()
{
    Console.WriteLine(
        """
        usage:
        scpktool pack <directory> <out.scpackr> [big-endian]
        scpktool unpack <in.scpackr> <directory>
        """
    );
}
