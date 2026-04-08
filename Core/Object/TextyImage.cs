using System.Text;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Texty;

public class TextyImage : TextyObject
{
    private readonly Image<Rgba32> image;
    private Config config;

    public override Config Config => config;

    public float CharWidth { get; set; }
    public float CharHeight { get; set; }

    public Font Font => SystemFonts.CreateFont(config.FontName, config.FontSize);

    public TextyImage(Config config)
    {
        this.config = config;       
        image = Image.Load<Rgba32>(TextyLoader.Load(config));      
        this.config.Height = (int)(image.Height * ((float)config.Width / image.Width));

        var options = new TextOptions(Font);
        var size = TextMeasurer.MeasureSize("M", options);

        CharWidth = size.Width;
        CharHeight = size.Height;
    }

    public TextyImage(Image<Rgba32> image, Config config)
    {
        this.image = image;
        this.config = config;
        this.config.Height = (int)(image.Height * ((float)config.Width / image.Width));

        var options = new TextOptions(Font);
        var size = TextMeasurer.MeasureSize("M", options);

        CharWidth = size.Width;
        CharHeight = size.Height;
    }

    public override string Texty()
    {
        var sb = new StringBuilder(config.Height * (config.Width + 1));
        var (width, height) = (config.Width, config.Height);
        using var resized = CloneImage(width, height);

        resized.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);

                for (int x = 0; x < width; x++)
                {
                    var p = row[x];
                    char c = GetCharFromPixel(p, config.CharSet);

                    if (config.Color)
                    {
                        if (config.Background)
                        {
                            sb.Append("\x1b[48;2;")
                              .Append(p.R).Append(';')
                              .Append(p.G).Append(';')
                              .Append(p.B).Append('m')
                              .Append(' '); 
                        }
                        else
                        {
                            sb.Append("\x1b[38;2;")
                              .Append(p.R).Append(';')
                              .Append(p.G).Append(';')
                              .Append(p.B).Append('m')
                              .Append(c);
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                sb.Append('\n');
            }
        });

        if (config.Color)
            sb.Append("\x1b[0m");


        return sb.ToString();
    }

    public override async IAsyncEnumerable<string> TextyAsync()
    {
        yield return Texty();
    }

    public IEnumerable<string> TextyLine()
    {
        var sb = new StringBuilder(config.Height * (config.Width + 1));
        var (width, height) = (config.Width, config.Height);
        using var resized = CloneImage(width, height);

        for (int y = 0; y < config.Height; y++)
        {
            for (int x = 0; x < config.Width; x++)
            {
                var p = resized[x, y];
                char c = GetCharFromPixel(p, config.CharSet);

                if (config.Color)
                {
                    if (config.Background)
                    {
                        sb.Append("\x1b[48;2;")
                          .Append(p.R).Append(';')
                          .Append(p.G).Append(';')
                          .Append(p.B).Append('m')
                          .Append(' ');
                    }
                    else
                    {
                        sb.Append("\x1b[38;2;")
                          .Append(p.R).Append(';')
                          .Append(p.G).Append(';')
                          .Append(p.B).Append('m')
                          .Append(c);
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            sb.Append('\n');
            yield return sb.ToString();
            sb.Clear();
        }

        if (config.Color)
            sb.Append("\x1b[0m");


        yield return sb.ToString();
    }

    public async IAsyncEnumerable<string> TextyLineAsync()
    {
        var sb = new StringBuilder(config.Height * (config.Width + 1));
        var (width, height) = (config.Width, config.Height);
        using var resized = CloneImage(width, height);

        for (int y = 0; y < config.Height; y++)
        {
            for (int x = 0; x < config.Width; x++)
            {
                var p = resized[x, y];
                char c = GetCharFromPixel(p, config.CharSet);

                if (config.Color)
                {
                    if (config.Background)
                    {
                        sb.Append("\x1b[48;2;")
                          .Append(p.R).Append(';')
                          .Append(p.G).Append(';')
                          .Append(p.B).Append('m')
                          .Append(' ');
                    }
                    else
                    {
                        sb.Append("\x1b[38;2;")
                          .Append(p.R).Append(';')
                          .Append(p.G).Append(';')
                          .Append(p.B).Append('m')
                          .Append(c);
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            sb.Append('\n');
            yield return sb.ToString();
            sb.Clear();
        }

        if (config.Color)
            sb.Append("\x1b[0m");


        yield return sb.ToString();
    }

    public IEnumerable<(char c, byte r, byte g, byte b, bool bg)> TextyANSI()
    {
        var (width, height) = (config.Width, config.Height);
        using var resized = CloneImage(width, height);

        for (int y = 0; y < config.Height; y++)
        {
            for (int x = 0; x < config.Width; x++)
            {
                var p = resized[x, y];
                char c = GetCharFromPixel(p, config.CharSet);

                if (config.Background)
                    yield return (' ', p.R, p.G, p.B, true);
                else
                    yield return (c, p.R, p.G, p.B, false);

            }

            yield return ('\n', 0, 0, 0, false);
        }        
    }

    public async IAsyncEnumerable<(char c, byte r, byte g, byte b, bool bg)> TextyANSIAsync()
    {
        var (width, height) = (config.Width, config.Height);
        using var resized = CloneImage(width, height);

        for (int y = 0; y < config.Height; y++)
        {
            for (int x = 0; x < config.Width; x++)
            {
                var p = resized[x, y];
                char c = GetCharFromPixel(p, config.CharSet);

                if (config.Background)
                    yield return (' ', p.R, p.G, p.B, true);
                else
                    yield return (c, p.R, p.G, p.B, false);

            }

            yield return ('\n', 0, 0, 0, false);
        }
    }

    public override void Save()
    {
        if (string.IsNullOrEmpty(config.Output))
            throw new ArgumentException("Output path is required. Please specify --output <path>");

        var image = Render();

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

    public Image<Rgba32> Render()
    {
        var beforeConfig = config;
        config = new Config(beforeConfig) { Color = false };

        try
        {
            var (width, height) = GetSize();
            var image = new Image<Rgba32>(width, height, Color.White);

            image.Mutate(ctx =>
            {
                int y = 0;
                foreach (var line in TextyLine())
                {
                    ctx.DrawText(line, Font, Color.Black, new PointF(0, y * config.FontSize * CharHeight));
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

    public async Task<Image<Rgba32>> RenderAsync()
    {
        return await Task.Run(() =>
        {
            var beforeConfig = config;
            config = new Config(beforeConfig) { Color = false };

            try
            {
                var (width, height) = GetSize();
                var image = new Image<Rgba32>(width, height, Color.White);

                image.Mutate(ctx =>
                {
                    int y = 0;
                    foreach (var line in TextyLine())
                    {
                        ctx.DrawText(line, Font, Color.Black, new PointF(0, y * CharHeight));
                        y++;
                    }
                });

                return image;
            }
            finally
            {
                config = beforeConfig;
            }
        });
    }

    public Image<Rgba32> RenderANSI()
    {
        var (width, height) = GetSize();
        var image = new Image<Rgba32>(width, height, Color.White);

        image.Mutate(ctx =>
        {
            float x = 0, y = 0;

            var drawingOptions = new DrawingOptions
            {
                GraphicsOptions = new GraphicsOptions
                {
                    Antialias = false,
                    AntialiasSubpixelDepth = 0
                }
            };

            var sb = new StringBuilder();
            Rgba32 lastColor = default;
            PointF startPoint = new(0, 0);

            foreach (var (c, r, g, b, bg) in TextyANSI())
            {
                if (c == '\n')
                {
                    FlushBuffer(lastColor);
                    y += CharHeight;
                    x = 0;
                    continue;
                }

                var currentColor = new Rgba32(r, g, b, 255);

                if (sb.Length > 0 && currentColor != lastColor)
                {
                    FlushBuffer(lastColor);
                }

                if (sb.Length == 0)
                {
                    startPoint = new PointF(x * CharWidth, y);
                    lastColor = currentColor;
                }

                sb.Append(c);
                x += 1;
            }

            FlushBuffer(lastColor);

            void FlushBuffer(Rgba32 color)
            {
                if (sb.Length > 0)
                {
                    ctx.DrawText(drawingOptions, sb.ToString(), Font, color, startPoint);
                    sb.Clear();
                }
            }
        });

        return image;
    }

    public async Task<Image<Rgba32>> RenderANSIAsync()
    {
        return await Task.Run(() =>
        {
            var (width, height) = GetSize();
            var image = new Image<Rgba32>(width, height, Color.White);

            image.Mutate(ctx =>
            {
                float x = 0, y = 0;

                var drawingOptions = new DrawingOptions
                {
                    GraphicsOptions = new GraphicsOptions 
                    {
                        Antialias = false,
                        AntialiasSubpixelDepth = 0 
                    }
                };

                var sb = new StringBuilder();
                Rgba32 lastColor = default;
                PointF startPoint = new(0, 0);               

                foreach (var (c, r, g, b, bg) in TextyANSI())
                {
                    if (c == '\n')
                    {
                        FlushBuffer(lastColor);
                        y += CharHeight;
                        x = 0;
                        continue;
                    }

                    var currentColor = new Rgba32(r, g, b, 255);

                    if (sb.Length > 0 && currentColor != lastColor)
                    {
                        FlushBuffer(lastColor);
                    }

                    if (sb.Length == 0)
                    {
                        startPoint = new PointF(x * CharWidth, y);
                        lastColor = currentColor;
                    }

                    sb.Append(c);
                    x += 1;
                }

                FlushBuffer(lastColor);

                void FlushBuffer(Rgba32 color)
                {
                    if (sb.Length > 0)
                    {
                        ctx.DrawText(drawingOptions, sb.ToString(), Font, color, startPoint);
                        sb.Clear();
                    }
                }
            });

            return image;
        });

    }

    private char GetCharFromPixel(Rgba32 p, string chars)
    {
        const double rW = 0.299 / 255.0;
        const double gW = 0.587 / 255.0;
        const double bW = 0.114 / 255.0;

        double gray = (p.R * rW) + (p.G * gW) + (p.B * bW);
        if (config.Invert) gray = 1.0 - gray;

        int index = Math.Clamp((int)(gray * (chars.Length - 1)), 0, chars.Length - 1);
        return chars[index];
    }

    private (int width, int height) GetSize() => ((int)(config.Width * CharWidth), (int)(config.Height * CharHeight));

    private Image<Rgba32> CloneImage(int width, int height) => image.Clone(ctx => ctx.Resize(new ResizeOptions
    {
        Size = new Size(width, height),
        Sampler = KnownResamplers.NearestNeighbor
    }));

    public override void Dispose()
    {
        image.Dispose();
        GC.SuppressFinalize(this);
    }
}
