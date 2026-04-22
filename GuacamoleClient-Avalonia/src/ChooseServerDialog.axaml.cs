using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using GuacamoleClient.Common.Localization;
using GuacamoleClient.Common.Settings;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GuacClient;

public partial class ChooseServerDialog : Window
{
    private readonly GuacamoleSettingsManager? _manager;
    private readonly ObservableCollection<ProfileListItem> _items = new();
    private ListBox _profilesListBox = default!;
    private Button _openButton = default!;
    private Button _editButton = default!;
    private Button _removeButton = default!;
    private Button _setDefaultButton = default!;
    private Button _closeButton = default!;

    public ChooseServerDialog()
    {
        InitializeDialog();
    }

    public ChooseServerDialog(GuacamoleSettingsManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        InitializeDialog();
        ApplyLocalization();
        RefreshList();
    }

    public GuacamoleServerProfile? SelectedProfile { get; private set; }

    private void InitializeDialog()
    {
        InitializeComponent();

        _profilesListBox = this.FindControl<ListBox>("ProfilesListBox")!;
        _openButton = this.FindControl<Button>("OpenButton")!;
        _editButton = this.FindControl<Button>("EditButton")!;
        _removeButton = this.FindControl<Button>("RemoveButton")!;
        _setDefaultButton = this.FindControl<Button>("SetDefaultButton")!;
        _closeButton = this.FindControl<Button>("CloseButton")!;

        _profilesListBox.ItemsSource = _items;
        _profilesListBox.SelectionChanged += (_, __) => UpdateButtons();
        _profilesListBox.DoubleTapped += (_, __) => OpenSelected();
    }

    private void ApplyLocalization()
    {
        Title = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Title);
        this.FindControl<Button>("OpenButton")!.Content = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Button_Open);
        this.FindControl<Button>("AddButton")!.Content = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Button_Add);
        this.FindControl<Button>("EditButton")!.Content = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Button_Edit);
        this.FindControl<Button>("RemoveButton")!.Content = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Button_Remove);
        this.FindControl<Button>("SetDefaultButton")!.Content = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Button_SetDefault);
        _closeButton.Content = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Button_Close);
    }

    private void RefreshList()
    {
        if (_manager == null)
            return;

        _items.Clear();
        foreach (var profile in _manager.ServerProfiles)
        {
            var scheme = profile.LookupColorScheme();
            var title = profile.GetDisplayText();
            if (profile.IsDefault)
                title += " " + LocalizationProvider.Get(LocalizationKeys.Common_Suffix_Default);

            _items.Add(new ProfileListItem(
                profile,
                title,
                profile.Url,
                Brush.Parse(scheme.PrimaryColorHexValue),
                Brush.Parse(scheme.TextColorHexValue)));
        }

        if (_items.Count > 0)
            _profilesListBox.SelectedIndex = Math.Max(0, _profilesListBox.SelectedIndex);

        UpdateButtons();
    }

    private ProfileListItem? GetSelectedItem()
        => _profilesListBox.SelectedItem as ProfileListItem;

    private void UpdateButtons()
    {
        bool hasSelection = GetSelectedItem() != null;
        _openButton.IsEnabled = hasSelection;
        _editButton.IsEnabled = hasSelection;
        _removeButton.IsEnabled = hasSelection;
        _setDefaultButton.IsEnabled = hasSelection;
    }

    private async void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_manager == null)
            return;

        var dialog = new AddEditServerDialog(_manager, null, _manager.ServerProfiles.Count == 0)
        {
            Icon = Icon
        };
        await dialog.ShowDialog<GuacamoleServerProfile?>(this);
        RefreshList();
    }

    private async void EditButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_manager == null)
            return;

        var selected = GetSelectedItem()?.Profile;
        if (selected == null)
            return;

        var dialog = new AddEditServerDialog(_manager, selected, false)
        {
            Icon = Icon
        };
        await dialog.ShowDialog<GuacamoleServerProfile?>(this);
        RefreshList();
    }

    private async void RemoveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_manager == null)
            return;

        var selected = GetSelectedItem()?.Profile;
        if (selected == null)
            return;

        var result = await ConfirmDialog.ShowYesNoAsync(
            this,
            LocalizationProvider.Get(LocalizationKeys.ChooseServer_ConfirmRemove_Title),
            LocalizationProvider.Get(LocalizationKeys.ChooseServer_ConfirmRemove_Text, selected.GetDisplayText())).ConfigureAwait(true);

        if (!result)
            return;

        _manager.Remove(selected.Id);
        await _manager.SaveAsync().ConfigureAwait(true);
        RefreshList();
    }

    private async void SetDefaultButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_manager == null)
            return;

        var selected = GetSelectedItem()?.Profile;
        if (selected == null)
            return;

        _manager.SetDefault(selected.Id);
        await _manager.SaveAsync().ConfigureAwait(true);
        RefreshList();
    }

    private void OpenButton_Click(object? sender, RoutedEventArgs e)
        => OpenSelected();

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
        => Close(null);

    private void OpenSelected()
    {
        SelectedProfile = GetSelectedItem()?.Profile;
        if (SelectedProfile != null)
            Close(SelectedProfile);
    }

}

public sealed record ProfileListItem(
    GuacamoleServerProfile Profile,
    string Title,
    string Url,
    IBrush BackgroundBrush,
    IBrush ForegroundBrush);
