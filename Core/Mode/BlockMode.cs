using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Texty.Core.Configuration;
using Texty.Core.Object;

namespace Texty.Core.Mode;

public class BlockMode : ITextyMode
{
    public static readonly string CharSet = " ▘▝▀▖▌▞▛▗▚▐▜▄▙▟█";

    public TextyPixel[] Texty(Image<Rgba32> image, Config config)
    {
        int width = config.Width;
        int height = (int)(config.Height * Config.CHARRATIO);

        using var resized = ITextyMode.Clone(image, width, height, config);
        var result = new TextyPixel[width * height];

        resized.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row0 = accessor.GetRowSpan(y * 2);
                var row1 = accessor.GetRowSpan(y * 2 + 1);

                int baseIndex = y * width;

                for (int x = 0; x < width; x++)
                {
                    var a = row0[x * 2];
                    var b = row0[x * 2 + 1];
                    var c = row1[x * 2];
                    var d = row1[x * 2 + 1];

                    int index =
                        (a.R > 128 ? 1 : 0) |
                        (b.R > 128 ? 2 : 0) |
                        (c.R > 128 ? 4 : 0) |
                        (d.R > 128 ? 8 : 0);

                    result[baseIndex + x] = new TextyPixel(a, (byte)index);
                }
            }
        });

        return result;
    }
}