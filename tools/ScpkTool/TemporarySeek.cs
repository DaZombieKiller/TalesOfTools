namespace ScpkTool;

public readonly struct TemporarySeek : IDisposable
{
    private readonly Stream _stream;

    private readonly long _previousPosition;

    public TemporarySeek(Stream stream, long position)
    {
        _stream = stream;
        _previousPosition = stream.Position;
        stream.Position = position;
    }

    public void Dispose()
    {
        _stream.Position = _previousPosition;
    }
}
