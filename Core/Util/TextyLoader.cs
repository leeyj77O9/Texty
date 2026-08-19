using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Globalization;
using System.Runtime.CompilerServices;
using Texty.Core.Configuration;

namespace Texty.Core.Util;

public static class TextyLoader
{
    private static readonly HttpClient _http = new();

    public static byte[] Load(TextyConfig config) => LoadAsync(config).GetAwaiter().GetResult();

    public static async Task<byte[]> LoadAsync(TextyConfig config, CancellationToken ct = default)
        => await (config.IsUrl ? _http.GetByteArrayAsync(config.Input, ct).ConfigureAwait(false)
        : File.ReadAllBytesAsync(config.Input, ct).ConfigureAwait(false));

    public static async IAsyncEnumerable<Image<Rgba32>> ExtractFramesAsync(TextyConfig config, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var (width, height) = (config.Width, config.Height);
        int frameSize = width * height * TextyConfig.PIXELFORMAT;

        using var ffmpeg = FFmpeg.Decoder(config, width, height);

        ffmpeg.Start();
        
        _ = FFmpeg.ReadErrorAsync(ffmpeg, line => { }, ct).ConfigureAwait(false);

        var output = ffmpeg.StandardOutput.BaseStream;
        var buffer = new byte[frameSize];

        try
        {
            while (true)
            {
                int read = 0;

                while (read < frameSize)
                {
                    int n = await output.ReadAsync(buffer.AsMemory(read, frameSize - read), ct).ConfigureAwait(false);

                    if (n == 0)
                        break;

                    read += n;
                }

                if (read == 0)
                    break;

                if (read < frameSize)
                    break;

                yield return Image.LoadPixelData<Rgba32>(buffer, width, height);
            }

            await ffmpeg.WaitForExitAsync(ct).ConfigureAwait(false);

            if (ffmpeg.ExitCode != 0)
                throw new Exception("FFmpeg failed");
        }
        finally
        {
            try
            {
                if (!ffmpeg.HasExited)
                    ffmpeg.Kill(true);
            }
            catch
            {
            }

            try
            {
                await ffmpeg.WaitForExitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }

    public static async Task<string> DownloadFile(TextyConfig config, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{config.Extension}");
        try
        {
            using var res = await _http.GetAsync(config.Input, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            if (!res.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to download file. Status: {(int)res.StatusCode} {res.ReasonPhrase}");
            }

            await using var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 81920, 
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            await res.Content.CopyToAsync(fs, ct).ConfigureAwait(false);

            return tempPath;
        }
        catch
        {
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
                
            }

            throw;
        }
    }

    public static (int width, int height, double duration) GetVideoInfo(TextyConfig config)
    {
        using var ffprobe = FFprobe.Create(config);

        ffprobe.Start();

        string output = ffprobe.StandardOutput.ReadToEnd();
        ffprobe.WaitForExit();

        int width = 0, height = 0;
        double duration = 0;

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("width="))
                width = int.Parse(line.AsSpan(6));

            else if (line.StartsWith("height="))
                height = int.Parse(line.AsSpan(7));

            else if (line.StartsWith("duration="))
                _ = double.TryParse(line.AsSpan(9), NumberStyles.Float, CultureInfo.InvariantCulture, out duration);
        }

        if (width == 0 || height == 0)
            throw new FormatException("Failed to parse resolution");

        return (width, height, duration);
    }
}
