namespace Texty;

public record Config
{
    public string Input { get; init; } = string.Empty;
    public int Width { get; init; } = 100;
    public double Ratio { get; init; } = 0.45;

    public int Height { get; init; }
    public bool Invert { get; init; }
    public int Depth { get; init; } = 10;
    public string CharSet { get; init; } = " .'`^\",:;Il!i~+_-?][}{1)(|\\/tfjrxnuvczXYUJCLQ0OZmwqpdbkhao*#MW&8%B@$";
    public int Fps { get; init; } = 30;
    public string? Output { get; init; }
    public bool Loop { get; init; }
    public double Speed { get; init; } = 1.0;
    public bool NoClear { get; init; }
    public bool Color { get; init; }
    public int FontSize { get; init; } = 12;
    public string FontName { get; init; } = "Consolas";
    public bool CopyToClipboard { get; init; }

    public bool IsUrl => Uri.IsWellFormedUriString(Input, UriKind.Absolute);
    public string Extension => IsUrl
        ? Path.GetExtension(new Uri(Input).AbsolutePath).ToLowerInvariant()
        : Path.GetExtension(Input).ToLowerInvariant();

    public bool IsImage => ImageExtensions.Contains(Extension);
    public bool IsVideo => VideoExtensions.Contains(Extension);

    public int Crf { get; init; } = 26;
    public string Preset { get; init; } = "veryfast";
    public string Codec { get; init; } = "libx264";
    public string? Quality { get; init; }

    public string? StartTime { get; init; } 
    public string? Duration { get; init; } 
    public string? EndTime { get; init; } 

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
        var charSet = " .'`^\",:;Il!i~+_-?][}{1)(|\\/tfjrxnuvczXYUJCLQ0OZmwqpdbkhao*#MW&8%B@$";
        var fps = 30;
        var loop = false;
        var speed = 1.0;
        var noClear = false;
        var color = false;
        var fontSize = 12;
        var fontName = "Consolas";
        var copyToClipboard = false;
        var crf = 26;
        var preset = "veryfast";
        var codec = "libx264";
        string? quality = null;
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
                        throw new ArgumentException($"Invalid width: {args[i]}");
                    break;

                case "--ratio":
                    if (!double.TryParse(NextValue(), out ratio))
                        throw new ArgumentException($"Invalid ratio: {args[i]}");
                    break;

                case "--invert":
                case "-i":
                    invert = true;
                    break;

                case "--depth":
                    if (!int.TryParse(NextValue(), out depth))
                        throw new ArgumentException($"Invalid depth: {args[i]}");
                    break;

                case "--charset":
                    charSet = NextValue();
                    break;

                case "--fps":
                    if (!int.TryParse(NextValue(), out fps))
                        throw new ArgumentException($"Invalid fps: {args[i]}");
                    break;

                case "--output":
                case "-o":
                    output = NextValue();
                    break;

                case "--loop":
                    loop = true;
                    break;

                case "--speed":
                    if (!double.TryParse(NextValue(), out speed))
                        throw new ArgumentException($"Invalid speed: {args[i]}");
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
                        throw new ArgumentException($"Invalid font size: {args[i]}");
                    break;

                case "--font-name":
                case "-fn":
                    fontName = NextValue();
                    break;

                case "--copy":
                case "-c":
                    copyToClipboard = true;
                    break;

                case "--crf":
                    if (!int.TryParse(NextValue(), out crf))
                        throw new ArgumentException($"Invalid crf: {args[i]}");
                    break;

                case "--preset":
                    preset = NextValue();
                    break;

                case "--codec":
                    codec = NextValue();
                    break;

                case "--quality":
                case "-q":
                    quality = NextValue().ToLowerInvariant();
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

                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        var config = new Config
        {
            Input = input, Width = width, Ratio = ratio,
            Invert = invert, Depth = depth, CharSet = charSet,
            Fps = fps, Output = output, Loop = loop, Speed = speed, NoClear = noClear,
            Color = color, FontSize = fontSize, FontName = fontName, CopyToClipboard = copyToClipboard,
            Crf = crf, Preset = preset, Codec = codec, Quality = quality,
            StartTime = startTime, Duration = duration, EndTime = endTime,
        };

        return ApplyQualitySettings(config);
    }

    private static Config ApplyQualitySettings(Config config)
    {
        return config.Quality switch
        {
            "fast" => config with { Codec = "libx264", Crf = 26, Preset = "veryfast" },
            "balanced" => config with { Codec = "libx264", Crf = 28, Preset = "faster" },
            "small" => config with { Codec = "libx265", Crf = 28, Preset = "fast" },
            _ => config
        };
    }
}