using GuacamoleClient.Common;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace GuacClient;

internal static class CefScreenshotCapture
{
    internal static async Task<byte[]> CaptureAsync(CefBrowser browser)
    {
        var completion = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observer = new ScreenshotObserver(completion);
        CefRegistration? registration = null;

        // CEF has its own UI thread, separate from Avalonia's dispatcher.
        if (!CefRuntime.PostTask(CefThreadId.UI, new BrowserTask(() =>
        {
            try
            {
                using var host = browser.GetHost();
                registration = host.AddDevToolsMessageObserver(observer);
                using var parameters = CefDictionaryValue.Create();
                parameters.SetString("format", "png");
                parameters.SetBool("fromSurface", true);
                parameters.SetBool("captureBeyondViewport", false);
                observer.MessageId = host.ExecuteDevToolsMethod(0, BrowserScreenshot.CaptureMethod, parameters);
                if (observer.MessageId == 0)
                    completion.TrySetException(new InvalidOperationException("The browser rejected the screenshot request."));
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        })))
            throw new InvalidOperationException("The browser is unavailable.");

        try
        {
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
        finally
        {
            // Remove the observer on its owning thread, including after a timeout.
            CefRuntime.PostTask(CefThreadId.UI, new BrowserTask(() => registration?.Dispose()));
        }
    }

    private sealed class BrowserTask(Action action) : CefTask
    {
        /// <inheritdoc/>
        protected override void Execute() => action();
    }

    private sealed class ScreenshotObserver(TaskCompletionSource<byte[]> completion) : CefDevToolsMessageObserver
    {
        internal int MessageId { get; set; }

        /// <inheritdoc/>
        protected override bool OnDevToolsMessage(CefBrowser browser, IntPtr message, int messageSize) => false;

        /// <inheritdoc/>
        protected override void OnDevToolsMethodResult(CefBrowser browser, int messageId, bool success, IntPtr result, int resultSize)
        {
            if (messageId != MessageId)
                return;

            try
            {
                if (!success)
                    throw new InvalidOperationException("The browser could not capture a screenshot.");

                string response = Marshal.PtrToStringUTF8(result, resultSize) ?? string.Empty;
                completion.TrySetResult(BrowserScreenshot.DecodePngResponse(response));
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }

        /// <inheritdoc/>
        protected override void OnDevToolsEvent(CefBrowser browser, string method, IntPtr parameters, int parametersSize) { }

        /// <inheritdoc/>
        protected override void OnDevToolsAgentAttached(CefBrowser browser) { }

        /// <inheritdoc/>
        protected override void OnDevToolsAgentDetached(CefBrowser browser)
            => completion.TrySetException(new InvalidOperationException("The browser closed during screenshot capture."));
    }
}
