using Avalonia;
using GuacamoleClient.Common.Localization;
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
            StartupErrorDialog.Show(LocalizationProvider.Get(LocalizationKeys.AppStart_StartupError_Title), BuildErrorMessage(ex));
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
                StartupErrorDialog.Show(LocalizationProvider.Get(LocalizationKeys.AppStart_UnexpectedError_Title), BuildErrorMessage(ex));
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            StartupErrorDialog.Show(LocalizationProvider.Get(LocalizationKeys.AppStart_BackgroundError_Title), BuildErrorMessage(e.Exception));
            e.SetObserved();
        };
    }

    private static string BuildErrorMessage(Exception ex)
    {
        return
            LocalizationProvider.Get(LocalizationKeys.AppStart_ErrorMessage_Text)
            + "\r\n\r\n"
            + ex.Message
            + "\r\n\r\n"
            + LocalizationProvider.Get(LocalizationKeys.AppStart_ErrorDetails_Label)
            + "\r\n"
            + ex;
    }
}
