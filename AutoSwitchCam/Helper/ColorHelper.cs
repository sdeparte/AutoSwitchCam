using AutoSwitchCam.Model;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows;

namespace AutoSwitchCam.Helper
{
    class ColorHelper
    {
        public static Regex IsAcceptableColorString = new Regex("^[0-9A-Fa-f]{0,6}$");

        public static Regex IsColorString = new Regex("^[0-9A-Fa-f]{1,6}$");

        public static Color ToColor(string color)
        {
            if (IsColorString.IsMatch(color))
            {
                return ColorTranslator.FromHtml($"#{color}");
            }

            return Color.Black;
        }
    }
}
