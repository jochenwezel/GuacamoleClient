using System;
using System.Drawing;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    internal sealed partial class AddEditServerForm
    {
        private System.ComponentModel.IContainer? components;

        private TextBox _txtUrl;
        private TextBox _txtName;
        private ComboBox _cmbColor;
        private TextBox _txtCustomHex;
        private Panel _pnlColorPreview;
        private CheckBox _chkIgnoreCert;
        private Button _btnSave;
        private Button _btnCancel;

        private Label _lblUrl;
        private Label _lblName;
        private Label _lblColor;
        private Label _lblCustomHex;

        private TableLayoutPanel _layout;
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

            _txtUrl = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
            _txtName = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
            _cmbColor = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
            _txtCustomHex = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Visible = false };
            _pnlColorPreview = new Panel { Width = 32, Height = 18, BorderStyle = BorderStyle.FixedSingle };
            _chkIgnoreCert = new CheckBox { AutoSize = true };

            _btnSave = new Button { DialogResult = DialogResult.None, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            _btnCancel = new Button { DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };

            _lblUrl = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Text = nameof(_lblUrl) };
            _lblName = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Text = nameof(_lblName) };
            _lblColor = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Text = nameof(_lblColor) };
            _lblCustomHex = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Text = nameof(_lblCustomHex) };

            _layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 6,
                Padding = new Padding(12),
            };
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));

            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _layout.Controls.Add(_lblUrl, 0, 0);
            _layout.Controls.Add(_txtUrl, 1, 0);
            _layout.SetColumnSpan(_txtUrl, 2);

            _layout.Controls.Add(_lblName, 0, 1);
            _layout.Controls.Add(_txtName, 1, 1);
            _layout.SetColumnSpan(_txtName, 2);

            _layout.Controls.Add(_lblColor, 0, 2);
            _layout.Controls.Add(_cmbColor, 1, 2);
            _layout.Controls.Add(_pnlColorPreview, 2, 2);

            _layout.Controls.Add(_lblCustomHex, 0, 3);
            _layout.Controls.Add(_txtCustomHex, 1, 3);
            _layout.SetColumnSpan(_txtCustomHex, 2);

            _layout.Controls.Add(_chkIgnoreCert, 1, 4);
            _layout.SetColumnSpan(_chkIgnoreCert, 2);

            _buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12),
                Height = 48,
            };
            _buttons.Controls.Add(_btnSave);
            _buttons.Controls.Add(_btnCancel);

            AcceptButton = _btnSave;
            CancelButton = _btnCancel;

            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(620, 250);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "AddEditServerForm";

            Controls.Add(_layout);
            Controls.Add(_buttons);
        }
    }
}
