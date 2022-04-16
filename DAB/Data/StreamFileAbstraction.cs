

namespace DAB.Data;

internal class StreamFileAbstraction : TagLib.File.IFileAbstraction
{
    private readonly Stream _stream;

    internal StreamFileAbstraction(Stream stream, string? name = null)
    {
        _stream = stream;
        Name = name;
    }

    public string? Name { get; }

    public Stream ReadStream => _stream;

    public Stream WriteStream => _stream;

    public void CloseStream(Stream stream)
    {
        
    }
}
