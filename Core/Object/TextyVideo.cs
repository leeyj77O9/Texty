using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;

namespace Texty;

public class TextyVideo : TextyObject
{
    private readonly IAsyncEnumerable<Image<Rgba32>> images;
    private readonly Config config;

    public override Config Config => config;

    public TextyVideo(Config config)
    {
        var (width, height) = TextyLoader.GetResolution(config);           
        this.config = config;
        this.config.Height = (int)(height * ((float)config.Width / width));

        images = TextyLoader.ExtractFramesAsync(config);
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
                        using var renderedImage = (config.Color ? textyImageObj.RenderANSI() : textyImageObj.Render());

                        renderedImage.SaveAsPng(stdin);
                        stdin.Flush();
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

        using var process = CreateFFmpeg(width, height);

        process.Start();

        try
        {
            using (var stdin = process.StandardInput.BaseStream)
            {
                await foreach (var frame in images)
                {
                    using (frame)
                    {
                        var textyImageObj = new TextyImage(frame, config);
                        using var renderedImage = (await (config.Color ? textyImageObj.RenderANSIAsync() : textyImageObj.RenderAsync()));                     
    
                        await renderedImage.SaveAsPngAsync(stdin);
                        await stdin.FlushAsync();
                    }
                }
            }
            process.StandardInput.Close();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync();
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

    private (int width, int height) GetSize() => ((int)(config.Width * config.FontSize * (config.Color ? 1 : 0.54)), (int)(config.Height * config.FontSize * (config.Color ? 1 : 0.54)));

    private Process CreateFFmpeg(int width, int height) => new()
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -f image2pipe -vcodec png -r {config.Fps} -i - " +
                        $"-c:v libx264 -crf 0 -preset slow -pix_fmt yuv444p -tune animation -vf \"scale={width}:{height}\" " +
                        $"\"{config.Output}\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
