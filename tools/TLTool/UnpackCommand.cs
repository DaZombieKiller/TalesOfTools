using System.Buffers;
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

    public Option<string> FileDictionaryPath { get; } = new("--dictionary", "Path to name dictionary file");

    public UnpackCommand()
    {
        Command.AddArgument(HeaderPath);
        Command.AddArgument(TLFilePath);
        Command.AddArgument(OutputPath);
        Command.AddOption(FileDictionaryPath);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var header = new DataHeader();
        var mapper = new Dictionary<uint, string>();
        var output = context.ParseResult.GetValueForArgument(OutputPath);

        using (var stream = File.OpenRead(context.ParseResult.GetValueForArgument(HeaderPath)))
            header.ReadFrom(stream, new FileInfo(context.ParseResult.GetValueForArgument(TLFilePath)));

        if (context.ParseResult.HasOption(FileDictionaryPath))
        {
            using var reader = new StreamReader(context.ParseResult.GetValueForOption(FileDictionaryPath)!);

            for (string? line; (line = reader.ReadLine()) is { };)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                mapper.TryAdd(NameHash.Compute(line), line);
            }
        }

        foreach (var (hash, entry) in header.Entries)
        {
            if (!mapper.TryGetValue(hash, out string? name))
                name = $"${hash:X8}.{entry.Extension}";

            Directory.CreateDirectory(Path.Combine(output, entry.Extension));
            using var stream = File.Create(Path.Combine(output, entry.Extension, name));
            using var source = entry.OpenRead();
            CopyAndFillRemainingBytes(source, stream, entry.Length);
        }
    }

    // Handles potentially corrupt FILEHEADER.TOFHDB files referencing out of bounds data in TLFILE.TLDAT
    private static void CopyAndFillRemainingBytes(Stream source, Stream destination, long length, int bufferSize = 81920)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        while (length > 0)
        {
            int count = (int)long.Min(buffer.Length, length);
            int read  = source.ReadAtLeast(buffer, count, throwOnEndOfStream: false);

            if (read == 0)
                break;

            destination.Write(buffer, 0, read);
            length -= read;
        }

        if (length == 0)
            goto Completed;

        // Ensure that the buffer is full of 0x00 bytes.
        // We're going to reuse it to fill the rest of the stream.
        Array.Clear(buffer, 0, bufferSize);

        while (length > 0)
        {
            int count = (int)long.Min(length, bufferSize);
            destination.Write(buffer, 0, count);
            length -= count;
        }

    Completed:
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
