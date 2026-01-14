using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GuacamoleClient.Common.Settings;
using GuacamoleClient.Common.Localization;

namespace GuacamoleClient.WinForms
{
    internal sealed partial class ChooseServerForm : Form
    {
        private readonly GuacamoleSettingsManager _manager;
        // Controls are defined in ManageServersForm.Designer.cs

        public ChooseServerForm(GuacamoleSettingsManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));

            InitializeComponent();

            _list.DoubleClick += (_, __) => OpenSelected();
            _list.SelectedIndexChanged += (_, __) => UpdateButtons();

            _btnOpen.Click += (_, __) => OpenSelected();
            _btnAdd.Click += (_, __) => AddNew();
            _btnEdit.Click += (_, __) => EditSelected();
            _btnRemove.Click += (_, __) => RemoveSelected();
            _btnSetDefault.Click += (_, __) => SetDefaultSelected();

            Load += (_, __) =>
            {
                ApplyLocalization();
                RefreshList();
            };
        }

        public GuacamoleServerProfile? SelectedProfile { get; private set; }

        private void ApplyLocalization()
        {
            Text = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Title);

            _list.Columns.Clear();
            _list.Columns.Add(LocalizationProvider.Get(LocalizationKeys.ChooseServer_Column_Name), 200);
            _list.Columns.Add(LocalizationProvider.Get(LocalizationKeys.ChooseServer_Column_Url), 460);

            _btnOpen.Text = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Button_Open);
            _btnAdd.Text = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Button_Add);
            _btnEdit.Text = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Button_Edit);
            _btnRemove.Text = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Button_Remove);
            _btnSetDefault.Text = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Button_SetDefault);
            _btnClose.Text = LocalizationProvider.Get(LocalizationKeys.ChooseServer_Button_Close);
        }

        private void RefreshList()
        {
            _list.Items.Clear();
            foreach (var p in _manager.ServerProfiles)
            {
                var name = p.GetDisplayText();
                if (p.IsDefault) name += " " + LocalizationProvider.Get(LocalizationKeys.Common_Suffix_Default);
                var item = new ListViewItem(name) { Tag = p.Id };
                item.SubItems.Add(p.Url);
                var colorScheme = p.LookupColorScheme();
                item.BackColor = UITools.ParseHexColor(colorScheme.PrimaryColorHexValue);
                item.ForeColor = UITools.ParseHexColor(colorScheme.TextColorHexValue);
                _list.Items.Add(item);
            }
            UpdateButtons();
        }

        private GuacamoleServerProfile? GetSelected()
        {
            if (_list.SelectedItems.Count == 0) return null;
            var id = (Guid)_list.SelectedItems[0].Tag!;
            return _manager.ServerProfiles.FirstOrDefault(p => p.Id == id);
        }

        private void UpdateButtons()
        {
            var has = _list.SelectedItems.Count > 0;
            _btnOpen.Enabled = has;
            _btnEdit.Enabled = has;
            _btnRemove.Enabled = has;
            _btnSetDefault.Enabled = has;
        }

        private void AddNew()
        {
            using var dlg = new AddEditServerForm(_manager, null, isFirstProfile: _manager.ServerProfiles.Count == 0);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                RefreshList();
        }

        private void EditSelected()
        {
            var sel = GetSelected();
            if (sel == null) return;
            using var dlg = new AddEditServerForm(_manager, sel, isFirstProfile: false);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                RefreshList();
        }

        private void RemoveSelected()
        {
            var sel = GetSelected();
            if (sel == null) return;

            if (MessageBox.Show(this,
                    LocalizationProvider.Get(LocalizationKeys.ChooseServer_ConfirmRemove_Text),
                    LocalizationProvider.Get(LocalizationKeys.ChooseServer_ConfirmRemove_Title),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _manager.Remove(sel.Id);
            _manager.SaveAsync().GetAwaiter().GetResult();
            RefreshList();
        }

        private void SetDefaultSelected()
        {
            var sel = GetSelected();
            if (sel == null) return;
            _manager.SetDefault(sel.Id);
            _manager.SaveAsync().GetAwaiter().GetResult();
            RefreshList();
        }

        private void OpenSelected()
        {
            var sel = GetSelected();
            if (sel == null) return;
            SelectedProfile = sel;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
