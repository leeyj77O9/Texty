using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Texty.Core.Configuration;
using Texty.Core.Object;

namespace Texty.Core.Mode;

public class ShadeMode : ITextyMode
{
    public static readonly string CharSet = " ░▒▓█";

    public TextyPixel[] Texty(Image<Rgba32> image, TextyConfig config)
    {
        var (width, height) = config.GetRenderSize();
        using var resized = ITextyMode.Clone(image, width, height, config);

        var result = new TextyPixel[width * height];

        resized.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                int baseIndex = y * width;

                for (int x = 0; x < width; x++)
                {
                    var p = row[x];

                    int gray = (p.R * 77 + p.G * 150 + p.B * 29) >> 8;

                    int idx = (gray * (config.CharSet.Length - 1)) >> 8;

                    result[baseIndex + x] = new TextyPixel(p, (byte)idx);
                }
            }
        });

        return result;
    }
}