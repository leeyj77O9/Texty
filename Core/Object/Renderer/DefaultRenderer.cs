using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Texty.Configuration;
using Texty.Core.Object;

namespace Texty.Renderer;

public class DefaultRenderer : IRenderer
{
    public Image<Rgba32> Render(TextyPixel[] frame, RenderContext ctx)
    {
        var (width, height) = ctx.GetSize();
        var (charWidth, charHeight) = ctx.GetCharSize();
        var (renderWidth, renderHeight) = ctx.GetRenderSize();

        var image = new Image<Rgba32>(renderWidth, renderHeight, ctx.Config.BgColor);

        var cfg = ctx.Config;
        var atlas = ctx.Atlas;
        var src = atlas.AsSpan();
        var lenRow = charWidth * cfg.CharSet.Length;
        uint byteCount = (uint)(charWidth * Config.PIXELFORMAT);

        Parallel.For(0, height, x =>
        {
            var posRow = new int[width];
            for (var i = 0; i < width; i++)
            {
                var (_, _, _, idx) = frame[x * width + i];
                posRow[i] = atlas.GetPos(idx);
            }

            image.ProcessPixelRows(accessor =>
            {
                var src = atlas.AsSpan();

                    for (int row = 0; row < charHeight; row++)
                    {
                        var dest = accessor.GetRowSpan(x * charHeight + row);

                        for (var i = 0; i < width; i++)
                        {
                            var srcStart = row * lenRow + posRow[i];
                            var srcSlice = src.Slice(srcStart, charWidth);
                            var destSlice = dest.Slice(i * charWidth, charWidth);
                            srcSlice.CopyTo(destSlice);
                        }
                    }
            });
        });

        return image;
    }
}
