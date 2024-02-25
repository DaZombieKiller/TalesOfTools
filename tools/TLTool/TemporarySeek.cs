namespace TLTool;

/// <summary>A temporary <see cref="Stream"/> seek operation.</summary>
public readonly struct TemporarySeek : IDisposable
{
    /// <summary>The <see cref="Stream"/> instance that will be seeked.</summary>
    private readonly Stream _stream;

    /// <summary>The position of the <see cref="Stream"/> instance prior to seeking.</summary>
    private readonly long _previousPosition;

    /// <summary>Initializes a new <see cref="TemporarySeek"/> instance.</summary>
    public TemporarySeek(Stream stream, long position)
    {
        _stream = stream;
        _previousPosition = stream.Position;
        stream.Position = position;
    }

    /// <summary>Restores the original position of the <see cref="Stream"/>.</summary>
    public void Dispose()
    {
        _stream.Position = _previousPosition;
    }
}
