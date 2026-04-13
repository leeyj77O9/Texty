using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;
using Texty.Configuration;

namespace Texty.Core.Object.Renderer;

public class Atlas
{
    public static Atlas Empty => new();

    public string CharSet { get; private set; }
    public int CharWidth { get; private set; }
    public int CharHeight { get; private set; }
    public Font Font { get; private set; }
    public Color FontColor { get; private set; }
    public Dictionary<Rune, FontRectangle> CharBounds { get; private set; }

    private Rgba32[]? rgbas;
    private Dictionary<Rune, int>? charPos;
    private int[]? fastCharPos;

    private Atlas()
    {
        CharSet = "";
        Font = null!;
        CharBounds = null!;
    }

    public Atlas(Config config)
    {
        CharSet = config.CharSet;
        Font = SystemFonts.CreateFont(config.FontName, config.FontSize, config.FontStyle);
        FontColor = config.FontColor;
        CharBounds = [];

        Update(config);
    }

    public bool GetPos(char c, out int pos)
    {
        if (fastCharPos is null)
        {
            pos = -1;
            return false;
        }
        pos = fastCharPos[c];
        return pos >= 0;
    }

    public bool GetPos(Rune r, out int pos)
    {
        int val = r.Value;

        if (val < 65536 && fastCharPos != null)
        {
            pos = fastCharPos[val];
            return pos >= 0;
        }

        if (charPos != null && charPos.TryGetValue(r, out pos))
            return true;

        pos = 0;
        return false;
    }

    public void Update(Config config)
    {        
        var options = new TextOptions(Font);

        foreach (var c in config.CharSet.EnumerateRunes())
            CharBounds.TryAdd(c, TextMeasurer.MeasureBounds(c.ToString(), options));

        CharWidth = CharBounds.Max(x => (int)Math.Ceiling(x.Value.Width));
        CharHeight = CharBounds.Max(x => (int)Math.Ceiling(x.Value.Height));

        BuildAtlas();
    }

    public bool IsUpdated(Config config)
    {
        if (config.CharSet != CharSet) return true;
        if (config.FontName != Font.Name) return true;
        if (config.FontSize != Font.Size) return true;

        return false;
    }

    public Size GetCharSize() => new(CharWidth, CharHeight);

    private void BuildAtlas()
    {
        var (width, height) = (CharWidth * CharSet.Length, CharHeight);
        rgbas = new Rgba32[width * height];
        fastCharPos = new int[65536];
        Array.Fill(fastCharPos, -1);
        charPos = [];

        var atlas = new Image<Rgba32>(width, height, Color.White);

        atlas.Mutate(ctx =>
        {
            int i = 0; 
            var runes = CharSet.EnumerateRunes();

            foreach (var rune in runes)
            {
                var rect = CharBounds[rune];

                var x = i * CharWidth + (CharWidth - rect.Width) / 2 - rect.Left;
                var y = (CharHeight - rect.Height) / 2 - rect.Top;

                ctx.DrawText(rune.ToString(), Font, FontColor, new PointF(x, y));

                int pos = i * CharWidth;

                if (rune.Value < 65536)
                    fastCharPos[rune.Value] = pos;
                else
                    charPos[rune] = pos;
                i++;
            }

        });

        atlas.ProcessPixelRows(ctx =>
        {
            int width = CharWidth * CharSet.Length;

            for (int y = 0; y < height; y++)
            {
                var srcRow = ctx.GetRowSpan(y);
                var destRow = rgbas.AsSpan(y * width);

                srcRow.CopyTo(destRow);
            }
        });
    }

    public Span<Rgba32> AsSpan() => rgbas.AsSpan();

    public Span<Rgba32> AsSpan(int start) => rgbas.AsSpan(start);

    public Span<Rgba32> AsSpan(int start, int length) => rgbas.AsSpan(start, length);

    public ReadOnlySpan<Rgba32> AsReadOnly() => rgbas.AsSpan();

    public ReadOnlySpan<Rgba32> AsReadOnly(int start) => rgbas.AsSpan(start);

    public ReadOnlySpan<Rgba32> AsReadOnly(int start, int length) => rgbas.AsSpan(start, length);
}
