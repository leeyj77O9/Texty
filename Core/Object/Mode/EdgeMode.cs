using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Texty.Configuration;
using Texty.Core.Object;

namespace Texty.Mode;

public class EdgeMode : ITextyMode
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

        int threshold = config.Quality switch
        {
            TextyQuality.Small => 60,
            TextyQuality.Balanced => 40,
            TextyQuality.Fast => 25,
            _ => 30
        };

        Parallel.For(0, height, y =>
        {
            int pos = y * width;
            resized.ProcessPixelRows(accessor =>
            {
                var row = accessor.GetRowSpan(y);
                var next = y + 1 == height ? accessor.GetRowSpan(y - 1) : accessor.GetRowSpan(y + 1);

                for (int x = 0; x < width; x++)
                {
                    var p = row[x];
                    var right = x + 1 == width ? row[x - 1] : row[x + 1];
                    var down = next[x];

                    int gray = (p.R * 77 + p.G * 150 + p.B * 29) >> 8;
                    int grayR = (right.R * 77 + right.G * 150 + right.B * 29) >> 8;
                    int grayD = (down.R * 77 + down.G * 150 + down.B * 29) >> 8;

                    int dx = Math.Abs(gray - grayR), dy = Math.Abs(gray - grayD), edge = Math.Max(dx, dy);

                    result[pos + x] = new(p.R, p.G, p.B, (byte)(edge < threshold ? config.Runes.Length - 1 : 0));
                }

            });
        });

        return result;
    }
}
