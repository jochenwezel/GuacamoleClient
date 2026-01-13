using GuacamoleClient.Common.Settings;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    internal static class TitleBarHelper
    {
        // DWM-Attribute (Windows 11+)
        private enum DWMWINDOWATTRIBUTE
        {
            DWMWA_CAPTION_COLOR = 35,
            DWMWA_TEXT_COLOR = 36
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            DWMWINDOWATTRIBUTE dwAttribute,
            ref int pvAttribute,
            int cbAttribute
        );

        /// <summary>
        /// Setzt eine TitleBar mit frei definierbarer Hintergrund- und Textfarbe.
        /// Schlägt auf nicht unterstützten Windows-Versionen stillschweigend fehl.
        /// </summary>
        public static void ApplyTitleBarColors(Form form, GuacamoleColorScheme colorScheme) =>
            ApplyTitleBarColors(
                form,
                UITools.ParseHexColor(colorScheme.PrimaryColorHexValue),
                UITools.ParseHexColor(colorScheme.TextColorHexValue)
            );

        /// <summary>
        /// Setzt eine TitleBar mit frei definierbarer Hintergrund- und Textfarbe.
        /// Schlägt auf nicht unterstützten Windows-Versionen stillschweigend fehl.
        /// </summary>
        private static void ApplyTitleBarColors(Form form, Color backgroundColor, Color textColor)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            // Nur unter Windows sinnvoll
            if (!Environment.OSVersion.Platform.ToString()
                .StartsWith("Win", StringComparison.OrdinalIgnoreCase))
                return;

            // Mindestens Windows 10
            Version v = Environment.OSVersion.Version;
            if (v.Major < 10)
                return;

            IntPtr hwnd = form.Handle;
            int backgroundColorRef = ToColorRef(backgroundColor);
            int textColorRef = ToColorRef(textColor);

            try
            {
                // Hintergrundfarbe der TitleBar
                DwmSetWindowAttribute(
                    hwnd,
                    DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR,
                    ref backgroundColorRef,
                    sizeof(int));

                // Textfarbe der TitleBar
                DwmSetWindowAttribute(
                    hwnd,
                    DWMWINDOWATTRIBUTE.DWMWA_TEXT_COLOR,
                    ref textColorRef,
                    sizeof(int));
            }
            catch (DllNotFoundException)
            {
                // dwmapi.dll nicht vorhanden
            }
            catch (EntryPointNotFoundException)
            {
                // Attribut oder API nicht verfügbar
            }
            catch (Exception)
            {
                // bewusst ignoriert oder hier optional loggen
            }
        }

        /// <summary>
        /// Wandelt System.Drawing.Color in einen COLORREF (0x00BBGGRR) um.
        /// </summary>
        private static int ToColorRef(Color color)
        {
            return color.R | (color.G << 8) | (color.B << 16);
        }
    }
}