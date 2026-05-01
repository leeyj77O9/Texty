using Texty.Configuration;

namespace Texty.Renderer;

public static class TextyRendererProvider
{
    public static ITextyRenderer Get(Config config) => config.IsColor ? new ColorRenderer() : new DefaultTextyRenderer();
}
