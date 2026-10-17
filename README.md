# To-Do

A lightweight desktop to-do list widget and task organizer for Windows.

I built this app with a single goal in mind: killing procrastination. I often found myself getting distracted on my PC and completely forgetting what I actually sat down to do. This app sits on your desktop, keeps your tasks right in front of you, and reminds you when things are due so you stay on track.

No Chromium, no Electron, and no background memory hogging. It is built natively with C# and WPF on .NET 8.
Light Weight

---

## What It Does

- Desktop Widget: A compact square widget that lives right on your desktop. You can set it to stay pinned behind all open windows like wallpaper, or float on top of everything.
- Voice Notes (New in 2.0.0 Beta): Attach audio memos directly to any task using Windows native sound recording. Listen to them right from the dashboard or directly from the desktop widget.
- Task Management: Create one-time tasks or recurring weekly schedules (like study sessions on Mon/Wed/Fri or gym on Tue/Thu/Sat).
- Smart Reminders: Native Windows notifications send alerts before tasks are due. You can snooze a reminder once for up to an hour if you are in the middle of something.
- Daily Streaks: Completing all your scheduled tasks before the day ends increases your streak counter. If a task is missed and left incomplete, the streak resets.
- Low Resource Usage: Idles at near zero CPU and uses minimal RAM while running silently in the system tray.

---

## How to Use It

1. Quick Adding Tasks
   - Type your task title directly into the input bar at the bottom of the widget and press Enter.
   - For recurring tasks, specific due times, or priorities, open the dashboard and click "+ New Task".

2. Recording Voice Notes
   - While creating or editing a task in the dashboard, click the Record Voice Note button to capture your thoughts.
   - Preview or pause your recording, then save the task.
   - Play the voice note anytime directly from the task list in the dashboard or via the play button on the desktop widget.

3. Completing a Task
   - Click the checkmark box next to any task.
   - A quick confirmation will ask if the task is done. Confirming crosses the task out and saves your progress.

4. Widget Placement
   - Drag the widget anywhere by clicking and holding the top bar.
   - Open Settings from the gear icon to snap the widget to preset corners (Top-Right, Top-Left, Bottom-Right, Bottom-Left, Center-Right) or adjust its dimensions.

5. Background & System Tray
   - Closing the dashboard window does not close the app; it minimizes to your Windows system tray.
   - Right-click the tray icon at any time to reopen the dashboard, toggle widget visibility, or exit completely.

---

## Installation

### Option 1: Standalone Installer (Recommended)
1. Head over to the Releases section on GitHub.
2. Download `ToDoSetup-2.0.0.exe`.
3. Run the installer and follow the on-screen steps. It sets up desktop shortcuts, start menu entries, and an optional startup launch.

### Option 2: Building from Source
Requirements:
- Windows 10 (Version 2004 / Build 19041 or higher) or Windows 11
- .NET 8.0 SDK

Steps:
1. Clone the repository:
   git clone https://github.com/Kartik-181123/To-Do-App.git
   cd To-Do-App

2. Build and run:
   dotnet run --project src/TodoWidget/TodoWidget.csproj

3. Or publish a self-contained release build:
   powershell -ExecutionPolicy Bypass -File .\build.ps1 -Clean

---

## About the Author

Hey, I am Kartik. I built this app to solve my own procrastination habits while pair-programming and learning desktop application development with C#. If this app helps you stay productive, feel free to star the repository or report any feedback in the Issues tab.

GitHub: https://github.com/Kartik-048
