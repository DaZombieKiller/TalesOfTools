using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace TLTool;

public sealed class UnpackCommand
{
    public Command Command { get; } = new("unpack");

    public Argument<string> HeaderPath { get; } = new("header-path", "Path to FILEHEADER.TOFHDB");

    public Argument<string> TLFilePath { get; } = new("tlfile-path", "Path to TLFILE.TLDAT");

    public Argument<string> OutputPath { get; } = new("output-path", "Folder to unpack files into");

    public Option<string> Encrypted { get; } = new("--encrypted", "Path to FILEHEADER.TOFHDA");

    public Option<string> FileDictionaryPath { get; } = new("--dictionary", "Path to name dictionary file");

    public Option<bool> Is32Bit { get; } = new("--bit32", "File is 32-bit (Xillia, Zestiria)");

    public Option<bool> IsBigEndian { get; } = new("--big-endian", "File is big-endian");

    public UnpackCommand()
    {
        Command.AddArgument(HeaderPath);
        Command.AddArgument(TLFilePath);
        Command.AddArgument(OutputPath);
        Command.AddOption(FileDictionaryPath);
        Command.AddOption(Is32Bit);
        Command.AddOption(IsBigEndian);
        Command.AddOption(Encrypted);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var header = new DataHeader();
        var mapper = new NameDictionary();
        var output = context.ParseResult.GetValueForArgument(OutputPath);
        var is32Bit = context.ParseResult.GetValueForOption(Is32Bit);
        var bigEndian = context.ParseResult.GetValueForOption(IsBigEndian);
        var buffer = File.ReadAllBytes(context.ParseResult.GetValueForArgument(HeaderPath));
        DataEncryptHeader? encrypt = null;

        if (context.ParseResult.HasOption(Encrypted))
        {
            encrypt = new DataEncryptHeader(File.ReadAllBytes(context.ParseResult.GetValueForOption(Encrypted)!));
            TLCrypt.Decrypt(buffer, encrypt.GetHeaderKey());
        }

        using (var stream = new MemoryStream(buffer))
        using (var reader = bigEndian ? new BigEndianBinaryReader(stream) : new BinaryReader(stream))
            header.ReadFrom(reader, new FileInfo(context.ParseResult.GetValueForArgument(TLFilePath)), is32Bit);

        if (context.ParseResult.HasOption(FileDictionaryPath))
            mapper.AddNamesFromFile(context.ParseResult.GetValueForOption(FileDictionaryPath)!);

        Parallel.ForEach(header.Entries, entry =>
        {
            var name = mapper.GetNameOrFallback(entry.NameHash, entry.Extension);
            Directory.CreateDirectory(Path.Combine(output, entry.Extension));
            using var source = GetStream((TLFileDataSource)entry.DataSource, encrypt);
            using var stream = File.Create(Path.Combine(output, entry.Extension, name));
            source.CopyTo(stream);
        });
    }

    private static Stream GetStream(TLFileDataSource source, DataEncryptHeader? encrypt)
    {
        if (encrypt == null || !encrypt.GetFileKey(source.Index, out var key))
        {
            var stream = source.OpenRead();

            if (source.IsCompressed)
                stream = TLFileDataSource.GetDecompressionStream(stream, leaveOpen: false);

            return stream;
        }

        var ms = new MemoryStream((int)source.Length);

        using (var stream = source.OpenRead())
        {
            stream.CopyTo(ms);
            ms.Position = 0;
        }

        ms.TryGetBuffer(out var buffer);
        TLCrypt.Decrypt(buffer, key);

        if (source.IsCompressed)
            return TLFileDataSource.GetDecompressionStream(ms, leaveOpen: false);

        return ms;
    }
}
