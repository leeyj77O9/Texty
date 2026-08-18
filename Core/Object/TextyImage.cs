using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using Texty.Core.Configuration;
using Texty.Core.Mode;
using Texty.Core.Renderer;
using Texty.Core.Util;

namespace Texty.Core.Object;

public class TextyImage : TextyObject
{
    private readonly Image<Rgba32> image;
    private readonly Config config;

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

    public override string Texty() => Texty(TextyModeProvider.Get(config.Mode));

    public string Texty(TextyMode mode) => Texty(TextyModeProvider.Get(mode));

    public string Texty(ITextyMode mode)
    {
        var (width, height) = (config.Width, (int)(config.Height * config.CharRatio));
        var pixels = mode.Texty(image, config);
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

                if (config.IsColor && last != p)
                {
                    last = p;
                    sb.Append('\x1b').Append('[').Append('3').Append('8').Append(';').Append('2').Append(';')
                      .Append(p.R).Append(';').Append(p.G).Append(';').Append(p.B).Append('m');
                }
                sb.Append(c);
            }
            lines[y] = sb.ToString();
        });


        if (config.IsColor)
            lines[^1] += "\x1b[0m";

        return string.Join('\n', lines);
    }

    public override void Save() => SaveAsync().GetAwaiter().GetResult();

    public override async Task SaveAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(config.Output))
            throw new ArgumentException("Output path is required. Please specify --output <path>");

        var mode = TextyModeProvider.Get(config.Mode);
        var renderer = TextyRendererProvider.Get(config);
        var ctx = new RenderContext(image.Width, image.Height, config);

        try
        {
            var pixels = await mode.TextyAsync(image, config, ct).ConfigureAwait(false);
            using var img = await renderer.RenderAsync(pixels, ctx, ct).ConfigureAwait(false);

            try
            {
                await img.SaveAsync(config.Output, ct).ConfigureAwait(false);
            }
            catch (UnknownImageFormatException)
            {
                Console.WriteLine("Unsupported image format. Saving as PNG instead.");

                await img.SaveAsPngAsync(Path.ChangeExtension(config.Output, ".png"), ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.WriteLine("Operation canceled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save image: {ex.Message}");
            throw;
        }
    }

    public override void Dispose()
    {
        image.Dispose();
        GC.SuppressFinalize(this);
    }
}