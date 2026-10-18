# To-Do

A clean, ultra-lightweight desktop to-do list widget and task organizer for Windows.

I built this app with a single goal in mind: killing procrastination. I often found myself getting distracted on my PC and completely forgetting what I actually sat down to do. This app sits on your desktop, keeps your tasks right in front of you, and reminds you when things are due so you stay on track.

No Chromium, no Electron, and no background memory hogging. It is built natively with C# and WPF on .NET 8.

---

## Performance & System Impact

Unlike modern productivity tools that consume hundreds of megabytes of RAM running headless browsers, To-Do is written in pure C# WPF. It runs quietly in the background with virtually zero impact on your system.

![Resource Footprint](docs/images/performance.png)

- Idle CPU Usage: 0%
- Memory Footprint: ~27.8 MB RAM
- Zero Background Telemetry or Bloat

---

## What It Does

- Desktop Widget: A compact widget that lives right on your desktop. You can pin it directly behind all open windows like interactive wallpaper, or let it float comfortably on top.
- Interactive Desktop Pinning: Click, check tasks, and play voice notes directly from your desktop even while pinned.
- Recurring Schedules: Set up tasks that repeat on chosen days of the week with custom start and end times in 12-hour AM/PM format.
- Voice Notes: Record and attach audio memos to any task. Listen to them right from the dashboard or directly from the desktop widget.
- Widget Appearance & Themes: Customize widget background colors, opacity, corner radius, fonts, and dimensions to blend into your desktop wallpaper.
- Configurable Animations: Smooth transitions and visual feedback that can be enabled or disabled with a single toggle in Settings.
- Smart Reminders: Native Windows notifications alert you before tasks are due, with one-click snooze and completion actions.
- Daily Streaks: Complete all scheduled tasks before midnight to build your daily streak.
- In-App Auto Updates: Seamlessly check for new GitHub releases and install updates directly from within the app.

---

## How to Use It

1. Quick Adding Tasks
   - Type your task title directly into the input bar at the bottom of the widget and press Enter.
   - For recurring schedules, AM/PM times, priorities, or voice notes, open the dashboard and click "+ New Task".

2. Recurring Tasks & Time Entry
   - In the task editor, check "Recurring Schedule" to pick days (e.g. Mon / Wed / Fri).
   - Use the dropdown or type your start and end times in standard format (e.g. 9:00 AM, 2:30 PM).

3. Recording Voice Notes
   - While creating or editing a task, click "Record Voice Note" to speak your notes.
   - Preview your audio note and save. Play it back directly from the widget or dashboard.

4. Widget Placement & Docking
   - Drag the widget anywhere by clicking and holding the top bar.
   - Open Settings to dock the widget to preset corners (Top-Right, Top-Left, Bottom-Right, Bottom-Left, Center-Right) or customize its dimensions.
   - Choose between "Float Always on Top" and "Pin Directly to Desktop".

5. Animations & Appearance
   - In Settings under "System & Startup", toggle smooth UI animations on or off according to your preference.
   - Adjust widget background colors, opacity, and typography under "Widget Appearance".

6. In-App Updates
   - Open Settings from the gear icon.
   - Under "App Updates & Version", click "Check for Updates" to install the latest release automatically.

7. System Tray
   - Closing the dashboard minimizes the app to your Windows system tray with its custom icon.
   - Right-click the tray icon to quickly reopen the dashboard, toggle widget visibility, or exit.

---

## Installation

### Option 1: Standalone Installer (Recommended)
1. Head over to the Releases section on GitHub.
2. Download `ToDoSetup-3.0.0.exe`.
3. Run the installer and follow the on-screen steps. It creates desktop shortcuts, start menu entries, and registers the system tray companion.

### Option 2: Building from Source
Requirements:
- Windows 10 (Version 2004 / Build 19041 or higher) or Windows 11
- .NET 8.0 SDK

Steps:
1. Clone the repository:
   ```bash
   git clone https://github.com/Kartik-181123/To-Do-App.git
   cd To-Do-App
   ```

2. Build and run:
   ```bash
   dotnet run --project src/TodoWidget/TodoWidget.csproj
   ```

3. Or publish a self-contained release build:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\build.ps1
   ```

---

## About the Author

Hey, I am Kartik. I built this app to solve my own procrastination habits while pair-programming and learning desktop application development with C#. If this app helps you stay productive, feel free to star the repository or report feedback in the Issues tab.

GitHub: https://github.com/Kartik-048
