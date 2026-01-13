using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GuacamoleClient.Common;
using GuacamoleClient.Common.Settings;
using GuacamoleClient.Common.Localization;

namespace GuacamoleClient.WinForms
{
    internal sealed partial class AddEditServerForm : Form
    {
        private readonly GuacamoleSettingsManager _manager;
        private readonly GuacamoleServerProfile? _editing;
        private readonly bool _isFirstProfile;

        // Controls are defined in AddEditServerForm.Designer.cs

        public GuacamoleServerProfile? ResultProfile { get; private set; }

        public AddEditServerForm(GuacamoleSettingsManager manager, GuacamoleServerProfile? editing, bool isFirstProfile)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _editing = editing;
            _isFirstProfile = isFirstProfile;

            InitializeComponent();

            _cmbColor.Items.AddRange(GuacamoleColorPalette.Keys.OrderBy(k => k).Cast<object>().ToArray());
            _cmbColor.Items.Add("Custom");
            _cmbColor.SelectedIndexChanged += (_, __) => UpdateColorUi();
            _txtCustomHex.TextChanged += (_, __) => UpdateColorUi();

            _btnSave.Click += async (_, __) => await SaveAsync();

            Load += (_, __) =>
            {
                ApplyLocalization();
                Populate();
            };
        }

        private void ApplyLocalization()
        {
            Text = _editing == null
                ? LocalizationProvider.Get(LocalizationKeys.AddServer_Title)
                : LocalizationProvider.Get(LocalizationKeys.EditServer_Title);

            _lblUrl.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Label_ServerUrl);
            _lblName.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Label_DisplayNameOptional);
            _lblColor.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Label_ColorScheme);
            _lblCustomHex.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Label_CustomColorHex);

            _chkIgnoreCert.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Check_IgnoreCertificateErrorsUnsafe);
            _btnSave.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Button_Save);
            _btnCancel.Text = LocalizationProvider.Get(LocalizationKeys.Common_Button_Cancel);
        }

        private void Populate()
        {
            if (_editing != null)
            {
                _txtUrl.Text = _editing.Url;
                _txtName.Text = _editing.DisplayName ?? string.Empty;
                _chkIgnoreCert.Checked = _editing.IgnoreCertificateErrors;
                // Determine selection
                if (GuacamoleColorPalette.Colors.ContainsKey(_editing.ColorValue))
                {
                    _cmbColor.SelectedItem = _editing.ColorValue;
                }
                else
                {
                    _cmbColor.SelectedItem = "Custom";
                    _txtCustomHex.Text = _editing.ColorValue;
                }
            }
            else
            {
                _txtUrl.Text = "https://";
                _txtName.Text = string.Empty;

                // First profile should be Red by default.
                _cmbColor.SelectedItem = "Red";
                _chkIgnoreCert.Checked = false;
            }

            UpdateColorUi();
        }

        private void UpdateColorUi()
        {
            var sel = _cmbColor.SelectedItem?.ToString();
            bool isCustom = string.Equals(sel, "Custom", StringComparison.OrdinalIgnoreCase);
            _txtCustomHex.Visible = isCustom;

            string colorValue = isCustom ? _txtCustomHex.Text : (sel ?? "Red");
            if (ColorValueResolver.TryResolveToHex(colorValue, out var hex))
            {
                _pnlColorPreview.BackColor = UITools.ParseHexColor(hex);
            }
            else
            {
                _pnlColorPreview.BackColor = SystemColors.Control;
            }
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            _btnSave.Enabled = false;
            try
            {
                var url = _txtUrl.Text?.Trim() ?? string.Empty;
                var displayName = string.IsNullOrWhiteSpace(_txtName.Text) ? null : _txtName.Text.Trim();
                var ignoreCert = _chkIgnoreCert.Checked;

                var sel = _cmbColor.SelectedItem?.ToString() ?? "Red";
                var colorValue = string.Equals(sel, "Custom", StringComparison.OrdinalIgnoreCase)
                    ? (_txtCustomHex.Text?.Trim() ?? string.Empty)
                    : sel;

                if (string.IsNullOrWhiteSpace(url))
                {
                    MessageBox.Show(this,
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_ServerUrlRequired),
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_Title),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (!GuacamoleUrlAndContentChecks.IsValidUrlAndAcceptedScheme(url))
                {
                    MessageBox.Show(this,
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_InvalidUrlScheme),
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_Title),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // No URL normalization -> compare exact
                var exceptId = _editing?.Id;
                if (_manager.UrlExists(url, exceptId))
                {
                    MessageBox.Show(this,
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_DuplicateUrl),
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_Title),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (!ColorValueResolver.TryResolveToHex(colorValue, out var _))
                {
                    MessageBox.Show(this,
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_InvalidColor),
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_Title),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // Color collision warning (only warning)
                var normalizedHex = ColorValueResolver.ResolveToHex(colorValue);
                var collisions = _manager.ServerProfiles
                    .Where(p => _editing == null || p.Id != _editing.Id)
                    .Where(p => string.Equals(ColorValueResolver.ResolveToHex(p.ColorValue), normalizedHex, StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.GetDisplayText())
                    .ToList();

                if (collisions.Count > 0)
                {
                    var msg = LocalizationProvider.Get(LocalizationKeys.AddEdit_Warn_ColorAlreadyInUse_Text, string.Join(", ", collisions));
                    if (MessageBox.Show(this,
                        msg,
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_Warn_ColorAlreadyInUse_Title),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) != DialogResult.Yes)
                        return;
                }

                // Mandatory server test on save (reuse existing logic)
                bool ok = GuacamoleUrlAndContentChecks.IsGuacamoleResponseWithStartPage(url, ignoreCert);
                if (!ok)
                {
                    MessageBox.Show(this,
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_TestFailed_Text, "https://remote.example.com/guacamole/"),
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_TestFailed_Title),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                var profile = _editing != null
                    ? new GuacamoleServerProfile { Id = _editing.Id }
                    : new GuacamoleServerProfile();

                profile.Url = url;
                profile.DisplayName = displayName;
                profile.IgnoreCertificateErrors = ignoreCert;
                profile.ColorValue = string.Equals(sel, "Custom", StringComparison.OrdinalIgnoreCase) ? normalizedHex : sel;

                bool creating = _editing == null;
                _manager.Upsert(profile);
                if (creating && _isFirstProfile)
                    _manager.SetDefault(profile.Id);

                await _manager.SaveAsync().ConfigureAwait(true);

                ResultProfile = _manager.ServerProfiles.First(p => p.Id == profile.Id);
                DialogResult = DialogResult.OK;
                Close();
            }
            finally
            {
                _btnSave.Enabled = true;
            }
        }
    }
}
