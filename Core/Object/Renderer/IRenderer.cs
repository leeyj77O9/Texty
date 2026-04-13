using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Texty.Configuration;
using Texty.Core.Object;

namespace Texty.Renderer;

internal interface IRenderer
{
    Image<Rgba32> Render(TextyPixel[] frame, RenderContext ctx);

    Task<Image<Rgba32>> RenderAsync(TextyPixel[] frame, RenderContext ctx) => Task.Run(() => Render(frame, ctx));

    public static IRenderer Get(Config config) => config.IsColor ? new ColorRenderer() : new DefaultRenderer();

}
