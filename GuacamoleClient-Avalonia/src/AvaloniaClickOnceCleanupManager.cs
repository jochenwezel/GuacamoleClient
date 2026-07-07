using Avalonia.Controls;
using Avalonia.Layout;
using GuacamoleClient.Common.Localization;
using GuacamoleClient.Common.Updates;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GuacClient;

internal sealed class AvaloniaClickOnceCleanupManager
{
    private static readonly TimeSpan PendingCandidateMaxAge = TimeSpan.FromDays(1);

    private readonly ClickOnceCleanupStateStore _stateStore;
    private readonly AppInfo _currentAppInfo;
    private readonly string _currentAppDirectory;

    public AvaloniaClickOnceCleanupManager(
        ClickOnceCleanupStateStore stateStore,
        AppInfo currentAppInfo,
        string currentAppDirectory)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _currentAppInfo = currentAppInfo ?? throw new ArgumentNullException(nameof(currentAppInfo));
        _currentAppDirectory = currentAppDirectory ?? throw new ArgumentNullException(nameof(currentAppDirectory));
    }

    public async Task RegisterCurrentDirectoryForCleanupAsync()
    {
        if (!IsEnabled)
            return;

        await _stateStore.AddPendingCandidateAsync(new ClickOnceCleanupCandidate
        {
            AppId = _currentAppInfo.AppId,
            Channel = _currentAppInfo.Channel,
            DeploymentType = _currentAppInfo.DeploymentType,
            Version = _currentAppInfo.CurrentVersion,
            Path = _currentAppDirectory,
            CreatedUtc = DateTimeOffset.UtcNow
        }).ConfigureAwait(false);
    }

    public async Task ProcessPendingCleanupAsync(Window owner)
    {
        if (!IsEnabled)
            return;

        ClickOnceCleanupState state = await _stateStore.LoadAsync().ConfigureAwait(true);
        ClickOnceCleanupStateStore.RemoveExpiredPendingCandidates(
            state,
            DateTimeOffset.UtcNow,
            PendingCandidateMaxAge);
        AddDiscoveredCandidates(state);

        foreach (ClickOnceCleanupCandidate candidate in state.PendingCandidates.ToArray())
        {
            if (IsIgnored(state, candidate))
            {
                state.PendingCandidates.Remove(candidate);
                continue;
            }

            if (!IsValidCandidate(candidate))
            {
                state.PendingCandidates.Remove(candidate);
                continue;
            }

            ClickOnceCleanupDialogResult dialogResult = await ShowCleanupDialogAsync(owner, candidate).ConfigureAwait(true);
            if (dialogResult == ClickOnceCleanupDialogResult.Cancel)
                break;

            if (dialogResult == ClickOnceCleanupDialogResult.No)
            {
                state.PendingCandidates.Remove(candidate);
                state.IgnoredCandidates.Add(new ClickOnceIgnoredCleanupCandidate
                {
                    Path = candidate.Path,
                    IgnoredUtc = DateTimeOffset.UtcNow
                });
                continue;
            }

            if (TryDeleteDirectory(candidate.Path))
                state.PendingCandidates.Remove(candidate);
        }

        await _stateStore.SaveOrDeleteIfEmptyAsync(state).ConfigureAwait(true);
    }

    private bool IsEnabled
        => OperatingSystem.IsWindows()
            && string.Equals(_currentAppInfo.DeploymentType, "clickonce", StringComparison.OrdinalIgnoreCase);

    private void AddDiscoveredCandidates(ClickOnceCleanupState state)
    {
        var scanner = new ClickOnceCleanupScanner(_currentAppInfo, _currentAppDirectory);
        foreach (ClickOnceCleanupCandidate candidate in scanner.DiscoverCandidates(DateTimeOffset.UtcNow))
        {
            if (IsIgnored(state, candidate)
                || state.PendingCandidates.Any(existing => ClickOnceCleanupStateStore.AreSamePath(existing.Path, candidate.Path)))
            {
                continue;
            }

            state.PendingCandidates.Add(candidate);
        }
    }

    private bool IsValidCandidate(ClickOnceCleanupCandidate candidate)
    {
        if (!IsSameApp(candidate)
            || string.IsNullOrWhiteSpace(candidate.Path)
            || !Directory.Exists(candidate.Path))
        {
            return false;
        }

        if (ClickOnceCleanupStateStore.PathsOverlap(candidate.Path, _currentAppDirectory))
            return false;

        AppInfo? candidateAppInfo = ClickOnceCleanupScanner.TryLoadAppInfo(candidate.Path);
        return candidateAppInfo != null
            && IsSameApp(candidateAppInfo)
            && ClickOnceCleanupScanner.IsOlderVersion(candidateAppInfo.CurrentVersion, _currentAppInfo.CurrentVersion);
    }

    private bool IsSameApp(ClickOnceCleanupCandidate candidate)
        => string.Equals(candidate.AppId, _currentAppInfo.AppId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidate.Channel, _currentAppInfo.Channel, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidate.DeploymentType, _currentAppInfo.DeploymentType, StringComparison.OrdinalIgnoreCase);

    private bool IsSameApp(AppInfo candidateAppInfo)
        => string.Equals(candidateAppInfo.AppId, _currentAppInfo.AppId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidateAppInfo.Channel, _currentAppInfo.Channel, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidateAppInfo.DeploymentType, _currentAppInfo.DeploymentType, StringComparison.OrdinalIgnoreCase);

    private static bool IsIgnored(ClickOnceCleanupState state, ClickOnceCleanupCandidate candidate)
        => state.IgnoredCandidates.Any(ignored => ClickOnceCleanupStateStore.AreSamePath(ignored.Path, candidate.Path));

    private static async Task<ClickOnceCleanupDialogResult> ShowCleanupDialogAsync(Window owner, ClickOnceCleanupCandidate candidate)
    {
        var yesButton = new Button
        {
            Content = LocalizationProvider.Get(LocalizationKeys.Common_Button_Yes),
            MinWidth = 86
        };
        var noButton = new Button
        {
            Content = LocalizationProvider.Get(LocalizationKeys.Common_Button_No),
            MinWidth = 86
        };
        var cancelButton = new Button
        {
            Content = LocalizationProvider.Get(LocalizationKeys.Common_Button_Cancel),
            MinWidth = 86
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttonPanel.Children.Add(yesButton);
        buttonPanel.Children.Add(noButton);
        buttonPanel.Children.Add(cancelButton);

        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            Margin = new Avalonia.Thickness(16),
            RowSpacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = LocalizationProvider.Get(
                        LocalizationKeys.ClickOnceCleanup_Confirm_Text,
                        candidate.Version,
                        candidate.Path),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    MaxWidth = 560
                },
                buttonPanel
            }
        };
        Grid.SetRow(buttonPanel, 1);

        var dialog = new Window
        {
            Title = LocalizationProvider.Get(LocalizationKeys.ClickOnceCleanup_Title),
            Width = 640,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = content
        };

        ClickOnceCleanupDialogResult result = ClickOnceCleanupDialogResult.Cancel;
        yesButton.Click += (_, __) =>
        {
            result = ClickOnceCleanupDialogResult.Yes;
            dialog.Close();
        };
        noButton.Click += (_, __) =>
        {
            result = ClickOnceCleanupDialogResult.No;
            dialog.Close();
        };
        cancelButton.Click += (_, __) =>
        {
            result = ClickOnceCleanupDialogResult.Cancel;
            dialog.Close();
        };

        await dialog.ShowDialog(owner).ConfigureAwait(true);
        return result;
    }

    private static bool TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private enum ClickOnceCleanupDialogResult
    {
        Yes,
        No,
        Cancel
    }
}
