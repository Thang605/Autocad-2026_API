using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Form nhập thông số sắp xếp Section View vào khung in
    /// </summary>
    public class SapXepLayoutForm : Form
    {
        // Static variables to remember last input values
        private static double _lastChieuRongKhung = 76;
        private static double _lastChieuCaoKhung = 49;
        private static int _lastSoCot = 3;
        private static int _lastSoHang = 3;
        private static double _lastKhoangCachTrang = 5;
        private static double _lastKhoangDichDung = 0;

        // Properties to return data
        public double ChieuRongKhung { get; private set; } = 76;
        public double ChieuCaoKhung { get; private set; } = 49;
        public int SoCot { get; private set; } = 3;
        public int SoHang { get; private set; } = 3;
        public double KhoangCachTrang { get; private set; } = 5;
        public double KhoangDichDung { get; private set; } = 0;
        public bool FormAccepted { get; private set; } = false;

        // Computed: số section view tối đa trên 1 trang
        public int SoSVPerPage => SoCot * SoHang;

        // UI Controls
        private WinFormsLabel lblTitle = null!;
        private GroupBox grpKhungIn = null!;
        private GroupBox grpBoTri = null!;

        private WinFormsLabel lblChieuRong = null!;
        private WinFormsLabel lblChieuCao = null!;
        private NumericUpDown numChieuRong = null!;
        private NumericUpDown numChieuCao = null!;

        private WinFormsLabel lblSoCot = null!;
        private WinFormsLabel lblSoHang = null!;
        private WinFormsLabel lblKCTrang = null!;
        private WinFormsLabel lblDichDung = null!;
        private NumericUpDown numSoCot = null!;
        private NumericUpDown numSoHang = null!;
        private NumericUpDown numKCTrang = null!;
        private NumericUpDown numDichDung = null!;

        private WinFormsLabel lblPreview = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;

        public SapXepLayoutForm()
        {
            InitializeComponent();
            RestoreLastUsedValues();
            UpdatePreview();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form
            this.Text = "Sắp Xếp Section View vào Khung In";
            this.Size = new Size(440, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Title
            this.lblTitle = new WinFormsLabel();
            this.lblTitle.Text = "BỐ TRÍ SECTION VIEW VÀO KHUNG IN";
            this.lblTitle.Font = new WinFormsFont("Microsoft Sans Serif", 11F, FontStyle.Bold);
            this.lblTitle.Location = new WinFormsPoint(20, 12);
            this.lblTitle.Size = new Size(400, 25);
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.ForeColor = Color.DarkBlue;

            // ========== Group Khung In ==========
            this.grpKhungIn = new GroupBox();
            this.grpKhungIn.Text = "Kích thước khung in (đơn vị bản vẽ)";
            this.grpKhungIn.Location = new WinFormsPoint(12, 45);
            this.grpKhungIn.Size = new Size(400, 75);

            this.lblChieuRong = new WinFormsLabel();
            this.lblChieuRong.Text = "Chiều rộng (B):";
            this.lblChieuRong.Location = new WinFormsPoint(15, 30);
            this.lblChieuRong.Size = new Size(100, 23);

            this.numChieuRong = new NumericUpDown();
            this.numChieuRong.Location = new WinFormsPoint(120, 28);
            this.numChieuRong.Size = new Size(80, 23);
            this.numChieuRong.DecimalPlaces = 1;
            this.numChieuRong.Minimum = 10;
            this.numChieuRong.Maximum = 500;
            this.numChieuRong.Value = 76;
            this.numChieuRong.ValueChanged += (s, e) => UpdatePreview();

            this.lblChieuCao = new WinFormsLabel();
            this.lblChieuCao.Text = "Chiều cao (H):";
            this.lblChieuCao.Location = new WinFormsPoint(215, 30);
            this.lblChieuCao.Size = new Size(100, 23);

            this.numChieuCao = new NumericUpDown();
            this.numChieuCao.Location = new WinFormsPoint(320, 28);
            this.numChieuCao.Size = new Size(65, 23);
            this.numChieuCao.DecimalPlaces = 1;
            this.numChieuCao.Minimum = 10;
            this.numChieuCao.Maximum = 500;
            this.numChieuCao.Value = 49;
            this.numChieuCao.ValueChanged += (s, e) => UpdatePreview();

            this.grpKhungIn.Controls.AddRange(new Control[] {
                lblChieuRong, numChieuRong,
                lblChieuCao, numChieuCao
            });

            // ========== Group Bố Trí ==========
            this.grpBoTri = new GroupBox();
            this.grpBoTri.Text = "Thông số bố trí lưới";
            this.grpBoTri.Location = new WinFormsPoint(12, 130);
            this.grpBoTri.Size = new Size(400, 135);

            this.lblSoCot = new WinFormsLabel();
            this.lblSoCot.Text = "Số cột:";
            this.lblSoCot.Location = new WinFormsPoint(15, 30);
            this.lblSoCot.Size = new Size(100, 23);

            this.numSoCot = new NumericUpDown();
            this.numSoCot.Location = new WinFormsPoint(120, 28);
            this.numSoCot.Size = new Size(80, 23);
            this.numSoCot.Minimum = 1;
            this.numSoCot.Maximum = 20;
            this.numSoCot.Value = 3;
            this.numSoCot.ValueChanged += (s, e) => UpdatePreview();

            this.lblSoHang = new WinFormsLabel();
            this.lblSoHang.Text = "Số hàng:";
            this.lblSoHang.Location = new WinFormsPoint(215, 30);
            this.lblSoHang.Size = new Size(100, 23);

            this.numSoHang = new NumericUpDown();
            this.numSoHang.Location = new WinFormsPoint(320, 28);
            this.numSoHang.Size = new Size(65, 23);
            this.numSoHang.Minimum = 1;
            this.numSoHang.Maximum = 20;
            this.numSoHang.Value = 3;
            this.numSoHang.ValueChanged += (s, e) => UpdatePreview();

            this.lblKCTrang = new WinFormsLabel();
            this.lblKCTrang.Text = "KC giữa các trang:";
            this.lblKCTrang.Location = new WinFormsPoint(15, 65);
            this.lblKCTrang.Size = new Size(130, 23);

            this.numKCTrang = new NumericUpDown();
            this.numKCTrang.Location = new WinFormsPoint(150, 63);
            this.numKCTrang.Size = new Size(60, 23);
            this.numKCTrang.DecimalPlaces = 1;
            this.numKCTrang.Minimum = 0;
            this.numKCTrang.Maximum = 200;
            this.numKCTrang.Value = 5;

            this.lblDichDung = new WinFormsLabel();
            this.lblDichDung.Text = "Dịch đứng (↕):";
            this.lblDichDung.Location = new WinFormsPoint(225, 65);
            this.lblDichDung.Size = new Size(100, 23);

            this.numDichDung = new NumericUpDown();
            this.numDichDung.Location = new WinFormsPoint(330, 63);
            this.numDichDung.Size = new Size(55, 23);
            this.numDichDung.DecimalPlaces = 1;
            this.numDichDung.Minimum = -100;
            this.numDichDung.Maximum = 100;
            this.numDichDung.Value = 0;

            // Preview label
            this.lblPreview = new WinFormsLabel();
            this.lblPreview.Text = "";
            this.lblPreview.Location = new WinFormsPoint(15, 100);
            this.lblPreview.Size = new Size(370, 23);
            this.lblPreview.ForeColor = Color.DarkGreen;
            this.lblPreview.Font = new WinFormsFont("Microsoft Sans Serif", 9F, FontStyle.Italic);

            this.grpBoTri.Controls.AddRange(new Control[] {
                lblSoCot, numSoCot,
                lblSoHang, numSoHang,
                lblKCTrang, numKCTrang,
                lblDichDung, numDichDung,
                lblPreview
            });

            // ========== Info label ==========
            var lblInfo = new WinFormsLabel();
            lblInfo.Text = "💡 Chọn điểm gốc (góc trái trên khung in đầu tiên), sau đó chọn các section view.";
            lblInfo.Location = new WinFormsPoint(12, 275);
            lblInfo.Size = new Size(400, 40);
            lblInfo.ForeColor = Color.DarkOrange;

            // ========== Buttons ==========
            this.btnOK = new Button();
            this.btnOK.Text = "OK";
            this.btnOK.Location = new WinFormsPoint(220, 320);
            this.btnOK.Size = new Size(90, 30);
            this.btnOK.Font = new WinFormsFont("Microsoft Sans Serif", 9F, FontStyle.Bold);
            this.btnOK.Click += BtnOK_Click;

            this.btnCancel = new Button();
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new WinFormsPoint(320, 320);
            this.btnCancel.Size = new Size(90, 30);
            this.btnCancel.Click += BtnCancel_Click;

            // Add to form
            this.Controls.AddRange(new Control[] {
                lblTitle,
                grpKhungIn,
                grpBoTri,
                lblInfo,
                btnOK,
                btnCancel
            });

            this.ResumeLayout(false);
        }

        private void UpdatePreview()
        {
            int soCot = (int)numSoCot.Value;
            int soHang = (int)numSoHang.Value;
            int total = soCot * soHang;
            lblPreview.Text = $"→ Mỗi trang chứa {soCot}×{soHang} = {total} cắt ngang";
        }

        private void RestoreLastUsedValues()
        {
            numChieuRong.Value = (decimal)_lastChieuRongKhung;
            numChieuCao.Value = (decimal)_lastChieuCaoKhung;
            numSoCot.Value = _lastSoCot;
            numSoHang.Value = _lastSoHang;
            numKCTrang.Value = (decimal)_lastKhoangCachTrang;
            numDichDung.Value = (decimal)_lastKhoangDichDung;
        }

        private void SaveLastUsedValues()
        {
            _lastChieuRongKhung = (double)numChieuRong.Value;
            _lastChieuCaoKhung = (double)numChieuCao.Value;
            _lastSoCot = (int)numSoCot.Value;
            _lastSoHang = (int)numSoHang.Value;
            _lastKhoangCachTrang = (double)numKCTrang.Value;
            _lastKhoangDichDung = (double)numDichDung.Value;
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            ChieuRongKhung = (double)numChieuRong.Value;
            ChieuCaoKhung = (double)numChieuCao.Value;
            SoCot = (int)numSoCot.Value;
            SoHang = (int)numSoHang.Value;
            KhoangCachTrang = (double)numKCTrang.Value;
            KhoangDichDung = (double)numDichDung.Value;

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
