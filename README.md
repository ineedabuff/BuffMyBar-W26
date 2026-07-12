<div align="center">

<img src="Assets/logo.png" width="128" alt="BuffMyBar Logo">

# BuffMyBar

### A native Windows desktop bar inspired by the flexibility of Linux desktops.

Lightweight • Native WPF • Multi-monitor • Windows 11 Integration • Open Source

![Platform](https://img.shields.io/badge/Platform-Windows%2011-0078D4?style=for-the-badge)
![Framework](https://img.shields.io/badge/.NET-8-512BD4?style=for-the-badge)
![UI](https://img.shields.io/badge/WPF-Native-5C2D91?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-success?style=for-the-badge)

---

<img src="Assets/screenshots/desktop-dark.png" alt="BuffMyBar Screenshot">

</div>

---

# What is BuffMyBar?

BuffMyBar is a **native Windows desktop bar** designed for users who love the flexibility of Linux desktop environments while keeping the performance and compatibility of Windows.

It is **not a Waybar clone**.

Instead, BuffMyBar follows the same philosophy:

- Native performance
- Minimal memory usage
- Beautiful animations
- Highly customizable
- Windows-first integration

Everything is built using native WPF and Windows APIs.

No Electron.

No Chromium.

No web technologies.

---

# Features

## Desktop

- Native Windows desktop bar
- Multi-monitor support
- Automatic DPI awareness
- Auto-hide support
- Fullscreen friendly

---

## Widgets

- Clock
- Date
- Weather
- Audio Visualizer
- Network
- Media
- Bluetooth
- Battery
- Volume
- OBS Integration

---

## Windows Integration

- Windows 11 Theme Synchronization
- Light / Dark Mode
- Accent Color Detection
- Acrylic / Mica compatible
- Dynamic monitor detection

---

## Visuals

- Native animations
- Buff Glitch animations
- Smooth fades
- Modern flyouts
- Cyberpunk inspired design

---

# Why BuffMyBar?

Linux users have access to incredible desktop bars such as:

- Waybar
- Polybar
- KDE Panels

Windows deserves the same level of customization.

BuffMyBar aims to become that desktop bar.

Not by copying Linux…

…but by embracing native Windows technologies.

---

# Philosophy

BuffMyBar follows five principles:

- Native first
- Lightweight
- Beautiful
- Fast
- Open Source

Every feature should respect these principles.

---

# Installation

## Download

Download the latest release from GitHub:

https://github.com/ineedabuff/BuffMyBar-W26/releases

---

## Build from source

```powershell
git clone https://github.com/ineedabuff/BuffMyBar-W26.git

cd BuffMyBar-W26

dotnet build

dotnet run --project BuffBar
```

---

# Architecture

BuffMyBar is built around independent services.

```text
┌─────────────────────────────────────────────┐
│                BuffMyBar                    │
├─────────────────────────────────────────────┤
│                 Widgets                     │
│ Weather │ Volume │ Network │ OBS │ Battery  │
├─────────────────────────────────────────────┤
│            Widget Scheduler                 │
├─────────────────────────────────────────────┤
│          Animation Framework                │
│ Glitch │ Fade │ Spawn │ Scan │ Typewriter   │
├─────────────────────────────────────────────┤
│          Windows Theme Sync                 │
│ Light / Dark │ Accent │ Auto-follow         │
├─────────────────────────────────────────────┤
│        Windows 11 / WPF / .NET 8            │
└─────────────────────────────────────────────┘
```

This architecture keeps widgets simple while allowing the framework to evolve independently.

---

# Gallery

## Dark Theme

![Dark Theme](Assets/screenshots/desktop-dark.png)

---

## Windows Theme Synchronization

![Windows Theme](Assets/screenshots/windows-theme.png)

---

## Buff Accent Monitor

![Buff Accent](Assets/screenshots/buff-monitor.png)

---

## Audio Visualizer

![Visualizer](Assets/screenshots/visualizer.png)

---

## OBS Integration

![OBS](Assets/screenshots/obs.png)

---

# Roadmap

## v0.8

- Native Windows Theme Synchronization
- Buff Animation Framework
- Theme Manager

---

## v0.9

- Flyout Framework
- Windows Notifications
- Improved Visualizer

---

## v1.0

- Plugin API
- Theme Packs
- Auto Update
- Settings Application
- Winget Distribution

---

## Future

- Spotify Widget
- Discord Widget
- Steam Widget
- Crossout Widget
- System Monitor
- Workspace Support
- Community Themes

---

# Performance Goals

BuffMyBar is designed to remain lightweight.

Target specifications:

| Resource | Goal |
|-----------|------|
| RAM | < 80 MB |
| CPU (Idle) | < 0.5 % |
| GPU | Minimal |
| Startup | < 1 second |

---

# Contributing

Contributions are welcome.

Before opening a Pull Request:

- Follow the coding style.
- Keep widgets independent.
- Avoid unnecessary dependencies.
- Prefer native Windows APIs.
- Keep memory usage low.

See:

```
CONTRIBUTING.md
```

---

# Bug Reports

Please include:

- Windows version
- BuffMyBar version
- Screenshots
- Logs
- Reproduction steps

GitHub Issues are the preferred way to report bugs.

---

# FAQ

## Is BuffMyBar a Waybar port?

No.

BuffMyBar is a native Windows application inspired by the flexibility of Linux desktop environments.

---

## Does BuffMyBar use Electron?

No.

Everything is written in native WPF using .NET 8.

---

## Does BuffMyBar replace the Windows taskbar?

No.

It complements Windows while integrating naturally with it.

---

## Does BuffMyBar support multiple monitors?

Yes.

Each monitor gets its own bar, and they all follow the Windows theme.

---

## Will Windows updates break BuffMyBar?

The goal is to rely on documented Windows APIs to minimize compatibility issues.

---

# License

BuffMyBar is released under the MIT License.

See:

```
LICENSE
```

---

# Acknowledgements

Special thanks to the Linux desktop community for years of inspiration.

Projects that influenced BuffMyBar include:

- KDE Plasma
- Waybar
- Polybar
- YASB
- Cava

BuffMyBar is not a clone of these projects.

It is a Windows-native implementation inspired by the same philosophy.

---

<div align="center">

Made with ❤️ in Québec 🇨🇦

**BuffMyBar — The desktop bar Windows should have shipped with.**

</div>
