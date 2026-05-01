using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Texty.Configuration;
using Texty.Core.Object;

namespace Texty.Mode;

public class DefaultTextyMode : ITextyMode
{
    public TextyPixel[] Texty(Image<Rgba32> image, Config config)
    {
        var (width, height) = (config.Width, (int)(config.Height * Config.CHARRATIO));
        using var resized = image.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Stretch,
            Sampler = KnownResamplers.NearestNeighbor,
        }));

        var result = new TextyPixel[width * height];

        Parallel.For(0, height, x =>
        {
            resized.ProcessPixelRows(accessor =>
            {
                var row = accessor.GetRowSpan(x);
                int start = x * width;

                for (int y = 0; y < width; y++)
                {
                    var p = row[y];
                    int gray = (p.R * 77 + p.G * 150 + p.B * 29) >> 8;
                    int index = (gray * (config.CharSet.Length - 1)) >> 8;

                    result[start + y] = new(p, (byte)index);
                }
            });
        });

        return result;
    }
}
