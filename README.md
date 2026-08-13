# Stellaris Mod Checker

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![AvaloniaUI](https://img.shields.io/badge/UI-Avalonia-purple)](https://avaloniaui.net/)
[![Velopack](https://img.shields.io/badge/Updates-Velopack-blue)](https://velopack.io)

**Stellaris Mod Checker** is a fast, lightweight, and cross-platform desktop application designed to inspect your Paradox Interactive Stellaris playsets. It automatically detects missing sub-dependencies and required mods on the Steam Workshop, ensuring your game runs smoothly without surprise crashes or missing content.

---

## Key Features

- **Automatic Playset Detection**: Instantly reads your local Paradox Launcher SQLite database (`launcher-v2.sqlite`) on Windows, Linux, **macOS IS NOT SUPPORTED CURRENTLY**.
- **Deep Dependency Cascading**: Scans every mod in your active playset and recursively resolves all required dependencies from the Steam Workshop.
- **Local SQLite Caching**: Caches previously scanned mod dependencies locally for instantaneous load times and zero Steam API rate-limiting issues.
- **Remote Database Sync**: Automatically syncs with a database maintained by myself on startup to download known mod dependency trees offline.
- **Multi-language Support**: Real-time language switching (English / French) directly within the UI.
- **Auto-Updates**: Powered by **Velopack** for seamless background auto-updates directly from GitHub Releases.
- **Steam Workshop Integration**: Quick right-click shortcuts to open any missing or installed mod directly in Steam.

---

## Screenshots

![stellaris mod checker app](https://raw.githubusercontent.com/cyrizon/projects-pictures/main/stellaris-mod-checker/1.png)

---

## Supported Platforms

Stellaris Mod Checker natively detects game configurations on:

| Operating System | Default Stellaris Path |
| :--- | :--- |
| **Windows** | `%USERPROFILE%\Documents\Paradox Interactive\Stellaris` |
| **Linux** | `~/.local/share/Paradox Interactive/Stellaris` |
| **macOS** | `~/Library/Application Support/Paradox Interactive/Stellaris` |

---

## Installation & Downloads

### Download Binary
Grab the latest installer or portable executable for your operating system from the [**Releases Page**](https://github.com/cyrizon/StellarisModChecker/releases).

### Building from Source

**Prerequisites:**
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or higher.

# 1. Clone the repository
```bash
git clone [https://github.com/cyrizon/StellarisModChecker.git](https://github.com/cyrizon/StellarisModChecker.git)
cd StellarisModChecker
```

# 2. Build the project
```
dotnet build -c Release
```

# 3. Run the application
```
dotnet run --project StellarisModChecker.csproj
```

## Architecture & Tech Stack

- Framework: .NET 9.0 / C# 13

- UI Toolkit: Avalonia UI 11 (MVVM Pattern with CommunityToolkit.Mvvm)

- Logging: Serilog (File rotation & Console sinks)

- Installer & Updates: Velopack

- Data Storage: Microsoft.Data.Sqlite

## Licence

Distributed under the **MIT Licence**. See `LICENCE` for more information.
