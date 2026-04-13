using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.CompilerServices;
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
        var lenRow = charWidth * ctx.Config.CharSet.Length;
        uint byteCount = (uint)(charWidth * Config.PIXELFORMAT);

        Parallel.For(0, height, x =>
        {
            image.ProcessPixelRows(accessor =>
            {
                var src = ctx.Atlas.AsSpan();
                for (var i = 0; i < width; i++)
                {
                    var (r, _) = frame[x * width + i];
                    if (ctx.Atlas.GetPos(r, out var pos))
                    {
                        for (int row = 0; row < charHeight; row++)
                        {
                            var dest = accessor.GetRowSpan(x * charHeight + row);

                            Unsafe.CopyBlock(
                                ref Unsafe.As<Rgba32, byte>(ref dest[i * charWidth]),
                                ref Unsafe.As<Rgba32, byte>(ref src[row * lenRow + pos]),
                                byteCount);
                        }
                    }
                }
            });
        });
        return image;
    }
}
