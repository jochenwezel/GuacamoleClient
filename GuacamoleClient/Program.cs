using CompuMaster.GuacLauncher;
using System;
using System.Windows.Forms;

namespace GuacShim.ControllerHost
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            string startUrl = GuacConfig.GetOrAskStartUrl();
            if (startUrl == null) return; // Benutzer hat abgebrochen

            Application.Run(new MainForm(startUrl));
        }
    }
}
