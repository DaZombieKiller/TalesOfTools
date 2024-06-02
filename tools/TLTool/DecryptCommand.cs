using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.MemoryMappedFiles;

namespace TLTool;

public sealed class DecryptCommand
{
    public Command Command { get; } = new("decrypt");

    public Argument<string> EncryptPath { get; } = new("encrypt-path", "Path to FILEHEADER.TOFHDA");

    public Argument<string> HeaderPath { get; } = new("header-path", "Path to FILEHEADER.TOFHDB");

    public Argument<string> TLFilePath { get; } = new("tlfile-path", "Path to TLFILE.TLDAT");

    public DecryptCommand()
    {
        Command.AddArgument(EncryptPath);
        Command.AddArgument(HeaderPath);
        Command.AddArgument(TLFilePath);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var header = new DataHeader();
        var crypto = new DataEncryptHeader(File.ReadAllBytes(context.ParseResult.GetValueForArgument(EncryptPath)));
        var buffer = File.ReadAllBytes(context.ParseResult.GetValueForArgument(HeaderPath));

        // Decrypt the FILEHEADER
        TLCrypt.Decrypt(buffer, crypto.GetHeaderKey());
        File.WriteAllBytes(context.ParseResult.GetValueForArgument(HeaderPath), buffer);

        // Read the header and decrypt the TLDAT
        using var mmf = MemoryMappedFile.CreateFromFile(context.ParseResult.GetValueForArgument(TLFilePath));
        using var mma = mmf.CreateViewAccessor();

        using (var stream = new MemoryStream(buffer))
        using (var reader = new BinaryReader(stream))
            header.ReadFrom(reader, data: null, is32Bit: false);

        Parallel.ForEach(header.Entries, entry =>
        {
            var source = (TLFileDataSource)entry.DataSource;

            if (!crypto.GetFileKey(source.Index, out var key))
                return;

            var buffer = new byte[source.CompressedLength];
            mma.ReadArray(source.Offset, buffer, 0, buffer.Length);
            TLCrypt.Decrypt(buffer, key);
            mma.WriteArray(source.Offset, buffer, 0, buffer.Length);
        });
    }
}
