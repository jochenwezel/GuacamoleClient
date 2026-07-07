using GuacamoleClient.Common.Localization;
using GuacamoleClient.Common.Updates;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    internal sealed class ClickOnceCleanupManager
    {
        private static readonly TimeSpan PendingCandidateMaxAge = TimeSpan.FromDays(1);

        private readonly ClickOnceCleanupStateStore _stateStore;
        private readonly AppInfo _currentAppInfo;
        private readonly string _currentAppDirectory;

        public ClickOnceCleanupManager(
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
            if (!IsClickOnceApp(_currentAppInfo))
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

        public async Task ProcessPendingCleanupAsync(IWin32Window owner)
        {
            if (!IsClickOnceApp(_currentAppInfo))
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

                CandidateValidationResult validation = ValidateCandidate(candidate);
                if (validation == CandidateValidationResult.RemoveSilently)
                {
                    state.PendingCandidates.Remove(candidate);
                    continue;
                }

                if (validation == CandidateValidationResult.KeepPending)
                    continue;

                DialogResult dialogResult = MessageBox.Show(
                    owner,
                    LocalizationProvider.Get(
                        LocalizationKeys.ClickOnceCleanup_Confirm_Text,
                        candidate.Version,
                        candidate.Path),
                    LocalizationProvider.Get(LocalizationKeys.ClickOnceCleanup_Title),
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (dialogResult == DialogResult.Cancel)
                    break;

                if (dialogResult == DialogResult.No)
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

        private CandidateValidationResult ValidateCandidate(ClickOnceCleanupCandidate candidate)
        {
            if (!IsSameApp(candidate)
                || string.IsNullOrWhiteSpace(candidate.Path)
                || !Directory.Exists(candidate.Path))
            {
                return CandidateValidationResult.RemoveSilently;
            }

            if (ClickOnceCleanupStateStore.PathsOverlap(candidate.Path, _currentAppDirectory))
                return CandidateValidationResult.RemoveSilently;

            AppInfo? candidateAppInfo = ClickOnceCleanupScanner.TryLoadAppInfo(candidate.Path);
            if (candidateAppInfo == null
                || !IsSameApp(candidateAppInfo)
                || !ClickOnceCleanupScanner.IsOlderVersion(candidateAppInfo.CurrentVersion, _currentAppInfo.CurrentVersion))
            {
                return CandidateValidationResult.RemoveSilently;
            }

            return CandidateValidationResult.PromptUser;
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

        private static bool IsClickOnceApp(AppInfo appInfo)
            => string.Equals(appInfo.DeploymentType, "clickonce", StringComparison.OrdinalIgnoreCase);

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

        private enum CandidateValidationResult
        {
            RemoveSilently,
            KeepPending,
            PromptUser
        }
    }
}
