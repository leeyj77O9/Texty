
using SixLabors.ImageSharp.PixelFormats;

namespace Texty.Core.Object;

public struct TextyPixel
{
    public byte R;
    public byte G;
    public byte B;
    public byte Index;

    public TextyPixel(Rgba32 rgb, byte index) : this()
    {
        R = rgb.R;
        G = rgb.G;
        B = rgb.B;
        Index = index;
    }

    public TextyPixel(byte r, byte g, byte b, byte index) : this()
    {
        R = r;
        G = g;
        B = b;
        Index = index;
    }

    public readonly void Deconstruct(out byte r, out byte g, out byte b, out byte index)
    {
        r = R;
        g = G;
        b = B;
        index = Index;
    }
}
