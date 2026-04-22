using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using GuacamoleClient.Common;
using GuacamoleClient.Common.Localization;
using GuacamoleClient.Common.Settings;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GuacClient;

public partial class AddEditServerDialog : Window
{
    private const string SetupGuideUrl = "https://github.com/jochenwezel/GuacamoleClient/blob/main/docs/SetupTestGuacamoleServer.md";

    private readonly GuacamoleSettingsManager? _manager;
    private readonly GuacamoleServerProfile? _editing;
    private readonly bool _isFirstProfile;

    private TextBox _urlTextBox = default!;
    private TextBox _displayNameTextBox = default!;
    private ComboBox _colorComboBox = default!;
    private TextBox _customColorTextBox = default!;
    private TextBlock _customColorLabel = default!;
    private Border _colorPreviewBorder = default!;
    private TextBlock _colorPreviewTextBlock = default!;
    private CheckBox _ignoreCertificateErrorsCheckBox = default!;
    private TextBlock _localCacheLabel = default!;
    private RadioButton _disableLocalCacheRadioButton = default!;
    private RadioButton _enableLocalCacheRadioButton = default!;
    private TextBlock _localCacheInfoTextBlock = default!;
    private Button _setupGuideButton = default!;
    private Button _saveButton = default!;
    private Button _cancelButton = default!;

    public AddEditServerDialog()
    {
        InitializeDialog();
    }

    public AddEditServerDialog(GuacamoleSettingsManager manager, GuacamoleServerProfile? editing, bool isFirstProfile)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _editing = editing;
        _isFirstProfile = isFirstProfile;

        InitializeDialog();
        ApplyLocalization();
        Populate();
    }

    public GuacamoleServerProfile? ResultProfile { get; private set; }

    private void InitializeDialog()
    {
        InitializeComponent();

        _urlTextBox = this.FindControl<TextBox>("UrlTextBox")!;
        _displayNameTextBox = this.FindControl<TextBox>("DisplayNameTextBox")!;
        _colorComboBox = this.FindControl<ComboBox>("ColorComboBox")!;
        _customColorTextBox = this.FindControl<TextBox>("CustomColorTextBox")!;
        _customColorLabel = this.FindControl<TextBlock>("CustomColorLabel")!;
        _colorPreviewBorder = this.FindControl<Border>("ColorPreviewBorder")!;
        _colorPreviewTextBlock = this.FindControl<TextBlock>("ColorPreviewTextBlock")!;
        _ignoreCertificateErrorsCheckBox = this.FindControl<CheckBox>("IgnoreCertificateErrorsCheckBox")!;
        _localCacheLabel = this.FindControl<TextBlock>("LocalCacheLabel")!;
        _disableLocalCacheRadioButton = this.FindControl<RadioButton>("DisableLocalCacheRadioButton")!;
        _enableLocalCacheRadioButton = this.FindControl<RadioButton>("EnableLocalCacheRadioButton")!;
        _localCacheInfoTextBlock = this.FindControl<TextBlock>("LocalCacheInfoTextBlock")!;
        _setupGuideButton = this.FindControl<Button>("SetupGuideButton")!;
        _saveButton = this.FindControl<Button>("SaveButton")!;
        _cancelButton = this.FindControl<Button>("CancelButton")!;

        _colorComboBox.ItemsSource = GuacamoleColorPalette.Keys.OrderBy(k => k).Concat(new[] { "Custom" }).ToArray();
        _colorComboBox.SelectionChanged += (_, __) => UpdateColorUi();
        _disableLocalCacheRadioButton.IsCheckedChanged += (_, __) => UpdateLocalCacheInfoUi();
        _enableLocalCacheRadioButton.IsCheckedChanged += (_, __) => UpdateLocalCacheInfoUi();
        _customColorTextBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
                UpdateColorUi();
        };
    }

    private void ApplyLocalization()
    {
        Title = _editing == null
            ? LocalizationProvider.Get(LocalizationKeys.AddEdit_ModeAddServer_Title)
            : LocalizationProvider.Get(LocalizationKeys.AddEdit_ModeEditServer_Title);
        _ignoreCertificateErrorsCheckBox.Content = LocalizationProvider.Get(LocalizationKeys.AddEdit_Check_IgnoreCertificateErrorsUnsafe);
        _localCacheLabel.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Label_LocalWebViewCache);
        _disableLocalCacheRadioButton.Content = LocalizationProvider.Get(LocalizationKeys.AddEdit_Radio_DisableLocalCacheRecommended);
        _enableLocalCacheRadioButton.Content = LocalizationProvider.Get(LocalizationKeys.AddEdit_Radio_EnableLocalCache);
        _setupGuideButton.Content = LocalizationProvider.Get(LocalizationKeys.AddEdit_Link_SetupGuideGuacamoleTestServer);
        _cancelButton.Content = LocalizationProvider.Get(LocalizationKeys.Common_Button_Cancel);
        _saveButton.Content = LocalizationProvider.Get(LocalizationKeys.AddEdit_Button_Save);
    }

    private void Populate()
    {
        if (_editing != null)
        {
            _urlTextBox.Text = _editing.Url;
            _displayNameTextBox.Text = _editing.DisplayName ?? string.Empty;
            _ignoreCertificateErrorsCheckBox.IsChecked = _editing.IgnoreCertificateErrors;
            _enableLocalCacheRadioButton.IsChecked = _editing.LocalCacheEnabled;
            _disableLocalCacheRadioButton.IsChecked = !_editing.LocalCacheEnabled;
            if (GuacamoleColorPalette.Colors.ContainsKey(_editing.PrimaryColorValue))
            {
                _colorComboBox.SelectedItem = _editing.PrimaryColorValue;
            }
            else
            {
                _colorComboBox.SelectedItem = "Custom";
                _customColorTextBox.Text = _editing.PrimaryColorValue;
            }
        }
        else
        {
            _urlTextBox.Text = "https://";
            _displayNameTextBox.Text = string.Empty;
            _colorComboBox.SelectedItem = "Red";
            _ignoreCertificateErrorsCheckBox.IsChecked = false;
            _disableLocalCacheRadioButton.IsChecked = true;
            _enableLocalCacheRadioButton.IsChecked = false;
        }

        UpdateColorUi();
        UpdateLocalCacheInfoUi();
    }

    private void UpdateColorUi()
    {
        bool isCustom = string.Equals(_colorComboBox.SelectedItem?.ToString(), "Custom", StringComparison.OrdinalIgnoreCase);
        _customColorLabel.IsVisible = isCustom;
        _customColorTextBox.IsVisible = isCustom;

        string colorValue = GetSelectedColorValue();
        if (GuacamoleColorPalette.TryResolveToHex(colorValue, out var hex))
        {
            var colorScheme = new GuacamoleColorScheme(colorValue);
            _colorPreviewBorder.Background = Brush.Parse(hex);
            _colorPreviewTextBlock.Foreground = Brush.Parse(colorScheme.TextColorHexValue);
            _colorPreviewTextBlock.Text = $"{hex} ({colorScheme.TextColorHexValue})";
        }
        else
        {
            _colorPreviewBorder.Background = Brushes.Transparent;
            _colorPreviewTextBlock.Foreground = Brushes.Black;
            _colorPreviewTextBlock.Text = "Invalid color";
        }
    }

    private string GetSelectedColorValue()
    {
        string selection = _colorComboBox.SelectedItem?.ToString() ?? "Red";
        return string.Equals(selection, "Custom", StringComparison.OrdinalIgnoreCase)
            ? (_customColorTextBox.Text?.Trim() ?? string.Empty)
            : selection;
    }

    private void UpdateLocalCacheInfoUi()
    {
        if (_enableLocalCacheRadioButton.IsChecked == true)
        {
            _localCacheInfoTextBlock.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Warning_LocalCacheEnabled);
            _localCacheInfoTextBlock.Foreground = Brush.Parse("#B45309");
        }
        else
        {
            _localCacheInfoTextBlock.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Info_LocalCacheDisabled);
            _localCacheInfoTextBlock.Foreground = Brushes.Black;
        }
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
        => await SaveAsync();

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
        => Close(null);

    private async void SetupGuideButton_Click(object? sender, RoutedEventArgs e)
    {
        if (Uri.TryCreate(SetupGuideUrl, UriKind.Absolute, out var uri))
            await Launcher.LaunchUriAsync(uri);
    }

    private async Task SaveAsync()
    {
        if (_manager == null)
            return;

        _saveButton.IsEnabled = false;
        try
        {
            string url = _urlTextBox.Text?.Trim() ?? string.Empty;
            string? displayName = string.IsNullOrWhiteSpace(_displayNameTextBox.Text) ? null : _displayNameTextBox.Text!.Trim();
            bool ignoreCert = _ignoreCertificateErrorsCheckBox.IsChecked == true;
            bool localCacheEnabled = _enableLocalCacheRadioButton.IsChecked == true;
            string colorValue = GetSelectedColorValue();

            if (string.IsNullOrWhiteSpace(url))
            {
                await MessageBoxSimple.Show(this,
                    LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_Title),
                    LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_ServerUrlRequired));
                return;
            }

            if (!GuacamoleUrlAndContentChecks.IsValidUrlAndAcceptedScheme(url))
            {
                await MessageBoxSimple.Show(this,
                    LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_Title),
                    LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_InvalidUrlScheme));
                return;
            }

            if (_manager.UrlExists(url, _editing?.Id))
            {
                await MessageBoxSimple.Show(this,
                    LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_Title),
                    LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_DuplicateUrl));
                return;
            }

            if (!GuacamoleColorPalette.TryResolveToHex(colorValue, out var normalizedHex))
            {
                await MessageBoxSimple.Show(this,
                    LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_Title),
                    LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_InvalidColor));
                return;
            }

            bool ok = await Task.Run(() => GuacamoleUrlAndContentChecks.IsGuacamoleResponseWithStartPage(url, ignoreCert)).ConfigureAwait(true);
            if (!ok)
            {
                await MessageBoxSimple.Show(this,
                    LocalizationProvider.Get(LocalizationKeys.AddEdit_TestFailed_Title),
                    LocalizationProvider.Get(LocalizationKeys.AddEdit_TestFailed_Text, "https://remote.example.com/guacamole/"));
                return;
            }

            string primaryColorValue = string.Equals(_colorComboBox.SelectedItem?.ToString(), "Custom", StringComparison.OrdinalIgnoreCase)
                ? normalizedHex
                : _colorComboBox.SelectedItem?.ToString() ?? "Red";

            var profile = _editing != null
                ? _editing.CloneAndUpdate(url, displayName!, primaryColorValue, ignoreCert, localCacheEnabled)
                : new GuacamoleServerProfile(url, displayName!, primaryColorValue, ignoreCert, localCacheEnabled, false);

            bool creating = _editing == null;
            bool shouldDeleteCache = _editing?.LocalCacheEnabled == true && !localCacheEnabled;
            _manager.Upsert(profile);
            if (creating && _isFirstProfile)
                _manager.SetDefault(profile.Id);

            await _manager.SaveAsync().ConfigureAwait(true);
            if (shouldDeleteCache)
                GuacamoleBrowserCache.DeleteProfileCacheDirectory("GuacamoleClient-Avalonia", profile.Id);

            ResultProfile = _manager.ServerProfiles.First(p => p.Id == profile.Id);
            Close(ResultProfile);
        }
        finally
        {
            _saveButton.IsEnabled = true;
        }
    }
}
