using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Windows.Forms;
using GuacamoleClient.Common.Localization;
using GuacamoleClient.Common.Settings;

namespace GuacamoleClient.WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            RegisterGlobalExceptionHandlers();
            ClickOnceDeploymentInfo? clickOnceDeploymentInfo = ClickOnceDeploymentInfo.TryCreate();
            if (clickOnceDeploymentInfo != null)
                ClickOnceWindowsIntegration.ApplyBestEffortFixes(clickOnceDeploymentInfo);

            // Load settings (JSON). If not present, migrate from legacy Registry configuration.
            var store = new JsonFileGuacamoleSettingsStore(GuacamoleSettingsPaths.GetSettingsFilePath());
            var settings = GuacamoleSettingsManager.LoadAsync(store).GetAwaiter().GetResult();

            if (settings.ServerProfiles.Count == 0)
            {
                // Try migrate from Registry
                if (LegacyRegistryMigration.TryMigrateLegacyRegistryToSettings(settings))
                {
                    settings.SaveAsync().GetAwaiter().GetResult();
                }
            }

            // First-run: require at least one profile
            if (settings.ServerProfiles.Count == 0)
            {
                using var dlg = new AddEditServerForm(settings, editing: null, isFirstProfile: true);
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;
            }

            GuacamoleBrowserCache.DeleteDisabledProfileCaches("GuacamoleClient", settings.ServerProfiles);

            var defaultProfile = settings.GetDefaultOrFirstOrNull();
            if (defaultProfile == null) return;

            Uri homeUri = new Uri(defaultProfile.Url);

            var app = new MyApplication(() => new MainForm(settings, defaultProfile, homeUri));
            try
            {
                app.Run(Environment.GetCommandLineArgs());
            }
            catch (Exception ex)
            {
                ShowFatalError(LocalizationKeys.AppStart_StartupError_Title, ex);
            }
        }

        private static void RegisterGlobalExceptionHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => ShowFatalError(LocalizationKeys.AppStart_UnexpectedError_Title, e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    ShowFatalError(LocalizationKeys.AppStart_UnexpectedError_Title, ex);
                }
            };
        }

        private static void ShowFatalError(LocalizationKeys titleKey, Exception ex)
        {
            MessageBox.Show(
                LocalizationProvider.Get(LocalizationKeys.AppStart_UnexpectedErrorWillClose_Text)
                + Environment.NewLine + Environment.NewLine
                + LocalizationProvider.Get(LocalizationKeys.AppStart_ErrorDetails_Label)
                + Environment.NewLine
                + ex,
                LocalizationProvider.Get(titleKey),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    internal sealed class MyApplication : WindowsFormsApplicationBase
    {
        private readonly Func<Form> _mainFormFactory;

        public MyApplication(Func<Form> mainFormFactory)
        {
            ShutdownStyle = ShutdownMode.AfterAllFormsClose;
            _mainFormFactory = mainFormFactory ?? throw new ArgumentNullException(nameof(mainFormFactory));
        }

        protected override void OnCreateMainForm()
        {
            MainForm = _mainFormFactory();
        }
    }
}
