using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GuacamoleClient.Common.Settings;
using GuacamoleClient.Common.Localization;

namespace GuacamoleClient.WinForms
{
    internal sealed partial class ManageServersForm : Form
    {
        private readonly GuacamoleSettingsManager _manager;
        // Controls are defined in ManageServersForm.Designer.cs

        public ManageServersForm(GuacamoleSettingsManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));

            InitializeComponent();

            _list.DoubleClick += (_, __) => EditSelected();
            _list.SelectedIndexChanged += (_, __) => UpdateButtons();

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

        private void ApplyLocalization()
        {
            Text = LocalizationProvider.Get(LocalizationKeys.ManageServers_Title);

            _list.Columns.Clear();
            _list.Columns.Add(LocalizationProvider.Get(LocalizationKeys.ChooseServer_Column_Name), 200);
            _list.Columns.Add(LocalizationProvider.Get(LocalizationKeys.ChooseServer_Column_Url), 460);
            _list.Columns.Add(LocalizationProvider.Get(LocalizationKeys.ChooseServer_Column_Color), 100);

            _btnAdd.Text = LocalizationProvider.Get(LocalizationKeys.ManageServers_Button_Add);
            _btnEdit.Text = LocalizationProvider.Get(LocalizationKeys.ManageServers_Button_Edit);
            _btnRemove.Text = LocalizationProvider.Get(LocalizationKeys.ManageServers_Button_Remove);
            _btnSetDefault.Text = LocalizationProvider.Get(LocalizationKeys.ManageServers_Button_SetDefault);
            _btnClose.Text = LocalizationProvider.Get(LocalizationKeys.ManageServers_Button_Close);
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
                item.SubItems.Add(ColorValueResolver.ResolveToHex(p.ColorValue));
                _list.Items.Add(item);
            }
            UpdateButtons();
        }

        private GuacamoleServerProfile? GetSelected()
        {
            if (_list.SelectedItems.Count == 0) return null;
            var id = (Guid)_list.SelectedItems[0].Tag;
            return _manager.ServerProfiles.FirstOrDefault(p => p.Id == id);
        }

        private void UpdateButtons()
        {
            var has = _list.SelectedItems.Count > 0;
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
                    LocalizationProvider.Get(LocalizationKeys.ManageServers_ConfirmRemove_Text),
                    LocalizationProvider.Get(LocalizationKeys.ManageServers_ConfirmRemove_Title),
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
    }
}
