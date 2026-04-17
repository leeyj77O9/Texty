using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections;
using System.Diagnostics;
using Texty.Configuration;
using Texty.Renderer;

namespace Texty;

public class TextyVideo : TextyObject, IEnumerable<string>, IAsyncEnumerable<string>
{
    private readonly IAsyncEnumerable<Image<Rgba32>> images;
    private readonly Config config;

    public TextyVideo(Config config)
    {
        var (width, height) = TextyLoader.GetResolution(config);
        this.config = config with { Height = (int)(height * ((float)config.Width / width)) };

        images = TextyLoader.ExtractFramesAsync(this.config);
    }

    public override string Texty() => throw new NotSupportedException("Use TextyAsync()");

    public override void Save()
    {
        if (string.IsNullOrEmpty(config.Output))
            throw new ArgumentException("Output path is required. Please specify --output <path>");

        var (width, height) = GetSize();
        width &= ~1;
        height &= ~1;

        IRenderer renderer = IRenderer.Get(config);
        var ctx = new RenderContext(width, height, config);
        var frameBytes = new byte[width * height * Config.PIXELFORMAT];
        using var process = CreateFFmpeg(width, height);

        process.Start();

        try
        {
            var stderrTask = process.StandardError.ReadToEndAsync();
            var stdin = process.StandardInput.BaseStream;
            foreach (var frame in images.ToBlockingEnumerable())
            {
                using (frame)
                {
                    var textyImageObj = new TextyImage(frame, config);
                    using var renderedImage = renderer.Render(textyImageObj.TextyAuto(), ctx);
                    renderedImage.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(width, height),
                        Mode = ResizeMode.Stretch,
                        Sampler = KnownResamplers.NearestNeighbor
                    }));

                    renderedImage.CopyPixelDataTo(frameBytes);
                    stdin.Write(frameBytes);
                }
            }

            stdin.Flush();
            process.StandardInput.Close();

            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception($"FFmpeg exited with code {process.ExitCode}. Error: {stderrTask.Result}");

        }
        catch (Exception ex)
        {
            process.Kill();
            Console.WriteLine($"Error during saving video: {ex.Message}");
        }
    }

    public override async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(config.Output))
            throw new ArgumentException("Output path is required. Please specify --output <path>");

        var (width, height) = GetSize();
        width &= ~1;
        height &= ~1;

        IRenderer renderer = IRenderer.Get(config);
        var ctx = new RenderContext(width, height, config);
        var frameBytes = new byte[width * height * Config.PIXELFORMAT];
        using var process = CreateFFmpeg(width, height);

        process.Start();

        try
        {
            var stderrTask = process.StandardError.ReadToEndAsync();
            var stdin = new BufferedStream(process.StandardInput.BaseStream, 1 << 20);
            await foreach (var frame in images)
            {
                using (frame)
                {
                    var textyImageObj = new TextyImage(frame, config);
                    using var renderedImage = await renderer.RenderAsync(textyImageObj.TextyAuto(), ctx);
                    renderedImage.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(width, height),
                        Mode = ResizeMode.Stretch,
                        Sampler = KnownResamplers.NearestNeighbor
                    }));

                    renderedImage.CopyPixelDataTo(frameBytes);
                    await stdin.WriteAsync(frameBytes);
                }
            }

            await stdin.FlushAsync();
            process.StandardInput.Close();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"FFmpeg exited with code {process.ExitCode}. Error: {stderrTask.Result}");

        }
        catch (Exception ex)
        {
            process.Kill();
            Console.WriteLine($"Error during saving video: {ex.Message}");
        }
    }

    private (int width, int height) GetSize() => (config.Width * config.FontSize, config.Height * config.FontSize);

    private Process CreateFFmpeg(int width, int height)
    {
        bool isGif = config.Output?.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ?? false;

        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments =
                           $"-y -f rawvideo -pixel_format rgba -video_size {width}x{height} -r {config.Fps} -i - " +
                $"{(isGif ? "-vf \"split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse=dither=none\" -f gif " :
                           $"-c:v {config.Codec} -crf {config.Crf} -preset {config.Preset} -pix_fmt yuv420p ")}" +
                           $"\"{config.Output}\"",
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public IEnumerator<string> GetEnumerator()
        => images.ToBlockingEnumerable().Select(img => new TextyImage(img, config).Texty()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => images.Select(img => new TextyImage(img, config).Texty()).GetAsyncEnumerator(cancellationToken);
}