namespace TodoWidget.Services;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using Microsoft.Win32;
public static class StartupManager
{
    private const string RunRegistrySubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppKeyName = "TodoWidget";
    public static bool SetStartupEnabled(bool enabled)
    {
        try
        {
            using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RunRegistrySubKey, writable: true);
            if (runKey == null)
            {
                Debug.WriteLine("[StartupManager] Failed to open HKCU Run key for write access.");
                return false;
            }

            if (enabled)
            {
                string? executablePath = GetExecutablePath();
                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    Debug.WriteLine("[StartupManager] Could not determine application executable path.");
                    return false;
                }
                string commandLine = $"\"{executablePath}\"";
                runKey.SetValue(AppKeyName, commandLine, RegistryValueKind.String);
                Debug.WriteLine($"[StartupManager] Enabled startup in registry: {commandLine}");
            }
            else
            {
                runKey.DeleteValue(AppKeyName, throwOnMissingValue: false);
                Debug.WriteLine("[StartupManager] Removed startup registry entry.");
            }

            return true;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            Debug.WriteLine($"[StartupManager] Registry access error while setting startup enabled={enabled}: {ex.Message}");
            return false;
        }
    }
    public static bool IsStartupEnabled()
    {
        try
        {
            using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RunRegistrySubKey, writable: false);
            if (runKey == null)
            {
                return false;
            }

            object? value = runKey.GetValue(AppKeyName);
            return value != null;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            Debug.WriteLine($"[StartupManager] Registry access error while checking startup status: {ex.Message}");
            return false;
        }
    }
    public static string? GetExecutablePath()
    {
        string? processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            return processPath;
        }
        string? assemblyLocation = Assembly.GetEntryAssembly()?.Location
            ?? Assembly.GetExecutingAssembly().Location;

        if (!string.IsNullOrWhiteSpace(assemblyLocation))
        {
            if (assemblyLocation.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                string candidateExe = Path.ChangeExtension(assemblyLocation, ".exe");
                if (File.Exists(candidateExe))
                {
                    return candidateExe;
                }
            }

            return assemblyLocation;
        }

        return null;
    }
}
