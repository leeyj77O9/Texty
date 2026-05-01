using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Texty.Configuration;
using Texty.Core.Object;

namespace Texty.Mode;

public interface ITextyMode
{
    TextyPixel[] Texty(Image<Rgba32> image, Config config);

    Task<TextyPixel[]> TextyAsync(Image<Rgba32> image, Config config, CancellationToken cancellationToken = default) => Task.Run(() => Texty(image, config), default);
}