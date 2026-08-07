using System;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Form nhập và điều chỉnh thông số đường đỏ (PVI) trong Civil 3D
    /// </summary>
    public class DieuChinhDuongDoForm : Form
    {
        // Remember last-used parameters across command invocations
        private static double _lastSlope = 0.0;
        private static double _lastDistance = 50.0;
        private static bool _lastShiftSubsequent = false;

        // Selected Object References
        public ObjectId ProfileId { get; set; } = ObjectId.Null;
        public ObjectId ProfileViewId { get; set; } = ObjectId.Null;

        // Selected PVI Data
        public int Pvi1Index { get; set; } = -1;
        public double Pvi1Station { get; set; } = 0.0;
        public double Pvi1Elevation { get; set; } = 0.0;

        public int Pvi2Index { get; set; } = -1;
        public double Pvi2Station { get; set; } = 0.0;
        public double Pvi2Elevation { get; set; } = 0.0;

        // Form Outputs
        public double SlopePercent => (double)numSlope.Value;
        public double NewDistance => (double)numDistance.Value;
        public bool ShiftSubsequent => chkShiftSubsequent.Checked;
        public bool FormAccepted { get; private set; } = false;

        // UI Controls
        private WinFormsLabel lblTitle = null!;
        
        // Group Đối tượng
        private GroupBox grpObjects = null!;
        private WinFormsLabel lblProfile = null!;
        public TextBox txtProfileName = null!;
        private WinFormsLabel lblProfileView = null!;
        public TextBox txtProfileViewName = null!;
        public Button btnPickProfileView = null!;

        // Group PVI
        private GroupBox grpPVI = null!;
        public Button btnPickSegment = null!;
        private WinFormsLabel lblPvi1 = null!;
        public TextBox txtPvi1Info = null!;
        public Button btnPickPvi1 = null!;
        private WinFormsLabel lblPvi2 = null!;
        public TextBox txtPvi2Info = null!;
        public Button btnPickPvi2 = null!;

        // Group Thông số
        private GroupBox grpParams = null!;
        private WinFormsLabel lblSlope = null!;
        public NumericUpDown numSlope = null!;

        private WinFormsLabel lblDistance = null!;
        public NumericUpDown numDistance = null!;
        public Button btnPickPoint2 = null!;

        public CheckBox chkShiftSubsequent = null!;

        // Action Buttons
        public Button btnOK = null!;
        public Button btnCancel = null!;

        public DieuChinhDuongDoForm()
        {
            InitializeComponent();
            RestoreLastUsedValues();
        }

        private void InitializeComponent()
        {
            var standardFont = new WinFormsFont("Segoe UI", 9.5F, FontStyle.Regular);
            var boldFont = new WinFormsFont("Segoe UI", 9.5F, FontStyle.Bold);
            var titleFont = new WinFormsFont("Segoe UI", 13F, FontStyle.Bold);

            // Control instantiation
            lblTitle = new WinFormsLabel();

            grpObjects = new GroupBox();
            lblProfile = new WinFormsLabel();
            txtProfileName = new TextBox();
            lblProfileView = new WinFormsLabel();
            txtProfileViewName = new TextBox();
            btnPickProfileView = new Button();

            grpPVI = new GroupBox();
            btnPickSegment = new Button();
            lblPvi1 = new WinFormsLabel();
            txtPvi1Info = new TextBox();
            btnPickPvi1 = new Button();
            lblPvi2 = new WinFormsLabel();
            txtPvi2Info = new TextBox();
            btnPickPvi2 = new Button();

            grpParams = new GroupBox();
            lblSlope = new WinFormsLabel();
            numSlope = new NumericUpDown();

            lblDistance = new WinFormsLabel();
            numDistance = new NumericUpDown();
            btnPickPoint2 = new Button();

            chkShiftSubsequent = new CheckBox();

            btnOK = new Button();
            btnCancel = new Button();

            this.SuspendLayout();

            // Form properties
            this.Text = "Điều Chỉnh Đường Đỏ (PVI)";
            this.Size = new Size(540, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = standardFont;

            // Title
            lblTitle.Text = "ĐIỀU CHỈNH ĐƯỜNG ĐỎ (PROFILE PVI)";
            lblTitle.Font = titleFont;
            lblTitle.Location = new WinFormsPoint(15, 12);
            lblTitle.Size = new Size(495, 30);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.ForeColor = Color.FromArgb(0, 102, 204);

            // Group 1: Đối tượng chọn
            grpObjects.Text = "1. Đối tượng chọn trên bản vẽ";
            grpObjects.Font = boldFont;
            grpObjects.Location = new WinFormsPoint(15, 45);
            grpObjects.Size = new Size(495, 95);

            lblProfileView.Text = "Trắc dọc:";
            lblProfileView.Font = standardFont;
            lblProfileView.Location = new WinFormsPoint(15, 25);
            lblProfileView.Size = new Size(70, 23);

            txtProfileViewName.Location = new WinFormsPoint(90, 23);
            txtProfileViewName.Size = new Size(250, 24);
            txtProfileViewName.ReadOnly = true;
            txtProfileViewName.Font = standardFont;
            txtProfileViewName.Text = "(Chưa chọn Trắc dọc)";

            btnPickProfileView.Text = "🎯 Chọn Trắc dọc";
            btnPickProfileView.Font = standardFont;
            btnPickProfileView.Location = new WinFormsPoint(350, 22);
            btnPickProfileView.Size = new Size(130, 27);

            lblProfile.Text = "Profile:";
            lblProfile.Font = standardFont;
            lblProfile.Location = new WinFormsPoint(15, 58);
            lblProfile.Size = new Size(70, 23);

            txtProfileName.Location = new WinFormsPoint(90, 56);
            txtProfileName.Size = new Size(390, 24);
            txtProfileName.ReadOnly = true;
            txtProfileName.Font = standardFont;
            txtProfileName.Text = "(Chưa chọn Profile)";

            grpObjects.Controls.AddRange(new Control[] {
                lblProfileView, txtProfileViewName, btnPickProfileView,
                lblProfile, txtProfileName
            });

            // Group 2: PVI
            grpPVI.Text = "2. Chọn các đỉnh PVI";
            grpPVI.Font = boldFont;
            grpPVI.Location = new WinFormsPoint(15, 145);
            grpPVI.Size = new Size(495, 132);

            btnPickSegment.Text = "🎯 Pick 1 điểm trên đoạn Profile (Tự động lấy 2 PVI)";
            btnPickSegment.Font = boldFont;
            btnPickSegment.ForeColor = Color.DarkGreen;
            btnPickSegment.Location = new WinFormsPoint(15, 24);
            btnPickSegment.Size = new Size(465, 30);

            lblPvi1.Text = "PVI 1 (Đầu):";
            lblPvi1.Font = standardFont;
            lblPvi1.Location = new WinFormsPoint(15, 62);
            lblPvi1.Size = new Size(85, 23);

            txtPvi1Info.Location = new WinFormsPoint(105, 60);
            txtPvi1Info.Size = new Size(265, 24);
            txtPvi1Info.ReadOnly = true;
            txtPvi1Info.Font = standardFont;
            txtPvi1Info.Text = "(Chưa chọn PVI 1 cố định)";

            btnPickPvi1.Text = "📍 Pick PVI 1";
            btnPickPvi1.Font = standardFont;
            btnPickPvi1.Location = new WinFormsPoint(380, 59);
            btnPickPvi1.Size = new Size(100, 27);

            lblPvi2.Text = "PVI 2 (Sau):";
            lblPvi2.Font = standardFont;
            lblPvi2.Location = new WinFormsPoint(15, 95);
            lblPvi2.Size = new Size(85, 23);

            txtPvi2Info.Location = new WinFormsPoint(105, 93);
            txtPvi2Info.Size = new Size(265, 24);
            txtPvi2Info.ReadOnly = true;
            txtPvi2Info.Font = standardFont;
            txtPvi2Info.Text = "(Chưa chọn PVI 2 thay đổi)";

            btnPickPvi2.Text = "📍 Pick PVI 2";
            btnPickPvi2.Font = standardFont;
            btnPickPvi2.Location = new WinFormsPoint(380, 92);
            btnPickPvi2.Size = new Size(100, 27);

            grpPVI.Controls.AddRange(new Control[] {
                btnPickSegment,
                lblPvi1, txtPvi1Info, btnPickPvi1,
                lblPvi2, txtPvi2Info, btnPickPvi2
            });

            // Group 3: Thông số điều chỉnh
            grpParams.Text = "3. Thông số điều chỉnh";
            grpParams.Font = boldFont;
            grpParams.Location = new WinFormsPoint(15, 283);
            grpParams.Size = new Size(495, 195);

            // Slope i (%)
            lblSlope.Text = "Dốc i (%):";
            lblSlope.Font = standardFont;
            lblSlope.Location = new WinFormsPoint(15, 30);
            lblSlope.Size = new Size(110, 23);

            numSlope.Location = new WinFormsPoint(130, 28);
            numSlope.Size = new Size(120, 24);
            numSlope.Font = standardFont;
            numSlope.DecimalPlaces = 4;
            numSlope.Minimum = -100;
            numSlope.Maximum = 100;
            numSlope.Increment = 0.1m;

            // Distance L (m)
            lblDistance.Text = "Khoảng cách L (m):";
            lblDistance.Font = standardFont;
            lblDistance.Location = new WinFormsPoint(15, 73);
            lblDistance.Size = new Size(115, 23);

            numDistance.Location = new WinFormsPoint(130, 71);
            numDistance.Size = new Size(120, 24);
            numDistance.Font = standardFont;
            numDistance.DecimalPlaces = 3;
            numDistance.Minimum = 0.001m;
            numDistance.Maximum = 50000m;
            numDistance.Increment = 5m;
            numDistance.Value = 50m;

            btnPickPoint2.Text = "🎯 Pick điểm thứ 2 (Tính L & i)";
            btnPickPoint2.Font = boldFont;
            btnPickPoint2.ForeColor = Color.FromArgb(0, 102, 204);
            btnPickPoint2.Location = new WinFormsPoint(265, 69);
            btnPickPoint2.Size = new Size(215, 28);

            // CheckBox
            chkShiftSubsequent.Text = "Tịnh tiến các PVI phía sau PVI 2 theo ΔS (Station offset)";
            chkShiftSubsequent.Font = standardFont;
            chkShiftSubsequent.Location = new WinFormsPoint(15, 115);
            chkShiftSubsequent.Size = new Size(465, 55);

            grpParams.Controls.AddRange(new Control[] {
                lblSlope, numSlope,
                lblDistance, numDistance, btnPickPoint2,
                chkShiftSubsequent
            });

            // Action Buttons
            btnOK.Text = "Thực hiện";
            btnOK.Font = boldFont;
            btnOK.Location = new WinFormsPoint(280, 492);
            btnOK.Size = new Size(110, 34);
            btnOK.Click += BtnOK_Click;

            btnCancel.Text = "Đóng";
            btnCancel.Font = standardFont;
            btnCancel.Location = new WinFormsPoint(400, 492);
            btnCancel.Size = new Size(110, 34);
            btnCancel.Click += BtnCancel_Click;

            // Form assembly
            this.Controls.AddRange(new Control[] {
                lblTitle,
                grpObjects,
                grpPVI,
                grpParams,
                btnOK,
                btnCancel
            });

            this.ResumeLayout(false);
        }

        private void RestoreLastUsedValues()
        {
            try
            {
                numSlope.Value = (decimal)_lastSlope;
                numDistance.Value = (decimal)_lastDistance;
                chkShiftSubsequent.Checked = _lastShiftSubsequent;
            }
            catch { }
        }

        private void SaveLastUsedValues()
        {
            _lastSlope = (double)numSlope.Value;
            _lastDistance = (double)numDistance.Value;
            _lastShiftSubsequent = chkShiftSubsequent.Checked;
        }

        public void UpdatePvi1Display()
        {
            if (Pvi1Index >= 0)
            {
                txtPvi1Info.Text = $"[PVI #{Pvi1Index}] Sta: {Pvi1Station:F2}m | Elev: {Pvi1Elevation:F3}m";
            }
            else
            {
                txtPvi1Info.Text = "(Chưa chọn PVI 1 cố định)";
            }
        }

        public void UpdatePvi2Display()
        {
            if (Pvi2Index >= 0)
            {
                txtPvi2Info.Text = $"[PVI #{Pvi2Index}] Sta: {Pvi2Station:F2}m | Elev: {Pvi2Elevation:F3}m";
            }
            else
            {
                txtPvi2Info.Text = "(Chưa chọn PVI 2 thay đổi)";
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (ProfileId.IsNull || !ProfileId.IsValid)
            {
                MessageBox.Show("Vui lòng chọn Profile trên trắc dọc trước!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (Pvi1Index < 0 || Pvi2Index < 0)
            {
                MessageBox.Show("Vui lòng chọn đủ 2 đỉnh PVI (PVI 1 cố định và PVI 2 thay đổi)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (Pvi1Index == Pvi2Index)
            {
                MessageBox.Show("PVI 1 và PVI 2 không được trùng nhau!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
