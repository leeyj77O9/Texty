using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;
using Texty.Core.Configuration;

namespace Texty.Core.Renderer;

public class Atlas
{
    public static Atlas Empty => new();

    public int CharWidth { get; private set; }
    public int CharHeight { get; private set; }
    public Font Font { get; private set; }
    public Config Config { get; private set; }
    public Dictionary<Rune, FontRectangle> CharBounds { get; private set; }

    private Rgba32[]? rgbas;
    private int[]? charPos;

    private Atlas()
    {
        Font = null!;
        Config = null!;
        CharBounds = null!;
    }

    public Atlas(Config config)
    {
        Font = SystemFonts.CreateFont(config.FontName, config.FontSize, config.FontStyle);
        CharBounds = [];
        Config = config;

        Update();
    }

    public bool GetPos(byte idx, out int pos)
    {
        if (charPos == null)
        {
            pos = -1;
            return false;
        }

        pos = charPos[idx];
        return true;
    }

    public void Update()
    {        
        var options = new TextOptions(Font);

        foreach (var c in Config.Runes)
            CharBounds.TryAdd(c, TextMeasurer.MeasureBounds(c.ToString(), options));

        CharWidth = CharBounds.Max(x => (int)Math.Ceiling(x.Value.Width));
        CharHeight = CharBounds.Max(x => (int)Math.Ceiling(x.Value.Height));

        BuildAtlas();
    }

    public bool IsUpdated(Config config)
    {
        if (config.CharSet != Config.CharSet) return true;
        if (config.FontName != Font.Name) return true;
        if (config.FontSize != Font.Size) return true;

        return false;
    }

    public Size GetCharSize() => new(CharWidth, CharHeight);

    private void BuildAtlas()
    {
        var (width, height) = (CharWidth * Config.CharSet.Length, CharHeight);
        rgbas = new Rgba32[width * height];
        charPos = new int[255];
        Array.Fill(charPos, -1);

        var atlas = new Image<Rgba32>(width, height, Config.BgColor);

        atlas.Mutate(ctx =>
        {
            int i = 0; 

            foreach (var rune in Config.Runes)
            {
                var rect = CharBounds[rune];

                var x = i * CharWidth + (CharWidth - rect.Width) / 2 - rect.Left;
                var y = (CharHeight - rect.Height) / 2 - rect.Top;

                ctx.DrawText(rune.ToString(), Font, Config.FontColor, new PointF(x, y));

                int pos = i * CharWidth;
                charPos[i++] = pos;
            }

        });

        atlas.ProcessPixelRows(ctx =>
        {
            int width = CharWidth * Config.CharSet.Length;

            for (int y = 0; y < height; y++)
            {
                var srcRow = ctx.GetRowSpan(y);
                var destRow = rgbas.AsSpan(y * width);

                srcRow.CopyTo(destRow);
            }
        });
    }

    public Span<Rgba32> AsSpan() => rgbas.AsSpan();

    public ReadOnlySpan<Rgba32> AsReadOnly() => rgbas.AsSpan();

}
