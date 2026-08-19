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
    private readonly TextyConfig config;
    private readonly string? filePath;
    private readonly double? duration;

    private TextyVideo(TextyConfig config, string? filePath)
    {
        this.config = config;
        this.filePath = filePath;

        var (width, height, duration) = TextyLoader.GetVideoInfo(config);
        if (config.Height < 1)
            this.config = config with { Height = (int)(height * ((float)config.Width / width)) };
        else 
            this.config = config;

        if (config.Duration is not null)
        {
            this.duration = config.Duration.Value.TotalSeconds;
        }
        else if (config.EndTime is not null || config.StartTime is not null)
        {
            var end = config.EndTime ?? TimeSpan.FromSeconds(duration);
            var start = config.StartTime ?? TimeSpan.Zero;
            this.duration = (end - start).TotalSeconds;
        }
        else this.duration = duration;

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
        var frameBytes = new byte[width * height * TextyConfig.PIXELFORMAT];
        using var ffmpeg = FFmpeg.Encoder(config, width, height);
        var option = new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Stretch,
            Sampler = KnownResamplers.NearestNeighbor
        };

        ffmpeg.Start();

        var progress = FFmpeg.MonitorProgressAsync(ffmpeg, duration ?? 0, ct);        

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
            if (!ffmpeg.HasExited)
                ffmpeg.Kill(true);
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during saving video: {ex.Message}");
            throw;
        }
        finally
        {
            if (!ffmpeg.HasExited)
                ffmpeg.Kill(true);
        }
    }

    public override void Dispose()
    {
        try
        {
            if (filePath is not null && File.Exists(filePath))
                File.Delete(filePath);
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
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

    public async IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken ct = default)
    {
        await foreach(var img in images.WithCancellation(ct))   
        {
            using (img)
            {
                yield return await new TextyImage(img, config).TextyAsync(ct).ConfigureAwait(false);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static async Task<TextyVideo> CreateAsync(TextyConfig config, CancellationToken ct = default)
    {
        string? filePath = null;

        if (config.IsUrl)
        {
            filePath = await TextyLoader.DownloadFile(config, ct).ConfigureAwait(false);

            config = config with { Input = filePath };
        }

        return new TextyVideo(config, filePath);
    }
}