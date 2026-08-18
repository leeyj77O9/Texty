using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Texty.Core.Object;

namespace Texty.Core.Renderer;

public class ColorRenderer : ITextyRenderer
{
    public Image<Rgba32> Render(TextyPixel[] frame, RenderContext ctx, CancellationToken ct = default)
    {
        var (width, height) = ctx.GetSize();
        var (charWidth, charHeight) = ctx.GetCharSize();
        var (renderWidth, renderHeight) = (width * charWidth, height * charHeight);

        var image = new Image<Rgba32>(renderWidth, renderHeight, ctx.Config.BgColor);
        int rowWidth = charWidth * ctx.Config.CharSet.Length;

        image.ProcessPixelRows(accessor =>
        {
            var src = ctx.Atlas.AsReadOnly();

            for (int x = 0; x < height; x++)
            {
                ct.ThrowIfCancellationRequested();

                int destX = x * charHeight;

                for (int y = 0; y < width; y++)
                {
                    var (r, g, b, idx) = frame[width * x + y];
                    int destY = y * charWidth;

                    if (!ctx.Atlas.GetPos(idx, out var pos))
                        continue;

                    for (int row = 0; row < charHeight; row++)
                    {

                        int offset = row * rowWidth + pos;
                        var dest = accessor.GetRowSpan(destX + row);

                        for (int i = 0; i < charWidth; i++)
                        {
                            byte intensity = src[offset + i].R;
                            byte inv = (byte)(255 - intensity);
                            var k = 255 * intensity;

                            dest[destY + i].R = (byte)((r * inv + k) >> 8);
                            dest[destY + i].G = (byte)((g * inv + k) >> 8);
                            dest[destY + i].B = (byte)((b * inv + k) >> 8);
                        }
                    }
                }
            }


        });

        return image;
    }

}
