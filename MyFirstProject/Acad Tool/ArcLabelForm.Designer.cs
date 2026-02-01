namespace MyFirstProject
{
    partial class ArcLabelForm
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
            lblCurveStyle = new Label();
            cmbCurveLabelStyle = new ComboBox();
            lblRatio = new Label();
            numRatio = new NumericUpDown();
            lblRatioHint = new Label();
            btnOK = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)numRatio).BeginInit();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTitle.Location = new Point(20, 20);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(200, 28);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Ghi Label cho Arc";
            // 
            // lblCurveStyle
            // 
            lblCurveStyle.AutoSize = true;
            lblCurveStyle.Font = new Font("Segoe UI", 10F);
            lblCurveStyle.Location = new Point(20, 70);
            lblCurveStyle.Name = "lblCurveStyle";
            lblCurveStyle.Size = new Size(160, 23);
            lblCurveStyle.TabIndex = 1;
            lblCurveStyle.Text = "Curve Label Style:";
            // 
            // cmbCurveLabelStyle
            // 
            cmbCurveLabelStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCurveLabelStyle.Font = new Font("Segoe UI", 10F);
            cmbCurveLabelStyle.FormattingEnabled = true;
            cmbCurveLabelStyle.Location = new Point(20, 100);
            cmbCurveLabelStyle.Name = "cmbCurveLabelStyle";
            cmbCurveLabelStyle.Size = new Size(360, 31);
            cmbCurveLabelStyle.TabIndex = 2;
            // 
            // lblRatio
            // 
            lblRatio.AutoSize = true;
            lblRatio.Font = new Font("Segoe UI", 10F);
            lblRatio.Location = new Point(20, 150);
            lblRatio.Name = "lblRatio";
            lblRatio.Size = new Size(147, 23);
            lblRatio.TabIndex = 3;
            lblRatio.Text = "Vị trí Label (0-1):";
            // 
            // numRatio
            // 
            numRatio.DecimalPlaces = 2;
            numRatio.Font = new Font("Segoe UI", 10F);
            numRatio.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numRatio.Location = new Point(20, 180);
            numRatio.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            numRatio.Name = "numRatio";
            numRatio.Size = new Size(100, 30);
            numRatio.TabIndex = 4;
            numRatio.Value = new decimal(new int[] { 5, 0, 0, 65536 });
            // 
            // lblRatioHint
            // 
            lblRatioHint.AutoSize = true;
            lblRatioHint.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblRatioHint.ForeColor = Color.Gray;
            lblRatioHint.Location = new Point(130, 185);
            lblRatioHint.Name = "lblRatioHint";
            lblRatioHint.Size = new Size(192, 20);
            lblRatioHint.TabIndex = 5;
            lblRatioHint.Text = "(0.5 = giữa, 0 = đầu, 1 = cuối)";
            // 
            // btnOK
            // 
            btnOK.Font = new Font("Segoe UI", 10F);
            btnOK.Location = new Point(200, 240);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(90, 35);
            btnOK.TabIndex = 6;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Font = new Font("Segoe UI", 10F);
            btnCancel.Location = new Point(300, 240);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 35);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Hủy";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // ArcLabelForm
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(410, 295);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(lblRatioHint);
            Controls.Add(numRatio);
            Controls.Add(lblRatio);
            Controls.Add(cmbCurveLabelStyle);
            Controls.Add(lblCurveStyle);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ArcLabelForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Ghi Label cho Arc";
            ((System.ComponentModel.ISupportInitialize)numRatio).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private Label lblCurveStyle;
        private ComboBox cmbCurveLabelStyle;
        private Label lblRatio;
        private NumericUpDown numRatio;
        private Label lblRatioHint;
        private Button btnOK;
        private Button btnCancel;
    }
}
