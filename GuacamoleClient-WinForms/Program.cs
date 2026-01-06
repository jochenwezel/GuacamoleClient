using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            string? startUrl = GuacConfig.GetOrAskStartUrl();
            if (string.IsNullOrWhiteSpace(startUrl)) return; // Benutzer hat abgebrochen
            Uri startUri = new Uri(startUrl);

            var app = new MyApplication(() => new MainForm(startUri, startUri));
            app.Run(Environment.GetCommandLineArgs());
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
