using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Texty.Core.Configuration;
using Texty.Core.Object;

namespace Texty.Core.Mode;

public class DitherMode : ITextyMode
{
    public TextyPixel[] Texty(Image<Rgba32> image, TextyConfig config)
    {
        var (width, height) = config.GetRenderSize();
        using var resized = ITextyMode.Clone(image, width, height, config);

        var result = new TextyPixel[width * height];
        var buffer = new float[width * height];

        resized.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    var p = row[x];
                    buffer[y * width + x] = (p.R * 77 + p.G * 150 + p.B * 29) / 256f;
                }
            }
        });

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                float oldPixel = buffer[i];

                int index = (int)(oldPixel * (config.CharSet.Length - 1) / 255f);
                index = Math.Clamp(index, 0, config.CharSet.Length - 1);

                float newPixel = index * (255f / (config.CharSet.Length - 1));
                float error = oldPixel - newPixel;

                result[i] = new TextyPixel((byte)newPixel, (byte)newPixel, (byte)newPixel, (byte)index);

                void Add(int dx, int dy, float factor)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        buffer[ny * width + nx] += error * factor;
                }

                Add(1, 0, 7f / 16f);
                Add(-1, 1, 3f / 16f);
                Add(0, 1, 5f / 16f);
                Add(1, 1, 1f / 16f);
            }
        }

        return result;
    }
}