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
    private readonly TextyConfig config;

    public TextyImage(Image<Rgba32> image, TextyConfig config)
    {
        this.image = image;
        if (config.Height < 1)
            this.config = config with { Height = (int)(image.Height * ((float)config.Width / image.Width)) };
        else 
            this.config = config;
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
            throw new InvalidOperationException("Output path is required. Please specify --output <path>");

        var mode = TextyModeProvider.Get(config.Mode);
        var renderer = TextyRendererProvider.Get(config);
        var ctx = new RenderContext(image.Width, image.Height, config);
        var ext = Path.GetExtension(config.Output).ToLowerInvariant();

        try
        {
            var pixels = await mode.TextyAsync(image, config, ct).ConfigureAwait(false);

            if (ext.Equals(".txt"))
            {
                await using var stream = new FileStream(config.Output, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 64 * 1024, useAsync: true);
                var buffer = new byte[64 * 1024];
                int count = 0, index = 0, width = config.Width;

                foreach (var pixel in pixels)
                {
                    var rune = config.Runes[pixel.Index];

                    if (count > buffer.Length - 4)
                    {
                        await stream.WriteAsync(buffer.AsMemory(0, count), ct).ConfigureAwait(false);
                        count = 0;
                    }

                    count += rune.EncodeToUtf8(buffer.AsSpan(count));

                    if (++index == width)
                    {
                        buffer[count++] = (byte)'\n';
                        index = 0;
                    }
                }

                if (count > 0)
                    await stream.WriteAsync(buffer.AsMemory(0, count), ct);
                return;
            }

            using var img = await renderer.RenderAsync(pixels, ctx, ct).ConfigureAwait(false);

            try
            {
                await img.SaveAsync(config.Output, ct).ConfigureAwait(false);
            }
            catch (UnknownImageFormatException)
            {
                var fallback = Path.ChangeExtension(config.Output, ".png");

                Console.WriteLine($"Unsupported image format. Saving as PNG instead: {fallback}");

                await img.SaveAsPngAsync(fallback, ct).ConfigureAwait(false);
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

    public static async Task<TextyImage> CreateAsync(TextyConfig config, CancellationToken ct = default)
    {
        var bytes = await TextyLoader.LoadAsync(config, ct);
        using var stream = new MemoryStream(bytes);
        var image = await Image.LoadAsync<Rgba32>(stream, ct).ConfigureAwait(false);

        if (config.Height < 1)
            return new TextyImage(image, config with { Height = (int)(image.Height * ((float)config.Width / image.Width)) });
        else
            return new TextyImage(image, config);
    }
}