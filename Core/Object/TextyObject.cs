namespace Texty;

public abstract class TextyObject : IDisposable
{
    public abstract Config Config { get; }

    public abstract string Texty();

    public abstract IAsyncEnumerable<string> TextyAsync();

    public abstract void Save();

    public abstract Task SaveAsync();

    public abstract void Dispose();

    public static TextyObject FromConfig(Config config) => config.IsImage ? new TextyImage(config) : new TextyVideo(config);
}
