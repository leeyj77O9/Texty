using SixLabors.Fonts;
using SixLabors.ImageSharp;
using System.Text;
using Texty.Core.Mode;

namespace Texty.Core.Configuration;

public record TextyConfig
{
    public static readonly TextyConfig Default = new();

    public const int PIXELFORMAT = 4;
    public const string SET = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz0123456789@#S%?*+;:,.";

    public int CharWidth { get; init; }
    public int CharHeight { get; init; }
    public double CharRatio { get; init; } = -1;
    public int Threshold { get; init; } = 128;

    public string Input { get; init; } = string.Empty;
    public int Width { get; init; } = 100;    
    public int Height { get; init; } = 0;

    public bool Invert { get; init; } = false;
    public string CharSet { get; init; } = " .:=*M#@";
    public int Fps { get; init; } = 30;
    public Color BgColor { get; init; } = Color.White;

    public float Blur { get; init; } = 0f;
    public float Contrast { get; init; } = 1f;
    public float Brightness { get; init; } = 1f;
    public float Saturation { get; init; } = 1f;

    public string? Output { get; init; }
    public bool Loop { get; init; } = false;
    public double Speed { get; init; } = 1.0;
    public bool NoClear { get; init; } = false;
    public bool IsColor { get; init; } = false;

    public int FontSize { get; init; } = 12;
    public string FontName { get; init; } = "Consolas";
    public Color FontColor { get; init; } = Color.Black;
    public FontStyle FontStyle { get; init; } = FontStyle.Regular;
    public Font Font { get; init; }

    public bool CopyToClipboard { get; init; } = false;

    public bool IsUrl => Uri.IsWellFormedUriString(Input, UriKind.Absolute);
    public string Extension => (IsUrl ? Path.GetExtension(new Uri(Input).AbsolutePath) : Path.GetExtension(Input)).ToLowerInvariant();

    public bool IsImage => ImageExtensions.Contains(Extension);
    public bool IsVideo => VideoExtensions.Contains(Extension);

    public TextyMode Mode { get; init; } = TextyMode.Default;

    public int Crf { get; init; } = 26;
    public string EncodeSpeed { get; init; } = "veryfast";
    public string Codec { get; init; } = "libx264";
    public TextyQuality Quality { get; init; } = TextyQuality.Balanced;

    public TimeSpan? StartTime { get; init; } = null;
    public TimeSpan? Duration { get; init; } = null;
    public TimeSpan? EndTime { get; init; } = null;

    public Rune[] Runes { get; init; }

    public static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".jfif", ".webp"];
    public static readonly HashSet<string> VideoExtensions = [".mp4", ".avi", ".mov", ".mkv", ".webm", ".gif"];

    public TextyConfig() { Runes = []; Font = null!; } 

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Input)) 
            throw new ArgumentException("Input is required");

        if (Width <= 0) 
            throw new ArgumentException("Width must be > 0");

        if (!IsImage && !IsVideo) 
            throw new ArgumentException($"Unsupported file type: {Extension}");

        if (IsImage && Loop) 
            throw new ArgumentException("Looping is not supported for images");

        if (StartTime.HasValue && StartTime.Value < TimeSpan.Zero)
            throw new ArgumentException("Start time cannot be negative.");

        if (Duration.HasValue && Duration.Value < TimeSpan.Zero)
            throw new ArgumentException("Duration cannot be negative.");

        if (EndTime.HasValue && EndTime.Value < TimeSpan.Zero)
            throw new ArgumentException("End time cannot be negative.");

        if (Duration.HasValue && EndTime.HasValue)
            throw new ArgumentException("--duration and --to cannot be used together.");

        if (EndTime.HasValue)
        {
            var start = StartTime ?? TimeSpan.Zero;

            if (EndTime <= start)
                throw new ArgumentException(
                    "--to must be greater than --start.");
        }
    }

    public Size GetRenderSize() => new(Width, (int)(Height * CharRatio));

    public static TextyConfig FromArgs(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Input argument is required");

        string input = args[0];

        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException($"Input is required");

        if (!File.Exists(input) && !Uri.IsWellFormedUriString(input, UriKind.Absolute))
            throw new ArgumentException($"Input file does not exist: {input}");

        var charRatio = Default.CharRatio;
        var threshold = Default.Threshold;

        var width = Default.Width;
        var height = Default.Height;
        var invert = Default.Invert;
        var charSet = Default.CharSet;
        var fps = Default.Fps;
        var bgColor = Default.BgColor;

        var loop = Default.Loop;
        var speed = Default.Speed;
        var noClear = Default.NoClear;
        var color = Default.IsColor;

        var blur = Default.Blur;
        var contrast = Default.Contrast;
        var brightness = Default.Brightness;
        var saturation = Default.Saturation;

        var fontSize = Default.FontSize;
        var fontName = Default.FontName;
        var fontColor = Default.FontColor;
        var fontStyle = Default.FontStyle;

        var copyToClipboard = Default.CopyToClipboard;

        var mode = Default.Mode;

        var crf = Default.Crf;
        var encodeSpeed = Default.EncodeSpeed;
        var codec = Default.Codec;
        var quality = Default.Quality;

        var output = Default.Output;
        var startTime = Default.StartTime;
        var duration = Default.Duration;
        var endTime = Default.EndTime;

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

                case "--height":
                case "-h":
                    if (!int.TryParse(NextValue(), out height))
                        throw new ArgumentException($"Invalid height: {arg}");
                    break;

                case "--invert":
                case "-i":
                    invert = true;
                    break;

                case "--charset":
                    charSet = NextValue();
                    break;

                case "--charset-file":
                    var charsetFile = NextValue();
                    if (File.Exists(charsetFile))
                        charSet = File.ReadAllText(charsetFile);
                    else if (Uri.IsWellFormedUriString(charsetFile, UriKind.Absolute))
                    {
                        using var client = new HttpClient();
                        charSet = client.GetStringAsync(charsetFile).Result;
                    }
                    else
                        throw new ArgumentException($"Charset file does not exist: {charsetFile}");
                    break;

                case "--char-ratio":
                    if (!double.TryParse(NextValue(), out charRatio))
                        throw new ArgumentException($"Invalid char-ratio: {arg}");
                    break;

                case "--threshold":
                    if (!int.TryParse(NextValue(), out threshold))
                        throw new ArgumentException($"Invalid threshold: {arg}");
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
                    if (!Enum.TryParse(NextValue(), true, out fontStyle))
                        throw new ArgumentException($"Invalid font-style: {arg}");              
                    break;

                case "--blur":
                    if (!float.TryParse(NextValue(), out blur))
                        throw new ArgumentException($"Invalid blur: {arg}");
                    break;

                case "--contrast":
                    if (!float.TryParse(NextValue(), out contrast))
                        throw new ArgumentException($"Invalid contrast: {arg}");
                    break;

                case "--brightness":
                    if (!float.TryParse(NextValue(), out brightness))
                        throw new ArgumentException($"Invalid brightness: {arg}");
                    break;

                case "--saturation":
                    if (!float.TryParse(NextValue(), out saturation))
                        throw new ArgumentException($"Invalid saturation: {arg}");
                    break;

                case "--copy":
                case "-c":
                    copyToClipboard = true;
                    break;

                case "--crf":
                    if (!int.TryParse(NextValue(), out crf))
                        throw new ArgumentException($"Invalid crf: {arg}");
                    break;

                case "--encode-speed":
                case "-es":
                    encodeSpeed = NextValue();
                    break;

                case "--codec":
                    codec = NextValue();
                    break;

                case "--quality":
                case "-q":
                    {
                        var value = NextValue();

                        if (!Enum.TryParse(value, true, out quality))
                            throw new ArgumentException($"Invalid quality: {value}");

                        (codec, crf, encodeSpeed) = quality switch
                        {
                            TextyQuality.UltraFast => ("libx264", 32, "ultrafast"),
                            TextyQuality.Fast => ("libx264", 28, "veryfast"),
                            TextyQuality.Balanced => ("libx264", 23, "fast"),
                            TextyQuality.High => ("libx265", 22, "medium"),
                            TextyQuality.VeryHigh => ("libx265", 20, "slow"),
                            TextyQuality.Max => ("libx265", 18, "veryslow"),
                            TextyQuality.Lossless => ("libx265", 0, "veryslow"),
                            TextyQuality.Small => ("libx265", 28, "slow"),
                            TextyQuality.VerySmall => ("libx265", 32, "veryslow"),

                            _ => throw new ArgumentException($"Invalid quality: {value}")
                        };

                        break;
                    }

                case "--start":
                case "-ss":
                    if (!TimeSpan.TryParse(NextValue(), out var st))
                        throw new ArgumentException($"Invalid start time: {arg}");
                    startTime = st;
                    break;

                case "--duration":
                case "-t":
                    if (!TimeSpan.TryParse(NextValue(), out var du))
                        throw new ArgumentException($"Invalid duration: {arg}");
                    duration = du;  
                    break;

                case "--to":
                    if (!TimeSpan.TryParse(NextValue(), out var et))
                        throw new ArgumentException($"Invalid end time: {arg}");
                    endTime = et;
                    break;

                case "--mode":
                case "-m":
                    if (!Enum.TryParse(NextValue(), true, out mode))
                        throw new ArgumentException($"Invalid mode: {arg}");
                    charSet = mode switch
                    {
                        TextyMode.Shade => ShadeMode.CharSet,
                        _ => charSet
                    };
                    break;

                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        width = width % 2 == 0 ? width : width == 1 ? 2 : width - 1;
        height = height % 2 == 0 ? height : height == 1 ? 2 : height - 1;        

        var font = SystemFonts.CreateFont(fontName, fontSize, fontStyle);

        var (charWidth, charHeight, ratio) = GetCharInfo(font);

        if (charRatio == -1)
            charRatio = ratio;

        var config = new TextyConfig
        {
            Input = input,
            Width = width,
            Height = height,
            Invert = invert,
            CharSet = charSet,
            Fps = fps,
            BgColor = bgColor,
            Output = output,
            Loop = loop,
            Speed = speed,
            Blur = blur,
            Contrast = contrast,
            Brightness = brightness,
            Saturation = saturation,
            NoClear = noClear,
            IsColor = color,
            FontSize = fontSize,
            FontName = fontName,
            FontColor = fontColor,
            FontStyle = fontStyle,
            Font = font,
            CopyToClipboard = copyToClipboard,
            Mode = mode,
            Crf = crf,
            EncodeSpeed = encodeSpeed,
            Codec = codec,
            Quality = quality,
            StartTime = startTime,
            Duration = duration,
            EndTime = endTime,
            Runes = [.. invert ? charSet.EnumerateRunes().Reverse() : charSet.EnumerateRunes()],
            CharWidth = charWidth,
            CharHeight = charHeight,
            CharRatio = charRatio,
            Threshold = threshold,
        };

        return config;
    }

    private static (int Width, int Height, double Ratio) GetCharInfo(Font font)
    {    
        var options = new TextOptions(font);

        var bounds = TextMeasurer.MeasureBounds(SET, options);

        var charWidth = (int)Math.Ceiling(bounds.Width / SET.Length);
        var charHeight = (int)Math.Ceiling(bounds.Height);

        var ratio = (double)charWidth / charHeight;

        return (charWidth, charHeight, ratio);
    }
}