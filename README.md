# Texty

<p align="center">
  <b>High-performance character-based image and video renderer for your terminal</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-6%2B-blue" />
  <img src="https://img.shields.io/badge/platform-Windows%20%7C%20Linux-lightgrey" />
  <img src="https://img.shields.io/badge/license-MIT-green" />
</p>

Texty is a powerful C#-based CLI tool that transforms images and videos into stunning ASCII or ANSI art. Whether you want to watch a video in your terminal in real-time or export a high-quality stylized video file, Texty provides the tools to do it with precision.

---

## Features

- **Image & Video Support:** Render static images or play videos directly in the console.
- **Full Color & Grayscale:** Support for 24-bit ANSI color and custom character sets.
- **Real-time Playback:** Adjustable FPS, playback speed, and looping for videos.
- **Video Encoding:** Export rendered ASCII animations back to video files (MP4/MKV) using FFmpeg-based encoding options.
- **Clipboard & Export:** Instantly copy frames to your clipboard or save them as text files.
- **Cross-Platform:** Built with .NET, optimized for Windows and Linux environments.

---

## Installation

```bash
# Clone the repository
git clone https://github.com/leey0ngjun/Texty.git

# Navigate to the project directory
cd Texty

# Build the project
dotnet build -c Release
````

-----

## Usage

```bash
texty <input> [options]
```

### Examples

```bash
# Render an image with default settings
Texty image.jpg

# Play a video in color with custom speed
Texty video.mp4 --color --speed 2.0

# Export a high-quality ASCII video using H.265 codec
Texty video.mp4 --codec libx265 --crf 24 -o output.mp4

# Render with custom width and copy the first frame to clipboard
Texty input.png -w 200 --copy
```

-----

## Configuration Options

### Rendering

| Option | Shorthand | Default | Description |
| :--- | :--- | :--- | :--- |
| `--width` | `-w` | `100` | Target output width in characters |
| `--ratio` | | `0.45` | Vertical stretch ratio to compensate for font height |
| `--charset` | | | Custom characters used for rendering |
| `--depth` | | `10` | Number of grayscale levels/depth |
| `--invert` | `-i` | | Invert brightness (useful for light/dark themes) |

### Font (for Image/Video Output)

| Option | Shorthand | Default | Description |
| :--- | :--- | :--- | :--- |
| `--font-size`| `-fs` | `12` | Font size for rendered output |
| `--font-name`| `-fn` | `Consolas`| Font family name |

### Video Playback

| Option | Shorthand | Default | Description |
| :--- | :--- | :--- | :--- |
| `--fps` | | `30` | Frames per second |
| `--loop` | | | Loop video playback |
| `--speed` | | `1.0` | Playback speed multiplier |

### Encoding (File Export)

| Option | Default | Description |
| :--- | :--- | :--- |
| `--quality, -q`| `balanced` | Preset quality: `fast`, `balanced`, `small` |
| `--crf` | `26` | Quality constant (lower = better quality) |
| `--preset` | `veryfast` | Encoding speed (ultrafast, superfast, ..., slow) |
| `--codec` | `libx264` | Video codec (`libx264`, `libx265`) |

### Output & Misc

| Option | Shorthand | Description |
| :--- | :--- | :--- |
| `--output` | `-o` | Save the rendered result to a file |
| `--color` | | Enable 24-bit ANSI color output |
| `--no-clear` | | Disable console refresh (print continuously) |
| `--copy` | `-c` | Copy the first frame to clipboard |

-----

## Supported Formats

  - **Images:** `.jpg`, `.png`, `.bmp`, `.gif`, `.webp`
  - **Videos:** `.mp4`, `.avi`, `.mov`, `.mkv`, `.webm`

-----

## Support

If you find this project interesting, please consider giving it a \!

*License: [MIT](https://www.google.com/search?q=LICENSE)*
