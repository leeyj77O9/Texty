namespace Texty;

public class Config
{
    public string Input { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double Ratio { get; set; }
    public bool Invert { get; set; }
    public int Depth { get; set; }
    public string CharSet { get; set; }
    public int Fps { get; set; }
    public string? Output { get; set; }
    public bool Loop { get; set; }
    public double Speed { get; set; }
    public bool NoClear { get; set; }
    public bool Color { get; set; }
    public bool Background { get; set; }
    public int FontSize { get; set; }
    public string FontName { get; set; }
    public bool CopyToClipboard { get; set; }

    public bool IsUrl { get; set; }
    public string Extension { get; set; }
    public bool IsImage { get; set; }
    public bool IsVideo { get; set; }

    private HashSet<string> imageExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".jfif", ".webp"];
    private HashSet<string> videoExtensions = [".mp4", ".avi", ".mov", ".mkv", ".webm"];

    public Config(Config otherConfig) : this(
        otherConfig.Input, otherConfig.Width, otherConfig.Ratio, otherConfig.Invert, otherConfig.Depth,
        otherConfig.CharSet, otherConfig.Fps, otherConfig.Output, otherConfig.Loop, otherConfig.Speed,
        otherConfig.NoClear, otherConfig.Color, otherConfig.Background, otherConfig.FontSize,
        otherConfig.FontName, otherConfig.CopyToClipboard)
    {
        Height = otherConfig.Height;
    }

    public Config(string input, int width, double ratio, bool invert, int depth, string charSet, int fps,
        string? output, bool loop, double speed, bool noClear, bool color, bool background, int fontSize,
        string fontName, bool copyToClipboard)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input is required");

        if (width <= 0)
            throw new ArgumentException("Width must be > 0");

        if (ratio <= 0)
            throw new ArgumentException("Ratio must be > 0");

        if (depth <= 0)
            throw new ArgumentException("Depth must be > 0");

        if (string.IsNullOrWhiteSpace(charSet) || charSet.Length < 2)
            throw new ArgumentException("Charset must have at least 2 characters");

        if (fontSize <= 0)
            throw new ArgumentException("FontSize must be > 0");

        Input = input;
        Width = width;
        Ratio = ratio;

        Invert = invert;
        Depth = depth;
        CharSet = charSet;
        Fps = fps <= 0 ? 1 : fps;
        FontSize = fontSize;
        FontName = string.IsNullOrWhiteSpace(fontName) ? "Consolas" : fontName;
        CopyToClipboard = copyToClipboard;

        Output = string.IsNullOrWhiteSpace(output) ? null : output;

        IsUrl = Uri.IsWellFormedUriString(input, UriKind.Absolute);

        Extension = IsUrl
            ? Path.GetExtension(new Uri(input).AbsolutePath)
            : Path.GetExtension(input);

        var ext = Extension.ToLowerInvariant() ?? "";

        IsImage = imageExtensions.Contains(ext);
        IsVideo = videoExtensions.Contains(ext);

        Loop = loop;
        Speed = speed <= 0 ? 1.0 : speed;
        NoClear = noClear;
        Color = color;
        Background = background;

        if (!IsImage && !IsVideo)
            throw new ArgumentException($"Unsupported file type: {ext}");
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
        var background = false;
        var fontSize = 12;
        var fontName = "Consolas";
        var copyToClipboard = false;
        string? output = null;

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

                case "--bg":
                    background = true;
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

                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        return new Config(
            input, width, ratio, invert, depth, charSet, fps, output, loop, speed, noClear,
            color, background, fontSize, fontName, copyToClipboard
        );
    }
}