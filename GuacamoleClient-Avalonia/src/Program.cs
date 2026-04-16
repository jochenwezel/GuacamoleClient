using Avalonia;
using System;
using System.Threading.Tasks;

namespace GuacClient;
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        RegisterGlobalExceptionHandlers();

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            StartupErrorDialog.Show("Startup Error", BuildErrorMessage(ex));
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                StartupErrorDialog.Show("Unexpected Error", BuildErrorMessage(ex));
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            StartupErrorDialog.Show("Background Error", BuildErrorMessage(e.Exception));
            e.SetObserved();
        };
    }

    private static string BuildErrorMessage(Exception ex)
    {
        return
            "The application could not be started correctly.\r\n\r\n"
            + ex.Message
            + "\r\n\r\nDetails:\r\n"
            + ex;
    }
}
