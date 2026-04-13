using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using Texty;
using Texty.Configuration;

[DllImport("kernel32.dll", SetLastError = true)]
static extern IntPtr GetStdHandle(int nStdHandle);

[DllImport("kernel32.dll", SetLastError = true)]
static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

[DllImport("kernel32.dll", SetLastError = true)]
static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

if (HandleSpecialArgs(args))
    return;

Config config = ParseArgs();

using var obj = TextyObject.FromConfig(config);

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

    Console.WriteLine($"Time: {sw.Elapsed.TotalSeconds:F3}s ");
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

    using var writer = new StreamWriter(Console.OpenStandardOutput(), Encoding.Unicode, 65536)
    { 
        AutoFlush = false 
    };

    if (obj is TextyImage img)
    {
        writer.WriteLine(img.Texty());
        return;
    }

    var video = obj as TextyVideo;

    do
    {
        var frameTime = 1000d / config.Fps / config.Speed;
        var start = Stopwatch.GetTimestamp();
        long frameIndex = 0;

        await foreach (var frame in video!)
        {
            if (!config.NoClear)
                writer.Write("\x1b[H");

            writer.Write(frame);

            frameIndex++;

            var targetTime = frameIndex * frameTime;
            var elapsed = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;

            var delay = targetTime - elapsed;

            if (delay > 1)
                await Task.Delay((int)delay);
            else if (delay > 0)
                Thread.SpinWait(100);
        }

    } while (config.Loop);

    Console.CursorVisible = true;
}

bool HandleSpecialArgs(string[] args)
{
    if (args.Length == 0)
    {
        Console.WriteLine("Texty - Character-based Image/Video Renderer");
        Console.WriteLine("Usage: Texty <input> [options]");
        Console.WriteLine("\nTry 'Texty --help' for more information.");
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

        return null!;
    }    
}

void ShowHelp()
{
    Console.WriteLine("""
Texty - Character-based Image/Video Renderer

Usage:
  texty <input> [options]

Arguments:
  <input>                    File path or URL

Rendering Options:
  --width, -w <int>          Output width (default: 100)
  --ratio <float>            Height ratio (default: 0.45)
  --charset <string>         Characters used for rendering
  --depth <int>              Depth (default: 10)
  --invert, -i               Invert brightness

Font Options (for Image/Video output):
  --font-size, -fs <int>     Font size for rendering (default: 12)
  --font-name, -fn <str>     Font family name (default: Consolas)
  --font-color, -fc <string> Select ther background color (default: black)    

Video Options:
  --fps <int>                Frames per second (default: 30)
  --loop                     Loop video playback
  --speed <float>            Playback speed (default: 1.0)
  --start, -ss <time>        Start time (e.g., 00:00:05 or 5)
  --to <time>                End time (e.g., 00:00:15)
  --duration, -t <time>      Duration to render (e.g., 10)

Encoding Options:
  --crf <int>                Quality (lower = better, default: 26)
  --preset <string>          Encoding speed preset (default: veryfast)
                             (ultrafast, superfast, veryfast, faster, fast, medium, slow)
  --codec <string>           Video codec (default: libx264)
                             (libx264, libx265)
  --quality, -q <mode>       Preset quality mode (default: balanced)
                              fast      = fastest encoding, larger file
                              balanced  = recommended
                              small     = smallest file (slower)

Output Options:
  --output, -o <path>        Save output to file
  --no-clear                 Disable console overwrite (print continuously)
  --copy, -c                 Copy the result (first frame) to clipboard
  --mode, -m                 Select the rendering mode

Color Options:
  --color                    Enable ANSI color output
  --bg-color <string | int>  Select ther background color (default: white)             

Other:
  --help, -h                 Show this help
  --version, -v              Show version

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