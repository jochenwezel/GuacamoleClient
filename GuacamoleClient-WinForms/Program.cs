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

            Application.Run(new MainForm(new Uri(startUrl)));
        }
    }
}
