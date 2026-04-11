using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Buffers;
using System.Diagnostics;

namespace Texty;

public static class TextyLoader
{
    public static async Task<byte[]> LoadAsync(Config config)
    {
        if (config.IsUrl)
        {
            using var client = new HttpClient();
            return await client.GetByteArrayAsync(config.Input);
        }
        else
        {
            return await File.ReadAllBytesAsync(config.Input);
        }
    }

    public static byte[] Load(Config config)
    {
        if (config.IsUrl)
        {
            using var client = new HttpClient();
            return client.GetByteArrayAsync(config.Input).Result;
        }
        else
        {
            return File.ReadAllBytes(config.Input);
        }
    }

    public static async IAsyncEnumerable<Image<Rgb24>> ExtractFramesAsync(Config config)
    {
        int width = config.Width;
        int height = config.Height;
        int frameSize = width * height * 3;
        var startTimeArg = !string.IsNullOrEmpty(config.StartTime) ? $"-ss {config.StartTime} " : "";
        var durationArg = !string.IsNullOrEmpty(config.Duration) ? $"-t {config.Duration} " : "";

        if (!string.IsNullOrEmpty(config.EndTime))
            if (TimeSpan.TryParse(config.EndTime, out var end) && TimeSpan.TryParse(config.StartTime, out var start))
                durationArg = $"-t {(end - start).TotalSeconds} ";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments =
                    $"-analyzeduration 0 -probesize 32 " +
                    $"{startTimeArg}-i \"{config.Input}\" {durationArg}" +
                    $"-vf scale={width}:{height}:flags=neighbor,fps={config.Fps} " +
                    "-vsync 0 -f rawvideo -pix_fmt rgb24 pipe:1",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };


        process.Start();

        _ = Task.Run(async () =>
        {
            string? line;
            while ((line = await process.StandardError.ReadLineAsync()) != null);
        });

        var output = process.StandardOutput.BaseStream;
        var buffer = new byte[frameSize];

        try
        {
            while (true)
            {
                int read = 0;

                while (read < frameSize)
                {
                    int n = await output.ReadAsync(buffer.AsMemory(read, frameSize - read));
                    if (n == 0)
                        goto END;
                    read += n;
                }

                if (read < frameSize)
                    break;

                yield return Image.LoadPixelData<Rgb24>(buffer, width, height);
            }
        END:;
            if (process.ExitCode != 0)
                throw new Exception("FFmpeg failed");
        }
        finally
        {
            await process.WaitForExitAsync();
            process.Dispose();
        }
        
    }

    public static (int width, int height) GetResolution(Config config)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 \"{config.Input}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        string result = process.StandardOutput.ReadToEnd();

        process.WaitForExit();

        var parts = result.Trim().Split('x');
        var width = int.Parse(parts[0]);
        var height = int.Parse(parts[1]);

        return (width, height);
    }
}
