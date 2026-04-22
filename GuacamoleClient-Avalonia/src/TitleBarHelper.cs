using Avalonia.Controls;
using Avalonia.Platform;
using GuacamoleClient.Common.Settings;
using System;
using System.Runtime.InteropServices;

namespace GuacClient;

internal static class TitleBarHelper
{
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
        int cbAttribute);

    public static void ApplyTitleBarColors(Window window, GuacamoleColorScheme colorScheme)
    {
        if (window == null)
            throw new ArgumentNullException(nameof(window));
        if (colorScheme == null)
            throw new ArgumentNullException(nameof(colorScheme));

        if (!OperatingSystem.IsWindows())
            return;

        var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
            return;

        int backgroundColorRef = ToColorRef(colorScheme.PrimaryColorHexValue);
        int textColorRef = ToColorRef(colorScheme.TextColorHexValue);

        try
        {
            DwmSetWindowAttribute(handle, DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR, ref backgroundColorRef, sizeof(int));
            DwmSetWindowAttribute(handle, DWMWINDOWATTRIBUTE.DWMWA_TEXT_COLOR, ref textColorRef, sizeof(int));
        }
        catch
        {
            // Keep platform default title bar when unsupported.
        }
    }

    private static int ToColorRef(string hexColor)
    {
        string hex = hexColor.TrimStart('#');
        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
        return r | (g << 8) | (b << 16);
    }
}
