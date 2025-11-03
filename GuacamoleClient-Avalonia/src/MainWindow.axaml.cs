using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebViewControl;

namespace GuacClient
{
    public partial class MainWindow : Window
    {
        private readonly IStartUrlStore _store = StartUrlStoreFactory.Create();
        private readonly HashSet<string> _trustedHosts = new(StringComparer.OrdinalIgnoreCase);
        private WebView _web = default!;

        public MainWindow()
        {
            InitializeComponent();

            // XAML: <wv:WebView x:Name="Web" />
            _web = this.FindControl<WebView>("Web")!;

            // Beim Öffnen URL prüfen und laden
            this.Opened += async (_, __) => await EnsureAndLoadUrlAsync();

            // TODO: Key-Event-Handler mit Modifier-Erkennung
            if (false) this.AddHandler(KeyDownEvent, (sender, e) =>
            {
                var mods = e.KeyModifiers;
                bool ctrl = (mods & KeyModifiers.Control) != 0;
                bool alt = (mods & KeyModifiers.Alt) != 0;
                bool shift = (mods & KeyModifiers.Shift) != 0;

                // Wichtig:
                // - Unter macOS entspricht ⌘ Command dem "Meta"-Modifier
                // - Unter Windows ist "Windows" die Super/Windows-Taste
                bool meta = (mods & KeyModifiers.Meta) != 0;     // ⌘ auf macOS
                //bool win = (mods & KeyModifiers.Windows) != 0;     // ⊞ auf Windows

                // TODO: Ihre Logik …
            });

            // Shortcuts: Ctrl+U (URL zurücksetzen), Ctrl+Q (Beenden)
            this.KeyDown += async (s, e) =>
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.U)
                {
                    await ResetUrlAndReloadAsync();
                    e.Handled = true;
                }
                else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Q)
                {
                    Close();
                    e.Handled = true;
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task EnsureAndLoadUrlAsync()
        {
            var url = _store.Load();
            if (!UrlInputDialog.IsValidUrl(url))
            {
                var dlg = new UrlInputDialog { Icon = this.Icon };
                url = await dlg.ShowDialog<string?>(this);
                if (!UrlInputDialog.IsValidUrl(url))
                {
                    await MessageBoxSimple.Show(this, "Start-URL erforderlich",
                        "Ohne gültige Start-URL kann die Anwendung nicht fortfahren.");
                    Close();
                    return;
                }
                _store.Save(url!);
            }

            // (Optional) Host für evtl. spätere Policies vormerken
            _trustedHosts.Clear();
            _trustedHosts.Add(new Uri(url!).Host);

            try
            {
                // CEF/WebViewControl-Avalonia navigiert über die Url-Eigenschaft
                _web.Address = url!;
                Title = $"GuacamoleClient v{VersionUtil.InformationalVersion()} – {url}";
            }
            catch (Exception ex)
            {
                await MessageBoxSimple.Show(this, "WebView-Fehler", ex.Message);
                Close();
            }
        }

        private async Task ResetUrlAndReloadAsync()
        {
            _store.Delete();
            await EnsureAndLoadUrlAsync();
        }
    }
}
