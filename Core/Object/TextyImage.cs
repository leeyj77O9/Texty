using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace Texty;

public class TextyImage : TextyObject
{
    private readonly Image<Rgb24> image;
    private Config config;

    public override Config Config => config;

    public int CharWidth { get; set; }
    public int CharHeight { get; set; }

    private Rgb24? Last { get; set; } = null;
    private readonly Font font;
    private Rgb24[]? Atlas;
    private Dictionary<Rune, int>? CharPos;

    public TextyImage(Config config)
    {     
        this.image = Image.Load<Rgb24>(TextyLoader.Load(config));      
        this.config = config with { Height = (int)(image.Height * ((float)config.Width / image.Width)) };
        font = SystemFonts.CreateFont(config.FontName, config.FontSize);

        var options = new TextOptions(font);
        foreach (var c in Config.CharSet)
        {
            var rect = TextMeasurer.MeasureBounds(c.ToString(), options);

            CharWidth = Math.Max(CharWidth, (int)Math.Ceiling(rect.Width));
            CharHeight = Math.Max(CharHeight, (int)Math.Ceiling(rect.Height));
        }
        BuildAtlas();
    }

    public TextyImage(Image<Rgb24> image, Config config)
    {
        this.image = image;
        this.config = config with { Height = (int)(image.Height * ((float)config.Width / image.Width)) };
        font = SystemFonts.CreateFont(config.FontName, config.FontSize);

        var options = new TextOptions(font);
        foreach (var c in Config.CharSet)
        {
            var rect = TextMeasurer.MeasureBounds(c.ToString(), options);

            CharWidth = Math.Max(CharWidth, (int)Math.Ceiling(rect.Width));
            CharHeight = Math.Max(CharHeight, (int)Math.Ceiling(rect.Height));
        }

        BuildAtlas();
    }

    public override string Texty()
    {
        var (width, height) = GetSize();
        using var resized = CloneImage(width, height);       
        var lines = new string[height];

        Parallel.For(0, height, x =>
        {
            resized.ProcessPixelRows(accessor =>
            {
                var row = accessor.GetRowSpan(x);
                var sb = new StringBuilder(width * (config.Color ? 20 : 1));
                for (int y = 0; y < width; y++)
                {
                    var p = row[y];
                    AddChar(sb, GetCharFromPixel(p), p);
                }
                lines[x] = sb.ToString();
            });
        });


        if (config.Color)
            lines[^1] += "\x1b[0m";

        return string.Join('\n', lines);
    }

    public override async IAsyncEnumerable<string> TextyAsync()
    {
        yield return Texty();
    }


    public (Rune r, Rgb24 rgb)[] TextyANSI()
    {
        var (width, height) = GetSize();
        using var resized = CloneImage(width, height);

        var result = new (Rune, Rgb24)[width * height];

        Parallel.For(0, height, x =>
        {
            resized.ProcessPixelRows(accessor =>
            {
                var row = accessor.GetRowSpan(x);
                int start = x * width;

                for (int y = 0; y < width; y++)
                {
                    var p = row[y];
                    var c = GetCharFromPixel(p);

                    result[start + y] = (c, p);
                }      
            });
        });

        return result;
    }

    public async IAsyncEnumerable<(Rune r, Rgb24 rgb)[]> TextyANSIAsync()
    {
        yield return TextyANSI();
    }



    public override void Save()
    {
        if (string.IsNullOrEmpty(config.Output))
            throw new ArgumentException("Output path is required. Please specify --output <path>");

        var image = config.Color ? RenderANSI() : Render();

        try
        {
            image.Save(config.Output);
        }
        catch (UnknownImageFormatException)
        {
            Console.WriteLine("Unsupported image format. Saving as PNG instead.");
            image.SaveAsPng(Path.ChangeExtension(config.Output, ".png"));
        }
    }

    public override async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(config.Output))
            throw new ArgumentException("Output path is required. Please specify --output <path>");
        
        var image = await (config.Color ? RenderANSIAsync() : RenderAsync());

        try
        {
            await image.SaveAsync(config.Output);
        }
        catch (UnknownImageFormatException)
        {
            Console.WriteLine("Unsupported image format. Saving as PNG instead.");
            await image.SaveAsPngAsync(Path.ChangeExtension(config.Output, ".png"));
        }
    }



    public Image<Rgb24> Render()
    {
        var beforeConfig = config;
        config = beforeConfig with { Color = false };

        try
        {
            var (width, height) = GetRenderSize();
            var image = new Image<Rgb24>(width, height, Color.White);
            var lines = Texty().Split('\n');

            image.ProcessPixelRows(ctx =>
            {
                int y = 0, lenRow = CharWidth * config.CharSet.Length;

                foreach (var line in lines)
                {
                    int x = 0;

                    foreach (var c in line)
                    {
                        var r = new Rune(c);
                        if (CharPos.TryGetValue(r, out var pos))
                        {
                            int destX = x * CharWidth;
                            int destY = y * CharHeight;

                            for (int row = 0; row < CharHeight; row++)
                            {
                                var srcRow = Atlas.AsSpan(row * lenRow).Slice(pos, CharWidth);
                                var destRow = ctx.GetRowSpan(destY + row).Slice(destX, CharWidth);

                                srcRow.CopyTo(destRow);
                            }
                        }
                        x++;
                    }
                    y++;
                }

            });

            return image;
        }
        finally
        {
            config = beforeConfig;
        }
    }
    public Task<Image<Rgb24>> RenderAsync() => Task.Run(() => Render());



    public Image<Rgb24> RenderANSI()
    {
        var (width, height) = GetRenderSize();
        var size = GetSize();
        var image = new Image<Rgb24>(width, height, Color.White);
        var ansis = TextyANSI();

        Parallel.For(0, size.height, x =>
        {
            int destX = x * CharHeight, lenRow = CharWidth * config.CharSet.Length; ;

            image.ProcessPixelRows(ctx =>
            {
                for (int y = 0; y < size.width; y++)
                {
                    var (c, rgb) = ansis[size.width * x + y];
                    if (!CharPos.TryGetValue(c, out var pos))
                        continue;

                    int destY = y * CharWidth;

                    for (int row = 0; row < CharHeight; row++)
                    {
                        var srcRow = Atlas.AsSpan(row * lenRow).Slice(pos, CharWidth);
                        var destRow = ctx.GetRowSpan(destX + row).Slice(destY, CharWidth);

                        for (int i = 0; i < CharWidth; i++)
                            BlurColor(srcRow[i], ref destRow[i], rgb);
                    }
                }

            });
        });        

        return image;
    }

    public Task<Image<Rgb24>> RenderANSIAsync() => Task.Run(() => RenderANSI());



    private Rune GetCharFromPixel(Rgb24 p)
    {
        var chars = config.CharSet;
        int gray = (p.R * 77 + p.G * 150 + p.B * 29) >> 8;
        if (config.Invert) gray = 255 - gray;

        int index = (gray * (chars.Length - 1)) >> 8;
        return new Rune(chars[index]);
    }

    private void BlurColor(Rgb24 src, ref Rgb24 dst, Rgb24 rgb)
    {
        byte intensity = src.R;
        byte inv = (byte)(255 - intensity);

        dst.R = (byte)((rgb.R * inv + 255 * intensity) >> 8);
        dst.G = (byte)((rgb.G * inv + 255 * intensity) >> 8);
        dst.B = (byte)((rgb.B * inv + 255 * intensity) >> 8);
    }

    private void AddChar(StringBuilder sb, Rune c, Rgb24 p)
    {
        if (config.Color)
        {
            if (Last.HasValue && Last.Value.Equals(p))
            {
                sb.Append(c);
            }         
            else
            {
                Last = p;
                sb.Append('\x1b').Append('[').Append('3').Append('8').Append(';').Append('2').Append(';')
                  .Append(p.R).Append(';').Append(p.G).Append(';').Append(p.B).Append('m').Append(c);
            }
        }
        else
        {
            sb.Append(c);
        }
    }



    private (int width, int height) GetRenderSize()
    {
        var (width, height) = GetSize();
        return (width * CharWidth, height * CharHeight);
    }

    private (int width, int height) GetSize() => (config.Width, (int)(config.Height * 0.54f));



    private Image<Rgb24> CloneImage(int width, int height)
    {
        if (width == image.Width && height == image.Height)
            return image.Clone();

        return image.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Stretch,
            Sampler = KnownResamplers.NearestNeighbor,
        }));
    }

    private void BuildAtlas()
    {
        var (width, height) = (CharWidth * config.CharSet.Length, CharHeight);
        Atlas = new Rgb24[width * height];
        CharPos = [];

        var options = new TextOptions(font);
        var atlas = new Image<Rgb24>(width, height, Color.White);

        atlas.Mutate(ctx =>
        {
            for (int i = 0; i < config.CharSet.Length; i++)
            {
                var text = config.CharSet[i].ToString();
                var rect = TextMeasurer.MeasureBounds(text, options);

                float x = i * CharWidth + (CharWidth - rect.Width) / 2 - rect.Left;
                float y = (CharHeight - rect.Height) / 2 - rect.Top;

                ctx.DrawText(text, font, Color.Black, new PointF(x, y));
                CharPos.TryAdd(new Rune(config.CharSet[i]), i * CharWidth);
            }
        });

        atlas.ProcessPixelRows(ctx =>
        {
            for (int i = 0; i < ctx.Height; i++)
            {
                var atlasRow = Atlas.AsSpan(i * CharWidth * config.CharSet.Length);
                ctx.GetRowSpan(i).CopyTo(atlasRow);
            }
        });
    }

    public override void Dispose()
    {
        image.Dispose();
        GC.SuppressFinalize(this);
    }
}
