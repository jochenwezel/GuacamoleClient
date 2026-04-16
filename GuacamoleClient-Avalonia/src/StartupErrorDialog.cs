using System;
using System.Runtime.InteropServices;

namespace GuacClient;

internal static class StartupErrorDialog
{
    public static void Show(string title, string message)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                MessageBoxW(IntPtr.Zero, message, title, 0x00000010);
                return;
            }
        }
        catch
        {
            // Fallback to stderr below if the native dialog cannot be shown.
        }

        Console.Error.WriteLine(title);
        Console.Error.WriteLine(message);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
