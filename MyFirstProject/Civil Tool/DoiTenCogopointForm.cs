using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Form nhập template tên cho lệnh Đổi Tên CogoPoint
    /// </summary>
    public class DoiTenCogopointForm : Form
    {
        // Static variables to remember last input values
        private static string _lastNameTemplate = "";
        private static int _lastNumberStyleIndex = 0;
        private static int _lastStartingNumber = 1;
        private static int _lastIncrementValue = 1;

        // Properties to return data
        public string NameTemplate { get; private set; } = "";
        public NumberStyle SelectedNumberStyle { get; private set; } = NumberStyle.Arabic;
        public int StartingNumber { get; private set; } = 1;
        public int IncrementValue { get; private set; } = 1;
        public bool FormAccepted { get; private set; } = false;

        // Number style enum
        public enum NumberStyle
        {
            Arabic,      // 1, 2, 3...
            RomanUpper,  // I, II, III...
            RomanLower,  // i, ii, iii...
            LetterUpper, // A, B, C...
            LetterLower  // a, b, c...
        }

        // Property field enum for template
        public enum PropertyField
        {
            NextCounter,
            PointNumber,
            PointName,
            Description,
            Easting,
            Northing,
            Elevation
        }

        // UI Controls - Name formatting template group
        private GroupBox grpNameTemplate = null!;
        private WinFormsLabel lblPropertyFields = null!;
        private ComboBox cmbPropertyFields = null!;
        private Button btnInsert = null!;
        private WinFormsLabel lblName = null!;
        private TextBox txtName = null!;

        // UI Controls - Incremental number format group
        private GroupBox grpNumberFormat = null!;
        private WinFormsLabel lblNumberStyle = null!;
        private ComboBox cmbNumberStyle = null!;
        private WinFormsLabel lblStartingNumber = null!;
        private NumericUpDown numStartingNumber = null!;
        private WinFormsLabel lblIncrementValue = null!;
        private NumericUpDown numIncrementValue = null!;

        // Buttons
        private Button btnOK = null!;
        private Button btnCancel = null!;
        private Button btnHelp = null!;

        public DoiTenCogopointForm()
        {
            InitializeComponent();
            LoadPropertyFields();
            LoadNumberStyles();
            RestoreLastUsedValues();
        }

        private void InitializeComponent()
        {
            // Initialize controls
            this.grpNameTemplate = new GroupBox();
            this.lblPropertyFields = new WinFormsLabel();
            this.cmbPropertyFields = new ComboBox();
            this.btnInsert = new Button();
            this.lblName = new WinFormsLabel();
            this.txtName = new TextBox();

            this.grpNumberFormat = new GroupBox();
            this.lblNumberStyle = new WinFormsLabel();
            this.cmbNumberStyle = new ComboBox();
            this.lblStartingNumber = new WinFormsLabel();
            this.numStartingNumber = new NumericUpDown();
            this.lblIncrementValue = new WinFormsLabel();
            this.numIncrementValue = new NumericUpDown();

            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.btnHelp = new Button();

            this.SuspendLayout();

            // Form
            this.Text = "Name Template";
            this.Size = new Size(500, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = true;

            // === Name formatting template Group ===
            this.grpNameTemplate.Text = "Name formatting template";
            this.grpNameTemplate.Location = new WinFormsPoint(12, 12);
            this.grpNameTemplate.Size = new Size(460, 100);

            // Property fields label
            this.lblPropertyFields.Text = "Property fields:";
            this.lblPropertyFields.Location = new WinFormsPoint(15, 25);
            this.lblPropertyFields.Size = new Size(100, 20);

            // Property fields combobox
            this.cmbPropertyFields.Location = new WinFormsPoint(15, 45);
            this.cmbPropertyFields.Size = new Size(340, 23);
            this.cmbPropertyFields.DropDownStyle = ComboBoxStyle.DropDownList;

            // Insert button
            this.btnInsert.Text = "Insert";
            this.btnInsert.Location = new WinFormsPoint(365, 44);
            this.btnInsert.Size = new Size(80, 25);
            this.btnInsert.Click += BtnInsert_Click;

            // Name label
            this.lblName.Text = "Name:";
            this.lblName.Location = new WinFormsPoint(15, 75);
            this.lblName.Size = new Size(50, 20);

            // Name textbox
            this.txtName.Location = new WinFormsPoint(65, 72);
            this.txtName.Size = new Size(380, 23);

            // Add controls to Name Template group
            this.grpNameTemplate.Controls.AddRange(new Control[] {
                lblPropertyFields, cmbPropertyFields, btnInsert,
                lblName, txtName
            });

            // === Incremental number format Group ===
            this.grpNumberFormat.Text = "Incremental number format";
            this.grpNumberFormat.Location = new WinFormsPoint(12, 120);
            this.grpNumberFormat.Size = new Size(460, 100);

            // Number style label
            this.lblNumberStyle.Text = "Number style:";
            this.lblNumberStyle.Location = new WinFormsPoint(15, 22);
            this.lblNumberStyle.Size = new Size(100, 20);

            // Number style combobox
            this.cmbNumberStyle.Location = new WinFormsPoint(15, 42);
            this.cmbNumberStyle.Size = new Size(430, 23);
            this.cmbNumberStyle.DropDownStyle = ComboBoxStyle.DropDownList;

            // Starting number label
            this.lblStartingNumber.Text = "Starting number:";
            this.lblStartingNumber.Location = new WinFormsPoint(15, 72);
            this.lblStartingNumber.Size = new Size(100, 20);

            // Starting number input
            this.numStartingNumber.Location = new WinFormsPoint(120, 70);
            this.numStartingNumber.Size = new Size(80, 23);
            this.numStartingNumber.Minimum = 1;
            this.numStartingNumber.Maximum = 99999;
            this.numStartingNumber.Value = 1;

            // Increment value label
            this.lblIncrementValue.Text = "Increment value:";
            this.lblIncrementValue.Location = new WinFormsPoint(230, 72);
            this.lblIncrementValue.Size = new Size(100, 20);

            // Increment value input
            this.numIncrementValue.Location = new WinFormsPoint(340, 70);
            this.numIncrementValue.Size = new Size(80, 23);
            this.numIncrementValue.Minimum = 1;
            this.numIncrementValue.Maximum = 100;
            this.numIncrementValue.Value = 1;

            // Add controls to Number Format group
            this.grpNumberFormat.Controls.AddRange(new Control[] {
                lblNumberStyle, cmbNumberStyle,
                lblStartingNumber, numStartingNumber,
                lblIncrementValue, numIncrementValue
            });

            // === Buttons ===
            // OK Button
            this.btnOK.Text = "OK";
            this.btnOK.Location = new WinFormsPoint(228, 230);
            this.btnOK.Size = new Size(80, 27);
            this.btnOK.Click += BtnOK_Click;

            // Cancel Button
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Location = new WinFormsPoint(315, 230);
            this.btnCancel.Size = new Size(80, 27);
            this.btnCancel.Click += BtnCancel_Click;

            // Help Button
            this.btnHelp.Text = "Help";
            this.btnHelp.Location = new WinFormsPoint(402, 230);
            this.btnHelp.Size = new Size(70, 27);
            this.btnHelp.Click += BtnHelp_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                grpNameTemplate,
                grpNumberFormat,
                btnOK, btnCancel, btnHelp
            });

            this.ResumeLayout(false);
        }

        private void LoadPropertyFields()
        {
            cmbPropertyFields.Items.Clear();
            cmbPropertyFields.Items.Add("Next Counter");
            cmbPropertyFields.Items.Add("Point Number");
            cmbPropertyFields.Items.Add("Point Name");
            cmbPropertyFields.Items.Add("Description");
            cmbPropertyFields.Items.Add("Easting");
            cmbPropertyFields.Items.Add("Northing");
            cmbPropertyFields.Items.Add("Elevation");
            cmbPropertyFields.SelectedIndex = 0;
        }

        private void LoadNumberStyles()
        {
            cmbNumberStyle.Items.Clear();
            cmbNumberStyle.Items.Add("1, 2, 3...");
            cmbNumberStyle.Items.Add("I, II, III...");
            cmbNumberStyle.Items.Add("i, ii, iii...");
            cmbNumberStyle.Items.Add("A, B, C...");
            cmbNumberStyle.Items.Add("a, b, c...");
            cmbNumberStyle.SelectedIndex = 0;
        }

        private void RestoreLastUsedValues()
        {
            if (!string.IsNullOrEmpty(_lastNameTemplate))
                txtName.Text = _lastNameTemplate;

            if (_lastNumberStyleIndex >= 0 && _lastNumberStyleIndex < cmbNumberStyle.Items.Count)
                cmbNumberStyle.SelectedIndex = _lastNumberStyleIndex;

            numStartingNumber.Value = _lastStartingNumber;
            numIncrementValue.Value = _lastIncrementValue;
        }

        private void SaveLastUsedValues()
        {
            _lastNameTemplate = txtName.Text;
            _lastNumberStyleIndex = cmbNumberStyle.SelectedIndex;
            _lastStartingNumber = (int)numStartingNumber.Value;
            _lastIncrementValue = (int)numIncrementValue.Value;
        }

        private void BtnInsert_Click(object? sender, EventArgs e)
        {
            string fieldCode = cmbPropertyFields.SelectedIndex switch
            {
                0 => "<[Next Counter]>",
                1 => "<[Point Number]>",
                2 => "<[Point Name]>",
                3 => "<[Description]>",
                4 => "<[Easting]>",
                5 => "<[Northing]>",
                6 => "<[Elevation]>",
                _ => ""
            };

            // Insert at cursor position
            int cursorPos = txtName.SelectionStart;
            txtName.Text = txtName.Text.Insert(cursorPos, fieldCode);
            txtName.SelectionStart = cursorPos + fieldCode.Length;
            txtName.Focus();
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Vui lòng nhập template tên!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            // Get values
            NameTemplate = txtName.Text;
            SelectedNumberStyle = (NumberStyle)cmbNumberStyle.SelectedIndex;
            StartingNumber = (int)numStartingNumber.Value;
            IncrementValue = (int)numIncrementValue.Value;

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

        private void BtnHelp_Click(object? sender, EventArgs e)
        {
            string helpText = @"Hướng dẫn sử dụng Name Template:

1. Property Fields:
   - Next Counter: Tự động đánh số tăng dần
   - Point Number: Số thứ tự điểm
   - Point Name: Tên hiện tại của điểm
   - Description: Mô tả điểm
   - Easting/Northing: Tọa độ X/Y
   - Elevation: Cao độ

2. Name Template:
   - Nhập mẫu tên mong muốn
   - Nhấn Insert để chèn các trường thuộc tính
   - Ví dụ: 'POINT-<[Next Counter]>' → POINT-1, POINT-2...

3. Number Style:
   - Chọn kiểu đánh số (1,2,3 hoặc I,II,III...)

4. Starting Number: Số bắt đầu cho counter

5. Increment Value: Giá trị tăng mỗi lần";

            MessageBox.Show(helpText, "Trợ giúp - Name Template", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Tạo tên mới cho CogoPoint dựa trên template
        /// </summary>
        public string GenerateName(int counter, uint pointNumber, string pointName,
            string description, double easting, double northing, double elevation)
        {
            string result = NameTemplate;

            // Replace Next Counter with formatted number
            string counterStr = FormatNumber(counter);
            result = result.Replace("<[Next Counter]>", counterStr);

            // Replace other fields
            result = result.Replace("<[Point Number]>", pointNumber.ToString());
            result = result.Replace("<[Point Name]>", pointName);
            result = result.Replace("<[Description]>", description);
            result = result.Replace("<[Easting]>", easting.ToString("F3"));
            result = result.Replace("<[Northing]>", northing.ToString("F3"));
            result = result.Replace("<[Elevation]>", elevation.ToString("F3"));

            return result;
        }

        private string FormatNumber(int num)
        {
            return SelectedNumberStyle switch
            {
                NumberStyle.Arabic => num.ToString(),
                NumberStyle.RomanUpper => ToRoman(num).ToUpper(),
                NumberStyle.RomanLower => ToRoman(num).ToLower(),
                NumberStyle.LetterUpper => ToLetter(num).ToUpper(),
                NumberStyle.LetterLower => ToLetter(num).ToLower(),
                _ => num.ToString()
            };
        }

        private static string ToRoman(int number)
        {
            if (number < 1) return "";
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900);
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            return "";
        }

        private static string ToLetter(int number)
        {
            string result = "";
            while (number > 0)
            {
                number--;
                result = (char)('A' + number % 26) + result;
                number /= 26;
            }
            return result;
        }
    }
}
