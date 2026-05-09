using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Texty.Core.Object;

[StructLayout(LayoutKind.Explicit)]
public readonly struct TextyPixel : IEquatable<TextyPixel>
{
    [FieldOffset(0)] public readonly byte Index;
    [FieldOffset(1)] public readonly byte B;
    [FieldOffset(2)] public readonly byte G;
    [FieldOffset(3)] public readonly byte R;

    [FieldOffset(0)] public readonly uint Value;

    public TextyPixel(Rgba32 rgb, byte index) : this(rgb.R, rgb.G, rgb.B, index) { }

    public TextyPixel(Rgb24 rgb, byte index) : this(rgb.R, rgb.G, rgb.B, index) { }

    public TextyPixel(byte r, byte g, byte b, byte index)
    {       
        Unsafe.SkipInit(out this);
        R = r; G = g; B = b; Index = index;
    }

    public readonly void Deconstruct(out byte r, out byte g, out byte b, out byte index)
    {
        r = R;
        g = G;
        b = B;
        index = Index;
    }

    public bool Equals(TextyPixel other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is TextyPixel other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(TextyPixel left, TextyPixel right) => left.Value == right.Value;
    public static bool operator !=(TextyPixel left, TextyPixel right) => left.Value != right.Value;
}
