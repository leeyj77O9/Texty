using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Texty;

[DllImport("kernel32.dll", SetLastError = true)]
static extern IntPtr GetStdHandle(int nStdHandle);

[DllImport("kernel32.dll", SetLastError = true)]
static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

[DllImport("kernel32.dll", SetLastError = true)]
static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

if (HandleSpecialArgs(args))
    return;

Config config;

try
{
    config = Config.FromArgs(args);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine("Use --help for usage.");
    return;
}

var obj = TextyObject.FromConfig(config);

EnableAnsi();

if (obj == null)
{
    Console.WriteLine("Failed to create object.");
    return;
}

if (config.Output != null)
{
    var sw = Stopwatch.StartNew();
    await obj.SaveAsync();
    sw.Stop();

    Console.WriteLine($"Time: {sw.Elapsed.TotalSeconds:F2}s ");
}
else if (config.CopyToClipboard)
{
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
else
{
    Console.CursorVisible = false;
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true; 

        obj.Dispose();

        Console.CursorVisible = true;
        Environment.Exit(0); 
    };

    using var writer = new StreamWriter(Console.OpenStandardOutput());

    do
    {
        var frameTime = 1000.0 / config.Fps / config.Speed;
        var sw = Stopwatch.StartNew();

        await foreach (var frame in obj.TextyAsync())
        {
            if (!config.NoClear)
                Console.SetCursorPosition(0, 0);

            writer.Write(frame);
            writer.Flush();

            sw.Stop();
            var elapsed = sw.Elapsed.TotalMilliseconds;
            var delay = Math.Max(0, frameTime - elapsed);

            await Task.Delay((int)delay);
            sw.Restart();
        }

    } while (config.Loop);

    Console.CursorVisible = true;
}

bool HandleSpecialArgs(string[] args)
{
    var argSet = args.ToHashSet(StringComparer.OrdinalIgnoreCase);

    if (argSet.Count == 0)
    {
        Console.WriteLine("Texty - Character-based Image/Video Renderer");
        Console.WriteLine("Usage: Texty <input> [options]");
        Console.WriteLine("\nTry 'Texty --help' for more information.");
        return true;
    }

    if (argSet.Contains("--help") || argSet.Contains("-h"))
    {
        ShowHelp();
        return true;
    }

    if (argSet.Contains("--version") || argSet.Contains("-v"))
    {
        ShowVersion();
        return true;
    }

    return false;
}

void ShowHelp()
{
    Console.WriteLine("""
Texty - Character-based Image/Video Renderer

Usage:
  texty <input> [options]

Arguments:
  <input>                File path or URL

Rendering Options:
  --width, -w <int>      Output width (default: 100)
  --ratio <float>        Height ratio (default: 0.45)
  --charset <string>     Characters used for rendering
  --depth <int>          Depth (default: 10)
  --invert, -i           Invert brightness

Font Options (for Image/Video output):
  --font-size, -fs <int> Font size for rendering (default: 12)
  --font-name, -fn <str> Font family name (default: Consolas)

Video Options:
  --fps <int>            Frames per second (default: 30)
  --loop                 Loop video playback
  --speed <float>        Playback speed (default: 1.0)
  --start, -ss <time>    Start time (e.g., 00:00:05 or 5)
  --to <time>            End time (e.g., 00:00:15)
  --duration, -t <time>  Duration to render (e.g., 10)

Encoding Options:
  --crf <int>            Quality (lower = better, default: 26)
  --preset <string>      Encoding speed preset (default: veryfast)
                         (ultrafast, superfast, veryfast, faster, fast, medium, slow)
  --codec <string>       Video codec (default: libx264)
                         (libx264, libx265)
  --quality, -q <mode>   Preset quality mode (default: balanced)
                         fast      = fastest encoding, larger file
                         balanced  = recommended
                         small     = smallest file (slower)

Output Options:
  --output, -o <path>    Save output to file
  --no-clear             Disable console overwrite (print continuously)
  --copy, -c             Copy the result (first frame) to clipboard

Color Options:
  --color                Enable ANSI color output

Other:
  --help, -h             Show this help
  --version, -v          Show version

Examples:
  Texty image.jpg
  Texty video.mp4 --fps 30 --loop
  Texty video.mp4 --color --speed 2
  Texty input.png -w 200 -o output.txt

  Texty video.mp4 --crf 28 --preset fast
  Texty video.mp4 --codec libx265 --crf 28
  Texty video.mp4 --crf 26 --preset veryfast -o out.mp4
""");
}

void ShowVersion()
{
    var version = Assembly.GetExecutingAssembly().GetName().Version;
    Console.WriteLine($"Texty v{version}");
}

void EnableAnsi()
{
    if (!OperatingSystem.IsWindows())
        return;

    try
    {
        var handle = GetStdHandle(-11);

        if (GetConsoleMode(handle, out int mode))
        {
            SetConsoleMode(handle, mode | 0x0004);
        }
    }
    catch
    {
        
    }
}