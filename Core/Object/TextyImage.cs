using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;
using Texty.Configuration;
using Texty.Core.Object;
using Texty.Renderer;

namespace Texty;

public class TextyImage : TextyObject
{
    private readonly Image<Rgba32> image;
    private Config config;

    public TextyImage(Config config)
    {
        this.image = Image.Load<Rgba32>(TextyLoader.Load(config));
        this.config = config with { Height = (int)(image.Height * ((float)config.Width / image.Width)) };
    }

    public TextyImage(Image<Rgba32> image, Config config)
    {
        this.image = image;
        this.config = config with { Height = (int)(image.Height * ((float)config.Width / image.Width)) };
    }

    public override string Texty()
    {
        var (width, height) = GetSize();
        var pixels = TextyPixel();
        var lines = new string[height];

        Parallel.For(0, height, y =>
        {
            var last = new Rgba32(0, 0, 0);
            var sb = new StringBuilder(width * (config.IsColor ? 20 : 1));
            for (var x = 0; x < width; x++)
            {
                var (r, g, b, idx) = pixels[y * width + x];
                var p = new Rgba32(r, g, b);
                var c = config.Runes[idx];

                if (config.IsColor)
                {
                    if (last == p)
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        last = p;
                        sb.Append('\x1b').Append('[').Append('3').Append('8').Append(';').Append('2').Append(';')
                          .Append(p.R).Append(';').Append(p.G).Append(';').Append(p.B).Append('m').Append(c);
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            lines[y] = sb.ToString();
        });


        if (config.IsColor)
            lines[^1] += "\x1b[0m";

        return string.Join('\n', lines);
    }

    public TextyPixel[] TextyEdge()
    {
        var (width, height) = GetSize();
        using var resized = CloneImage(width, height);
        var result = new TextyPixel[width * height];
        int threshold = config.Quality switch
        {
            TextyQuality.Small => 60,
            TextyQuality.Balanced => 40,
            TextyQuality.Fast => 25,
            _ => 30
        };

        Parallel.For(0, height, y =>
        {
            int pos = y * width;
            resized.ProcessPixelRows(accessor =>
            {
                var row = accessor.GetRowSpan(y);
                var next = y + 1 == height ? accessor.GetRowSpan(y - 1) : accessor.GetRowSpan(y + 1);

                for (int x = 0; x < width; x++)
                {
                    var p = row[x];
                    var right = x + 1 == width ? row[x - 1] : row[x + 1];
                    var down = next[x];

                    int gray = (p.R * 77 + p.G * 150 + p.B * 29) >> 8;
                    int grayR = (right.R * 77 + right.G * 150 + right.B * 29) >> 8;
                    int grayD = (down.R * 77 + down.G * 150 + down.B * 29) >> 8;

                    int dx = gray - grayR;
                    if (dx < 0) dx = -dx;

                    int dy = gray - grayD;
                    if (dy < 0) dy = -dy;

                    int edge = dx > dy ? dx : dy;

                    result[pos + x] = new(p.R, p.G, p.B, (byte)(edge < threshold ? config.Runes.Length - 1 : 0));
                }

            });
        });

        return result;
    }

    public TextyPixel[] TextyPixel()
    {
        var (width, height) = GetSize();
        using var resized = CloneImage(width, height);
        var result = new TextyPixel[width * height];

        Parallel.For(0, height, x =>
        {
            resized.ProcessPixelRows(accessor =>
            {
                var row = accessor.GetRowSpan(x);
                int start = x * width;

                for (int y = 0; y < width; y++)
                {
                    var p = row[y];
                    int gray = (p.R * 77 + p.G * 150 + p.B * 29) >> 8;
                    int index = (gray * (config.CharSet.Length - 1)) >> 8;

                    result[start + y] = new(p, (byte)index);
                }
            });
        });

        return result;
    }

    public TextyPixel[] TextyAuto() => config.Mode switch
    {
        TextyMode.Edge => TextyEdge(),
        _ => TextyPixel(),
    };

    public override void Save()
    {
        if (string.IsNullOrEmpty(config.Output))
            throw new ArgumentException("Output path is required. Please specify --output <path>");

        var renderer = IRenderer.Get(config);
        var ctx = new RenderContext(this.image.Width, this.image.Height, config);
        var image = renderer.Render(TextyAuto(), ctx);

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

        var renderer = IRenderer.Get(config);
        var ctx = new RenderContext(this.image.Width, this.image.Height, config);
        var image = await renderer.RenderAsync(TextyAuto(), ctx);

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

    private (int width, int height) GetSize() => (config.Width, (int)(config.Height * Config.CHARRATIO));

    private Image<Rgba32> CloneImage(int width, int height)
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

    public override void Dispose()
    {
        image.Dispose();
        GC.SuppressFinalize(this);
    }
}