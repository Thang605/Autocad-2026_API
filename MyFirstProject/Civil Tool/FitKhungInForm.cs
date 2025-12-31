using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Form nhập thông số Fit Khung In cho SectionView
    /// </summary>
    public class FitKhungInForm : Form
    {
        // Static variables to remember last input values
        private static double _lastMoRongDungTren = 5;
        private static double _lastMoRongDungDuoi = 5;
        private static double _lastMoRongNgangTrai = 5;
        private static double _lastMoRongNgangPhai = 5;
        private static string _lastCodeSection = "top";

        // Properties to return data
        public double MoRongDungTren { get; private set; }
        public double MoRongDungDuoi { get; private set; }
        public double MoRongNgangTrai { get; private set; }
        public double MoRongNgangPhai { get; private set; }
        public string CodeSection { get; private set; } = "top";
        public bool FormAccepted { get; private set; } = false;

        // UI Controls
        private WinFormsLabel lblTitle = null!;
        private WinFormsLabel lblDungTren = null!;
        private WinFormsLabel lblDungDuoi = null!;
        private WinFormsLabel lblNgangTrai = null!;
        private WinFormsLabel lblNgangPhai = null!;
        private WinFormsLabel lblCodeSection = null!;

        private NumericUpDown numDungTren = null!;
        private NumericUpDown numDungDuoi = null!;
        private NumericUpDown numNgangTrai = null!;
        private NumericUpDown numNgangPhai = null!;
        private ComboBox cmbCodeSection = null!;

        private Button btnOK = null!;
        private Button btnCancel = null!;
        private GroupBox grpSettings = null!;

        public FitKhungInForm()
        {
            InitializeComponent();
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
            this.lblCodeSection = new WinFormsLabel();

            this.numDungTren = new NumericUpDown();
            this.numDungDuoi = new NumericUpDown();
            this.numNgangTrai = new NumericUpDown();
            this.numNgangPhai = new NumericUpDown();
            this.cmbCodeSection = new ComboBox();

            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.grpSettings = new GroupBox();

            this.SuspendLayout();

            // Form
            this.Text = "Fit Khung In - Section View";
            this.Size = new Size(420, 340);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Title Label
            this.lblTitle.Text = "THIẾT LẬP THÔNG SỐ FIT KHUNG IN";
            this.lblTitle.Font = new WinFormsFont("Microsoft Sans Serif", 11F, FontStyle.Bold);
            this.lblTitle.Location = new WinFormsPoint(20, 15);
            this.lblTitle.Size = new Size(370, 25);
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.ForeColor = Color.DarkBlue;

            // Settings Group
            this.grpSettings.Text = "Thông số mở rộng khung";
            this.grpSettings.Location = new WinFormsPoint(12, 50);
            this.grpSettings.Size = new Size(380, 195);

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

            // Code Section
            this.lblCodeSection.Text = "Code Section:";
            this.lblCodeSection.Location = new WinFormsPoint(15, 155);
            this.lblCodeSection.Size = new Size(150, 23);

            this.cmbCodeSection.Location = new WinFormsPoint(170, 153);
            this.cmbCodeSection.Size = new Size(180, 23);
            this.cmbCodeSection.DropDownStyle = ComboBoxStyle.DropDown;
            this.cmbCodeSection.Items.AddRange(new object[] { "top", "datum", "TN", "TK" });
            this.cmbCodeSection.SelectedIndex = 0;

            // OK Button
            this.btnOK.Text = "OK";
            this.btnOK.Location = new WinFormsPoint(200, 260);
            this.btnOK.Size = new Size(90, 30);
            this.btnOK.Font = new WinFormsFont("Microsoft Sans Serif", 9F, FontStyle.Bold);
            this.btnOK.Click += BtnOK_Click;

            // Cancel Button
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new WinFormsPoint(300, 260);
            this.btnCancel.Size = new Size(90, 30);
            this.btnCancel.Click += BtnCancel_Click;

            // Add controls to group
            this.grpSettings.Controls.AddRange(new Control[] {
                lblDungTren, numDungTren,
                lblDungDuoi, numDungDuoi,
                lblNgangTrai, numNgangTrai,
                lblNgangPhai, numNgangPhai,
                lblCodeSection, cmbCodeSection
            });

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                lblTitle,
                grpSettings,
                btnOK,
                btnCancel
            });

            this.ResumeLayout(false);
        }

        private void RestoreLastUsedValues()
        {
            numDungTren.Value = (decimal)_lastMoRongDungTren;
            numDungDuoi.Value = (decimal)_lastMoRongDungDuoi;
            numNgangTrai.Value = (decimal)_lastMoRongNgangTrai;
            numNgangPhai.Value = (decimal)_lastMoRongNgangPhai;

            // Restore code section
            if (!string.IsNullOrEmpty(_lastCodeSection))
            {
                int idx = cmbCodeSection.Items.IndexOf(_lastCodeSection);
                if (idx >= 0)
                {
                    cmbCodeSection.SelectedIndex = idx;
                }
                else
                {
                    cmbCodeSection.Text = _lastCodeSection;
                }
            }
        }

        private void SaveLastUsedValues()
        {
            _lastMoRongDungTren = (double)numDungTren.Value;
            _lastMoRongDungDuoi = (double)numDungDuoi.Value;
            _lastMoRongNgangTrai = (double)numNgangTrai.Value;
            _lastMoRongNgangPhai = (double)numNgangPhai.Value;
            _lastCodeSection = cmbCodeSection.Text;
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Validate code section
            if (string.IsNullOrWhiteSpace(cmbCodeSection.Text))
            {
                MessageBox.Show("Vui lòng nhập Code Section.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCodeSection.Focus();
                return;
            }

            // Get values
            MoRongDungTren = (double)numDungTren.Value;
            MoRongDungDuoi = (double)numDungDuoi.Value;
            MoRongNgangTrai = (double)numNgangTrai.Value;
            MoRongNgangPhai = (double)numNgangPhai.Value;
            CodeSection = cmbCodeSection.Text;

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
