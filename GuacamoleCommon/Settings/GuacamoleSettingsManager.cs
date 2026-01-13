using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GuacamoleClient.Common.Settings
{
    /// <summary>
    /// High-level API for managing persisted Guacamole server profiles.
    /// Note: Registry migration is platform-specific and implemented in the WinForms app.
    /// </summary>
    public sealed class GuacamoleSettingsManager
    {
        private readonly IGuacamoleSettingsStore _store;
        private GuacamoleSettingsDocument _doc;

        public GuacamoleSettingsManager(IGuacamoleSettingsStore store, GuacamoleSettingsDocument document)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _doc = document ?? throw new ArgumentNullException(nameof(document));
            NormalizeDefaultFlags();
        }

        public static async Task<GuacamoleSettingsManager> LoadAsync(IGuacamoleSettingsStore store)
        {
            var doc = await store.LoadAsync().ConfigureAwait(false);
            return new GuacamoleSettingsManager(store, doc);
        }

        public IReadOnlyList<GuacamoleServerProfile> ServerProfiles => _doc.ServerProfiles;

        public GuacamoleServerProfile? GetDefaultOrNull()
        {
            if (_doc.DefaultServerId != null)
            {
                var byId = _doc.ServerProfiles.FirstOrDefault(p => p.Id == _doc.DefaultServerId.Value);
                if (byId != null) return byId;
            }
            return _doc.ServerProfiles.FirstOrDefault(p => p.IsDefault);
        }

        public GuacamoleServerProfile? GetDefaultOrFirstOrNull()
            => GetDefaultOrNull() ?? _doc.ServerProfiles.FirstOrDefault();

        public void SetDefault(Guid profileId)
        {
            foreach (var p in _doc.ServerProfiles)
                p.IsDefault = p.Id == profileId;
            _doc.DefaultServerId = profileId;
        }

        public bool UrlExists(string url, Guid? exceptId = null)
        {
            return _doc.ServerProfiles.Any(p => string.Equals(p.Url, url, StringComparison.Ordinal)
                                             && (exceptId == null || p.Id != exceptId.Value));
        }

        /// <summary>
        /// Adds a new Guacamole server profile or updates an existing one with the specified values.
        /// </summary>
        /// <remarks>If a profile with the same Id already exists, its properties are updated with the
        /// values from the provided profile. If no such profile exists, a new one is added. The CreatedUtc and
        /// UpdatedUtc timestamps are set automatically. The IsDefault property is managed separately and is not
        /// modified by this method.</remarks>
        /// <param name="profile">The server profile to add or update. Must not be null, and its Url property must not be null or whitespace.</param>
        /// <exception cref="ArgumentNullException">Thrown if the profile parameter is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the Url property of profile is null or consists only of white-space characters.</exception>
        public void Upsert(GuacamoleServerProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (string.IsNullOrWhiteSpace(profile.Url)) throw new ArgumentException("Url required", nameof(profile));
            profile.UpdatedUtc = DateTimeOffset.UtcNow;

            var existing = _doc.ServerProfiles.FirstOrDefault(p => p.Id == profile.Id);
            if (existing == null)
            {
                profile.CreatedUtc = DateTimeOffset.UtcNow;
                _doc.ServerProfiles.Add(profile);
            }
            else
            {
                existing.Url = profile.Url;
                existing.DisplayName = profile.DisplayName;
                existing.ColorValue = profile.ColorValue;
                existing.IgnoreCertificateErrors = profile.IgnoreCertificateErrors;
                existing.UpdatedUtc = profile.UpdatedUtc;
                // IsDefault handled via SetDefault
            }

            NormalizeDefaultFlags();
        }

        public void Remove(Guid profileId)
        {
            var idx = _doc.ServerProfiles.FindIndex(p => p.Id == profileId);
            if (idx < 0) return;
            bool wasDefault = _doc.ServerProfiles[idx].IsDefault || _doc.DefaultServerId == profileId;
            _doc.ServerProfiles.RemoveAt(idx);

            if (wasDefault)
            {
                var next = _doc.ServerProfiles.FirstOrDefault();
                if (next != null)
                    SetDefault(next.Id);
                else
                    _doc.DefaultServerId = null;
            }

            NormalizeDefaultFlags();
        }

        public async Task SaveAsync() => await _store.SaveAsync(_doc).ConfigureAwait(false);

        public string SettingsFilePath => _store.SettingsFilePath;

        /// <summary>
        /// Ensures that the default server profile flags and identifiers are consistent within the server profiles
        /// collection.
        /// </summary>
        /// <remarks>This method synchronizes the default server profile by updating both the default
        /// server identifier and the IsDefault flag on each profile. If neither is set, the first server profile in the
        /// collection is designated as the default. This helps maintain a single, unambiguous default server
        /// profile.</remarks>
        private void NormalizeDefaultFlags()
        {
            // Keep IsDefault and DefaultServerId consistent.
            if (_doc.DefaultServerId != null)
            {
                foreach (var p in _doc.ServerProfiles)
                    p.IsDefault = p.Id == _doc.DefaultServerId.Value;
                return;
            }

            // If one IsDefault exists, use it
            var def = _doc.ServerProfiles.FirstOrDefault(p => p.IsDefault);
            if (def != null)
            {
                _doc.DefaultServerId = def.Id;
                foreach (var p in _doc.ServerProfiles)
                    p.IsDefault = p.Id == def.Id;
                return;
            }

            // No default set
            if (_doc.ServerProfiles.Count > 0)
            {
                _doc.ServerProfiles[0].IsDefault = true;
                _doc.DefaultServerId = _doc.ServerProfiles[0].Id;
            }
        }
    }
}
