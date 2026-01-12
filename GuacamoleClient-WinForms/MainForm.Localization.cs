using GuacamoleClient.Common.Localization;
using System;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    public partial class MainForm
    {
        /// <summary>
        /// Show tooltip message on top of form.
        /// </summary>
        public void ShowHint(LocalizationKey localizedString)
        {
            string text = LocalizedString(localizedString);
            try { _tip.Show(text, this, 20, 20, 5000); } catch { /*best effort*/ }
        }

        internal static string LocalizedString(LocalizationKey key)
            => LocalizationProvider.Get(key);
    }
}
