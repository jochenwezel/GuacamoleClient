using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GuacamoleClient.Common;
using GuacamoleClient.Common.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace GuacClient;

public partial class MainWindow
{
    private MenuItem _saveScreenshotMenuItem = default!;
    private MenuItem _copyScreenshotMenuItem = default!;
    private bool _screenshotInProgress;
    private bool _screenshotWindowClosed;

    private void InitializeScreenshotActions()
    {
        _saveScreenshotMenuItem = this.FindControl<MenuItem>("SaveScreenshotMenuItem")!;
        _copyScreenshotMenuItem = this.FindControl<MenuItem>("CopyScreenshotMenuItem")!;
        _saveScreenshotMenuItem.Click += async (_, _) => await TakeScreenshotAsync(saveToFile: true);
        _copyScreenshotMenuItem.Click += async (_, _) => await TakeScreenshotAsync(saveToFile: false);
        _actionsMenuItem.SubmenuOpened += (_, _) => UpdateScreenshotMenuState();
        _web.Navigated += (_, _) => Dispatcher.UIThread.Post(UpdateScreenshotMenuState);
        Closed += (_, _) => _screenshotWindowClosed = true;
        UpdateScreenshotMenuState();
    }

    private void UpdateScreenshotMenuState()
    {
        bool enabled = !_screenshotInProgress && !_screenshotWindowClosed
            && _web.IsVisible && !_emptyStateOverlay.IsVisible && _web.IsBrowserInitialized
            && !string.IsNullOrWhiteSpace(_web.Address) && _web.Address != "about:blank"
            && _web.Bounds.Width > 0 && _web.Bounds.Height > 0;
        _saveScreenshotMenuItem.IsEnabled = enabled && StorageProvider.CanSave;
        _copyScreenshotMenuItem.IsEnabled = enabled && Clipboard != null;
    }

    private async Task TakeScreenshotAsync(bool saveToFile)
    {
        UpdateScreenshotMenuState();
        if (!(saveToFile ? _saveScreenshotMenuItem.IsEnabled : _copyScreenshotMenuItem.IsEnabled))
            return;

        _screenshotInProgress = true;
        UpdateScreenshotMenuState();
        UpdateKeyboardHookState();
        try
        {
            string fileName = BrowserScreenshot.CreateFileName(_web.Title, _web.Address, DateTime.Now);
            object? wrapper = GetUnderlyingBrowser();
            // The WebView wrapper exposes its CEF browser only through an internal method.
            var browser = wrapper?.GetType().GetMethod("GetBrowser", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(wrapper, null) as CefBrowser
                ?? throw new InvalidOperationException("The browser is unavailable.");
            byte[] png = await CefScreenshotCapture.CaptureAsync(browser);
            if (_screenshotWindowClosed)
                return;

            if (saveToFile)
            {
                using var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = LocalizationProvider.Get(LocalizationKeys.Menu_SaveScreenshotAs),
                    SuggestedFileName = fileName,
                    DefaultExtension = "png",
                    ShowOverwritePrompt = true,
                    FileTypeChoices = [new FilePickerFileType("PNG")
                    {
                        Patterns = ["*.png"], MimeTypes = ["image/png"], AppleUniformTypeIdentifiers = ["public.png"]
                    }]
                });
                if (file == null || _screenshotWindowClosed)
                    return;

                await using var stream = await file.OpenWriteAsync();
                if (stream.CanSeek)
                    stream.SetLength(0);
                await stream.WriteAsync(png);
            }
            else
            {
                using var stream = new MemoryStream(png, writable: false);
                // Clipboard requests can be deferred, so the transfer owns the bitmap.
                var transfer = new ScreenshotClipboardTransfer(new Bitmap(stream));
                try
                {
                    await Clipboard!.SetDataAsync(transfer);
                }
                catch
                {
                    transfer.Dispose();
                    throw;
                }
                await Clipboard.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            if (!_screenshotWindowClosed)
                await MessageBoxSimple.Show(this,
                    LocalizationProvider.Get(LocalizationKeys.Screenshot_Failed_Title),
                    LocalizationProvider.Get(LocalizationKeys.Screenshot_Failed_Text, ex.Message));
        }
        finally
        {
            _screenshotInProgress = false;
            if (!_screenshotWindowClosed)
            {
                UpdateScreenshotMenuState();
                UpdateKeyboardHookState();
            }
        }
    }

    private sealed class ScreenshotClipboardTransfer(Bitmap bitmap) : IAsyncDataTransfer
    {
        /// <inheritdoc/>
        public IReadOnlyList<DataFormat> Formats { get; } = [DataFormat.Bitmap];

        /// <inheritdoc/>
        public IReadOnlyList<IAsyncDataTransferItem> Items { get; } = [DataTransferItem.Create(DataFormat.Bitmap, bitmap)];

        /// <inheritdoc/>
        public void Dispose() => bitmap.Dispose();
    }
}
