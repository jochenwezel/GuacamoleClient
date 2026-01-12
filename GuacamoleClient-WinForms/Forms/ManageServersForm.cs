using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GuacamoleClient.Common.Settings;

namespace GuacamoleClient.WinForms
{
    internal sealed class ManageServersForm : Form
    {
        private readonly GuacamoleSettingsManager _manager;
        private readonly ListView _list = new ListView { View = View.Details, FullRowSelect = true, HideSelection = false, Dock = DockStyle.Fill };
        private readonly Button _btnAdd = new Button { Text = "Add..." };
        private readonly Button _btnEdit = new Button { Text = "Edit..." };
        private readonly Button _btnRemove = new Button { Text = "Remove" };
        private readonly Button _btnSetDefault = new Button { Text = "Set default" };
        private readonly Button _btnClose = new Button { Text = "Close", DialogResult = DialogResult.OK };

        public ManageServersForm(GuacamoleSettingsManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));

            Text = "Manage Guacamole servers";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.Sizable;
            ClientSize = new Size(820, 420);

            _list.Columns.Add("Name", 200);
            _list.Columns.Add("URL", 460);
            _list.Columns.Add("Color", 100);
            _list.DoubleClick += (_, __) => EditSelected();
            _list.SelectedIndexChanged += (_, __) => UpdateButtons();

            _btnAdd.Click += (_, __) => AddNew();
            _btnEdit.Click += (_, __) => EditSelected();
            _btnRemove.Click += (_, __) => RemoveSelected();
            _btnSetDefault.Click += (_, __) => SetDefaultSelected();

            var right = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 140, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8) };
            right.Controls.Add(_btnAdd);
            right.Controls.Add(_btnEdit);
            right.Controls.Add(_btnRemove);
            right.Controls.Add(new Label { Height = 10 });
            right.Controls.Add(_btnSetDefault);
            right.Controls.Add(new Label { Height = 20 });
            right.Controls.Add(_btnClose);

            Controls.Add(_list);
            Controls.Add(right);

            Load += (_, __) => RefreshList();
        }

        private void RefreshList()
        {
            _list.Items.Clear();
            foreach (var p in _manager.ServerProfiles)
            {
                var name = p.GetDisplayText();
                if (p.IsDefault) name += " (Default)";
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

            if (MessageBox.Show(this, "Remove selected server profile?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
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
