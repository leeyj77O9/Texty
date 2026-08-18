using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Texty.Core.Configuration;

namespace Texty.Core.Util;

public static class TextyLoader
{
    private static readonly HttpClient _http = new();

    public static byte[] Load(Config config) => LoadAsync(config).GetAwaiter().GetResult();

    public static async Task<byte[]> LoadAsync(Config config, CancellationToken ct = default)
        => await (config.IsUrl ? _http.GetByteArrayAsync(config.Input, ct).ConfigureAwait(false)
        : File.ReadAllBytesAsync(config.Input, ct).ConfigureAwait(false));

    public static async IAsyncEnumerable<Image<Rgba32>> ExtractFramesAsync(Config config)
    {
        var (width, height) = (config.Width, config.Height);
        int frameSize = width * height * Config.PIXELFORMAT;

        using var ffmpeg = FFmpeg.Decoder(config, width, height);

        ffmpeg.Start();
        
        _ = FFmpeg.ReadErrorAsync(ffmpeg, line => { }).ConfigureAwait(false);

        var output = ffmpeg.StandardOutput.BaseStream;
        var buffer = new byte[frameSize];

        try
        {
            while (true)
            {
                int read = 0;

                while (read < frameSize)
                {
                    int n = await output.ReadAsync(buffer.AsMemory(read, frameSize - read)).ConfigureAwait(false);

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

            await ffmpeg.WaitForExitAsync().ConfigureAwait(false);

            if (ffmpeg.ExitCode != 0)
                throw new Exception("FFmpeg failed");
        }
        finally
        {
            await ffmpeg.WaitForExitAsync().ConfigureAwait(false);
        }
    }

    public static async Task<string> DownloadFile(Config config, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{config.Extension}");

        using var res = await _http.GetAsync(config.Input, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

        if (!res.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to download file. Status: {(int)res.StatusCode} {res.ReasonPhrase}");
        }

        await using var fs = new FileStream(
            tempPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await res.Content.CopyToAsync(fs, ct).ConfigureAwait(false);

        return tempPath;
    }

    public static (int width, int height, double duration) GetVideoInfo(Config config)
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
                _ = double.TryParse(line.AsSpan(9), out duration);
        }

        if (width == 0 || height == 0)
            throw new FormatException("Failed to parse resolution");

        return (width, height, duration);
    }
}
