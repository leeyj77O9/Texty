using SixLabors.ImageSharp.PixelFormats;
using System.Text;

namespace Texty.Core.Object;

public struct TextyPixel
{
    public Rune Rune;
    public Rgba32 Rgba32;

    public TextyPixel(Rune c, Rgba32 p) : this()
    {
        Rune = c;
        Rgba32 = p;
    }

    public readonly void Deconstruct(out Rune c, out Rgba32 rgb)
    {
        c = Rune;
        rgb = Rgba32;
    }
}
