using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Form nhập thông số cho lệnh Hiệu Chỉnh Khoảng Cách Nhóm Cọc
    /// </summary>
    public class HieuChinhKhoangCachCocForm : Form
    {
        // Static variables to remember last input values
        private static decimal _lastKhoangCach = 20;

        // Properties to return data
        public double KhoangCachYeuCau { get; private set; } = 20;
        public bool FormAccepted { get; private set; } = false;

        // UI Controls
        private WinFormsLabel lblTitle = null!;
        private WinFormsLabel lblKhoangCach = null!;
        private WinFormsLabel lblSoCoc = null!;
        private WinFormsLabel lblThongTin = null!;
        private WinFormsLabel lblSoCocValue = null!;
        private WinFormsLabel lblThongTinValue = null!;

        private NumericUpDown numKhoangCach = null!;
        private GroupBox grpThongSo = null!;
        private GroupBox grpThongTin = null!;

        private Button btnOK = null!;
        private Button btnCancel = null!;

        // Properties for displaying info
        public int SoCocTrongNhom
        {
            set { lblSoCocValue.Text = value.ToString() + " cọc"; }
        }

        public string ThongTinLyTrinh
        {
            set { lblThongTinValue.Text = value; }
        }

        public HieuChinhKhoangCachCocForm()
        {
            InitializeComponent();
            RestoreLastUsedValues();
        }

        private void InitializeComponent()
        {
            // Standard Font
            var standardFont = new WinFormsFont("Segoe UI", 10F, FontStyle.Regular);
            var boldFont = new WinFormsFont("Segoe UI", 10F, FontStyle.Bold);
            var titleFont = new WinFormsFont("Segoe UI", 14F, FontStyle.Bold);

            // Initialize controls
            this.lblTitle = new WinFormsLabel();
            this.lblKhoangCach = new WinFormsLabel();
            this.lblSoCoc = new WinFormsLabel();
            this.lblThongTin = new WinFormsLabel();
            this.lblSoCocValue = new WinFormsLabel();
            this.lblThongTinValue = new WinFormsLabel();

            this.numKhoangCach = new NumericUpDown();
            this.grpThongSo = new GroupBox();
            this.grpThongTin = new GroupBox();

            this.btnOK = new Button();
            this.btnCancel = new Button();

            this.SuspendLayout();

            // Form
            this.Text = "Hiệu Chỉnh Khoảng Cách Nhóm Cọc";
            this.Size = new Size(460, 360);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = standardFont;

            // Title Label
            this.lblTitle.Text = "HIỆU CHỈNH KHOẢNG CÁCH NHÓM CỌC";
            this.lblTitle.Font = titleFont;
            this.lblTitle.Location = new WinFormsPoint(20, 20);
            this.lblTitle.Size = new Size(400, 30);
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.ForeColor = Color.FromArgb(0, 102, 204); // Nice Blue

            // Thông số Group
            this.grpThongSo.Text = "Thông số";
            this.grpThongSo.Font = boldFont;
            this.grpThongSo.Location = new WinFormsPoint(20, 70);
            this.grpThongSo.Size = new Size(405, 80);
            this.grpThongSo.ForeColor = Color.Black;

            // Khoảng cách yêu cầu
            this.lblKhoangCach.Text = "Khoảng cách yêu cầu (m):";
            this.lblKhoangCach.Font = standardFont;
            this.lblKhoangCach.Location = new WinFormsPoint(20, 35);
            this.lblKhoangCach.Size = new Size(180, 25);

            this.numKhoangCach.Location = new WinFormsPoint(210, 33);
            this.numKhoangCach.Size = new Size(120, 25);
            this.numKhoangCach.Minimum = 1;
            this.numKhoangCach.Maximum = 1000;
            this.numKhoangCach.Value = 20;
            this.numKhoangCach.DecimalPlaces = 2;
            this.numKhoangCach.Increment = 5;
            this.numKhoangCach.Font = standardFont;

            // Add controls to thông số group
            this.grpThongSo.Controls.AddRange(new Control[] {
                lblKhoangCach, numKhoangCach
            });

            // Thông tin nhóm cọc Group
            this.grpThongTin.Text = "Thông tin nhóm cọc";
            this.grpThongTin.Font = boldFont;
            this.grpThongTin.Location = new WinFormsPoint(20, 170);
            this.grpThongTin.Size = new Size(405, 90);
            this.grpThongTin.ForeColor = Color.Black;

            // Số cọc
            this.lblSoCoc.Text = "Số cọc trong nhóm:";
            this.lblSoCoc.Font = standardFont;
            this.lblSoCoc.Location = new WinFormsPoint(20, 30);
            this.lblSoCoc.Size = new Size(150, 25);

            this.lblSoCocValue.Text = "0 cọc";
            this.lblSoCocValue.Font = new WinFormsFont("Segoe UI", 10F, FontStyle.Bold);
            this.lblSoCocValue.Location = new WinFormsPoint(180, 30);
            this.lblSoCocValue.Size = new Size(200, 25);
            this.lblSoCocValue.ForeColor = Color.DarkGreen;

            // Thông tin lý trình
            this.lblThongTin.Text = "Phạm vi lý trình:";
            this.lblThongTin.Font = standardFont;
            this.lblThongTin.Location = new WinFormsPoint(20, 60);
            this.lblThongTin.Size = new Size(150, 25);

            this.lblThongTinValue.Text = "---";
            this.lblThongTinValue.Font = standardFont;
            this.lblThongTinValue.Location = new WinFormsPoint(180, 60);
            this.lblThongTinValue.Size = new Size(200, 25);
            this.lblThongTinValue.ForeColor = Color.FromArgb(0, 102, 204);

            // Add controls to thông tin group
            this.grpThongTin.Controls.AddRange(new Control[] {
                lblSoCoc, lblSoCocValue,
                lblThongTin, lblThongTinValue
            });

            // OK Button
            this.btnOK.Text = "OK";
            this.btnOK.Location = new WinFormsPoint(230, 280);
            this.btnOK.Size = new Size(90, 32);
            this.btnOK.Font = boldFont;
            this.btnOK.Click += BtnOK_Click;

            // Cancel Button
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new WinFormsPoint(335, 280);
            this.btnCancel.Size = new Size(90, 32);
            this.btnCancel.Font = standardFont;
            this.btnCancel.Click += BtnCancel_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                lblTitle,
                grpThongSo,
                grpThongTin,
                btnOK,
                btnCancel
            });

            this.ResumeLayout(false);
        }

        private void RestoreLastUsedValues()
        {
            numKhoangCach.Value = _lastKhoangCach;
        }

        private void SaveLastUsedValues()
        {
            _lastKhoangCach = numKhoangCach.Value;
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Get values
            KhoangCachYeuCau = (double)numKhoangCach.Value;

            // Save for next time
            SaveLastUsedValues();

            FormAccepted = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            FormAccepted = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
