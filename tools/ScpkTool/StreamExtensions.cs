namespace ScpkTool;

public static class StreamExtensions
{
    public static void WriteAlign(this Stream stream, long alignment)
    {
        var aligned = (stream.Position + (alignment - 1)) & ~(alignment - 1);

        while (stream.Position < aligned)
        {
            stream.WriteByte(0);
        }
    }

    public static TemporarySeek TemporarySeek(this Stream stream)
    {
        return new TemporarySeek(stream, stream.Position);
    }

    public static TemporarySeek TemporarySeek(this Stream stream, long position)
    {
        return new TemporarySeek(stream, position);
    }
}
