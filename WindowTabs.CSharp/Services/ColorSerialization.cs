using System;
using System.Drawing;
using System.Globalization;

namespace WindowTabs.CSharp.Services
{
    internal static class ColorSerialization
    {
        public static Color FromRgb(int rgb)
        {
            return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }

        public static Color FromHexString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Color.Empty;
            }

            return FromRgb(int.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }

        public static string ToHexString(Color value)
        {
            return value.IsEmpty
                ? "0"
                : ((value.R << 16) | (value.G << 8) | value.B).ToString("X", CultureInfo.InvariantCulture);
        }
    }
}
