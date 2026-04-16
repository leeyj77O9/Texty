# Texty

<p align="center">
  <b>Render images and videos as ASCII/ANSI art — directly in your terminal</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-blue" />
  <img src="https://img.shields.io/badge/platform-Windows%20%7C%20Linux-lightgrey" />
  <img src="https://img.shields.io/badge/license-MIT-green" />
</p>

---

## Overview

Texty is a high-performance CLI tool that transforms images and videos into character-based art.

It supports real-time terminal playback and exporting rendered output to video files.

---

## Quick Start

```
texty <input> [options]
```

### Examples

```
texty image.jpg
texty video.mp4 --color
texty video.mp4 --speed 2
texty video.mp4 -o output.mp4
texty video.mp4 --codec libx265 --crf 24
```

---

## Features

- Fast pixel-to-character rendering pipeline
- Real-time video playback in terminal
- 24-bit ANSI color support
- Custom charset and depth control
- FFmpeg-based video encoding
- Cross-platform support (.NET)

---

## Design Philosophy

- CLI-first
- Performance-focused
- Flexible rendering
- Clean and consistent output

---

## Installation

```
git clone https://github.com/leeyj77O9/Texty.git
cd Texty
dotnet build -c Release
```

---

## Command

```
texty <input> [options]
```

---

## Notes

- Supports file paths and URLs
- Works with common image and video formats
- Requires FFmpeg for video encoding

---

## Support

If you find this project useful, consider giving it a star.
