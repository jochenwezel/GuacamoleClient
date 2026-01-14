using System;
using System.Drawing;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    internal sealed partial class ChooseServerForm
    {
        private System.ComponentModel.IContainer? components;

        private ListView _list;
        private Button _btnAdd;
        private Button _btnEdit;
        private Button _btnRemove;
        private Button _btnSetDefault;
        private Button _btnClose;
        private FlowLayoutPanel _right;
        private Label _spacer1;
        private Label _spacer2;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChooseServerForm));
            _list = new ListView();
            _btnAdd = new Button();
            _btnEdit = new Button();
            _btnRemove = new Button();
            _btnSetDefault = new Button();
            _btnClose = new Button();
            _right = new FlowLayoutPanel();
            _btnOpen = new Button();
            label1 = new Label();
            _spacer1 = new Label();
            _spacer2 = new Label();
            _right.SuspendLayout();
            SuspendLayout();
            // 
            // _list
            // 
            _list.Dock = DockStyle.Fill;
            _list.FullRowSelect = true;
            _list.Location = new Point(0, 0);
            _list.Name = "_list";
            _list.Size = new Size(680, 420);
            _list.TabIndex = 0;
            _list.UseCompatibleStateImageBehavior = false;
            _list.View = View.Details;
            // 
            // _btnAdd
            // 
            _btnAdd.Location = new Point(11, 54);
            _btnAdd.Name = "_btnAdd";
            _btnAdd.Size = new Size(120, 27);
            _btnAdd.TabIndex = 0;
            _btnAdd.Text = "_btnAdd";
            _btnAdd.UseVisualStyleBackColor = true;
            // 
            // _btnEdit
            // 
            _btnEdit.Enabled = false;
            _btnEdit.Location = new Point(11, 87);
            _btnEdit.Name = "_btnEdit";
            _btnEdit.Size = new Size(120, 27);
            _btnEdit.TabIndex = 1;
            _btnEdit.Text = "_btnEdit";
            _btnEdit.UseVisualStyleBackColor = true;
            // 
            // _btnRemove
            // 
            _btnRemove.Enabled = false;
            _btnRemove.Location = new Point(11, 120);
            _btnRemove.Name = "_btnRemove";
            _btnRemove.Size = new Size(120, 27);
            _btnRemove.TabIndex = 2;
            _btnRemove.Text = "_btnRemove";
            _btnRemove.UseVisualStyleBackColor = true;
            // 
            // _btnSetDefault
            // 
            _btnSetDefault.Enabled = false;
            _btnSetDefault.Location = new Point(11, 163);
            _btnSetDefault.Name = "_btnSetDefault";
            _btnSetDefault.Size = new Size(120, 27);
            _btnSetDefault.TabIndex = 4;
            _btnSetDefault.Text = "_btnSetDefault";
            _btnSetDefault.UseVisualStyleBackColor = true;
            // 
            // _btnClose
            // 
            _btnClose.DialogResult = DialogResult.OK;
            _btnClose.Location = new Point(11, 216);
            _btnClose.Name = "_btnClose";
            _btnClose.Size = new Size(120, 27);
            _btnClose.TabIndex = 6;
            _btnClose.Text = "_btnClose";
            _btnClose.UseVisualStyleBackColor = true;
            // 
            // _right
            // 
            _right.Controls.Add(_btnOpen);
            _right.Controls.Add(label1);
            _right.Controls.Add(_btnAdd);
            _right.Controls.Add(_btnEdit);
            _right.Controls.Add(_btnRemove);
            _right.Controls.Add(_spacer1);
            _right.Controls.Add(_btnSetDefault);
            _right.Controls.Add(_spacer2);
            _right.Controls.Add(_btnClose);
            _right.Dock = DockStyle.Right;
            _right.FlowDirection = FlowDirection.TopDown;
            _right.Location = new Point(680, 0);
            _right.Name = "_right";
            _right.Padding = new Padding(8);
            _right.Size = new Size(140, 420);
            _right.TabIndex = 1;
            _right.WrapContents = false;
            // 
            // _btnOpen
            // 
            _btnOpen.Location = new Point(11, 11);
            _btnOpen.Name = "_btnOpen";
            _btnOpen.Size = new Size(120, 27);
            _btnOpen.TabIndex = 7;
            _btnOpen.Text = "_btnOpen";
            _btnOpen.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.Location = new Point(11, 41);
            label1.Name = "label1";
            label1.Size = new Size(120, 10);
            label1.TabIndex = 8;
            // 
            // _spacer1
            // 
            _spacer1.Location = new Point(11, 150);
            _spacer1.Name = "_spacer1";
            _spacer1.Size = new Size(120, 10);
            _spacer1.TabIndex = 3;
            // 
            // _spacer2
            // 
            _spacer2.Location = new Point(11, 193);
            _spacer2.Name = "_spacer2";
            _spacer2.Size = new Size(120, 20);
            _spacer2.TabIndex = 5;
            // 
            // ManageServersForm
            // 
            AcceptButton = _btnOpen;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _btnClose;
            ClientSize = new Size(820, 420);
            Controls.Add(_list);
            Controls.Add(_right);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ManageServersForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "ManageServersForm";
            _right.ResumeLayout(false);
            ResumeLayout(false);
        }

        private Button _btnOpen;
        private Label label1;
    }
}
