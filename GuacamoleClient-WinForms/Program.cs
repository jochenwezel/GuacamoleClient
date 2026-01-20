using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Windows.Forms;
using GuacamoleClient.Common.Settings;

namespace GuacamoleClient.WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

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
                MessageBox.Show("An unexpected error occurred and the application must close:" + System.Environment.NewLine + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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