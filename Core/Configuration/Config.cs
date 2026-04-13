using SixLabors.Fonts;
using SixLabors.ImageSharp;
using System.Text;

namespace Texty.Configuration;

public record Config
{
    public const int PIXELFORMAT = 4;

    public string Input { get; init; } = string.Empty;
    public int Width { get; init; } = 100;
    public double Ratio { get; init; } = 0.45;

    public int Height { get; init; }
    public bool Invert { get; init; }
    public int Depth { get; init; } = 10;
    public string CharSet { get; init; } = " .:=*M#@";
    public int Fps { get; init; } = 30;
    public Color BgColor { get; init; } = Color.White;

    public string? Output { get; init; }
    public bool Loop { get; init; }
    public double Speed { get; init; } = 1.0;
    public bool NoClear { get; init; }
    public bool IsColor { get; init; }

    public int FontSize { get; init; } = 12;
    public string FontName { get; init; } = "Consolas";
    public Color FontColor { get; init; } = Color.Black;
    public FontStyle FontStyle { get; init; } = FontStyle.Regular;

    public bool CopyToClipboard { get; init; }

    public bool IsUrl => Uri.IsWellFormedUriString(Input, UriKind.Absolute);
    public string Extension => IsUrl
        ? Path.GetExtension(new Uri(Input).AbsolutePath).ToLowerInvariant()
        : Path.GetExtension(Input).ToLowerInvariant();

    public bool IsImage => ImageExtensions.Contains(Extension);
    public bool IsVideo => VideoExtensions.Contains(Extension);

    public TextyMode Mode { get; init; } = TextyMode.Default;

    public int Crf { get; init; } = 26;
    public string Preset { get; init; } = "veryfast";
    public string Codec { get; init; } = "libx264";
    public TextyQuality Quality { get; init; } = TextyQuality.Balanced;

    public string? StartTime { get; init; } 
    public string? Duration { get; init; } 
    public string? EndTime { get; init; } 

    public Rune[] Runes { get; init; }

    private static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".jfif", ".webp"];
    private static readonly HashSet<string> VideoExtensions = [".mp4", ".avi", ".mov", ".mkv", ".webm"];

    public Config() { } 

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Input)) throw new ArgumentException("Input is required");
        if (Width <= 0) throw new ArgumentException("Width must be > 0");
        if (!IsImage && !IsVideo) throw new ArgumentException($"Unsupported file type: {Extension}");
    }

    public static Config FromArgs(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Input argument is required");

        string input = args[0];

        var width = 100;
        var ratio = 0.45;
        var invert = false;
        var depth = 10;
        var charSet = " .:=*M#@";
        var fps = 30;
        var bgColor = Color.White;

        var loop = false;
        var speed = 1.0;
        var noClear = false;
        var color = false;

        var fontSize = 12;
        var fontName = "Consolas";
        var fontColor = Color.Black;
        var fontStyle = FontStyle.Regular;

        var copyToClipboard = false;

        var mode = TextyMode.Default;

        var crf = 26;
        var preset = "veryfast";
        var codec = "libx264";
        var quality = TextyQuality.Fast;

        string? output = null;
        string? startTime = null;
        string? duration = null;
        string? endTime = null;

        for (int i = 1; i < args.Length; i++)
        {
            string arg = args[i];

            string NextValue()
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"Missing value for {arg}");
                return args[++i];
            }

            switch (arg)
            {
                case "--width":
                case "-w":
                    if (!int.TryParse(NextValue(), out width))
                        throw new ArgumentException($"Invalid width: {arg}");
                    break;

                case "--ratio":
                    if (!double.TryParse(NextValue(), out ratio))
                        throw new ArgumentException($"Invalid ratio: {arg}");
                    break;

                case "--invert":
                case "-i":
                    invert = true;
                    break;

                case "--depth":
                    if (!int.TryParse(NextValue(), out depth))
                        throw new ArgumentException($"Invalid depth: {arg}");
                    break;

                case "--charset":
                    charSet = NextValue();
                    break;

                case "--fps":
                    if (!int.TryParse(NextValue(), out fps))
                        throw new ArgumentException($"Invalid fps: {arg}");
                    break;

                case "--background-color":
                case "-bc":
                    var v1 = NextValue();
                    if (Color.TryParse(v1, out bgColor))
                        break;
                    if (Color.TryParseHex(v1, out bgColor))
                        break;
                    throw new ArgumentException($"Invalid font-color: {arg}");

                case "--output":
                case "-o":
                    output = NextValue();
                    break;

                case "--loop":
                    loop = true;
                    break;

                case "--speed":
                    if (!double.TryParse(NextValue(), out speed))
                        throw new ArgumentException($"Invalid speed: {arg}");
                    break;

                case "--no-clear":
                    noClear = true;
                    break;

                case "--color":
                    color = true;
                    break;

                case "--font-size":
                case "-fs":
                    if (!int.TryParse(NextValue(), out fontSize))
                        throw new ArgumentException($"Invalid font size: {arg}");
                    break;

                case "--font-name":
                case "-fn":
                    fontName = NextValue();
                    break;

                case "--font-color":
                case "-fc":
                    var v2 = NextValue();
                    if (Color.TryParse(v2, out fontColor))
                        break;
                    if (Color.TryParseHex(v2, out fontColor))
                        break;
                    throw new ArgumentException($"Invalid font-color: {arg}");

                case "--font-style":
                case "-fst":
                    if (!Enum.TryParse(NextValue(), out fontStyle))
                        throw new ArgumentException($"Invalid font-style: {arg}");              
                    break;

                case "--copy":
                case "-c":
                    copyToClipboard = true;
                    break;

                case "--crf":
                    if (!int.TryParse(NextValue(), out crf))
                        throw new ArgumentException($"Invalid crf: {arg}");
                    break;

                case "--preset":
                    preset = NextValue();
                    break;

                case "--codec":
                    codec = NextValue();
                    break;

                case "--quality":
                case "-q":
                    if (!Enum.TryParse(NextValue(), out quality))
                        throw new ArgumentException($"Invalid quality: {arg}");
                    break;

                case "--start":
                case "-ss":
                    startTime = NextValue();
                    break;

                case "--duration":
                case "-t":
                    duration = NextValue();
                    break;

                case "--to":
                    endTime = NextValue();
                    break;

                case "--mode":
                case "-m":
                    if (!Enum.TryParse(NextValue(), true, out mode))
                        throw new ArgumentException($"Invalid mode: {arg}");
                    break;

                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        var config = new Config
        {
            Input = input,
            Width = width,
            Ratio = ratio,
            Invert = invert,
            Depth = depth,
            CharSet = charSet,
            Fps = fps,
            BgColor = bgColor,
            Output = output,
            Loop = loop,
            Speed = speed,
            NoClear = noClear,
            IsColor = color,
            FontSize = fontSize,
            FontName = fontName,
            FontColor = fontColor,
            FontStyle = fontStyle,
            CopyToClipboard = copyToClipboard,
            Mode = mode,
            Crf = crf,
            Preset = preset,
            Codec = codec,
            Quality = quality,
            StartTime = startTime,
            Duration = duration,
            EndTime = endTime,
            Runes = [.. invert ? charSet.EnumerateRunes().Reverse() : charSet.EnumerateRunes()],
        };

        return ApplyQualitySettings(config);
    }

    private static Config ApplyQualitySettings(Config config)
    {
        return config.Quality switch
        {
            TextyQuality.Fast => config with { Codec = "libx264", Crf = 26, Preset = "veryfast" },
            TextyQuality.Balanced => config with { Codec = "libx264", Crf = 28, Preset = "faster" },
            TextyQuality.Small => config with { Codec = "libx265", Crf = 28, Preset = "fast" },
            _ => config
        };
    }
}