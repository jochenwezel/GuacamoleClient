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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddEditServerForm));
            _txtUrl = new TextBox();
            _txtName = new TextBox();
            _cmbColor = new ComboBox();
            _txtCustomHex = new TextBox();
            _pnlColorPreview = new Panel();
            _chkIgnoreCert = new CheckBox();
            _btnSave = new Button();
            _btnCancel = new Button();
            _lblUrl = new Label();
            _lblName = new Label();
            _lblColor = new Label();
            _lblCustomHex = new Label();
            _layout = new TableLayoutPanel();
            linkLabelHelpGuacamoleTestServer = new LinkLabel();
            _buttons = new FlowLayoutPanel();
            _layout.SuspendLayout();
            _buttons.SuspendLayout();
            SuspendLayout();
            // 
            // _txtUrl
            // 
            _txtUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _layout.SetColumnSpan(_txtUrl, 2);
            _txtUrl.Location = new Point(175, 15);
            _txtUrl.Name = "_txtUrl";
            _txtUrl.Size = new Size(430, 23);
            _txtUrl.TabIndex = 1;
            // 
            // _txtName
            // 
            _txtName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _layout.SetColumnSpan(_txtName, 2);
            _txtName.Location = new Point(175, 43);
            _txtName.Name = "_txtName";
            _txtName.Size = new Size(430, 23);
            _txtName.TabIndex = 3;
            // 
            // _cmbColor
            // 
            _cmbColor.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _cmbColor.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbColor.Location = new Point(175, 71);
            _cmbColor.Name = "_cmbColor";
            _cmbColor.Size = new Size(370, 23);
            _cmbColor.TabIndex = 5;
            // 
            // _txtCustomHex
            // 
            _txtCustomHex.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _layout.SetColumnSpan(_txtCustomHex, 2);
            _txtCustomHex.Location = new Point(175, 99);
            _txtCustomHex.Name = "_txtCustomHex";
            _txtCustomHex.Size = new Size(430, 23);
            _txtCustomHex.TabIndex = 8;
            _txtCustomHex.Visible = false;
            // 
            // _pnlColorPreview
            // 
            _pnlColorPreview.BorderStyle = BorderStyle.FixedSingle;
            _pnlColorPreview.Location = new Point(551, 71);
            _pnlColorPreview.Name = "_pnlColorPreview";
            _pnlColorPreview.Size = new Size(54, 22);
            _pnlColorPreview.TabIndex = 6;
            // 
            // _chkIgnoreCert
            // 
            _chkIgnoreCert.AutoSize = true;
            _layout.SetColumnSpan(_chkIgnoreCert, 2);
            _chkIgnoreCert.Location = new Point(175, 127);
            _chkIgnoreCert.Name = "_chkIgnoreCert";
            _chkIgnoreCert.Size = new Size(106, 19);
            _chkIgnoreCert.TabIndex = 9;
            _chkIgnoreCert.Text = "_chkIgnoreCert";
            _chkIgnoreCert.UseVisualStyleBackColor = true;
            // 
            // _btnSave
            // 
            _btnSave.Location = new Point(503, 15);
            _btnSave.Name = "_btnSave";
            _btnSave.Size = new Size(90, 27);
            _btnSave.TabIndex = 0;
            _btnSave.Text = "_btnSave";
            _btnSave.UseVisualStyleBackColor = true;
            // 
            // _btnCancel
            // 
            _btnCancel.DialogResult = DialogResult.Cancel;
            _btnCancel.Location = new Point(407, 15);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new Size(90, 27);
            _btnCancel.TabIndex = 1;
            _btnCancel.Text = "_btnCancel";
            _btnCancel.UseVisualStyleBackColor = true;
            // 
            // _lblUrl
            // 
            _lblUrl.AutoSize = true;
            _lblUrl.Location = new Point(15, 12);
            _lblUrl.Name = "_lblUrl";
            _lblUrl.Size = new Size(40, 15);
            _lblUrl.TabIndex = 0;
            _lblUrl.Text = "_lblUrl";
            // 
            // _lblName
            // 
            _lblName.AutoSize = true;
            _lblName.Location = new Point(15, 40);
            _lblName.Name = "_lblName";
            _lblName.Size = new Size(57, 15);
            _lblName.TabIndex = 2;
            _lblName.Text = "_lblName";
            // 
            // _lblColor
            // 
            _lblColor.AutoSize = true;
            _lblColor.Location = new Point(15, 68);
            _lblColor.Name = "_lblColor";
            _lblColor.Size = new Size(54, 15);
            _lblColor.TabIndex = 4;
            _lblColor.Text = "_lblColor";
            // 
            // _lblCustomHex
            // 
            _lblCustomHex.AutoSize = true;
            _lblCustomHex.Location = new Point(15, 96);
            _lblCustomHex.Name = "_lblCustomHex";
            _lblCustomHex.Size = new Size(87, 15);
            _lblCustomHex.TabIndex = 7;
            _lblCustomHex.Text = "_lblCustomHex";
            // 
            // _layout
            // 
            _layout.ColumnCount = 3;
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
            _layout.Controls.Add(_lblUrl, 0, 0);
            _layout.Controls.Add(_txtUrl, 1, 0);
            _layout.Controls.Add(_lblName, 0, 1);
            _layout.Controls.Add(_txtName, 1, 1);
            _layout.Controls.Add(_lblColor, 0, 2);
            _layout.Controls.Add(_cmbColor, 1, 2);
            _layout.Controls.Add(_pnlColorPreview, 2, 2);
            _layout.Controls.Add(_lblCustomHex, 0, 3);
            _layout.Controls.Add(_txtCustomHex, 1, 3);
            _layout.Controls.Add(_chkIgnoreCert, 1, 4);
            _layout.Controls.Add(linkLabelHelpGuacamoleTestServer, 0, 5);
            _layout.Dock = DockStyle.Fill;
            _layout.Location = new Point(0, 0);
            _layout.Name = "_layout";
            _layout.Padding = new Padding(12);
            _layout.RowCount = 6;
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _layout.Size = new Size(620, 205);
            _layout.TabIndex = 0;
            // 
            // linkLabelHelpGuacamoleTestServer
            // 
            linkLabelHelpGuacamoleTestServer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            linkLabelHelpGuacamoleTestServer.AutoSize = true;
            _layout.SetColumnSpan(linkLabelHelpGuacamoleTestServer, 3);
            linkLabelHelpGuacamoleTestServer.Location = new Point(15, 178);
            linkLabelHelpGuacamoleTestServer.Name = "linkLabelHelpGuacamoleTestServer";
            linkLabelHelpGuacamoleTestServer.Size = new Size(321, 15);
            linkLabelHelpGuacamoleTestServer.TabIndex = 10;
            linkLabelHelpGuacamoleTestServer.TabStop = true;
            linkLabelHelpGuacamoleTestServer.Text = "Help link to setup guide Guacamole Test Server with Docker";
            linkLabelHelpGuacamoleTestServer.LinkClicked += linkLabelHelpGuacamoleTestServer_LinkClicked;
            // 
            // _buttons
            // 
            _buttons.Controls.Add(_btnSave);
            _buttons.Controls.Add(_btnCancel);
            _buttons.Dock = DockStyle.Bottom;
            _buttons.FlowDirection = FlowDirection.RightToLeft;
            _buttons.Location = new Point(0, 205);
            _buttons.Name = "_buttons";
            _buttons.Padding = new Padding(12);
            _buttons.Size = new Size(620, 48);
            _buttons.TabIndex = 1;
            // 
            // AddEditServerForm
            // 
            AcceptButton = _btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _btnCancel;
            ClientSize = new Size(620, 253);
            Controls.Add(_layout);
            Controls.Add(_buttons);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddEditServerForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "AddEditServerForm";
            _layout.ResumeLayout(false);
            _layout.PerformLayout();
            _buttons.ResumeLayout(false);
            ResumeLayout(false);
        }

        private LinkLabel linkLabelHelpGuacamoleTestServer;
    }
}
