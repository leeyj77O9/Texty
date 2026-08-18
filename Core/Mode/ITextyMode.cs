using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Texty.Core.Configuration;
using Texty.Core.Object;

namespace Texty.Core.Mode;

public interface ITextyMode
{
    TextyPixel[] Texty(Image<Rgba32> image, Config config);

    Task<TextyPixel[]> TextyAsync(Image<Rgba32> image, Config config, CancellationToken ct = default) => Task.Run(() => Texty(image, config), ct);

    protected static Image<Rgba32> Clone(Image<Rgba32> image, int width, int height, Config config)
    {
        var img = (width == image.Width && height == image.Height) ? image.Clone() : image.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Stretch,
            Sampler = KnownResamplers.NearestNeighbor,
        }));

        if (config.Blur > 0 ||
            config.Contrast != 1.0f ||
            config.Brightness != 1.0f ||
            config.Saturation != 1.0f)
        {
            img.Mutate(x =>
            {
                if (config.Blur > 0)
                    x.GaussianBlur(config.Blur);

                if (config.Contrast != 1.0f)
                    x.Contrast(config.Contrast);

                if (config.Brightness != 1.0f)
                    x.Brightness(config.Brightness);

                if (config.Saturation != 1.0f)
                    x.Saturate(config.Saturation);
            });
        }

        return img;
    }
}