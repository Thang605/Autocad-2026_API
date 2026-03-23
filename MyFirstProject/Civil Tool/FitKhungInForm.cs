using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Thông tin một section source để hiển thị trong form
    /// </summary>
    public class SectionSourceInfo
    {
        public string Name { get; set; } = "";
        public string SourceType { get; set; } = "";
        public Autodesk.AutoCAD.DatabaseServices.ObjectId SourceId { get; set; }

        public override string ToString() => $"{Name} ({SourceType})";
    }

    /// <summary>
    /// Form nhập thông số Fit Khung In cho SectionView - Hỗ trợ chọn nhiều Section Sources
    /// Danh sách section sources được đọc trực tiếp từ SectionView đã chọn
    /// </summary>
    public class FitKhungInForm : Form
    {
        // Static variables to remember last input values
        private static double _lastMoRongDungTren = 5;
        private static double _lastMoRongDungDuoi = 5;
        private static double _lastMoRongNgangTrai = 5;
        private static double _lastMoRongNgangPhai = 5;
        private static bool _lastApDungDung = true;
        private static bool _lastApDungNgang = true;
        private static HashSet<string> _lastSelectedSourceNames = new(StringComparer.OrdinalIgnoreCase) { "top" };

        // Properties to return data
        public double MoRongDungTren { get; private set; }
        public double MoRongDungDuoi { get; private set; }
        public double MoRongNgangTrai { get; private set; }
        public double MoRongNgangPhai { get; private set; }
        public List<SectionSourceInfo> SelectedSources { get; private set; } = new();
        public bool ApDungDung { get; private set; } = true;
        public bool ApDungNgang { get; private set; } = true;
        public bool FormAccepted { get; private set; } = false;

        // Danh sách sources từ SectionView
        private readonly List<SectionSourceInfo> _availableSources;

        // UI Controls
        private WinFormsLabel lblTitle = null!;
        private WinFormsLabel lblDungTren = null!;
        private WinFormsLabel lblDungDuoi = null!;
        private WinFormsLabel lblNgangTrai = null!;
        private WinFormsLabel lblNgangPhai = null!;

        private NumericUpDown numDungTren = null!;
        private NumericUpDown numDungDuoi = null!;
        private NumericUpDown numNgangTrai = null!;
        private NumericUpDown numNgangPhai = null!;
        private CheckedListBox clbSources = null!;
        private Button btnChonTatCa = null!;
        private Button btnBoChonTatCa = null!;
        private CheckBox chkApDungDung = null!;
        private CheckBox chkApDungNgang = null!;

        private Button btnOK = null!;
        private Button btnCancel = null!;
        private GroupBox grpMoRong = null!;
        private GroupBox grpSections = null!;

        /// <summary>
        /// Constructor nhận danh sách section sources đọc từ SectionView
        /// </summary>
        /// <param name="availableSources">Danh sách section sources có sẵn</param>
        public FitKhungInForm(List<SectionSourceInfo> availableSources)
        {
            _availableSources = availableSources;
            InitializeComponent();
            PopulateSources();
            RestoreLastUsedValues();
        }

        private void InitializeComponent()
        {
            // Initialize controls
            this.lblTitle = new WinFormsLabel();
            this.lblDungTren = new WinFormsLabel();
            this.lblDungDuoi = new WinFormsLabel();
            this.lblNgangTrai = new WinFormsLabel();
            this.lblNgangPhai = new WinFormsLabel();

            this.numDungTren = new NumericUpDown();
            this.numDungDuoi = new NumericUpDown();
            this.numNgangTrai = new NumericUpDown();
            this.numNgangPhai = new NumericUpDown();
            this.clbSources = new CheckedListBox();
            this.btnChonTatCa = new Button();
            this.btnBoChonTatCa = new Button();
            this.chkApDungDung = new CheckBox();
            this.chkApDungNgang = new CheckBox();

            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.grpMoRong = new GroupBox();
            this.grpSections = new GroupBox();

            this.SuspendLayout();

            // Form
            this.Text = "Fit Khung In - Section View";
            this.Size = new Size(480, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Title Label
            this.lblTitle.Text = "THIẾT LẬP THÔNG SỐ FIT KHUNG IN";
            this.lblTitle.Font = new WinFormsFont("Microsoft Sans Serif", 11F, FontStyle.Bold);
            this.lblTitle.Location = new WinFormsPoint(20, 15);
            this.lblTitle.Size = new Size(430, 25);
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.ForeColor = Color.DarkBlue;

            // ===== Group Mở rộng khung =====
            this.grpMoRong.Text = "Thông số mở rộng khung";
            this.grpMoRong.Location = new WinFormsPoint(12, 50);
            this.grpMoRong.Size = new Size(440, 185);

            // Mở rộng đứng trên
            this.lblDungTren.Text = "Mở rộng đứng trên:";
            this.lblDungTren.Location = new WinFormsPoint(15, 30);
            this.lblDungTren.Size = new Size(150, 23);

            this.numDungTren.Location = new WinFormsPoint(170, 28);
            this.numDungTren.Size = new Size(100, 23);
            this.numDungTren.DecimalPlaces = 1;
            this.numDungTren.Minimum = 0;
            this.numDungTren.Maximum = 1000;
            this.numDungTren.Value = 5;

            // Mở rộng đứng dưới
            this.lblDungDuoi.Text = "Mở rộng đứng dưới:";
            this.lblDungDuoi.Location = new WinFormsPoint(15, 60);
            this.lblDungDuoi.Size = new Size(150, 23);

            this.numDungDuoi.Location = new WinFormsPoint(170, 58);
            this.numDungDuoi.Size = new Size(100, 23);
            this.numDungDuoi.DecimalPlaces = 1;
            this.numDungDuoi.Minimum = 0;
            this.numDungDuoi.Maximum = 1000;
            this.numDungDuoi.Value = 5;

            // Mở rộng ngang trái
            this.lblNgangTrai.Text = "Mở rộng ngang trái:";
            this.lblNgangTrai.Location = new WinFormsPoint(15, 90);
            this.lblNgangTrai.Size = new Size(150, 23);

            this.numNgangTrai.Location = new WinFormsPoint(170, 88);
            this.numNgangTrai.Size = new Size(100, 23);
            this.numNgangTrai.DecimalPlaces = 1;
            this.numNgangTrai.Minimum = 0;
            this.numNgangTrai.Maximum = 1000;
            this.numNgangTrai.Value = 5;

            // Mở rộng ngang phải
            this.lblNgangPhai.Text = "Mở rộng ngang phải:";
            this.lblNgangPhai.Location = new WinFormsPoint(15, 120);
            this.lblNgangPhai.Size = new Size(150, 23);

            this.numNgangPhai.Location = new WinFormsPoint(170, 118);
            this.numNgangPhai.Size = new Size(100, 23);
            this.numNgangPhai.DecimalPlaces = 1;
            this.numNgangPhai.Minimum = 0;
            this.numNgangPhai.Maximum = 1000;
            this.numNgangPhai.Value = 5;

            // Checkbox áp dụng phương đứng
            this.chkApDungDung.Text = "Áp dụng phương đứng (Trên/Dưới)";
            this.chkApDungDung.Location = new WinFormsPoint(15, 150);
            this.chkApDungDung.Size = new Size(210, 23);
            this.chkApDungDung.Checked = true;
            this.chkApDungDung.CheckedChanged += ChkApDungDung_CheckedChanged;

            // Checkbox áp dụng phương ngang
            this.chkApDungNgang.Text = "Áp dụng ngang (Trái/Phải)";
            this.chkApDungNgang.Location = new WinFormsPoint(230, 150);
            this.chkApDungNgang.Size = new Size(200, 23);
            this.chkApDungNgang.Checked = true;
            this.chkApDungNgang.CheckedChanged += ChkApDungNgang_CheckedChanged;

            // Add controls to grpMoRong
            this.grpMoRong.Controls.AddRange(new Control[] {
                lblDungTren, numDungTren,
                lblDungDuoi, numDungDuoi,
                lblNgangTrai, numNgangTrai,
                lblNgangPhai, numNgangPhai,
                chkApDungDung, chkApDungNgang
            });

            // ===== Group Section Sources =====
            this.grpSections.Text = "Chọn Section Sources từ SectionView";
            this.grpSections.Location = new WinFormsPoint(12, 245);
            this.grpSections.Size = new Size(440, 185);

            // CheckedListBox hiển thị sources thực tế
            this.clbSources.Location = new WinFormsPoint(15, 25);
            this.clbSources.Size = new Size(330, 120);
            this.clbSources.CheckOnClick = true;

            // Buttons
            this.btnChonTatCa.Text = "Chọn tất cả";
            this.btnChonTatCa.Location = new WinFormsPoint(350, 25);
            this.btnChonTatCa.Size = new Size(80, 28);
            this.btnChonTatCa.Click += BtnChonTatCa_Click;

            this.btnBoChonTatCa.Text = "Bỏ chọn";
            this.btnBoChonTatCa.Location = new WinFormsPoint(350, 58);
            this.btnBoChonTatCa.Size = new Size(80, 28);
            this.btnBoChonTatCa.Click += BtnBoChonTatCa_Click;

            // Info label
            var lblInfo = new WinFormsLabel();
            lblInfo.Text = "💡 Khung in sẽ bao quanh tất cả sections được chọn";
            lblInfo.Location = new WinFormsPoint(15, 153);
            lblInfo.Size = new Size(400, 23);
            lblInfo.ForeColor = Color.Gray;
            lblInfo.Font = new WinFormsFont("Microsoft Sans Serif", 8F, FontStyle.Italic);

            // Add controls to grpSections
            this.grpSections.Controls.AddRange(new Control[] {
                clbSources,
                btnChonTatCa, btnBoChonTatCa,
                lblInfo
            });

            // OK Button
            this.btnOK.Text = "OK";
            this.btnOK.Location = new WinFormsPoint(260, 445);
            this.btnOK.Size = new Size(90, 30);
            this.btnOK.Font = new WinFormsFont("Microsoft Sans Serif", 9F, FontStyle.Bold);
            this.btnOK.Click += BtnOK_Click;

            // Cancel Button
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new WinFormsPoint(360, 445);
            this.btnCancel.Size = new Size(90, 30);
            this.btnCancel.Click += BtnCancel_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                lblTitle,
                grpMoRong,
                grpSections,
                btnOK,
                btnCancel
            });

            this.ResumeLayout(false);
        }

        /// <summary>
        /// Đưa danh sách section sources vào CheckedListBox
        /// </summary>
        private void PopulateSources()
        {
            clbSources.Items.Clear();
            foreach (var source in _availableSources)
            {
                clbSources.Items.Add(source);
            }
        }

        private void RestoreLastUsedValues()
        {
            numDungTren.Value = (decimal)_lastMoRongDungTren;
            numDungDuoi.Value = (decimal)_lastMoRongDungDuoi;
            numNgangTrai.Value = (decimal)_lastMoRongNgangTrai;
            numNgangPhai.Value = (decimal)_lastMoRongNgangPhai;

            // Restore checkbox states
            chkApDungDung.Checked = _lastApDungDung;
            chkApDungNgang.Checked = _lastApDungNgang;
            numDungTren.Enabled = _lastApDungDung;
            numDungDuoi.Enabled = _lastApDungDung;
            numNgangTrai.Enabled = _lastApDungNgang;
            numNgangPhai.Enabled = _lastApDungNgang;

            // Restore section selections - check items có tên trùng với lần chọn trước
            for (int i = 0; i < clbSources.Items.Count; i++)
            {
                if (clbSources.Items[i] is SectionSourceInfo info)
                {
                    if (_lastSelectedSourceNames.Contains(info.Name))
                    {
                        clbSources.SetItemChecked(i, true);
                    }
                }
            }
        }

        private void SaveLastUsedValues()
        {
            _lastMoRongDungTren = (double)numDungTren.Value;
            _lastMoRongDungDuoi = (double)numDungDuoi.Value;
            _lastMoRongNgangTrai = (double)numNgangTrai.Value;
            _lastMoRongNgangPhai = (double)numNgangPhai.Value;
            _lastApDungDung = chkApDungDung.Checked;
            _lastApDungNgang = chkApDungNgang.Checked;

            // Save selected source names
            _lastSelectedSourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in clbSources.CheckedItems)
            {
                if (item is SectionSourceInfo info)
                {
                    _lastSelectedSourceNames.Add(info.Name);
                }
            }
        }

        private void ChkApDungDung_CheckedChanged(object? sender, EventArgs e)
        {
            numDungTren.Enabled = chkApDungDung.Checked;
            numDungDuoi.Enabled = chkApDungDung.Checked;
        }

        private void ChkApDungNgang_CheckedChanged(object? sender, EventArgs e)
        {
            numNgangTrai.Enabled = chkApDungNgang.Checked;
            numNgangPhai.Enabled = chkApDungNgang.Checked;
        }

        private void BtnChonTatCa_Click(object? sender, EventArgs e)
        {
            for (int i = 0; i < clbSources.Items.Count; i++)
                clbSources.SetItemChecked(i, true);
        }

        private void BtnBoChonTatCa_Click(object? sender, EventArgs e)
        {
            for (int i = 0; i < clbSources.Items.Count; i++)
                clbSources.SetItemChecked(i, false);
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Validate: phải chọn ít nhất 1 section
            if (clbSources.CheckedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 Section Source.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Get values
            MoRongDungTren = (double)numDungTren.Value;
            MoRongDungDuoi = (double)numDungDuoi.Value;
            MoRongNgangTrai = (double)numNgangTrai.Value;
            MoRongNgangPhai = (double)numNgangPhai.Value;
            ApDungDung = chkApDungDung.Checked;
            ApDungNgang = chkApDungNgang.Checked;

            // Get selected sources
            SelectedSources = new List<SectionSourceInfo>();
            foreach (var item in clbSources.CheckedItems)
            {
                if (item is SectionSourceInfo info)
                {
                    SelectedSources.Add(info);
                }
            }

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
