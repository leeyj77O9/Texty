using Texty.Core.Configuration;

namespace Texty.Core.Renderer;

public static class TextyRendererProvider
{
    public static ITextyRenderer Get(TextyConfig config) => config.IsColor ? new ColorRenderer() : new DefaultTextyRenderer();
}
