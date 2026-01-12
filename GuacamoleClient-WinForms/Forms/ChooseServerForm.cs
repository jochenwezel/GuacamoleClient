using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GuacamoleClient.Common.Settings;

namespace GuacamoleClient.WinForms
{
    internal sealed class ChooseServerForm : Form
    {
        private readonly GuacamoleSettingsManager _manager;
        private readonly ListView _list = new ListView { View = View.Details, FullRowSelect = true, HideSelection = false, Dock = DockStyle.Fill };
        private readonly Button _btnOpen = new Button { Text = "Open (new window)", Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
        private readonly Button _btnManage = new Button { Text = "Manage...", Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
        private readonly Button _btnSetDefault = new Button { Text = "Set default", Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
        private readonly Button _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };

        public GuacamoleServerProfile? SelectedProfile { get; private set; }

        public ChooseServerForm(GuacamoleSettingsManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));

            Text = "Choose Guacamole server";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.Sizable;
            ClientSize = new Size(820, 420);

            _list.Columns.Add("Name", 220);
            _list.Columns.Add("URL", 460);
            _list.Columns.Add("Color", 100);
            _list.DoubleClick += (_, __) => OpenSelected();
            _list.SelectedIndexChanged += (_, __) => UpdateButtons();

            _btnOpen.Click += (_, __) => OpenSelected();
            _btnManage.Click += (_, __) => Manage();
            _btnSetDefault.Click += (_, __) => SetDefaultSelected();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12),
                Height = 52
            };
            buttons.Controls.Add(_btnCancel);
            buttons.Controls.Add(_btnOpen);
            buttons.Controls.Add(_btnManage);
            buttons.Controls.Add(_btnSetDefault);

            Controls.Add(_list);
            Controls.Add(buttons);

            AcceptButton = _btnOpen;
            CancelButton = _btnCancel;

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
            _btnOpen.Enabled = has;
            _btnSetDefault.Enabled = has;
        }

        private void OpenSelected()
        {
            var sel = GetSelected();
            if (sel == null) return;
            SelectedProfile = sel;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Manage()
        {
            using var dlg = new ManageServersForm(_manager);
            dlg.ShowDialog(this);
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
