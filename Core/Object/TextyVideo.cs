using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections;
using Texty.Core.Configuration;
using Texty.Core.Mode;
using Texty.Core.Renderer;
using Texty.Core.Util;

namespace Texty.Core.Object;

public class TextyVideo : TextyObject, IEnumerable<string>, IAsyncEnumerable<string>
{
    private readonly IAsyncEnumerable<Image<Rgba32>> images;
    private readonly Config config;
    private readonly string? FilePath;
    private readonly double? Duration;

    public TextyVideo(Config config)
    {
        if (config.IsUrl)
        {
            FilePath = TextyLoader.DownloadFile(config).Result;
            config = config with { Input = FilePath };
        }

        var (width, height, duration) = TextyLoader.GetVideoInfo(config);
        this.config = config with { Height = (int)(height * ((float)config.Width / width)) };

        if (!string.IsNullOrEmpty(config.Duration))
        {
            Duration = TimeSpan.Parse(config.Duration).TotalSeconds;
        }
        else if (!string.IsNullOrEmpty(config.EndTime) && !string.IsNullOrEmpty(config.StartTime))
        {
            var end = TimeSpan.Parse(config.EndTime);
            var start = TimeSpan.Parse(config.StartTime);
            Duration = (end - start).TotalSeconds;
        }
        else Duration = duration;

        images = TextyLoader.ExtractFramesAsync(this.config);
    }

    public override string Texty() => throw new NotSupportedException("Use IAsyncEnumerable<string> to iterate over frames");

    public override void Save() => SaveAsync().GetAwaiter().GetResult();

    public override async Task SaveAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(config.Output))
            throw new ArgumentException("Output path is required. Please specify --output <path>");

        var (width, height) = (config.Width * config.FontSize, config.Height * config.FontSize);
        width &= ~1;
        height &= ~1;

        if (height == 0)
            throw new ArgumentException("Height cannot be zero. Please specify a valid the width.");

        var tm = TextyModeProvider.Get(config.Mode);
        var renderer = TextyRendererProvider.Get(config);
        var ctx = new RenderContext(width, height, config);
        var frameBytes = new byte[width * height * Config.PIXELFORMAT];
        using var ffmpeg = FFmpeg.Encoder(config, width, height);
        var option = new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Stretch,
            Sampler = KnownResamplers.NearestNeighbor
        };

        ffmpeg.Start();

        var progress = FFmpeg.MonitorProgressAsync(ffmpeg, Duration ?? 0, ct);        

        try
        {
            await using var stdin = new BufferedStream(ffmpeg.StandardInput.BaseStream, 1 << 20);

            await foreach (var frame in images.WithCancellation(ct))
            {
                using (frame)
                {
                    var pixels = await tm.TextyAsync(frame, config, ct).ConfigureAwait(false);
                    using var img = await renderer.RenderAsync(pixels, ctx, ct).ConfigureAwait(false);

                    img.Mutate(x => x.Resize(option));
                    img.CopyPixelDataTo(frameBytes);

                    await stdin.WriteAsync(frameBytes, ct).ConfigureAwait(false);
                }
            }

            await stdin.FlushAsync(ct).ConfigureAwait(false);
            ffmpeg.StandardInput.Close();

            await ffmpeg.WaitForExitAsync(ct).ConfigureAwait(false);

            if (ffmpeg.ExitCode != 0)
                throw new Exception($"FFmpeg exited with code {ffmpeg.ExitCode}");

            await progress.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during saving video: {ex.Message}");
            throw;
        }
    }

    public override void Dispose()
    {
        if (FilePath is not null && File.Exists(FilePath))
            File.Delete(FilePath);

        GC.SuppressFinalize(this);
    }

    public IEnumerator<string> GetEnumerator()
    {
        foreach (var img in images.ToBlockingEnumerable())
        {
            using (img)
            {
                yield return new TextyImage(img, config).Texty();
            }
        }
    }    

    public async IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        await foreach(var img in images)   
        {
            using (img)
            {
                yield return new TextyImage(img, config).Texty();
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}