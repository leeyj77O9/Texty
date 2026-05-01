using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Texty.Configuration;
using Texty.Mode;
using Texty.Renderer;

namespace Texty;

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

    public override string Texty() => throw new NotSupportedException("Use TextyAsync()");

    public override void Save() => SaveAsync().GetAwaiter().GetResult();

    public override async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(config.Output))
            throw new ArgumentException("Output path is required. Please specify --output <path>");

        var (width, height) = (config.Width * config.FontSize, config.Height * config.FontSize);
        width &= ~1;
        height &= ~1;

        var tm = TextyModeProvider.Get(config.Mode);
        var renderer = TextyRendererProvider.Get(config);
        var ctx = new RenderContext(width, height, config);
        var frameBytes = new byte[width * height * Config.PIXELFORMAT];
        using var process = CreateFFmpeg(width, height);

        process.Start();
        var progress = StartFFmpegProgress(process);

        try
        {
            await using var stdin = new BufferedStream(process.StandardInput.BaseStream, 1 << 20);

            await foreach (var frame in images)
            {
                using (frame)
                {
                    var pixels = await tm.TextyAsync(frame, config).ConfigureAwait(false);
                    using var img = await renderer.RenderAsync(pixels, ctx).ConfigureAwait(false);

                    img.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(width, height),
                        Mode = ResizeMode.Stretch,
                        Sampler = KnownResamplers.NearestNeighbor
                    }));

                    img.CopyPixelDataTo(frameBytes);

                    await stdin.WriteAsync(frameBytes).ConfigureAwait(false);
                }
            }

            await stdin.FlushAsync().ConfigureAwait(false);
            process.StandardInput.Close();

            await process.WaitForExitAsync().ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg exited with code {process.ExitCode}.");
            }
        }
        catch (Exception ex)
        {
            process.Kill();
            Console.WriteLine($"Error during saving video: {ex.Message}");
        }
    }

    private Process CreateFFmpeg(int width, int height)
    {
        bool isGif = config.Output?.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ?? false;

        string inputArgs = $"-stats -stats_period 0.2 -y -f rawvideo -pix_fmt rgba -video_size {width}x{height} -r {config.Fps} -i - ";
        string outputArgs = isGif
            ? "-vf split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse=dither=none -f gif "
            : $"-c:v {config.Codec} -crf {config.Crf} -preset {config.EncodeSpeed} -pix_fmt yuv420p ";

        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = inputArgs + outputArgs + $"\"{config.Output}\"",
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
    }

    public Task StartFFmpegProgress(Process process)
    {
        var errorBuilder = new StringBuilder();        

        return Task.Run(async () =>
        {
            string? line;

            var startTime = Stopwatch.GetTimestamp();

            using var writer = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = false
            };

            int barWidth = 40;
            double smoothedSpeed = 0;
            const double alpha = 0.1;

            while ((line = await process.StandardError.ReadLineAsync()
                                                      .ConfigureAwait(false)) != null)
            {
                errorBuilder.AppendLine(line);

                const string timeKey = "time=";
                var timeIdx = line.IndexOf(timeKey);

                if (timeIdx < 0) continue;

                var timeStr = line.Substring(timeIdx + timeKey.Length,Math.Min(11, line.Length - (timeIdx + timeKey.Length)));

                if (!TimeSpan.TryParse(timeStr, out var current))
                    continue;

                if (current.TotalSeconds < 0.5)
                    continue;

                double speed = 0;
                var speedIdx = line.IndexOf("speed=");
                if (speedIdx >= 0)
                {
                    var end = line.IndexOf('x', speedIdx);
                    if (end > speedIdx)
                    {
                        var span = line.AsSpan(speedIdx + 6, end - (speedIdx + 6));
                        double.TryParse(span, NumberStyles.Any, CultureInfo.InvariantCulture, out speed);
                    }
                }

                if (speed > 0)
                    smoothedSpeed = smoothedSpeed == 0 ? speed : smoothedSpeed * (1 - alpha) + speed * alpha;

                double percent = 0;
                double eta = 0;

                if (Duration > 0)
                {
                    var progress = current.TotalSeconds / (double)Duration;
                    percent = progress * 100;

                    if (smoothedSpeed > 0.01)
                    {
                        var remaining = (double)Duration - current.TotalSeconds;
                        eta = remaining / smoothedSpeed;
                    }
                    else
                    {
                        var elapsed = (Stopwatch.GetTimestamp() - startTime) / (double)Stopwatch.Frequency;
                        eta = progress > 0 ? elapsed * (1 - progress) / progress : 0;
                    }
                }

                int filled = (int)(percent / 100 * barWidth);
                filled = Math.Clamp(filled, 0, barWidth);
                writer.Write($"\r[{new string('#', filled)}{new string('-', barWidth - filled)}] {percent,5:0.0}% | ETA: {eta,5:0}s ");
                writer.Flush();
            }

            if (process.ExitCode == 0 && Duration > 0)
            {
                writer.Write($"\r\x1b[2K[{new string('#', barWidth)}] 100.0%");
                writer.Flush();
            }

            writer.WriteLine();
            writer.Flush();

            if (process.ExitCode != 0)
                throw new Exception($"FFmpeg exited with code {process.ExitCode}\n{errorBuilder}");

            return errorBuilder.ToString();
        });
    }

    public override void Dispose()
    {
        if (FilePath is not null && File.Exists(FilePath))
            File.Delete(FilePath);

        GC.SuppressFinalize(this);
    }

    public IEnumerator<string> GetEnumerator()
        => images.ToBlockingEnumerable().Select(img => new TextyImage(img, config).Texty()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => images.Select(img => new TextyImage(img, config).Texty()).GetAsyncEnumerator(cancellationToken);
}