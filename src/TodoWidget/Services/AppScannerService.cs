namespace TodoWidget.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TodoWidget.Models;

/// <summary>
/// Scans system shortcuts, registry entries, and running programs to find installed applications.
/// Supports resolving executables, reading app icons, and processing dropped files.
/// </summary>
public class AppScannerService
{
    private static readonly Lazy<AppScannerService> _lazy = new(() => new AppScannerService());
    public static AppScannerService Instance => _lazy.Value;

    private static readonly HashSet<string> SystemIgnoreProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "todowidget", "dwm", "svchost", "csrss", "smss", "wininit", "services",
        "lsass", "winlogon", "fontdrvhost", "sihost", "taskhostw", "ctfmon", "shellexperiencehost",
        "startmenuexperiencehost", "searchapp", "textinputhost", "system", "idle", "conhost", "runtimebroker"
    };

    /// <summary>
    /// Scans the entire system for installed desktop applications asynchronously.
    /// </summary>
    public async Task<List<BlockedAppInfo>> ScanInstalledAppsAsync()
    {
        return await Task.Run(() =>
        {
            var results = new Dictionary<string, BlockedAppInfo>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // 1. Scan Start Menu Shortcuts (All Users and Current User)
                ScanStartMenuDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), results);
                ScanStartMenuDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Programs), results);

                // 2. Scan Registry for Installed Programs
                ScanRegistryUninstall(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", results);
                ScanRegistryUninstall(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", results);
                ScanRegistryUninstall(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall", results);

                // 3. Scan Running User Processes
                ScanRunningProcesses(results);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppScannerService] Error during app scan: {ex.Message}");
            }

            return results.Values
                .OrderBy(a => a.Name)
                .ToList();
        });
    }

    /// <summary>
    /// Creates a BlockedAppInfo from a file path (either .exe or .lnk shortcut).
    /// </summary>
    public BlockedAppInfo? CreateFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;

        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        string targetExe = filePath;
        string appName = Path.GetFileNameWithoutExtension(filePath);

        if (ext == ".lnk")
        {
            string? resolved = ResolveShortcutTarget(filePath);
            if (!string.IsNullOrEmpty(resolved) && File.Exists(resolved))
            {
                targetExe = resolved;
            }
        }

        if (!targetExe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !File.Exists(targetExe))
            return null;

        string processName = Path.GetFileNameWithoutExtension(targetExe);
        if (SystemIgnoreProcesses.Contains(processName))
            return null;

        // Try getting file description for a cleaner display name
        try
        {
            var vi = FileVersionInfo.GetVersionInfo(targetExe);
            if (!string.IsNullOrWhiteSpace(vi.FileDescription))
            {
                appName = vi.FileDescription.Trim();
            }
        }
        catch
        {
        }

        var app = new BlockedAppInfo
        {
            Name = CleanAppName(appName),
            ProcessName = processName,
            ExecutablePath = targetExe,
            IsEnabled = true,
            IsCustom = true,
            AddedAt = DateTime.Now,
            Icon = TryExtractIcon(targetExe)
        };

        return app;
    }

    /// <summary>
    /// Gets a curated list of common productivity-draining applications.
    /// </summary>
    public List<BlockedAppInfo> GetCommonDistractionPresets()
    {
        var presets = new List<(string Name, string ProcessName)>
        {
            ("Google Chrome", "chrome"),
            ("Microsoft Edge", "msedge"),
            ("Discord", "discord"),
            ("Spotify", "spotify"),
            ("Steam", "steam"),
            ("Epic Games Launcher", "EpicGamesLauncher"),
            ("Telegram", "telegram"),
            ("WhatsApp", "WhatsApp"),
            ("Mozilla Firefox", "firefox"),
            ("Brave Browser", "brave"),
            ("Opera", "opera"),
            ("Battle.net", "Battle.net"),
            ("Riot Client", "RiotClientServices"),
            ("League of Legends", "LeagueClient"),
            ("TikTok", "TikTok"),
            ("Reddit / Social Hub", "Reddit")
        };

        var list = new List<BlockedAppInfo>();
        foreach (var p in presets)
        {
            list.Add(new BlockedAppInfo
            {
                Name = p.Name,
                ProcessName = p.ProcessName,
                ExecutablePath = string.Empty,
                IsEnabled = true,
                IsCustom = false,
                AddedAt = DateTime.Now
            });
        }

        return list;
    }

    private void ScanStartMenuDirectory(string dirPath, Dictionary<string, BlockedAppInfo> results)
    {
        if (string.IsNullOrWhiteSpace(dirPath) || !Directory.Exists(dirPath))
            return;

        try
        {
            var lnkFiles = Directory.GetFiles(dirPath, "*.lnk", SearchOption.AllDirectories);
            foreach (var lnk in lnkFiles)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(lnk);
                    if (fileName.Contains("uninstall", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Contains("help", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Contains("manual", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Contains("readme", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string? target = ResolveShortcutTarget(lnk);
                    if (!string.IsNullOrEmpty(target) && target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(target))
                    {
                        string processName = Path.GetFileNameWithoutExtension(target);
                        if (SystemIgnoreProcesses.Contains(processName) || results.ContainsKey(processName))
                        {
                            continue;
                        }

                        string displayName = fileName;
                        try
                        {
                            var vi = FileVersionInfo.GetVersionInfo(target);
                            if (!string.IsNullOrWhiteSpace(vi.FileDescription))
                            {
                                displayName = vi.FileDescription.Trim();
                            }
                        }
                        catch
                        {
                        }

                        results[processName] = new BlockedAppInfo
                        {
                            Name = CleanAppName(displayName),
                            ProcessName = processName,
                            ExecutablePath = target,
                            IsEnabled = true,
                            AddedAt = DateTime.Now,
                            Icon = TryExtractIcon(target)
                        };
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    private void ScanRegistryUninstall(RegistryKey rootKey, string subKeyPath, Dictionary<string, BlockedAppInfo> results)
    {
        try
        {
            using var key = rootKey.OpenSubKey(subKeyPath);
            if (key == null) return;

            foreach (var subName in key.GetSubKeyNames())
            {
                try
                {
                    using var subKey = key.OpenSubKey(subName);
                    if (subKey == null) continue;

                    string? displayName = subKey.GetValue("DisplayName") as string;
                    string? displayIcon = subKey.GetValue("DisplayIcon") as string;
                    string? installLocation = subKey.GetValue("InstallLocation") as string;

                    if (string.IsNullOrWhiteSpace(displayName)) continue;

                    // Parse displayIcon to find actual exe path
                    string? targetExe = null;
                    if (!string.IsNullOrWhiteSpace(displayIcon))
                    {
                        string cleanIconPath = displayIcon.Split(',')[0].Trim('"').Trim();
                        if (cleanIconPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(cleanIconPath))
                        {
                            targetExe = cleanIconPath;
                        }
                    }

                    if (targetExe == null && !string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(installLocation))
                    {
                        var exes = Directory.GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly);
                        targetExe = exes.FirstOrDefault();
                    }

                    if (!string.IsNullOrEmpty(targetExe) && File.Exists(targetExe))
                    {
                        string processName = Path.GetFileNameWithoutExtension(targetExe);
                        if (!SystemIgnoreProcesses.Contains(processName) && !results.ContainsKey(processName))
                        {
                            results[processName] = new BlockedAppInfo
                            {
                                Name = CleanAppName(displayName),
                                ProcessName = processName,
                                ExecutablePath = targetExe,
                                IsEnabled = true,
                                AddedAt = DateTime.Now,
                                Icon = TryExtractIcon(targetExe)
                            };
                        }
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    private void ScanRunningProcesses(Dictionary<string, BlockedAppInfo> results)
    {
        try
        {
            var processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    string processName = proc.ProcessName;
                    if (SystemIgnoreProcesses.Contains(processName) || results.ContainsKey(processName))
                        continue;

                    // Check if it has a graphical window
                    if (proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrWhiteSpace(proc.MainWindowTitle))
                    {
                        string? exePath = null;
                        try
                        {
                            exePath = proc.MainModule?.FileName;
                        }
                        catch
                        {
                        }

                        results[processName] = new BlockedAppInfo
                        {
                            Name = CleanAppName(proc.MainWindowTitle.Length > 30 ? proc.ProcessName : proc.MainWindowTitle),
                            ProcessName = processName,
                            ExecutablePath = exePath ?? string.Empty,
                            IsEnabled = true,
                            AddedAt = DateTime.Now,
                            Icon = !string.IsNullOrEmpty(exePath) ? TryExtractIcon(exePath) : null
                        };
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    public static ImageSource? TryExtractIcon(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            return null;

        try
        {
            using var sysIcon = Icon.ExtractAssociatedIcon(exePath);
            if (sysIcon != null)
            {
                var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                    sysIcon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                bitmapSource.Freeze();
                return bitmapSource;
            }
        }
        catch
        {
        }

        return null;
    }

    private static string? ResolveShortcutTarget(string shortcutPath)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell != null)
                {
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    string target = shortcut.TargetPath;
                    Marshal.FinalReleaseComObject(shortcut);
                    Marshal.FinalReleaseComObject(shell);
                    return target;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static string CleanAppName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Application";
        string clean = raw.Trim();
        // Remove trailing version numbers or extra noise
        int dashIdx = clean.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIdx > 0 && dashIdx < clean.Length - 3)
        {
            clean = clean.Substring(0, dashIdx).Trim();
        }
        return clean;
    }
}
