using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Texty.Core.Configuration;
using Texty.Core.Object;

namespace Texty.Core.Mode;

public class DetailMode : ITextyMode
{
    public TextyPixel[] Texty(Image<Rgba32> image, TextyConfig config)
    {
        var (width, height) = config.GetRenderSize();
        using var resized = ITextyMode.Clone(image, width, height, config);

        var result = new TextyPixel[width * height];

        resized.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                int pos = y * width;
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

                    int edge = Math.Max(Math.Abs(gray - grayR), Math.Abs(gray - grayD));

                    int index = edge > config.Threshold ? 0 : (gray * (config.CharSet.Length - 1)) >> 8;

                    result[pos + x] = new TextyPixel(p, (byte)index);
                }
            }
        });

        return result;
    }
}