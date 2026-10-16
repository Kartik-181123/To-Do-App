using System.Globalization;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace TodoWidget.Helpers;
public static class ColorHelper
{
    public static Color HexToColor(string? hex, Color fallback = default)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback == default ? Colors.Black : fallback;
        }

        string cleanHex = hex.Trim().TrimStart('#');

        try
        {
            return cleanHex.Length switch
            {
                3 => Color.FromRgb(
                    byte.Parse(new string(cleanHex[0], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(new string(cleanHex[1], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(new string(cleanHex[2], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)),
                6 => Color.FromRgb(
                    byte.Parse(cleanHex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(cleanHex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(cleanHex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)),
                8 => Color.FromArgb(
                    byte.Parse(cleanHex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(cleanHex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(cleanHex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    byte.Parse(cleanHex.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)),
                _ => fallback == default ? Colors.Black : fallback
            };
        }
        catch
        {
            return fallback == default ? Colors.Black : fallback;
        }
    }
    public static Color FromHex(string? hex, Color fallback = default) => HexToColor(hex, fallback);
    public static SolidColorBrush HexToBrush(string? hex, Color fallbackColor = default)
    {
        Color color = HexToColor(hex, fallbackColor);
        var brush = new SolidColorBrush(color);
        if (brush.CanFreeze)
        {
            brush.Freeze();
        }
        return brush;
    }
    public static SolidColorBrush BrushFromHex(string? hex, Color fallbackColor = default) => HexToBrush(hex, fallbackColor);
    public static string ColorToHex(Color color, bool includeAlpha = false)
    {
        return includeAlpha
            ? $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}"
            : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
    public static Color Lighten(Color color, double factor)
    {
        factor = Math.Clamp(factor, 0.0, 1.0);

        byte r = (byte)Math.Clamp(color.R + (255 - color.R) * factor, 0, 255);
        byte g = (byte)Math.Clamp(color.G + (255 - color.G) * factor, 0, 255);
        byte b = (byte)Math.Clamp(color.B + (255 - color.B) * factor, 0, 255);

        return Color.FromArgb(color.A, r, g, b);
    }
    public static Color Darken(Color color, double factor)
    {
        factor = Math.Clamp(factor, 0.0, 1.0);

        byte r = (byte)Math.Clamp(color.R * (1.0 - factor), 0, 255);
        byte g = (byte)Math.Clamp(color.G * (1.0 - factor), 0, 255);
        byte b = (byte)Math.Clamp(color.B * (1.0 - factor), 0, 255);

        return Color.FromArgb(color.A, r, g, b);
    }
    public static bool IsDark(Color color)
    {
        double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
        return luminance < 128.0;
    }
    public static Color GetContrastingTextColor(Color backgroundColor)
    {
        return IsDark(backgroundColor) ? Colors.White : Colors.Black;
    }
}
