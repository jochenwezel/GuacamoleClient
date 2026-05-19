using System;
using System.Diagnostics;
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

            if (OperatingSystem.IsLinux() && TryShowLinuxDialog(title, message))
                return;
        }
        catch
        {
            // Fallback to stderr below if the native dialog cannot be shown.
        }

        Console.Error.WriteLine(title);
        Console.Error.WriteLine(message);
    }

    private static bool TryShowLinuxDialog(string title, string message)
    {
        return TryStartAndWait("zenity", "--error", "--no-wrap", "--title", title, "--text", message)
            || TryStartAndWait("kdialog", "--error", message, "--title", title)
            || TryStartAndWait("xmessage", "-center", "-title", title, message);
    }

    private static bool TryStartAndWait(string fileName, params string[] arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = false
                }
            };

            foreach (string argument in arguments)
                process.StartInfo.ArgumentList.Add(argument);

            if (!process.Start())
                return false;

            process.WaitForExit();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
