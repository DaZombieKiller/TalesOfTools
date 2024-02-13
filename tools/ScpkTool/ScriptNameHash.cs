using System.Text;

namespace ScpkTool;

public static class ScriptNameHash
{
    private const uint GoldenRatio = 0x61C88647;

    public static uint Compute(ReadOnlySpan<byte> name)
    {
        uint hash = 0;

        foreach (byte b in name)
            hash = Append(hash, ToUpper(b));

        return hash;
    }

    public static uint Compute(string name)
    {
        return Compute(Encoding.ASCII.GetBytes(name));
    }

    private static uint Append(uint hash, byte b)
    {
        return hash ^ (b + (hash << 6) + (hash >> 2) - GoldenRatio);
    }

    private static byte ToUpper(byte b)
    {
        return (byte)(b - 'a') < 0x1A ? (byte)(b - ' ') : b;
    }
}
