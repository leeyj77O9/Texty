using SixLabors.ImageSharp;
using Texty.Core.Configuration;

namespace Texty.Core.Renderer;

public class RenderContext
{
    public TextyConfig Config { get; }
    public Atlas Atlas { get; } 

    public RenderContext(int width, int height, TextyConfig config)
    {
        Config = config with { Height = (int)(height * ((float)config.Width / width)) };
        Atlas = new Atlas(config);
    }

    public Size GetSize() => new(Config.Width, (int)(Config.Height * Config.CharRatio));

    public Size GetCharSize() => Atlas.GetCharSize();

    public Size GetRenderSize()
    {
        var (width, height) = GetSize();
        var (charWidth, charHeight) = GetCharSize();

        return new(width * charWidth, height * charHeight);
    }
}