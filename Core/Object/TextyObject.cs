using Texty.Core.Configuration;

namespace Texty.Core.Object;

public abstract class TextyObject : IDisposable
{
    public abstract string Texty();

    public virtual Task<string> TextyAsync(CancellationToken ct = default) => Task.Run(() => Texty(), ct);

    public abstract void Save();

    public abstract Task SaveAsync(CancellationToken ct = default);

    public abstract void Dispose();

    public static TextyObject FromConfig(TextyConfig config) => config.IsImage ? TextyImage.CreateAsync(config).Result : TextyVideo.CreateAsync(config).Result;

    public static TextyImage FromConfigToImage(TextyConfig config) => TextyImage.CreateAsync(config).Result;

    public static TextyVideo FromConfigToVideo(TextyConfig config) => TextyVideo.CreateAsync(config).Result;
}
