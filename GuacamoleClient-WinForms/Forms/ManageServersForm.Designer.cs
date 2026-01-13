using System;
using System.Drawing;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    internal sealed partial class ManageServersForm
    {
        private System.ComponentModel.IContainer? components;

        private ListView _list;
        private Button _btnAdd;
        private Button _btnEdit;
        private Button _btnRemove;
        private Button _btnSetDefault;
        private Button _btnClose;
        private FlowLayoutPanel _right;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            _list = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                Dock = DockStyle.Fill
            };

            _btnAdd = new Button { Text = nameof(_btnAdd) };
            _btnEdit = new Button { Text = nameof(_btnEdit), Enabled = false };
            _btnRemove = new Button { Text = nameof(_btnRemove), Enabled = false };
            _btnSetDefault = new Button { Text = nameof(_btnSetDefault), Enabled = false };
            _btnClose = new Button { Text = nameof(_btnClose), DialogResult = DialogResult.OK };

            _right = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 140,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(8)
            };
            _right.Controls.Add(_btnAdd);
            _right.Controls.Add(_btnEdit);
            _right.Controls.Add(_btnRemove);
            _right.Controls.Add(new Label { Height = 10 });
            _right.Controls.Add(_btnSetDefault);
            _right.Controls.Add(new Label { Height = 20 });
            _right.Controls.Add(_btnClose);

            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(820, 420);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "ManageServersForm";

            Controls.Add(_list);
            Controls.Add(_right);
        }
    }
}
