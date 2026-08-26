# ⚡ Quick Access Hub

**Quick Access Hub** is a lightweight, fast, modern Windows desktop launcher application built using **C# .NET 8 WPF** and **SQLite**.

It runs silently in the background in your Windows System Tray and pops up anywhere on screen using a configurable global keyboard shortcut (default: `Ctrl + Space`).

---

## ✨ Features

- ⚡ **Global Keyboard Shortcut (`Ctrl + Space`)**: Press your hotkey anywhere—inside Chrome, VS Code, File Explorer, or games—to bring up Quick Access Hub instantly.
- ⚙ **Configurable Hotkey Recorder**: Custom shortcut recorder in Settings allows you to bind your preferred combination (`Alt + Space`, `Ctrl + Shift + Q`, etc.) with conflict validation.
- 📁 **Files & Folders (Zero Disk Copying)**: Add files and folders by path reference only. **No files are copied or duplicated**. Original files remain in their locations.
- 📤 **Drag & Drop (IN & OUT)**:
  - **Drag IN**: Drop any file, folder, or URL into Quick Access Hub to add it.
  - **Drag OUT**: Click and drag any item OUT of Quick Access Hub directly onto your Desktop, into File Explorer, Chrome, VS Code, or email.
- 🔗 **Web Links & 1-Click Copy**: Save URLs with display names. Click **▶ Open** to launch in your browser or **📋 Copy Link** to copy the exact URL to your clipboard.
- 🔍 **Instant Live Search**: Search across saved files, folders, and web links by name, path, or URL in real-time.
- 🏷 **Category Filters**: Filter items by General, Projects, College, Work, Websites, or custom categories.
- 🛡 **Missing File Grace Handling**: Displays `⚠ Missing` badges if an original file or folder is moved or deleted on disk without crashing the app.
- 📌 **System Tray & Windows Autostart**: Minimizes quietly to the system tray on startup (`HKCU Run` registry key configurable in settings).
- 🎨 **Window Moving & Resizing**: Drag the launcher anywhere on your screen via the top header bar and resize freely.

---

## 🖥 UI Preview

```text
┌──────────────────────────────────────────────────────────────┐
│ ⚡ QUICK ACCESS HUB                ✋ Hold & drag to move  ✕ │
├──────────────────────────────────────────────────────────────┤
│ 🔍 Search files, folders, or web links...       [ + Add ] [ ⚙ ]│
├──────────────────────────────────────────────────────────────┤
│ [ All ] [ General ] [ Projects ] [ Work ] [ Websites ]       │
├──────────────────────────────────────────────────────────────┤
│ FILES                                                        │
│ 📄 model.py            C:\Projects\model.py   ▶ Open 📍 Location│
│                                                              │
│ FOLDERS                                                      │
│ 📁 AI Projects         C:\Users\User\AI       ▶ Open 📍 Location│
│                                                              │
│ LINKS                                                        │
│ 🔗 GitHub              https://github.com     ▶ Open 📋 Copy     │
├──────────────────────────────────────────────────────────────┤
│ 📥 Drag items HERE to add  │  📤 Drag items OUT to Explorer  │
└──────────────────────────────────────────────────────────────┘
```

---

## 🚀 Installation

1. Download the latest installer executable **`QuickAccessHub-Setup.exe`**.
2. Run `QuickAccessHub-Setup.exe`.
3. Quick Access Hub installs into `%LocalAppData%\Programs\QuickAccessHub` **without requiring Administrator privileges**.
4. Press `Ctrl + Space` anytime to launch!

---

## 🛠 Building from Source

### Prerequisites
- Windows 10/11
- .NET 8.0 SDK
- Inno Setup 6 (optional, for compiling `QuickAccessHub-Setup.exe`)

### Steps

```powershell
# Clone the repository
git clone https://github.com/your-username/QuickAccessHub.git
cd QuickAccessHub

# Build & Publish the app and compile setup installer
.\build.ps1
```

The published executable will be generated at:
`QuickAccessHub\bin\Release\net8.0-windows\win-x64\publish\QuickAccessHub.exe`

The compiled setup installer will be generated at:
`installer_output\QuickAccessHub-Setup.exe`

---

## 📁 Project Architecture

```text
QuickAccessHub/
├── QuickAccessHub.csproj                # .NET 8 WPF Project File
├── App.xaml / App.xaml.cs                # Entry point, Mutex single-instance, System Tray
├── MainWindow.xaml / MainWindow.xaml.cs  # Launcher UI, Search, Drag IN/OUT, Hotkeys
├── SettingsWindow.xaml / .cs             # Settings UI, Hotkey Recorder, Autostart toggle
├── AddEditItemDialog.xaml / .cs          # Add/Edit item dialog
├── Models/
│   ├── QuickItem.cs                      # File / Folder / URL Data Model
│   ├── Category.cs                       # Category Model
│   └── HotkeyConfig.cs                   # Hotkey Configuration Model
├── Services/
│   ├── DatabaseService.cs                # SQLite storage (%LOCALAPPDATA%\QuickAccessHub\quickaccess.db)
│   ├── HotkeyManager.cs                  # Win32 RegisterHotKey API & WM_HOTKEY hook
│   ├── StartupManager.cs                 # Registry HKCU Run key for autostart
│   └── ItemExecutionService.cs          # Execution, Open Location, Clipboard handler
├── installer/
│   └── setup.iss                         # Inno Setup compilation script
└── build.ps1                             # PowerShell build & packaging automation
```

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.
