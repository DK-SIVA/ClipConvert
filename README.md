# ClipConvert

**Markdown ↔ Rich Text Clipboard Converter for Windows**

A lightweight Windows system tray application that converts clipboard content between Markdown and Rich Text (Word, Outlook, OneNote) using global hotkeys — convert and paste in a single keystroke.

![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![License](https://img.shields.io/badge/License-Proprietary-red)

---

## Features

- **One-step convert & paste** — press a hotkey to convert clipboard content and paste it instantly
- **Markdown → Rich Text** — paste formatted text into Word, Outlook, OneNote with proper styling
- **Rich Text → Markdown** — copy from Word/Outlook and paste clean Markdown into VS Code, Obsidian, etc.
- **Configurable hotkeys** — customize key combinations via the settings dialog
- **System tray app** — runs quietly in the background with minimal resource usage
- **Microsoft HTML cleanup** — strips Word/Outlook-specific bloat (MsoNormal, mso-styles, conditional comments)
- **Inline CSS styling** — headings, code blocks, tables, and blockquotes render correctly in Word/Outlook

## Default Hotkeys

| Hotkey | Action |
|---|---|
| `Ctrl+Shift+M` | Convert clipboard to **Markdown** and paste |
| `Ctrl+Shift+R` | Convert clipboard to **Rich Text** and paste |

Hotkeys are fully configurable via **right-click tray icon → Einstellungen**.

## How It Works

```
Copy text (Ctrl+C) → Press hotkey → Converted text is pasted automatically
```

**Rich Text → Markdown:**
1. Reads CF_HTML from clipboard
2. Strips Microsoft-specific HTML bloat
3. Converts to clean Markdown via ReverseMarkdown
4. Pastes as plain text

**Markdown → Rich Text:**
1. Reads plain text from clipboard
2. Converts to HTML via Markdig with inline CSS styles
3. Writes CF_HTML format to clipboard (recognized by Word/Outlook)
4. Pastes as formatted text

## Installation

### Setup Installer

Download the latest `ClipConvert_Setup_v*.exe` from [Releases](../../releases) and run it.

The installer offers:
- Desktop shortcut (optional)
- Windows autostart (optional)
- Start Menu entries with uninstaller

### Prerequisites

- Windows 10/11 (x64)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0/runtime) — the installer will prompt you if it's missing

## Build from Source

```bash
# Clone
git clone https://github.com/DK-SIVA/ClipConvert.git
cd ClipConvert

# Build
dotnet build src/ClipConvert/ClipConvert.csproj -c Release

# Publish single-file exe
dotnet publish src/ClipConvert/ClipConvert.csproj -c Release -o publish
```

### Build Installer

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php):

```bash
iscc installer/ClipConvert.iss
```

The setup file will be created in `output/`.

## Project Structure

```
ClipConvert/
├── ClipConvert.sln
├── installer/
│   └── ClipConvert.iss          # Inno Setup installer script
├── src/ClipConvert/
│   ├── App.xaml / App.xaml.cs   # WPF entry point, orchestration
│   ├── Core/
│   │   ├── CfHtmlHelper.cs     # CF_HTML header encode/decode
│   │   ├── ClipboardManager.cs # Clipboard read/write
│   │   ├── ConversionEngine.cs # Markdig + ReverseMarkdown conversion
│   │   ├── HotkeyManager.cs    # Global hotkeys via Win32 RegisterHotKey
│   │   └── KeyboardSimulator.cs# Paste simulation (SendKeys)
│   ├── Models/
│   │   └── AppSettings.cs      # Settings model with hotkey bindings
│   ├── Services/
│   │   └── SettingsService.cs   # JSON persistence (%AppData%)
│   └── UI/
│       ├── SettingsWindow.xaml  # Hotkey configuration dialog
│       └── TrayIconManager.cs   # System tray icon + context menu
```

## Technology Stack

| Component | Technology |
|---|---|
| Framework | .NET 8 / C# / WPF + WinForms |
| Markdown → HTML | [Markdig](https://github.com/xoofx/markdig) |
| HTML → Markdown | [ReverseMarkdown](https://github.com/mysticmind/reversemarkdown-net) |
| Global Hotkeys | Win32 `RegisterHotKey` via P/Invoke |
| System Tray | `System.Windows.Forms.NotifyIcon` |
| Installer | [Inno Setup 6](https://jrsoftware.org/isinfo.php) |

## Settings

Settings are stored in `%AppData%\ClipConvert\settings.json` and include:
- Custom hotkey bindings (modifiers + key)

---

© 2026 DeKode. All rights reserved.
