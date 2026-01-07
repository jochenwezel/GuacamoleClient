using GuacamoleClient.Common;
using Microsoft.Web.WebView2.Core;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    public partial class MainForm
    {
        /// <summary>
        /// Initialize everything to correctly handle all control focus and all (enabled/disabled) keyboard capturing
        /// </summary>
        void InitializeControlFocusManagementWithKeyboardCapturingHandler()
        {
            HookMenuItemsRecursive(mainMenuStrip!.Items);
            this.Activated += (_, __) => SetFocusToWebview2Control();
            this.Deactivate += (_, __) => DetachWebViewFocus(true);
            this.mainMenuStrip.Leave += (_, __) => SetFocusToWebview2Control();
            this.HintStopWebcontrol2FocusShortcut!.Visible = false;
        }

        /// <summary>
        /// The number of open dropdown menus
        /// </summary>
        private int _openDropdowns;

        /// <summary>
        /// Gets a value indicating whether the main menu or any of its dropdown menus is currently open or focused or selected.
        /// </summary>
        private bool IsMenuOpen
        {
            get
            {
                if (_openDropdowns > 0 || this.MainMenuStrip!.Focused || this.MainMenuStrip.ContainsFocus)
                    return true;
                else
                {
                    foreach (ToolStripMenuItem item in this.MainMenuStrip.Items)
                    {
                        if (item.Selected) return true;
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// Attaches event handlers to the DropDownOpened and DropDownClosed events of all ToolStripMenuItem objects in
        /// the specified collection and their submenus recursively.
        /// </summary>
        /// <remarks>This method ensures that event handlers are attached to all menu items in the
        /// hierarchy, allowing for consistent tracking of open and closed dropdown menus. It should be called whenever
        /// menu items are created or modified to maintain correct event handling.</remarks>
        /// <param name="items">The collection of ToolStripItem objects to process. All ToolStripMenuItem instances within this collection
        /// and their nested submenus will have event handlers attached.</param>
        private void HookMenuItemsRecursive(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripMenuItem mi)
                {
                    mi.DropDownOpened += (_, __) =>
                    {
                        _openDropdowns++;
                        DetachWebViewFocus(false);
                    };
                    mi.DropDownClosed += (_, __) =>
                    {
                        _openDropdowns = Math.Max(0, _openDropdowns - 1);
                        SetFocusToWebview2Control();
                    };

                    // Untermenüs rekursiv
                    if (mi.HasDropDownItems)
                        HookMenuItemsRecursive(mi.DropDownItems);
                }
            }
        }

        /// <summary>
        /// Set focus to the WebView2 control to allow keyboard capturing by the WebView2 control
        /// </summary>
        public void SetFocusToWebview2Control()
        {
            // Wenn Sie danach zurück in WebView wollen:
            WebBrowserHostPanel.Focus();
            _webview2_controller?.MoveFocus(CoreWebView2MoveFocusReason.Programmatic);
            IsKeyboardFocusBoundToWebview2Control = KeyboardCaptureMode.GrabbingEnabled_ShowKeyboardShortcutInfo;
        }

        /// <summary>
        /// Removes keyboard focus from the WebView2 control by setting focus to an alternative control within the form.
        /// </summary>
        /// <remarks>Use this method to ensure that the WebView2 control no longer receives keyboard
        /// input, which may be necessary before disposing the control or when redirecting user interaction to another
        /// part of the application (e.g. main menu). This method is intended for internal focus management and should be called only
        /// when it is necessary to explicitly change the active control.</remarks>
        public void DetachWebViewFocus(bool hideShortcutTip)
        {
            // Ein anderes Win32-Fokusziel setzen → WebView2 verliert Tastatur
            _focusSink.Focus();
            this.ActiveControl = _focusSink; // optional, hilft WinForms-intern
            if (hideShortcutTip)
                IsKeyboardFocusBoundToWebview2Control = KeyboardCaptureMode.GrabbingDisabled_HideKeyboardShortcutInfo;
            else
                IsKeyboardFocusBoundToWebview2Control = KeyboardCaptureMode.GrabbingDisabled_ShowKeyboardShortcutInfo;
        }

        /// <summary>
        /// Specifies the available modes for capturing keyboard input and displaying keyboard shortcut information.
        /// </summary>
        public enum KeyboardCaptureMode
        {
            GrabbingEnabled_ShowKeyboardShortcutInfo,
            GrabbingDisabled_ShowKeyboardShortcutInfo,
            GrabbingDisabled_HideKeyboardShortcutInfo,
        }

        private KeyboardCaptureMode _IsKeyboardFocusBoundToWebview2Control = KeyboardCaptureMode.GrabbingDisabled_HideKeyboardShortcutInfo;

        /// <summary>
        /// Gets or sets the current keyboard focus binding mode for the WebView2 control.
        /// </summary>
        /// <remarks>Use this property to control whether keyboard input is captured by the WebView2
        /// control or remains with the host application. Changing this value may affect how keyboard shortcuts and
        /// input are handled within the application.</remarks>
        public KeyboardCaptureMode IsKeyboardFocusBoundToWebview2Control
        {
            get { return _IsKeyboardFocusBoundToWebview2Control; }
            set
            {
                _IsKeyboardFocusBoundToWebview2Control = value;
                switch (value)
                {
                    case KeyboardCaptureMode.GrabbingEnabled_ShowKeyboardShortcutInfo:
                        this.HintStopWebcontrol2FocusShortcut!.Text = LocalizedString(LocalizationKeys.Tip_CtrlAltScroll_StopKeyboardGrabbingOfGuacamoleWindow);
                        this.HintStopWebcontrol2FocusShortcut!.Visible = true;
                        break;
                    case KeyboardCaptureMode.GrabbingDisabled_ShowKeyboardShortcutInfo:
                        this.HintStopWebcontrol2FocusShortcut!.Text = LocalizedString(LocalizationKeys.Tip_CtrlAltScroll_StartKeyboardGrabbingOfGuacamoleWindow);
                        this.HintStopWebcontrol2FocusShortcut!.Visible = true;
                        break;
                    case KeyboardCaptureMode.GrabbingDisabled_HideKeyboardShortcutInfo:
                        this.HintStopWebcontrol2FocusShortcut!.Visible = false;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
