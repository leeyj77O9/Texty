using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Texty.Core.Configuration;
using Texty.Core.Object;

if (HandleSpecialArgs(args))
    return;

Config config = ParseArgs();
using var obj = TextyObject.FromConfig(config) ?? throw new InvalidOperationException("Failed to create object.");
using var cts = new CancellationTokenSource();

if (config.Output != null)
    await HandleSave(obj, cts.Token);
else if (config.CopyToClipboard)
    HandleCopyToClipboard(obj);
else
    await HandleRender(obj, cts.Token);

bool HandleSpecialArgs(string[] args)
{
    if (args.Length == 0)
    {
        Console.WriteLine("Texty - Character-based Image/Video Renderer");
        Console.WriteLine("Usage: texty <input> [options]");
        Console.WriteLine("\nTry 'texty --help' for more information.");
        return true;
    }

    foreach (var arg in args) 
    {
        if (arg == "--help" || arg == "-h")
        {
            ShowHelp();
            return true;
        }

        if (arg == "--version" || arg == "-v")
        {
            ShowVersion();
            return true;
        }
    }

    return false;
}

Config ParseArgs()
{
    try
    {
        return Config.FromArgs(args);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine("Use --help for usage.");
        Environment.Exit(0);

        return null;   
    }    
}

void ShowHelp()
{
    Console.WriteLine("""
Texty - Character-based Image/Video Renderer

Usage:
  texty <input> [options]

Arguments:
  <input>                      File path or URL

Rendering Options:
  --width, -w <int>            Number of characters per line (default: 100)
  --charset <string>           Characters used for rendering (default: " .:=*M#@")
  --invert, -i                 Invert brightness

Video/Image Processing Options:
  --blur <float>              Apply Gaussian blur (default: 0)
  --contrast <float>          Adjust contrast (default: 1.0)
  --brightness <float>        Adjust brightness (default: 1.0)
  --saturation <float>        Adjust saturation (default: 1.0)

Font Options (Image/Video output):
  --font-size, -fs <int>       Font size (default: 12)
  --font-name, -fn <string>    Font family (default: Consolas)
  --font-color, -fc <string>   Font color (default: black)
  --font-style, -fst <style>   Font style (default: Regular)
  --background-color, -bc <string>
                               Background color (default: white)

Video Options:
  --fps <int>                  Frames per second (default: 30)
  --loop                       Loop playback (default: false)
  --speed <float>              Playback speed (default: 1.0)
  --start, -ss <time>          Start time (e.g. 00:00:05)
  --to <time>                  End time 
  --duration, -t <time>        Duration

Encoding Options:
  --crf <int>                  Quality (lower = better, default: 26)
  --encode-speed, -es <string> Encoding speed (default: veryfast)
                                ultrafast, superfast, veryfast, faster,
                                fast, medium, slow
  --codec <string>             Video codec (default: libx264)
                                libx264, libx265
  --quality, -q <mode>         Preset quality (default: fast)
                                ultrafast, fast, balanced,
                                high, veryHigh, lossless,
                                small, verysmall, max

Output Options:
  --output, -o <path>          Save output to file
  --no-clear                   Disable console clearing
  --copy, -c                   Copy first frame to clipboard
  --mode, -m <mode>            Rendering mode

Color Options:
  --color                      Enable ANSI color output

Other:
  --help, -h                   Show help
  --version, -v                Show version

Examples:
  texty image.jpg
  texty video.mp4 --fps 30 --loop
  texty video.mp4 --color --speed 2
  texty input.png -w 200 -o output.txt

  texty video.mp4 --crf 28 --preset fast
  texty video.mp4 --codec libx265 --crf 28
  texty video.mp4 --crf 26 --preset veryfast -o out.mp4
""");
}

void ShowVersion()
{
    var version = Assembly.GetExecutingAssembly().GetName().Version;
    Console.WriteLine($"Texty v{version}");
}

void EnableAnsi()
{
    const int ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;    

    if (!OperatingSystem.IsWindows())
        return;

    try
    {
        var handle = GetStdHandle(-11);

        if (GetConsoleMode(handle, out int mode))
        {
            SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }
    }
    catch
    {
        
    }
}

async Task HandleSave(TextyObject obj, CancellationToken ct = default)
{
    try
    {
        var sw = Stopwatch.StartNew();
        await obj.SaveAsync(ct);
        sw.Stop();
        Console.WriteLine($"Time: {sw.Elapsed.TotalSeconds:F3}s ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during saving: {ex.Message}");        
    }

}

void HandleCopyToClipboard(TextyObject obj)
{
    if (obj is not TextyImage img)
    {
        Console.WriteLine("Copy to clipboard is only supported for images.");
        return;
    }

    var text = obj.Texty();
    try
    {
        TextCopy.ClipboardService.SetText(text);
        Console.WriteLine("Output copied to clipboard.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to copy to clipboard: {ex.Message}");
    }
}

async Task HandleRender(TextyObject obj, CancellationToken ct = default)
{
    EnableAnsi();
    var sw = Stopwatch.StartNew();

    Console.OutputEncoding = Encoding.UTF8;

    Console.CursorVisible = false;
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;

        cts.Cancel();
        obj.Dispose();
        
        Console.CursorVisible = true;
    };

    using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false), 1 << 20)
    {
        AutoFlush = false
    };

    if (obj is TextyImage img)
    {
        writer.WriteLine(img.Texty());
        sw.Stop();
        writer.WriteLine($"Time: {sw.Elapsed.TotalSeconds:F3}s ");
        return;
    }

    if (obj is not TextyVideo video)
    {
        writer.WriteLine("It's not texty object");
        return;
    }

    do
    {
        var frameTime = 1000d / config.Fps / config.Speed;
        var frameIndex = 0;
        var start = Stopwatch.GetTimestamp();

        await foreach (string frame in video.WithCancellation(ct))
        {
            if (!config.NoClear)
                Console.SetCursorPosition(0, 0);

            writer.Write(frame);
            writer.Flush();           

            frameIndex++;

            var targetTime = frameIndex * frameTime;
            var elapsed = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
            var delay = targetTime - elapsed;

            if (delay > 2)
                await Task.Delay((int)delay - 1, ct);
            else if (delay > 0)
                Thread.SpinWait(50);
        }

    } while (config.Loop);

    sw.Stop();
    writer.WriteLine($"Time: {sw.Elapsed.TotalSeconds:F3}s ");
}

[DllImport("kernel32.dll", SetLastError = true)]
static extern IntPtr GetStdHandle(int nStdHandle);

[DllImport("kernel32.dll", SetLastError = true)]
static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

[DllImport("kernel32.dll", SetLastError = true)]
static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);