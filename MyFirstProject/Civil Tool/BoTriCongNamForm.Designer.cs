using System.Windows.Forms;
using System.Drawing;

namespace MyFirstProject.Civil_Tool
{
    partial class BoTriCongNamForm
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
            this.grpTarget = new System.Windows.Forms.GroupBox();
            this.lblAlign = new System.Windows.Forms.Label();
            this.txtAlignName = new System.Windows.Forms.TextBox();
            this.btnPickAlignment = new System.Windows.Forms.Button();
            this.lblEnt1 = new System.Windows.Forms.Label();
            this.btnPickEnt1 = new System.Windows.Forms.Button();
            this.lblEnt2 = new System.Windows.Forms.Label();
            this.btnPickEnt2 = new System.Windows.Forms.Button();

            this.grpParams = new System.Windows.Forms.GroupBox();
            this.lblStandard = new System.Windows.Forms.Label();
            this.cmbStandard = new System.Windows.Forms.ComboBox();
            this.lblVtk = new System.Windows.Forms.Label();
            this.cmbVtk = new System.Windows.Forms.ComboBox();
            this.lblAngleTitle = new System.Windows.Forms.Label();
            this.lblAngleVal = new System.Windows.Forms.Label();
            this.pnlSuggest = new System.Windows.Forms.Panel();
            this.lblSuggest = new System.Windows.Forms.Label();
            this.lblR = new System.Windows.Forms.Label();
            this.txtR = new System.Windows.Forms.TextBox();
            this.lblLs = new System.Windows.Forms.Label();
            this.txtLs = new System.Windows.Forms.TextBox();

            this.btnApply = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            this.grpTarget.SuspendLayout();
            this.grpParams.SuspendLayout();
            this.pnlSuggest.SuspendLayout();
            this.SuspendLayout();

            // 
            // grpTarget (Khung chọn đối tượng)
            // 
            this.grpTarget.Controls.Add(this.lblAlign);
            this.grpTarget.Controls.Add(this.txtAlignName);
            this.grpTarget.Controls.Add(this.btnPickAlignment);
            this.grpTarget.Controls.Add(this.lblEnt1);
            this.grpTarget.Controls.Add(this.btnPickEnt1);
            this.grpTarget.Controls.Add(this.lblEnt2);
            this.grpTarget.Controls.Add(this.btnPickEnt2);
            this.grpTarget.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpTarget.Location = new System.Drawing.Point(15, 12);
            this.grpTarget.Name = "grpTarget";
            this.grpTarget.Size = new System.Drawing.Size(450, 130);
            this.grpTarget.TabIndex = 0;
            this.grpTarget.TabStop = false;
            this.grpTarget.Text = "Đối tượng Tuyến && Cánh tuyến";

            // lblAlign
            this.lblAlign.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblAlign.Location = new System.Drawing.Point(15, 28);
            this.lblAlign.Name = "lblAlign";
            this.lblAlign.Size = new System.Drawing.Size(120, 23);
            this.lblAlign.Text = "Tuyến (Alignment):";
            this.lblAlign.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // txtAlignName
            this.txtAlignName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtAlignName.Location = new System.Drawing.Point(140, 28);
            this.txtAlignName.Name = "txtAlignName";
            this.txtAlignName.ReadOnly = true;
            this.txtAlignName.Size = new System.Drawing.Size(210, 23);

            // btnPickAlignment
            this.btnPickAlignment.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnPickAlignment.Location = new System.Drawing.Point(358, 27);
            this.btnPickAlignment.Name = "btnPickAlignment";
            this.btnPickAlignment.Size = new System.Drawing.Size(75, 25);
            this.btnPickAlignment.Text = "Chọn...";

            // lblEnt1
            this.lblEnt1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblEnt1.Location = new System.Drawing.Point(15, 62);
            this.lblEnt1.Name = "lblEnt1";
            this.lblEnt1.Size = new System.Drawing.Size(270, 23);
            this.lblEnt1.Text = "Cánh tuyến 1: (Chưa chọn)";
            this.lblEnt1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // btnPickEnt1
            this.btnPickEnt1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnPickEnt1.Location = new System.Drawing.Point(293, 61);
            this.btnPickEnt1.Name = "btnPickEnt1";
            this.btnPickEnt1.Size = new System.Drawing.Size(140, 25);
            this.btnPickEnt1.Text = "Chọn Cánh tuyến 1";

            // lblEnt2
            this.lblEnt2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblEnt2.Location = new System.Drawing.Point(15, 93);
            this.lblEnt2.Name = "lblEnt2";
            this.lblEnt2.Size = new System.Drawing.Size(270, 23);
            this.lblEnt2.Text = "Cánh tuyến 2: (Chưa chọn)";
            this.lblEnt2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // btnPickEnt2
            this.btnPickEnt2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnPickEnt2.Location = new System.Drawing.Point(293, 92);
            this.btnPickEnt2.Name = "btnPickEnt2";
            this.btnPickEnt2.Size = new System.Drawing.Size(140, 25);
            this.btnPickEnt2.Text = "Chọn Cánh tuyến 2";

            // 
            // grpParams (Khung thông số bố trí)
            // 
            this.grpParams.Controls.Add(this.lblStandard);
            this.grpParams.Controls.Add(this.cmbStandard);
            this.grpParams.Controls.Add(this.lblVtk);
            this.grpParams.Controls.Add(this.cmbVtk);
            this.grpParams.Controls.Add(this.lblAngleTitle);
            this.grpParams.Controls.Add(this.lblAngleVal);
            this.grpParams.Controls.Add(this.pnlSuggest);
            this.grpParams.Controls.Add(this.lblR);
            this.grpParams.Controls.Add(this.txtR);
            this.grpParams.Controls.Add(this.lblLs);
            this.grpParams.Controls.Add(this.txtLs);
            this.grpParams.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpParams.Location = new System.Drawing.Point(15, 148);
            this.grpParams.Name = "grpParams";
            this.grpParams.Size = new System.Drawing.Size(450, 220);
            this.grpParams.TabIndex = 1;
            this.grpParams.TabStop = false;
            this.grpParams.Text = "Thông số thiết kế Đường cong";

            // lblStandard
            this.lblStandard.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblStandard.Location = new System.Drawing.Point(15, 26);
            this.lblStandard.Name = "lblStandard";
            this.lblStandard.Size = new System.Drawing.Size(140, 23);
            this.lblStandard.Text = "Tiêu chuẩn TK:";
            this.lblStandard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // cmbStandard
            this.cmbStandard.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStandard.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbStandard.FormattingEnabled = true;
            this.cmbStandard.Items.AddRange(new object[] { "TCVN 4054:2005", "TCVN 5729:2012 (Cao tốc)", "QCVN 07:2023" });
            this.cmbStandard.Location = new System.Drawing.Point(160, 26);
            this.cmbStandard.Name = "cmbStandard";
            this.cmbStandard.Size = new System.Drawing.Size(273, 23);

            // lblVtk
            this.lblVtk.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblVtk.Location = new System.Drawing.Point(15, 56);
            this.lblVtk.Name = "lblVtk";
            this.lblVtk.Size = new System.Drawing.Size(140, 23);
            this.lblVtk.Text = "Vận tốc TK (km/h):";
            this.lblVtk.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // cmbVtk
            this.cmbVtk.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbVtk.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbVtk.FormattingEnabled = true;
            this.cmbVtk.Items.AddRange(new object[] { "40", "60", "80", "100", "120" });
            this.cmbVtk.Location = new System.Drawing.Point(160, 56);
            this.cmbVtk.Name = "cmbVtk";
            this.cmbVtk.Size = new System.Drawing.Size(100, 23);

            // lblAngleTitle
            this.lblAngleTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblAngleTitle.Location = new System.Drawing.Point(270, 56);
            this.lblAngleTitle.Name = "lblAngleTitle";
            this.lblAngleTitle.Size = new System.Drawing.Size(75, 23);
            this.lblAngleTitle.Text = "Góc chuyển α:";
            this.lblAngleTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // lblAngleVal
            this.lblAngleVal.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblAngleVal.ForeColor = System.Drawing.Color.DarkGreen;
            this.lblAngleVal.Location = new System.Drawing.Point(345, 56);
            this.lblAngleVal.Name = "lblAngleVal";
            this.lblAngleVal.Size = new System.Drawing.Size(88, 23);
            this.lblAngleVal.Text = "--°";
            this.lblAngleVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // pnlSuggest (Khung thông báo gợi ý 1 dòng duy nhất)
            this.pnlSuggest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.pnlSuggest.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlSuggest.Controls.Add(this.lblSuggest);
            this.pnlSuggest.Location = new System.Drawing.Point(15, 86);
            this.pnlSuggest.Name = "pnlSuggest";
            this.pnlSuggest.Size = new System.Drawing.Size(418, 28);

            // lblSuggest (Chỉ 1 dòng)
            this.lblSuggest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSuggest.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblSuggest.ForeColor = System.Drawing.Color.DarkBlue;
            this.lblSuggest.Location = new System.Drawing.Point(0, 0);
            this.lblSuggest.Name = "lblSuggest";
            this.lblSuggest.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSuggest.Size = new System.Drawing.Size(416, 26);
            this.lblSuggest.Text = "Gợi ý TC: Rmin = 400m | Ls = 85m";
            this.lblSuggest.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // lblR
            this.lblR.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblR.Location = new System.Drawing.Point(15, 124);
            this.lblR.Name = "lblR";
            this.lblR.Size = new System.Drawing.Size(140, 23);
            this.lblR.Text = "Bán kính R (m):";
            this.lblR.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // txtR
            this.txtR.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtR.Location = new System.Drawing.Point(160, 124);
            this.txtR.Name = "txtR";
            this.txtR.Size = new System.Drawing.Size(120, 23);

            // lblLs
            this.lblLs.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblLs.Location = new System.Drawing.Point(15, 155);
            this.lblLs.Name = "lblLs";
            this.lblLs.Size = new System.Drawing.Size(140, 23);
            this.lblLs.Text = "Chiều dài Ls (m):";
            this.lblLs.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // txtLs
            this.txtLs.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtLs.Location = new System.Drawing.Point(160, 155);
            this.txtLs.Name = "txtLs";
            this.txtLs.Size = new System.Drawing.Size(120, 23);

            // 
            // btnApply & btnCancel (Các nút thực thi)
            // 
            this.btnApply.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnApply.Location = new System.Drawing.Point(250, 380);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(105, 32);
            this.btnApply.TabIndex = 2;
            this.btnApply.Text = "Bố trí cong";
            this.btnApply.UseVisualStyleBackColor = true;

            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnCancel.Location = new System.Drawing.Point(363, 380);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(102, 32);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Hủy";
            this.btnCancel.UseVisualStyleBackColor = true;

            // 
            // BoTriCongNamForm
            // 
            this.AcceptButton = this.btnApply;
            this.CancelButton = this.btnCancel;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 425);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.grpParams);
            this.Controls.Add(this.grpTarget);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BoTriCongNamForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bố trí cong nằm tuyến đường";

            this.grpTarget.ResumeLayout(false);
            this.grpTarget.PerformLayout();
            this.grpParams.ResumeLayout(false);
            this.grpParams.PerformLayout();
            this.pnlSuggest.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.GroupBox grpTarget;
        private System.Windows.Forms.GroupBox grpParams;
        private System.Windows.Forms.Panel pnlSuggest;

        private System.Windows.Forms.Label lblStandard;
        private System.Windows.Forms.ComboBox cmbStandard;
        private System.Windows.Forms.Label lblVtk;
        private System.Windows.Forms.ComboBox cmbVtk;
        private System.Windows.Forms.Label lblAngleTitle;
        private System.Windows.Forms.Label lblAngleVal;
        private System.Windows.Forms.Label lblR;
        private System.Windows.Forms.TextBox txtR;
        private System.Windows.Forms.Label lblLs;
        private System.Windows.Forms.TextBox txtLs;
        private System.Windows.Forms.Button btnPickAlignment;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Label lblSuggest;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblAlign;
        private System.Windows.Forms.TextBox txtAlignName;
        private System.Windows.Forms.Button btnPickEnt1;
        private System.Windows.Forms.Button btnPickEnt2;
        private System.Windows.Forms.Label lblEnt1;
        private System.Windows.Forms.Label lblEnt2;
    }
}
