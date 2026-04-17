# Texty

<p align="center">
  <b>Render images and videos as ASCII / ANSI art — right in your terminal</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-blue" />
  <img src="https://img.shields.io/badge/platform-Windows%20%7C%20Linux-lightgrey" />
  <img src="https://img.shields.io/badge/license-MIT-green" />
</p>

---

## ✨ Features

* ⚡ High-performance pixel-to-character rendering
* 🎬 Real-time video playback in terminal
* 🌈 24-bit ANSI color support
* 🔤 Custom charset & depth control
* 📦 Export to video using FFmpeg
* 🖥 Cross-platform (.NET)

---

## 📸 Preview

### GIF (Grayscale)
<img src="./Core/assets/c.gif" width="400">

### GIF (Color)
<img src="./Core/assets/a.gif" width="400">

---

## 🚀 Quick Start

```bash
texty <input> [options]
```

### Examples

```bash
# Render image
texty image.jpg

# Play video with color
texty video.mp4 --color

# Speed up playback
texty video.mp4 --speed 2

# Export to video file
texty video.mp4 -o output.mp4

# Custom encoding
texty video.mp4 --codec libx265 --crf 24
```

---

## ⚙️ Installation

```bash
git clone https://github.com/leeyj77O9/Texty.git
cd Texty
dotnet build -c Release
```

---

## 🧠 Usage

```bash
texty <input> [options]
```

### Input

* Image files (`.jpg`, `.png`, ...)
* Video files (`.mp4`, `.webm`, ...)
* URLs

---

## 🛠 Options

| Option    | Description                 |
| --------- | --------------------------- |
| `--color` | Enable ANSI color rendering |
| `--speed` | Playback speed multiplier   |
| `-o`      | Output file path            |
| `--codec` | FFmpeg codec (e.g. libx265) |
| `--crf`   | Quality (lower = better)    |

---

## 📦 Requirements

* .NET 10 SDK
* FFmpeg *(required for video export)*

---

## 🎯 Design Philosophy

* **CLI-first** → no UI, just fast terminal workflows
* **Performance-focused** → optimized rendering pipeline
* **Flexible** → customizable output
* **Minimal & consistent** → predictable behavior

---

## 📄 License

MIT License

---

## ⭐ Support

If you find this project useful, consider giving it a star on GitHub!
