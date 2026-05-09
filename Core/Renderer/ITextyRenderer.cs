using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Texty.Core.Object;

namespace Texty.Core.Renderer;

public interface ITextyRenderer
{
    Image<Rgba32> Render(TextyPixel[] frame, RenderContext ctx);

    Task<Image<Rgba32>> RenderAsync(TextyPixel[] frame, RenderContext ctx, CancellationToken ct = default) => Task.Run(() => Render(frame, ctx), ct);
}
