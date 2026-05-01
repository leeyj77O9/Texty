using Texty.Configuration;

namespace Texty;

public abstract class TextyObject : IDisposable
{
    public abstract string Texty();

    public virtual Task<string> TextyAsync(CancellationToken cancellationToken = default) => Task.Run(() => Texty(), cancellationToken);

    public abstract void Save();

    public abstract Task SaveAsync();

    public abstract void Dispose();

    public static TextyObject FromConfig(Config config) => config.IsImage ? new TextyImage(config) : new TextyVideo(config);
}
