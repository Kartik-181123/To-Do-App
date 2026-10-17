namespace TodoWidget.Services;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using TodoWidget.Helpers;

public class AudioService : ObservableObject
{
    private static readonly Lazy<AudioService> _lazy = new(() => new AudioService());
    public static AudioService Instance => _lazy.Value;

    [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern int mciSendString(string command, StringBuilder? returnString, int returnLength, IntPtr hwndCallback);

    private readonly MediaPlayer _mediaPlayer = new();
    private readonly DispatcherTimer _recordingTimer;
    private readonly string _voiceNotesDirectory;

    private bool _isRecording;
    private int _recordingDurationSeconds;
    private bool _isPlaying;
    private string? _currentPlayingPath;

    public string VoiceNotesDirectory => _voiceNotesDirectory;

    public bool IsRecording
    {
        get => _isRecording;
        private set => SetProperty(ref _isRecording, value);
    }

    public int RecordingDurationSeconds
    {
        get => _recordingDurationSeconds;
        private set
        {
            if (SetProperty(ref _recordingDurationSeconds, value))
            {
                OnPropertyChanged(nameof(RecordingDurationText));
            }
        }
    }

    public string RecordingDurationText =>
        $"{RecordingDurationSeconds / 60:D2}:{RecordingDurationSeconds % 60:D2}";

    public bool IsPlaying
    {
        get => _isPlaying;
        private set => SetProperty(ref _isPlaying, value);
    }

    public string? CurrentPlayingPath
    {
        get => _currentPlayingPath;
        private set
        {
            if (SetProperty(ref _currentPlayingPath, value))
            {
                OnPropertyChanged(nameof(IsPlaying));
            }
        }
    }

    public event Action? PlaybackStateChanged;

    private AudioService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _voiceNotesDirectory = Path.Combine(localAppData, "TodoWidget", "VoiceNotes");

        if (!Directory.Exists(_voiceNotesDirectory))
        {
            Directory.CreateDirectory(_voiceNotesDirectory);
        }

        _recordingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _recordingTimer.Tick += (s, e) =>
        {
            RecordingDurationSeconds++;
        };

        _mediaPlayer.MediaEnded += (s, e) =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsPlaying = false;
                CurrentPlayingPath = null;
                PlaybackStateChanged?.Invoke();
            });
        };

        _mediaPlayer.MediaFailed += (s, e) =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsPlaying = false;
                CurrentPlayingPath = null;
                PlaybackStateChanged?.Invoke();
            });
        };
    }

    public bool StartRecording()
    {
        try
        {
            Stop();

            mciSendString("close to_do_rec", null, 0, IntPtr.Zero);

            int result = mciSendString("open new type waveaudio alias to_do_rec", null, 0, IntPtr.Zero);
            if (result != 0)
            {
                Debug.WriteLine($"[AudioService] Error opening waveaudio: {result}");
                return false;
            }

            mciSendString("set to_do_rec time format ms bitspersample 16 channels 1 samplespersec 22050 bytespersec 44100 alignment 2", null, 0, IntPtr.Zero);

            result = mciSendString("record to_do_rec", null, 0, IntPtr.Zero);
            if (result != 0)
            {
                mciSendString("close to_do_rec", null, 0, IntPtr.Zero);
                return false;
            }

            RecordingDurationSeconds = 0;
            IsRecording = true;
            _recordingTimer.Start();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Failed to start recording: {ex.Message}");
            return false;
        }
    }

    public (string? path, int duration) StopRecording()
    {
        if (!IsRecording) return (null, 0);

        try
        {
            _recordingTimer.Stop();
            int duration = Math.Max(1, RecordingDurationSeconds);

            mciSendString("stop to_do_rec", null, 0, IntPtr.Zero);

            string fileName = $"vn_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}.wav";
            string filePath = Path.Combine(_voiceNotesDirectory, fileName);

            int saveResult = mciSendString($"save to_do_rec \"{filePath}\"", null, 0, IntPtr.Zero);
            mciSendString("close to_do_rec", null, 0, IntPtr.Zero);

            IsRecording = false;
            RecordingDurationSeconds = 0;

            if (saveResult == 0 && File.Exists(filePath))
            {
                return (filePath, duration);
            }

            return (null, 0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Failed to stop/save recording: {ex.Message}");
            IsRecording = false;
            RecordingDurationSeconds = 0;
            return (null, 0);
        }
    }

    public void CancelRecording()
    {
        if (!IsRecording) return;

        try
        {
            _recordingTimer.Stop();
            mciSendString("stop to_do_rec", null, 0, IntPtr.Zero);
            mciSendString("close to_do_rec", null, 0, IntPtr.Zero);
        }
        catch
        {
        }
        finally
        {
            IsRecording = false;
            RecordingDurationSeconds = 0;
        }
    }

    public void Play(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

        try
        {
            if (IsPlaying)
            {
                Stop();
            }

            _mediaPlayer.Open(new Uri(filePath, UriKind.Absolute));
            _mediaPlayer.Play();

            IsPlaying = true;
            CurrentPlayingPath = filePath;
            PlaybackStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Playback error: {ex.Message}");
            IsPlaying = false;
            CurrentPlayingPath = null;
            PlaybackStateChanged?.Invoke();
        }
    }

    public void Stop()
    {
        try
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Close();
        }
        catch
        {
        }
        finally
        {
            IsPlaying = false;
            CurrentPlayingPath = null;
            PlaybackStateChanged?.Invoke();
        }
    }

    public void TogglePlay(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        if (IsPlaying && string.Equals(CurrentPlayingPath, filePath, StringComparison.OrdinalIgnoreCase))
        {
            Stop();
        }
        else
        {
            Play(filePath);
        }
    }

    public bool IsPathPlaying(string? filePath)
    {
        return IsPlaying &&
               !string.IsNullOrEmpty(filePath) &&
               string.Equals(CurrentPlayingPath, filePath, StringComparison.OrdinalIgnoreCase);
    }

    public void DeleteVoiceNote(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            if (IsPlaying && string.Equals(CurrentPlayingPath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                Stop();
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Delete voice note error: {ex.Message}");
        }
    }
}
