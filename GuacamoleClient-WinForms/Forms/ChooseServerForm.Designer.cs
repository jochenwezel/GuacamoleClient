using System;
using System.Drawing;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    internal sealed partial class ChooseServerForm
    {
        private System.ComponentModel.IContainer? components;

        private ListView _list;
        private Button _btnOpen;
        private Button _btnManage;
        private Button _btnSetDefault;
        private Button _btnCancel;
        private FlowLayoutPanel _buttons;

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

            _btnOpen = new Button { Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Text = nameof(_btnOpen) };
            _btnManage = new Button { Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Text = nameof(_btnManage) };
            _btnSetDefault = new Button { Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Text = nameof(_btnSetDefault) };
            _btnCancel = new Button { Anchor = AnchorStyles.Bottom | AnchorStyles.Right, DialogResult = DialogResult.Cancel, Text = nameof(_btnCancel) };

            _buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12),
                Height = 52
            };
            _buttons.Controls.Add(_btnCancel);
            _buttons.Controls.Add(_btnOpen);
            _buttons.Controls.Add(_btnManage);
            _buttons.Controls.Add(_btnSetDefault);

            AcceptButton = _btnOpen;
            CancelButton = _btnCancel;

            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(820, 420);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "ChooseServerForm";

            Controls.Add(_list);
            Controls.Add(_buttons);
        }
    }
}
