namespace MyFirstProject
{
    partial class DauCuoiTuyenForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblLabelSetStyle = new Label();
            cmbLabelSetStyle = new ComboBox();
            lblAlignments = new Label();
            chkListAlignments = new CheckedListBox();
            btnSelectAll = new Button();
            btnDeselectAll = new Button();
            btnOK = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTitle.Location = new Point(20, 15);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(280, 28);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Gắn Nhãn Đầu/Cuối Tuyến";
            // 
            // lblLabelSetStyle
            // 
            lblLabelSetStyle.AutoSize = true;
            lblLabelSetStyle.Font = new Font("Segoe UI", 10F);
            lblLabelSetStyle.Location = new Point(20, 55);
            lblLabelSetStyle.Name = "lblLabelSetStyle";
            lblLabelSetStyle.Size = new Size(162, 23);
            lblLabelSetStyle.TabIndex = 1;
            lblLabelSetStyle.Text = "Alignment Label Set:";
            // 
            // cmbLabelSetStyle
            // 
            cmbLabelSetStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLabelSetStyle.Font = new Font("Segoe UI", 10F);
            cmbLabelSetStyle.FormattingEnabled = true;
            cmbLabelSetStyle.Location = new Point(20, 85);
            cmbLabelSetStyle.Name = "cmbLabelSetStyle";
            cmbLabelSetStyle.Size = new Size(360, 31);
            cmbLabelSetStyle.TabIndex = 2;
            // 
            // lblAlignments
            // 
            lblAlignments.AutoSize = true;
            lblAlignments.Font = new Font("Segoe UI", 10F);
            lblAlignments.Location = new Point(20, 130);
            lblAlignments.Name = "lblAlignments";
            lblAlignments.Size = new Size(180, 23);
            lblAlignments.TabIndex = 3;
            lblAlignments.Text = "Chọn các tuyến (Alignments):";
            // 
            // chkListAlignments
            // 
            chkListAlignments.CheckOnClick = true;
            chkListAlignments.Font = new Font("Segoe UI", 9F);
            chkListAlignments.FormattingEnabled = true;
            chkListAlignments.Location = new Point(20, 160);
            chkListAlignments.Name = "chkListAlignments";
            chkListAlignments.Size = new Size(360, 180);
            chkListAlignments.TabIndex = 4;
            // 
            // btnSelectAll
            // 
            btnSelectAll.Font = new Font("Segoe UI", 9F);
            btnSelectAll.Location = new Point(20, 350);
            btnSelectAll.Name = "btnSelectAll";
            btnSelectAll.Size = new Size(100, 30);
            btnSelectAll.TabIndex = 5;
            btnSelectAll.Text = "Chọn tất cả";
            btnSelectAll.UseVisualStyleBackColor = true;
            btnSelectAll.Click += btnSelectAll_Click;
            // 
            // btnDeselectAll
            // 
            btnDeselectAll.Font = new Font("Segoe UI", 9F);
            btnDeselectAll.Location = new Point(130, 350);
            btnDeselectAll.Name = "btnDeselectAll";
            btnDeselectAll.Size = new Size(100, 30);
            btnDeselectAll.TabIndex = 6;
            btnDeselectAll.Text = "Bỏ chọn";
            btnDeselectAll.UseVisualStyleBackColor = true;
            btnDeselectAll.Click += btnDeselectAll_Click;
            // 
            // btnOK
            // 
            btnOK.Font = new Font("Segoe UI", 10F);
            btnOK.Location = new Point(190, 400);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(90, 35);
            btnOK.TabIndex = 7;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Font = new Font("Segoe UI", 10F);
            btnCancel.Location = new Point(290, 400);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 35);
            btnCancel.TabIndex = 8;
            btnCancel.Text = "Hủy";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // DauCuoiTuyenForm
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(400, 450);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(btnDeselectAll);
            Controls.Add(btnSelectAll);
            Controls.Add(chkListAlignments);
            Controls.Add(lblAlignments);
            Controls.Add(cmbLabelSetStyle);
            Controls.Add(lblLabelSetStyle);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "DauCuoiTuyenForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Gắn Nhãn Đầu/Cuối Tuyến";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private Label lblLabelSetStyle;
        private ComboBox cmbLabelSetStyle;
        private Label lblAlignments;
        private CheckedListBox chkListAlignments;
        private Button btnSelectAll;
        private Button btnDeselectAll;
        private Button btnOK;
        private Button btnCancel;
    }
}
