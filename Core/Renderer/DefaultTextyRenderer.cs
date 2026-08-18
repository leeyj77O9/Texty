using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Texty.Core.Configuration;
using Texty.Core.Object;

namespace Texty.Core.Renderer;

public class DefaultTextyRenderer : ITextyRenderer
{
    public Image<Rgba32> Render(TextyPixel[] frame, RenderContext ctx)
    {
        var (width, height) = ctx.GetSize();
        var (charWidth, charHeight) = ctx.GetCharSize();
        var (renderWidth, renderHeight) = ctx.GetRenderSize();

        var image = new Image<Rgba32>(renderWidth, renderHeight, ctx.Config.BgColor);

        var atlas = ctx.Atlas;
        var src = atlas.AsSpan();
        var lenRow = charWidth * ctx.Config.CharSet.Length;
        uint byteCount = (uint)(charWidth * Config.PIXELFORMAT);

        image.ProcessPixelRows(accessor =>
        {
            var src = atlas.AsSpan();

            for (int x = 0; x < height; x++)
            {
                var posRow = new int[width];

                for (int i = 0; i < width; i++)
                {
                    var (_, _, _, idx) = frame[x * width + i];
                    posRow[i] = ctx.Atlas.GetPos(idx, out var pos) ? pos : 0;
                }

                for (int row = 0; row < charHeight; row++)
                {
                    var dest = accessor.GetRowSpan(x * charHeight + row);

                    for (int i = 0; i < width; i++)
                    {
                        var srcStart = row * lenRow + posRow[i];

                        var srcSlice = src.Slice(srcStart, charWidth);
                        var destSlice = dest.Slice(i * charWidth, charWidth);

                        srcSlice.CopyTo(destSlice);
                    }
                }
            }
        });

        return image;
    }
}