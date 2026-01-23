using GuacamoleClient.Common;
using GuacamoleClient.Common.Localization;
using GuacamoleClient.Common.Settings;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Windows.Forms;

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
                ? LocalizationProvider.Get(LocalizationKeys.AddEdit_ModeAddServer_Title)
                : LocalizationProvider.Get(LocalizationKeys.AddEdit_ModeEditServer_Title);

            _lblUrl.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Label_ServerUrl);
            _lblName.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Label_DisplayNameOptional);
            _lblColor.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Label_ColorScheme);
            _lblCustomHex.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Label_CustomColorHex);
            linkLabelHelpGuacamoleTestServer.Text = LocalizationProvider.Get(LocalizationKeys.AddEdit_Link_SetupGuideGuacamoleTestServer);

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
                if (GuacamoleColorPalette.Colors.ContainsKey(_editing.PrimaryColorValue))
                {
                    _cmbColor.SelectedItem = _editing.PrimaryColorValue;
                }
                else
                {
                    _cmbColor.SelectedItem = "Custom";
                    _txtCustomHex.Text = _editing.PrimaryColorValue;
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
            if (GuacamoleColorPalette.TryResolveToHex(colorValue, out var hex))
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

                if (!GuacamoleColorPalette.TryResolveToHex(colorValue, out var _))
                {
                    MessageBox.Show(this,
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_InvalidColor),
                        LocalizationProvider.Get(LocalizationKeys.AddEdit_Validation_Title),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // Color collision warning (only warning)
                var normalizedHex = GuacamoleColorPalette.ResolveToHex(colorValue);
                var collisions = _manager.ServerProfiles
                    .Where(p => _editing == null || p.Id != _editing.Id)
                    .Where(p => string.Equals(GuacamoleColorPalette.ResolveToHex(p.PrimaryColorValue), normalizedHex, StringComparison.OrdinalIgnoreCase))
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

                string primaryColorValue = string.Equals(sel, "Custom", StringComparison.OrdinalIgnoreCase) ? normalizedHex : sel;
                var profile = _editing != null
                    ? _editing.CloneAndUpdate(url, displayName!, primaryColorValue, ignoreCert)
                    : new GuacamoleServerProfile(url, displayName!, primaryColorValue, ignoreCert, false);

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

        private void linkLabelHelpGuacamoleTestServer_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            UITools.OpenUrlInDefaultBrowser("https://github.com/jochenwezel/GuacamoleClient/blob/main/docs/SetupTestGuacamoleServer.md");
        }
    }
}
