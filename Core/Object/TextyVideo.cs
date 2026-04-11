using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;

namespace Texty;

public class TextyVideo : TextyObject
{
    private readonly IAsyncEnumerable<Image<Rgb24>> images;
    private readonly Config config;

    public override Config Config => config;

    public TextyVideo(Config config)
    {
        var (width, height) = TextyLoader.GetResolution(config);           
        this.config = config with { Height = (int)(height * ((float)config.Width / width)) };

        images = TextyLoader.ExtractFramesAsync(this.config);
    }

    public override async IAsyncEnumerable<string> TextyAsync()
    {
        await foreach (var image in images)
        {
            using (image)
            {
                yield return new TextyImage(image, config).Texty();              
            }
        }
    }

    public override string Texty() => throw new NotSupportedException("Use TextyAsync()");

    public override void Save()
    {
        if (string.IsNullOrEmpty(config.Output))
            throw new ArgumentException("Output path is required. Please specify --output <path>");

        var (width, height) = GetSize();
        width = width % 2 == 0 ? width : width - 1;
        height = height % 2 == 0 ? height : height - 1;

        using var process = CreateFFmpeg(width, height);

        process.Start();

        try
        {
            using (var stdin = process.StandardInput.BaseStream)
            {
                foreach (var frame in images.ToBlockingEnumerable())
                {
                    using (frame)
                    {
                        var textyImageObj = new TextyImage(frame, config);
                        using var renderedImage = config.Color ? textyImageObj.RenderANSI() : textyImageObj.Render();
                        renderedImage.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(width, height),
                            Mode = ResizeMode.Stretch,
                            Sampler = KnownResamplers.NearestNeighbor
                        }));

                        renderedImage.ProcessPixelRows(accessor =>
                        {
                            for (int y = 0; y < accessor.Height; y++)
                            {
                                var row = accessor.GetRowSpan(y);
                                var buffer = new byte[accessor.Width * 3];
                                for (int x = 0; x < accessor.Width; x++)
                                {
                                    var pixel = row[x];
                                    buffer[x * 3] = pixel.R;
                                    buffer[x * 3 + 1] = pixel.G;
                                    buffer[x * 3 + 2] = pixel.B;
                                }

                                stdin.Write(buffer, 0, buffer.Length);
                            }
                            stdin.Flush();
                        });
                    }
                }
            }
            process.StandardInput.Close();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string error = process.StandardError.ReadToEnd();
                throw new Exception($"FFmpeg exited with code {process.ExitCode}. Error: {error}");
            }

            Console.WriteLine($"Video successfully saved to: {config.Output}");
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
        width = width % 2 == 0 ? width : width - 1;
        height = height % 2 == 0 ? height : height - 1;

        byte[] frameBytes = new byte[width * height * 3];
        using var process = CreateFFmpeg(width, height);

        process.Start();

        try
        {
            var stderrTask = process.StandardError.ReadToEndAsync();
            var stdin = process.StandardInput.BaseStream;
            await foreach (var frame in images)
            {
                using (frame)
                {
                    var textyImageObj = new TextyImage(frame, config);
                    using var renderedImage = await (config.Color ? textyImageObj.RenderANSIAsync() : textyImageObj.RenderAsync());
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
            Console.WriteLine(ex.StackTrace);
        }
    }

    private (int width, int height) GetSize() => ((int)(config.Width * config.FontSize * (config.Color ? 1 : 0.54)), (int)(config.Height * config.FontSize * (config.Color ? 1 : 0.54)));

    private Process CreateFFmpeg(int width, int height)
    {
        bool isGif = config.Output?.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ?? false;
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-y -f rawvideo -pixel_format rgb24 -video_size {width}x{height} -r {config.Fps} -i - " +
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
}
