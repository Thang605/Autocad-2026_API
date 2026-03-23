using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Form cho lệnh CTSV_TongChieuDai_CatNgang
    /// Hiển thị danh sách link codes trong Corridor, cho phép xây dựng công thức tính
    /// </summary>
    public class LinkLengthForm : Form
    {
        // Static variables to remember last selections
        private static string _lastFormula = "";
        private static string _lastLabel = "CD:";
        private static double _lastTextHeight = 0.4;

        // Properties to return data
        public bool FormAccepted { get; private set; } = false;
        public string Formula => txtFormula.Text;
        public string ResultLabel => txtLabel.Text;
        public double TextHeight => (double)nudTextHeight.Value;

        // UI Controls
        private WinFormsLabel lblLinks = null!;
        private ListBox lstLinks = null!;
        private Button btnAdd = null!;
        private WinFormsLabel lblFormula = null!;
        private TextBox txtFormula = null!;
        private Button btnClear = null!;
        private WinFormsLabel lblLabel = null!;
        private TextBox txtLabel = null!;
        private WinFormsLabel lblTextHeight = null!;
        private NumericUpDown nudTextHeight = null!;
        private GroupBox grpFormula = null!;
        private GroupBox grpSettings = null!;
        private WinFormsLabel lblHelp = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;

        // Quick operator buttons
        private Button btnPlus = null!;
        private Button btnMinus = null!;
        private Button btnMultiply = null!;
        private Button btnDivide = null!;
        private Button btnOpenParen = null!;
        private Button btnCloseParen = null!;

        // Data
        private List<string> _linkCodeNames;

        public LinkLengthForm(List<string> linkCodeNames)
        {
            _linkCodeNames = linkCodeNames;
            InitializeComponent();
            LoadData();
            RestoreLastUsedValues();
        }

        private void InitializeComponent()
        {
            var standardFont = new WinFormsFont("Segoe UI", 9F, FontStyle.Regular);
            var boldFont = new WinFormsFont("Segoe UI", 9F, FontStyle.Bold);
            var monoFont = new WinFormsFont("Consolas", 10F, FontStyle.Regular);

            // Initialize controls
            this.lblLinks = new WinFormsLabel();
            this.lstLinks = new ListBox();
            this.btnAdd = new Button();
            this.lblFormula = new WinFormsLabel();
            this.txtFormula = new TextBox();
            this.btnClear = new Button();
            this.lblLabel = new WinFormsLabel();
            this.txtLabel = new TextBox();
            this.lblTextHeight = new WinFormsLabel();
            this.nudTextHeight = new NumericUpDown();
            this.grpFormula = new GroupBox();
            this.grpSettings = new GroupBox();
            this.lblHelp = new WinFormsLabel();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.btnPlus = new Button();
            this.btnMinus = new Button();
            this.btnMultiply = new Button();
            this.btnDivide = new Button();
            this.btnOpenParen = new Button();
            this.btnCloseParen = new Button();

            this.SuspendLayout();

            // Form settings
            this.Text = "Tính Chiều Dài Link trên Cắt Ngang";
            this.Size = new Size(620, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = standardFont;

            // ===== Links List =====
            this.lblLinks.Text = "Danh sách Link Codes trong Corridor:";
            this.lblLinks.Font = boldFont;
            this.lblLinks.Location = new WinFormsPoint(15, 12);
            this.lblLinks.Size = new Size(350, 20);

            this.lstLinks.Location = new WinFormsPoint(15, 35);
            this.lstLinks.Size = new Size(350, 160);
            this.lstLinks.Font = standardFont;
            this.lstLinks.SelectionMode = SelectionMode.One;
            this.lstLinks.DoubleClick += LstLinks_DoubleClick;

            this.btnAdd.Text = "Thêm vào\ncông thức >>";
            this.btnAdd.Font = boldFont;
            this.btnAdd.Location = new WinFormsPoint(375, 60);
            this.btnAdd.Size = new Size(100, 50);
            this.btnAdd.BackColor = Color.FromArgb(0, 120, 215);
            this.btnAdd.ForeColor = Color.White;
            this.btnAdd.FlatStyle = FlatStyle.Flat;
            this.btnAdd.Click += BtnAdd_Click;

            // ===== Formula GroupBox =====
            this.grpFormula.Text = "Công thức tính chiều dài";
            this.grpFormula.Font = boldFont;
            this.grpFormula.Location = new WinFormsPoint(15, 205);
            this.grpFormula.Size = new Size(575, 140);

            this.lblFormula.Text = "Công thức:";
            this.lblFormula.Font = standardFont;
            this.lblFormula.Location = new WinFormsPoint(10, 25);
            this.lblFormula.Size = new Size(80, 20);

            this.txtFormula.Location = new WinFormsPoint(10, 48);
            this.txtFormula.Size = new Size(470, 28);
            this.txtFormula.Font = monoFont;
            this.txtFormula.BackColor = Color.FromArgb(255, 255, 240);

            this.btnClear.Text = "Xóa";
            this.btnClear.Font = standardFont;
            this.btnClear.Location = new WinFormsPoint(490, 47);
            this.btnClear.Size = new Size(70, 28);
            this.btnClear.Click += BtnClear_Click;

            // Quick operator buttons
            int opBtnX = 10;
            int opBtnY = 85;
            int opBtnW = 42;
            int opBtnH = 30;
            int opBtnGap = 5;

            this.btnPlus.Text = "+";
            this.btnPlus.Font = boldFont;
            this.btnPlus.Location = new WinFormsPoint(opBtnX, opBtnY);
            this.btnPlus.Size = new Size(opBtnW, opBtnH);
            this.btnPlus.Click += (s, e) => InsertOperator("+");

            this.btnMinus.Text = "−";
            this.btnMinus.Font = boldFont;
            this.btnMinus.Location = new WinFormsPoint(opBtnX + (opBtnW + opBtnGap) * 1, opBtnY);
            this.btnMinus.Size = new Size(opBtnW, opBtnH);
            this.btnMinus.Click += (s, e) => InsertOperator("-");

            this.btnMultiply.Text = "×";
            this.btnMultiply.Font = boldFont;
            this.btnMultiply.Location = new WinFormsPoint(opBtnX + (opBtnW + opBtnGap) * 2, opBtnY);
            this.btnMultiply.Size = new Size(opBtnW, opBtnH);
            this.btnMultiply.Click += (s, e) => InsertOperator("*");

            this.btnDivide.Text = "÷";
            this.btnDivide.Font = boldFont;
            this.btnDivide.Location = new WinFormsPoint(opBtnX + (opBtnW + opBtnGap) * 3, opBtnY);
            this.btnDivide.Size = new Size(opBtnW, opBtnH);
            this.btnDivide.Click += (s, e) => InsertOperator("/");

            this.btnOpenParen.Text = "(";
            this.btnOpenParen.Font = boldFont;
            this.btnOpenParen.Location = new WinFormsPoint(opBtnX + (opBtnW + opBtnGap) * 4, opBtnY);
            this.btnOpenParen.Size = new Size(opBtnW, opBtnH);
            this.btnOpenParen.Click += (s, e) => InsertOperator("(");

            this.btnCloseParen.Text = ")";
            this.btnCloseParen.Font = boldFont;
            this.btnCloseParen.Location = new WinFormsPoint(opBtnX + (opBtnW + opBtnGap) * 5, opBtnY);
            this.btnCloseParen.Size = new Size(opBtnW, opBtnH);
            this.btnCloseParen.Click += (s, e) => InsertOperator(")");

            this.lblHelp.Text = "💡 Chọn link → bấm 'Thêm vào' hoặc double-click. VD: [Top] + [Datum]";
            this.lblHelp.Font = new WinFormsFont("Segoe UI", 8F, FontStyle.Italic);
            this.lblHelp.Location = new WinFormsPoint(opBtnX + (opBtnW + opBtnGap) * 6 + 5, opBtnY + 5);
            this.lblHelp.Size = new Size(280, 20);
            this.lblHelp.ForeColor = Color.Gray;

            this.grpFormula.Controls.AddRange(new Control[] {
                lblFormula, txtFormula, btnClear,
                btnPlus, btnMinus, btnMultiply, btnDivide, btnOpenParen, btnCloseParen,
                lblHelp
            });

            // ===== Settings GroupBox =====
            this.grpSettings.Text = "Cài đặt hiển thị trên cắt ngang";
            this.grpSettings.Font = boldFont;
            this.grpSettings.Location = new WinFormsPoint(15, 355);
            this.grpSettings.Size = new Size(575, 70);

            this.lblLabel.Text = "Nhãn:";
            this.lblLabel.Font = standardFont;
            this.lblLabel.Location = new WinFormsPoint(10, 28);
            this.lblLabel.Size = new Size(45, 20);

            this.txtLabel.Location = new WinFormsPoint(55, 25);
            this.txtLabel.Size = new Size(150, 25);
            this.txtLabel.Font = standardFont;
            this.txtLabel.Text = "CD:";

            this.lblTextHeight.Text = "Cao chữ:";
            this.lblTextHeight.Font = standardFont;
            this.lblTextHeight.Location = new WinFormsPoint(230, 28);
            this.lblTextHeight.Size = new Size(60, 20);

            this.nudTextHeight.Location = new WinFormsPoint(295, 25);
            this.nudTextHeight.Size = new Size(80, 25);
            this.nudTextHeight.Font = standardFont;
            this.nudTextHeight.DecimalPlaces = 2;
            this.nudTextHeight.Minimum = 0.1M;
            this.nudTextHeight.Maximum = 100;
            this.nudTextHeight.Value = 0.4M;
            this.nudTextHeight.Increment = 0.1M;

            this.grpSettings.Controls.AddRange(new Control[] {
                lblLabel, txtLabel, lblTextHeight, nudTextHeight
            });

            // ===== Buttons =====
            this.btnOK.Text = "OK";
            this.btnOK.Font = boldFont;
            this.btnOK.Location = new WinFormsPoint(400, 440);
            this.btnOK.Size = new Size(90, 32);
            this.btnOK.BackColor = Color.FromArgb(0, 120, 215);
            this.btnOK.ForeColor = Color.White;
            this.btnOK.FlatStyle = FlatStyle.Flat;
            this.btnOK.Click += BtnOK_Click;

            this.btnCancel.Text = "Hủy";
            this.btnCancel.Font = standardFont;
            this.btnCancel.Location = new WinFormsPoint(500, 440);
            this.btnCancel.Size = new Size(90, 32);
            this.btnCancel.Click += BtnCancel_Click;

            // Add all controls to form
            this.Controls.AddRange(new Control[] {
                lblLinks, lstLinks, btnAdd,
                grpFormula, grpSettings,
                btnOK, btnCancel
            });

            this.ResumeLayout(false);
        }

        private void LoadData()
        {
            lstLinks.Items.Clear();
            foreach (string linkCode in _linkCodeNames)
            {
                lstLinks.Items.Add(linkCode);
            }
        }

        private void RestoreLastUsedValues()
        {
            if (!string.IsNullOrEmpty(_lastFormula))
                txtFormula.Text = _lastFormula;
            if (!string.IsNullOrEmpty(_lastLabel))
                txtLabel.Text = _lastLabel;
            nudTextHeight.Value = (decimal)_lastTextHeight;
        }

        private void SaveLastUsedValues()
        {
            _lastFormula = txtFormula.Text;
            _lastLabel = txtLabel.Text;
            _lastTextHeight = (double)nudTextHeight.Value;
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            AddSelectedLink();
        }

        private void LstLinks_DoubleClick(object? sender, EventArgs e)
        {
            AddSelectedLink();
        }

        private void AddSelectedLink()
        {
            if (lstLinks.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một link code!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string linkName = lstLinks.SelectedItem.ToString() ?? "";
            string linkToken = $"[{linkName}]";

            // Thêm vào vị trí con trỏ trong textbox
            int cursorPos = txtFormula.SelectionStart;
            txtFormula.Text = txtFormula.Text.Insert(cursorPos, linkToken);
            txtFormula.SelectionStart = cursorPos + linkToken.Length;
            txtFormula.Focus();
        }

        private void InsertOperator(string op)
        {
            int cursorPos = txtFormula.SelectionStart;
            string padded = $" {op} ";
            if (op == "(" || op == ")")
                padded = op;
            txtFormula.Text = txtFormula.Text.Insert(cursorPos, padded);
            txtFormula.SelectionStart = cursorPos + padded.Length;
            txtFormula.Focus();
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            txtFormula.Text = "";
            txtFormula.Focus();
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFormula.Text))
            {
                MessageBox.Show("Vui lòng nhập công thức tính!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
