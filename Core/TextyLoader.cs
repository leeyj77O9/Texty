using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
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

    public static async IAsyncEnumerable<Image<Rgba32>> ExtractFramesAsync(Config config)
    {
        int width = config.Width;
        int height = config.Height;
        int frameSize = width * height * 4;

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments =
                    $"-i pipe:0 -vf fps={config.Fps},scale={width}:{height} -f rawvideo -pix_fmt rgba pipe:1",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var inputTask = Task.Run(async () =>
        {
            var inputData = await LoadAsync(config);
            await process.StandardInput.BaseStream.WriteAsync(inputData);
            process.StandardInput.Close();
        });

        _ = Task.Run(async () =>
        {
            string? line;
            while ((line = await process.StandardError.ReadLineAsync()) != null);                        
        });

        var output = process.StandardOutput.BaseStream;
        var buffer = new byte[frameSize];

        while (true)
        {
            int read = 0;

            while (read < frameSize)
            {
                int n = await output.ReadAsync(buffer.AsMemory(read, frameSize - read));
                if (n == 0) break;
                read += n;
            }

            if (read < frameSize)
                break;

            var frame = new byte[frameSize];
            Buffer.BlockCopy(buffer, 0, frame, 0, frameSize);

            yield return Image.LoadPixelData<Rgba32>(frame, width, height);
        }

        await inputTask;
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new Exception("FFmpeg failed");

        process.Dispose();
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
