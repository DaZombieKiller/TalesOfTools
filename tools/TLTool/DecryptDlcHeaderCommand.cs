using System.Buffers.Binary;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace TLTool;

public sealed class DecryptDlcHeaderCommand
{
    public Command Command { get; } = new("decrypt-dlc-header");

    public Argument<string> HeaderPath { get; } = new("header-path", "Path to DLCHEADERPACKAGE.DAT file");

    public Option<string> OutputPath { get; } = new("--output", "Path to output file");

    public DecryptDlcHeaderCommand()
    {
        Command.AddArgument(HeaderPath);
        Command.AddOption(OutputPath);
        Handler.SetHandler(Command, Execute);
    }

    public void Execute(InvocationContext context)
    {
        var path = context.ParseResult.GetValueForArgument(HeaderPath);
        var data = File.ReadAllBytes(path);

        if (BinaryPrimitives.ReadInt32LittleEndian(data) != 0x5FAB6C8D)
        {
            Console.WriteLine("File is not encrypted.");
            return;
        }

        var span = data.AsSpan(8);
        var hash = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4));
        Decrypt(span, key: 0x7AB58E6F);

        // Expected hash value immediately precedes encrypted data.
        if (TLHash.ComputeNoXor(span) != hash)
        {
            Console.WriteLine("Failed to decrypt data.");
            return;
        }

        if (context.ParseResult.HasOption(OutputPath))
            path = context.ParseResult.GetValueForOption(OutputPath)!;
        
        File.WriteAllBytes(path, span.ToArray());
    }

    private static void Decrypt(Span<byte> data, int key)
    {
        int i;
        int n = data.Length - (data.Length % sizeof(uint));
        
        for (i = 0; i < n; i += sizeof(uint))
        {
            var span = data.Slice(i, sizeof(uint));
            int temp = BinaryPrimitives.ReadInt32LittleEndian(span);
            BinaryPrimitives.WriteInt32LittleEndian(span, temp ^ key);
            key ^= span[1] | (span[3] << 8) | (span[0] << 16) | (span[2] << 24);
            key = 'A' * key + (key >>> 2) - 0x61C88647;
        }

        for (; i < data.Length; i++)
        {
            data[i] ^= (byte)(key >>> (8 * (i & (sizeof(uint) - 1))));
        }
    }
}
