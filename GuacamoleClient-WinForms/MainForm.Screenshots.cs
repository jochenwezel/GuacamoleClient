using GuacamoleClient.Common;
using GuacamoleClient.Common.Localization;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms;

public partial class MainForm
{
    private bool _screenshotInProgress;
    private bool _screenshotContentReady;

    private void UpdateScreenshotMenuState()
    {
        bool enabled = !_screenshotInProgress && _screenshotContentReady
            && _webview2_core != null && _webview2_controller?.IsVisible == true
            && WebBrowserHostPanel.ClientSize.Width > 0 && WebBrowserHostPanel.ClientSize.Height > 0;
        saveScreenshotToolStripMenuItem.Enabled = enabled;
        copyScreenshotToolStripMenuItem.Enabled = enabled;
    }

    private async void SaveScreenshotToolStripMenuItem_Click(object? sender, EventArgs e)
        => await TakeScreenshotAsync(saveToFile: true);

    private async void CopyScreenshotToolStripMenuItem_Click(object? sender, EventArgs e)
        => await TakeScreenshotAsync(saveToFile: false);

    private async Task TakeScreenshotAsync(bool saveToFile)
    {
        UpdateScreenshotMenuState();
        if (!saveScreenshotToolStripMenuItem.Enabled)
            return;

        _screenshotInProgress = true;
        UpdateScreenshotMenuState();
        UpdateKeyboardHookState();
        try
        {
            string fileName = BrowserScreenshot.CreateFileName(_webview2_core!.DocumentTitle, _webview2_core.Source, DateTime.Now);
            // Capture before opening the dialog so the saved image is the requested moment.
            string response = await _webview2_core!.CallDevToolsProtocolMethodAsync(
                BrowserScreenshot.CaptureMethod, BrowserScreenshot.CaptureParameters)
                .WaitAsync(TimeSpan.FromSeconds(15));
            byte[] png = BrowserScreenshot.DecodePngResponse(response);
            if (IsDisposed || Disposing)
                return;

            if (saveToFile)
            {
                using var dialog = new SaveFileDialog
                {
                    Title = LocalizationProvider.Get(LocalizationKeys.Menu_SaveScreenshotAs),
                    Filter = "PNG (*.png)|*.png",
                    DefaultExt = "png",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = fileName
                };
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                await File.WriteAllBytesAsync(dialog.FileName, png);
            }
            else
            {
                using var stream = new MemoryStream(png, writable: false);
                using var bitmap = new Bitmap(stream);
                // SetImage copies the image into the persistent Windows clipboard.
                Clipboard.SetImage(bitmap);
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                MessageBox.Show(this,
                    LocalizationProvider.Get(LocalizationKeys.Screenshot_Failed_Text, ex.Message),
                    LocalizationProvider.Get(LocalizationKeys.Screenshot_Failed_Title),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _screenshotInProgress = false;
            if (!IsDisposed && !Disposing)
            {
                UpdateScreenshotMenuState();
                UpdateKeyboardHookState();
            }
        }
    }
}
