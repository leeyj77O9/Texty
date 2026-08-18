using System.Diagnostics;
using System.Globalization;
using System.Text;
using Texty.Core.Configuration;

namespace Texty.Core.Util;

public static class FFmpeg
{
    public const double ALPHA = 0.1;
    public static int BarWidth { get; set; } = 40;

    public static Process Encoder(Config config, int width, int height)
    {
        var args = BuildEncoderArgs(config, width, height);

        return CreateProcess("ffmpeg", args, true, false);
    }

    public static Process Decoder(Config config, int width, int height)
    {
        var args = BuildDecoderArgs(config, width, height);

        return CreateProcess("ffmpeg", args, false, true);
    }

    private static string BuildEncoderArgs(Config config, int width, int height)
    {
        bool isGif = config.Output?.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ?? false;

        var sb = new StringBuilder();

        sb.Append("-stats -stats_period 0.2 -y ");
        sb.Append($"-f rawvideo -pix_fmt {(Config.PIXELFORMAT == 4 ? "rgba" : Config.PIXELFORMAT == 3 ? "rgb24" : "yuv420p")} -video_size {width}x{height} ");
        sb.Append($"-r {config.Fps} -i - ");

        if (isGif)
        {
            sb.Append("-vf split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse=dither=none ");
            sb.Append("-f gif ");
        }
        else
        {
            sb.Append($"-c:v {config.Codec} ");
            sb.Append($"-crf {config.Crf} ");
            sb.Append($"-preset {config.EncodeSpeed} ");
            sb.Append("-pix_fmt yuv420p ");
        }

        sb.Append($"\"{config.Output}\"");

        return sb.ToString();
    }

    private static string BuildDecoderArgs(Config config, int width, int height)
    {
        var startTimeArg = !string.IsNullOrEmpty(config.StartTime) ? $"-ss {config.StartTime} " : "";
        var durationArg = !string.IsNullOrEmpty(config.Duration) ? $"-t {config.Duration} " : "";

        if (!string.IsNullOrEmpty(config.EndTime))
            if (TimeSpan.TryParse(config.EndTime, out var end) &&TimeSpan.TryParse(config.StartTime, out var start))
                durationArg = $"-t {(end - start).TotalSeconds} ";

        return
            $"{startTimeArg}-i \"{config.Input}\" {durationArg}" +
            $"-vf scale={width}:{height}:flags=neighbor,fps={config.Fps} " +
            $"-vsync 0 -f rawvideo -pix_fmt {(Config.PIXELFORMAT == 4 ? "rgba" : "rgb24")} pipe:1";
    }

    private static Process CreateProcess(string fileName, string args, bool redirectInput, bool redirectOutput)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardInput = redirectInput,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
    }

    public static async Task ReadErrorAsync(Process process, Action<string> onLine, CancellationToken ct = default)
    {
        try
        {
            string? line;

            while ((line = await process.StandardError.ReadLineAsync(ct)) != null)
                onLine(line);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public static async Task<string> MonitorProgressAsync(Process ffmpeg, double duration, CancellationToken ct = default)
    {
        var errorBuilder = new StringBuilder();

        double smoothedSpeed = 0;
        long lastRender = 0;

        try
        {
            await ReadErrorAsync(ffmpeg, line =>
            {
                errorBuilder.AppendLine(line);

                var now = Stopwatch.GetTimestamp();

                if ((now - lastRender) * 1000.0 / Stopwatch.Frequency < 50)
                    return;

                lastRender = now;

                if (!TryParseProgress(line, out var current, out var speed))
                    return;

                if (speed > 0)
                    smoothedSpeed = smoothedSpeed == 0 ? speed : smoothedSpeed * (1 - ALPHA) + speed * ALPHA;

                double percent = duration > 0 ? current / duration : 0;
                percent = Math.Clamp(percent * 100, 0, 100);

                double eta = smoothedSpeed > 0 ? (duration - current) / smoothedSpeed : 0;

                RenderProgress(percent, eta, BarWidth);

            }, ct).ConfigureAwait(false);

            await ffmpeg.WaitForExitAsync(ct).ConfigureAwait(false);

            RenderProgress(100, 0, BarWidth);
            Console.WriteLine();

            if (ffmpeg.ExitCode != 0)
                throw new Exception($"FFmpeg failed:\n{errorBuilder}");

            return errorBuilder.ToString();
        }
        finally
        {
            try
            {
                if (!ffmpeg.HasExited)
                    ffmpeg.Kill(true);
            }
            catch { }

            ffmpeg.Dispose();
        }
    }

    private static bool TryParseProgress(string line, out double time, out double speed)
    {
        time = 0;
        speed = 0;

        var tIdx = line.IndexOf("time=");
        if (tIdx < 0) return false;

        var span = line.AsSpan(tIdx + 5);
        var end = span.IndexOf(' ');

        var timeStr = end > 0 ? span[..end] : span;

        if (!TimeSpan.TryParse(timeStr, out var ts))
            return false;

        time = ts.TotalSeconds;

        var sIdx = line.IndexOf("speed=");
        if (sIdx >= 0)
        {
            var sSpan = line.AsSpan(sIdx + 6);
            var xIdx = sSpan.IndexOf('x');
            var speedSpan = xIdx > 0 ? sSpan[..xIdx] : sSpan;

            double.TryParse(speedSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out speed);
        }

        return true;
    }

    private static void RenderProgress(double percent, double eta, int width)
    {
        int filled = (int)(percent / 100 * width);
        filled = Math.Clamp(filled, 0, width);

        Console.Write($"\r[{new string('#', filled)}{new string('-', width - filled)}] ");
        Console.Write($"{percent,5:0.0}% | ETA {eta,5:0}s   ");
    }

}