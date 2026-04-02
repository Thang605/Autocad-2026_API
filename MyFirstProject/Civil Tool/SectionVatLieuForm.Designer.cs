namespace MyFirstProject.Civil_Tool
{
    partial class SectionVatLieuForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.cbbLinkCodes = new System.Windows.Forms.ComboBox();
            this.lblLinkCode = new System.Windows.Forms.Label();
            this.txtPrefix = new System.Windows.Forms.TextBox();
            this.lblPrefix = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblLinkCode
            // 
            this.lblLinkCode.AutoSize = true;
            this.lblLinkCode.Location = new System.Drawing.Point(23, 27);
            this.lblLinkCode.Name = "lblLinkCode";
            this.lblLinkCode.Size = new System.Drawing.Size(126, 17);
            this.lblLinkCode.TabIndex = 0;
            this.lblLinkCode.Text = "Chọn cấu kiện Link Code:";
            // 
            // cbbLinkCodes
            // 
            this.cbbLinkCodes.FormattingEnabled = true;
            this.cbbLinkCodes.Location = new System.Drawing.Point(26, 47);
            this.cbbLinkCodes.Name = "cbbLinkCodes";
            this.cbbLinkCodes.Size = new System.Drawing.Size(325, 24);
            this.cbbLinkCodes.TabIndex = 1;
            // 
            // lblPrefix
            // 
            this.lblPrefix.AutoSize = true;
            this.lblPrefix.Location = new System.Drawing.Point(23, 86);
            this.lblPrefix.Name = "lblPrefix";
            this.lblPrefix.Size = new System.Drawing.Size(183, 17);
            this.lblPrefix.TabIndex = 2;
            this.lblPrefix.Text = "Tiền tố hiển thị (Vd: 'Chiều dài:'):";
            // 
            // txtPrefix
            // 
            this.txtPrefix.Location = new System.Drawing.Point(26, 106);
            this.txtPrefix.Name = "txtPrefix";
            this.txtPrefix.Size = new System.Drawing.Size(325, 22);
            this.txtPrefix.TabIndex = 3;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(155, 161);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(95, 30);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "Chấp nhận";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(256, 161);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(95, 30);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Hủy";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // SectionVatLieuForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(378, 212);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.txtPrefix);
            this.Controls.Add(this.lblPrefix);
            this.Controls.Add(this.cbbLinkCodes);
            this.Controls.Add(this.lblLinkCode);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SectionVatLieuForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cấu hình vẽ Polyline và gán Text Field";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.ComboBox cbbLinkCodes;
        private System.Windows.Forms.Label lblLinkCode;
        private System.Windows.Forms.TextBox txtPrefix;
        private System.Windows.Forms.Label lblPrefix;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}
