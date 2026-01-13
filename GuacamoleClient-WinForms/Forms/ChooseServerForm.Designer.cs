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
            _list = new ListView();
            _btnOpen = new Button();
            _btnManage = new Button();
            _btnSetDefault = new Button();
            _btnCancel = new Button();
            _buttons = new FlowLayoutPanel();
            _buttons.SuspendLayout();
            SuspendLayout();

            // 
            // _list
            // 
            _list.Dock = DockStyle.Fill;
            _list.FullRowSelect = true;
            _list.HideSelection = false;
            _list.Location = new Point(0, 0);
            _list.Name = "_list";
            _list.Size = new Size(820, 368);
            _list.TabIndex = 0;
            _list.UseCompatibleStateImageBehavior = false;
            _list.View = View.Details;

            // 
            // _btnOpen
            // 
            _btnOpen.Location = new Point(3, 3);
            _btnOpen.Name = "_btnOpen";
            _btnOpen.Size = new Size(140, 27);
            _btnOpen.TabIndex = 0;
            _btnOpen.Text = "_btnOpen";
            _btnOpen.UseVisualStyleBackColor = true;

            // 
            // _btnManage
            // 
            _btnManage.Location = new Point(149, 3);
            _btnManage.Name = "_btnManage";
            _btnManage.Size = new Size(140, 27);
            _btnManage.TabIndex = 1;
            _btnManage.Text = "_btnManage";
            _btnManage.UseVisualStyleBackColor = true;

            // 
            // _btnSetDefault
            // 
            _btnSetDefault.Location = new Point(295, 3);
            _btnSetDefault.Name = "_btnSetDefault";
            _btnSetDefault.Size = new Size(140, 27);
            _btnSetDefault.TabIndex = 2;
            _btnSetDefault.Text = "_btnSetDefault";
            _btnSetDefault.UseVisualStyleBackColor = true;

            // 
            // _btnCancel
            // 
            _btnCancel.DialogResult = DialogResult.Cancel;
            _btnCancel.Location = new Point(441, 3);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new Size(140, 27);
            _btnCancel.TabIndex = 3;
            _btnCancel.Text = "_btnCancel";
            _btnCancel.UseVisualStyleBackColor = true;

            // 
            // _buttons
            // 
            _buttons.Controls.Add(_btnCancel);
            _buttons.Controls.Add(_btnOpen);
            _buttons.Controls.Add(_btnManage);
            _buttons.Controls.Add(_btnSetDefault);
            _buttons.Dock = DockStyle.Bottom;
            _buttons.FlowDirection = FlowDirection.RightToLeft;
            _buttons.Location = new Point(0, 368);
            _buttons.Name = "_buttons";
            _buttons.Padding = new Padding(12);
            _buttons.Size = new Size(820, 52);
            _buttons.TabIndex = 1;

            // 
            // ChooseServerForm
            // 
            AcceptButton = _btnOpen;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _btnCancel;
            ClientSize = new Size(820, 420);
            Controls.Add(_list);
            Controls.Add(_buttons);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ChooseServerForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "ChooseServerForm";
            _buttons.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
