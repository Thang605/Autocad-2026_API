using System;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject.Civil_Tool
{
    public class DieuChinhProfileItem
    {
        public string Name { get; set; } = string.Empty;
        public ObjectId Id { get; set; } = ObjectId.Null;

        public DieuChinhProfileItem(string name, ObjectId id)
        {
            Name = name;
            Id = id;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Form nhập và điều chỉnh thông số đường đỏ (PVI) trong Civil 3D
    /// </summary>
    public class DieuChinhDuongDoForm : Form
    {
        // Remember last-used parameters across command invocations
        private static double _lastSlope = 0.0;
        private static double _lastDistance = 50.0;
        private static bool _lastShiftSubsequent = false;
        private static bool _lastIsFixPvi1 = true;
        private static int _lastSelectedTab = 0;

        // Selected Object References
        public ObjectId ProfileId { get; set; } = ObjectId.Null;
        public ObjectId ProfileViewId { get; set; } = ObjectId.Null;

        // Selected PVI Data (2-PVI Mode)
        public int Pvi1Index { get; set; } = -1;
        public double Pvi1Station { get; set; } = 0.0;
        public double Pvi1Elevation { get; set; } = 0.0;

        public int Pvi2Index { get; set; } = -1;
        public double Pvi2Station { get; set; } = 0.0;
        public double Pvi2Elevation { get; set; } = 0.0;

        // Selected PVI Data (1-PVI Mode)
        public int PviCenterIndex { get; set; } = -1;
        public double PviCenterStation { get; set; } = 0.0;
        public double PviCenterElevation { get; set; } = 0.0;

        public int PviPrevIndex { get; set; } = -1;
        public double PviPrevStation { get; set; } = 0.0;
        public double PviPrevElevation { get; set; } = 0.0;

        public int PviNextIndex { get; set; } = -1;
        public double PviNextStation { get; set; } = 0.0;
        public double PviNextElevation { get; set; } = 0.0;

        // Form Outputs
        public int SelectedTabMode => tabMain.SelectedIndex; // 0: 2-PVI Mode, 1: 1-PVI Mode
        public bool IsFixPvi1 => rdoFixPvi1.Checked;
        public double SlopePercent => (double)numSlope.Value;
        public double NewDistance => (double)numDistance.Value;
        public bool ShiftSubsequent => chkShiftSubsequent.Checked;

        public double SlopeBefore => (double)numSlopeBefore.Value;
        public double SlopeAfter => (double)numSlopeAfter.Value;
        public int CenterCalcMode => rdoCalcIntersection.Checked ? 0 : (rdoCalcFixStationBefore.Checked ? 1 : 2);

        public bool FormAccepted { get; private set; } = false;

        // UI Controls
        private WinFormsLabel lblTitle = null!;
        
        // Group Đối tượng
        private GroupBox grpObjects = null!;
        private WinFormsLabel lblProfile = null!;
        public ComboBox cboProfile = null!;
        public Button btnPickProfile = null!;
        private WinFormsLabel lblProfileView = null!;
        public TextBox txtProfileViewName = null!;
        public Button btnPickProfileView = null!;

        // Main Tab Control
        public TabControl tabMain = null!;
        public TabPage tab2PVI = null!;
        public TabPage tab1PVI = null!;

        // --- Controls for Tab 1 (2-PVI Mode) ---
        private GroupBox grpPVI = null!;
        public Button btnPickSegment = null!;
        public RadioButton rdoFixPvi1 = null!;
        public RadioButton rdoFixPvi2 = null!;
        private WinFormsLabel lblPvi1 = null!;
        public TextBox txtPvi1Info = null!;
        public Button btnPickPvi1 = null!;
        private WinFormsLabel lblPvi2 = null!;
        public TextBox txtPvi2Info = null!;
        public Button btnPickPvi2 = null!;

        private GroupBox grpParams = null!;
        private WinFormsLabel lblSlope = null!;
        public NumericUpDown numSlope = null!;
        private WinFormsLabel lblDistance = null!;
        public NumericUpDown numDistance = null!;
        public Button btnPickPoint2 = null!;
        public CheckBox chkShiftSubsequent = null!;

        // --- Controls for Tab 2 (1-PVI Mode) ---
        private GroupBox grpCenterPVI = null!;
        public Button btnPickCenterPvi = null!;
        public TextBox txtPrevPviInfo = null!;
        public TextBox txtCenterPviInfo = null!;
        public TextBox txtNextPviInfo = null!;

        private GroupBox grpCenterParams = null!;
        private WinFormsLabel lblSlopeBefore = null!;
        public NumericUpDown numSlopeBefore = null!;
        private WinFormsLabel lblSlopeAfter = null!;
        public NumericUpDown numSlopeAfter = null!;
        public RadioButton rdoCalcIntersection = null!;
        public RadioButton rdoCalcFixStationBefore = null!;
        public RadioButton rdoCalcFixStationAfter = null!;
        public TextBox txtCenterPreviewInfo = null!;

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
            lblProfileView = new WinFormsLabel();
            txtProfileViewName = new TextBox();
            btnPickProfileView = new Button();
            lblProfile = new WinFormsLabel();
            cboProfile = new ComboBox();
            btnPickProfile = new Button();

            tabMain = new TabControl();
            tab2PVI = new TabPage();
            tab1PVI = new TabPage();

            // Tab 1 controls
            grpPVI = new GroupBox();
            btnPickSegment = new Button();
            rdoFixPvi1 = new RadioButton();
            rdoFixPvi2 = new RadioButton();
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

            // Tab 2 controls
            grpCenterPVI = new GroupBox();
            btnPickCenterPvi = new Button();
            txtPrevPviInfo = new TextBox();
            txtCenterPviInfo = new TextBox();
            txtNextPviInfo = new TextBox();

            grpCenterParams = new GroupBox();
            lblSlopeBefore = new WinFormsLabel();
            numSlopeBefore = new NumericUpDown();
            lblSlopeAfter = new WinFormsLabel();
            numSlopeAfter = new NumericUpDown();
            rdoCalcIntersection = new RadioButton();
            rdoCalcFixStationBefore = new RadioButton();
            rdoCalcFixStationAfter = new RadioButton();
            txtCenterPreviewInfo = new TextBox();

            btnOK = new Button();
            btnCancel = new Button();

            this.SuspendLayout();

            // Form properties
            this.Text = "Điều Chỉnh Đường Đỏ (PVI)";
            this.Size = new Size(560, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = standardFont;

            // Title
            lblTitle.Text = "ĐIỀU CHỈNH ĐƯỜNG ĐỎ (PROFILE PVI)";
            lblTitle.Font = titleFont;
            lblTitle.Location = new WinFormsPoint(15, 10);
            lblTitle.Size = new Size(515, 30);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.ForeColor = Color.FromArgb(0, 102, 204);

            // Group 1: Đối tượng chọn
            grpObjects.Text = "1. Đối tượng chọn trên bản vẽ";
            grpObjects.Font = boldFont;
            grpObjects.Location = new WinFormsPoint(15, 42);
            grpObjects.Size = new Size(515, 95);

            lblProfileView.Text = "Trắc dọc:";
            lblProfileView.Font = standardFont;
            lblProfileView.Location = new WinFormsPoint(15, 25);
            lblProfileView.Size = new Size(70, 23);

            txtProfileViewName.Location = new WinFormsPoint(90, 23);
            txtProfileViewName.Size = new Size(270, 24);
            txtProfileViewName.ReadOnly = true;
            txtProfileViewName.Font = standardFont;
            txtProfileViewName.Text = "(Chưa chọn Trắc dọc)";

            btnPickProfileView.Text = "🎯 Chọn Trắc dọc";
            btnPickProfileView.Font = standardFont;
            btnPickProfileView.Location = new WinFormsPoint(370, 22);
            btnPickProfileView.Size = new Size(130, 27);

            lblProfile.Text = "Profile:";
            lblProfile.Font = standardFont;
            lblProfile.Location = new WinFormsPoint(15, 58);
            lblProfile.Size = new Size(70, 23);

            cboProfile.Location = new WinFormsPoint(90, 56);
            cboProfile.Size = new Size(270, 24);
            cboProfile.DropDownStyle = ComboBoxStyle.DropDownList;
            cboProfile.Font = standardFont;

            btnPickProfile.Text = "🎯 Chọn Profile";
            btnPickProfile.Font = standardFont;
            btnPickProfile.Location = new WinFormsPoint(370, 55);
            btnPickProfile.Size = new Size(130, 27);

            grpObjects.Controls.AddRange(new Control[] {
                lblProfileView, txtProfileViewName, btnPickProfileView,
                lblProfile, cboProfile, btnPickProfile
            });

            // Tab Control
            tabMain.Location = new WinFormsPoint(15, 143);
            tabMain.Size = new Size(515, 435);
            tabMain.Font = boldFont;

            tab2PVI.Text = "Điều chỉnh 2 PVI (L & i)";
            tab1PVI.Text = "Điều chỉnh 1 PVI (Dốc cánh trước & sau)";

            // ==================== TAB 1 (2 PVI) ====================
            grpPVI.Text = "2. Chọn các đỉnh PVI";
            grpPVI.Font = boldFont;
            grpPVI.Location = new WinFormsPoint(10, 10);
            grpPVI.Size = new Size(490, 150);

            btnPickSegment.Text = "🎯 Pick 1 điểm trên đoạn Profile (Tự động lấy 2 PVI)";
            btnPickSegment.Font = boldFont;
            btnPickSegment.ForeColor = Color.DarkGreen;
            btnPickSegment.Location = new WinFormsPoint(12, 22);
            btnPickSegment.Size = new Size(465, 28);

            rdoFixPvi1.Text = "Cố định PVI 1 (Sửa PVI 2)";
            rdoFixPvi1.Font = boldFont;
            rdoFixPvi1.Location = new WinFormsPoint(12, 54);
            rdoFixPvi1.Size = new Size(220, 24);
            rdoFixPvi1.Checked = true;
            rdoFixPvi1.CheckedChanged += FixMode_CheckedChanged;

            rdoFixPvi2.Text = "Cố định PVI 2 (Sửa PVI 1)";
            rdoFixPvi2.Font = boldFont;
            rdoFixPvi2.Location = new WinFormsPoint(240, 54);
            rdoFixPvi2.Size = new Size(235, 24);
            rdoFixPvi2.CheckedChanged += FixMode_CheckedChanged;

            lblPvi1.Text = "PVI 1 (Đầu):";
            lblPvi1.Font = standardFont;
            lblPvi1.Location = new WinFormsPoint(12, 84);
            lblPvi1.Size = new Size(80, 23);

            txtPvi1Info.Location = new WinFormsPoint(95, 82);
            txtPvi1Info.Size = new Size(275, 24);
            txtPvi1Info.ReadOnly = true;
            txtPvi1Info.Font = standardFont;
            txtPvi1Info.Text = "(Chưa chọn PVI 1 cố định)";

            btnPickPvi1.Text = "📍 Pick PVI 1";
            btnPickPvi1.Font = standardFont;
            btnPickPvi1.Location = new WinFormsPoint(377, 81);
            btnPickPvi1.Size = new Size(100, 27);

            lblPvi2.Text = "PVI 2 (Sau):";
            lblPvi2.Font = standardFont;
            lblPvi2.Location = new WinFormsPoint(12, 114);
            lblPvi2.Size = new Size(80, 23);

            txtPvi2Info.Location = new WinFormsPoint(95, 112);
            txtPvi2Info.Size = new Size(275, 24);
            txtPvi2Info.ReadOnly = true;
            txtPvi2Info.Font = standardFont;
            txtPvi2Info.Text = "(Chưa chọn PVI 2 thay đổi)";

            btnPickPvi2.Text = "📍 Pick PVI 2";
            btnPickPvi2.Font = standardFont;
            btnPickPvi2.Location = new WinFormsPoint(377, 111);
            btnPickPvi2.Size = new Size(100, 27);

            grpPVI.Controls.AddRange(new Control[] {
                btnPickSegment,
                rdoFixPvi1, rdoFixPvi2,
                lblPvi1, txtPvi1Info, btnPickPvi1,
                lblPvi2, txtPvi2Info, btnPickPvi2
            });

            grpParams.Text = "3. Thông số điều chỉnh";
            grpParams.Font = boldFont;
            grpParams.Location = new WinFormsPoint(10, 168);
            grpParams.Size = new Size(490, 220);

            lblSlope.Text = "Dốc i (%):";
            lblSlope.Font = standardFont;
            lblSlope.Location = new WinFormsPoint(12, 30);
            lblSlope.Size = new Size(110, 23);

            numSlope.Location = new WinFormsPoint(130, 28);
            numSlope.Size = new Size(120, 24);
            numSlope.Font = standardFont;
            numSlope.DecimalPlaces = 4;
            numSlope.Minimum = -100;
            numSlope.Maximum = 100;
            numSlope.Increment = 0.1m;

            lblDistance.Text = "Khoảng cách L (m):";
            lblDistance.Font = standardFont;
            lblDistance.Location = new WinFormsPoint(12, 70);
            lblDistance.Size = new Size(115, 23);

            numDistance.Location = new WinFormsPoint(130, 68);
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
            btnPickPoint2.Location = new WinFormsPoint(260, 66);
            btnPickPoint2.Size = new Size(215, 28);

            chkShiftSubsequent.Text = "Tịnh tiến các PVI phía sau PVI 2 theo ΔS (Station offset)";
            chkShiftSubsequent.Font = standardFont;
            chkShiftSubsequent.Location = new WinFormsPoint(12, 110);
            chkShiftSubsequent.Size = new Size(465, 55);

            grpParams.Controls.AddRange(new Control[] {
                lblSlope, numSlope,
                lblDistance, numDistance, btnPickPoint2,
                chkShiftSubsequent
            });

            tab2PVI.Controls.AddRange(new Control[] { grpPVI, grpParams });

            // ==================== TAB 2 (1 PVI) ====================
            grpCenterPVI.Text = "2. Chọn đỉnh PVI điều chỉnh (Giữ PVI trước & sau cố định)";
            grpCenterPVI.Font = boldFont;
            grpCenterPVI.Location = new WinFormsPoint(10, 10);
            grpCenterPVI.Size = new Size(490, 160);

            btnPickCenterPvi.Text = "🎯 Pick 1 đỉnh PVI trên bản vẽ (Tự động lấy PVI trước & sau)";
            btnPickCenterPvi.Font = boldFont;
            btnPickCenterPvi.ForeColor = Color.DarkGreen;
            btnPickCenterPvi.Location = new WinFormsPoint(12, 22);
            btnPickCenterPvi.Size = new Size(465, 28);

            txtPrevPviInfo.Location = new WinFormsPoint(12, 58);
            txtPrevPviInfo.Size = new Size(465, 24);
            txtPrevPviInfo.ReadOnly = true;
            txtPrevPviInfo.Font = standardFont;
            txtPrevPviInfo.Text = "[PVI trước]: (Chưa chọn PVI trung tâm)";

            txtCenterPviInfo.Location = new WinFormsPoint(12, 88);
            txtCenterPviInfo.Size = new Size(465, 24);
            txtCenterPviInfo.ReadOnly = true;
            txtCenterPviInfo.Font = boldFont;
            txtCenterPviInfo.ForeColor = Color.DarkBlue;
            txtCenterPviInfo.Text = "[PVI chọn]: (Chưa chọn PVI trung tâm)";

            txtNextPviInfo.Location = new WinFormsPoint(12, 118);
            txtNextPviInfo.Size = new Size(465, 24);
            txtNextPviInfo.ReadOnly = true;
            txtNextPviInfo.Font = standardFont;
            txtNextPviInfo.Text = "[PVI sau]: (Chưa chọn PVI trung tâm)";

            grpCenterPVI.Controls.AddRange(new Control[] {
                btnPickCenterPvi,
                txtPrevPviInfo, txtCenterPviInfo, txtNextPviInfo
            });

            grpCenterParams.Text = "3. Khai báo dốc cánh tuyến trước & sau";
            grpCenterParams.Font = boldFont;
            grpCenterParams.Location = new WinFormsPoint(10, 178);
            grpCenterParams.Size = new Size(490, 212);

            lblSlopeBefore.Text = "Dốc cánh trước i1 (%):";
            lblSlopeBefore.Font = standardFont;
            lblSlopeBefore.Location = new WinFormsPoint(12, 26);
            lblSlopeBefore.Size = new Size(140, 23);

            numSlopeBefore.Location = new WinFormsPoint(155, 24);
            numSlopeBefore.Size = new Size(110, 24);
            numSlopeBefore.Font = standardFont;
            numSlopeBefore.DecimalPlaces = 4;
            numSlopeBefore.Minimum = -100;
            numSlopeBefore.Maximum = 100;
            numSlopeBefore.Increment = 0.1m;
            numSlopeBefore.ValueChanged += (s, e) => UpdateCenterPreview();

            lblSlopeAfter.Text = "Dốc cánh sau i2 (%):";
            lblSlopeAfter.Font = standardFont;
            lblSlopeAfter.Location = new WinFormsPoint(275, 26);
            lblSlopeAfter.Size = new Size(130, 23);

            numSlopeAfter.Location = new WinFormsPoint(400, 24);
            numSlopeAfter.Size = new Size(77, 24);
            numSlopeAfter.Font = standardFont;
            numSlopeAfter.DecimalPlaces = 4;
            numSlopeAfter.Minimum = -100;
            numSlopeAfter.Maximum = 100;
            numSlopeAfter.Increment = 0.1m;
            numSlopeAfter.ValueChanged += (s, e) => UpdateCenterPreview();

            rdoCalcIntersection.Text = "Tính Lý trình & Cao độ theo GIAO ĐIỂM 2 dốc cánh tuyến";
            rdoCalcIntersection.Font = boldFont;
            rdoCalcIntersection.ForeColor = Color.DarkBlue;
            rdoCalcIntersection.Location = new WinFormsPoint(12, 56);
            rdoCalcIntersection.Size = new Size(465, 24);
            rdoCalcIntersection.Checked = true;
            rdoCalcIntersection.CheckedChanged += (s, e) => UpdateCenterPreview();

            rdoCalcFixStationBefore.Text = "Cố định Lý trình PVI chọn, tính Cao độ theo Dốc cánh trước";
            rdoCalcFixStationBefore.Font = standardFont;
            rdoCalcFixStationBefore.Location = new WinFormsPoint(12, 82);
            rdoCalcFixStationBefore.Size = new Size(465, 24);
            rdoCalcFixStationBefore.CheckedChanged += (s, e) => UpdateCenterPreview();

            rdoCalcFixStationAfter.Text = "Cố định Lý trình PVI chọn, tính Cao độ theo Dốc cánh sau";
            rdoCalcFixStationAfter.Font = standardFont;
            rdoCalcFixStationAfter.Location = new WinFormsPoint(12, 108);
            rdoCalcFixStationAfter.Size = new Size(465, 24);
            rdoCalcFixStationAfter.CheckedChanged += (s, e) => UpdateCenterPreview();

            txtCenterPreviewInfo.Location = new WinFormsPoint(12, 140);
            txtCenterPreviewInfo.Size = new Size(465, 60);
            txtCenterPreviewInfo.Multiline = true;
            txtCenterPreviewInfo.ReadOnly = true;
            txtCenterPreviewInfo.Font = standardFont;
            txtCenterPreviewInfo.BackColor = Color.Ivory;
            txtCenterPreviewInfo.Text = "Vị trí mới dự kiến: (Chưa chọn PVI)";

            grpCenterParams.Controls.AddRange(new Control[] {
                lblSlopeBefore, numSlopeBefore,
                lblSlopeAfter, numSlopeAfter,
                rdoCalcIntersection, rdoCalcFixStationBefore, rdoCalcFixStationAfter,
                txtCenterPreviewInfo
            });

            tab1PVI.Controls.AddRange(new Control[] { grpCenterPVI, grpCenterParams });

            tabMain.Controls.Add(tab2PVI);
            tabMain.Controls.Add(tab1PVI);

            // Action Buttons
            btnOK.Text = "Thực hiện";
            btnOK.Font = boldFont;
            btnOK.Location = new WinFormsPoint(300, 588);
            btnOK.Size = new Size(110, 34);
            btnOK.Click += BtnOK_Click;

            btnCancel.Text = "Đóng";
            btnCancel.Font = standardFont;
            btnCancel.Location = new WinFormsPoint(420, 588);
            btnCancel.Size = new Size(110, 34);
            btnCancel.Click += BtnCancel_Click;

            // Form assembly
            this.Controls.AddRange(new Control[] {
                lblTitle,
                grpObjects,
                tabMain,
                btnOK,
                btnCancel
            });

            this.ResumeLayout(false);
        }

        private void FixMode_CheckedChanged(object? sender, EventArgs e)
        {
            if (rdoFixPvi1.Checked)
            {
                btnPickPoint2.Text = "🎯 Pick điểm thứ 2 (Tính L & i)";
                chkShiftSubsequent.Text = "Tịnh tiến các PVI phía sau PVI 2 theo ΔS (Station offset)";
            }
            else
            {
                btnPickPoint2.Text = "🎯 Pick điểm thứ 1 (Tính L & i)";
                chkShiftSubsequent.Text = "Tịnh tiến các PVI phía trước PVI 1 theo ΔS (Station offset)";
            }
            UpdatePvi1Display();
            UpdatePvi2Display();
        }

        private void RestoreLastUsedValues()
        {
            try
            {
                numSlope.Value = (decimal)_lastSlope;
                numDistance.Value = (decimal)_lastDistance;
                chkShiftSubsequent.Checked = _lastShiftSubsequent;
                if (_lastIsFixPvi1) rdoFixPvi1.Checked = true;
                else rdoFixPvi2.Checked = true;

                if (_lastSelectedTab >= 0 && _lastSelectedTab < tabMain.TabCount)
                {
                    tabMain.SelectedIndex = _lastSelectedTab;
                }
            }
            catch { }
        }

        private void SaveLastUsedValues()
        {
            _lastSlope = (double)numSlope.Value;
            _lastDistance = (double)numDistance.Value;
            _lastShiftSubsequent = chkShiftSubsequent.Checked;
            _lastIsFixPvi1 = rdoFixPvi1.Checked;
            _lastSelectedTab = tabMain.SelectedIndex;
        }

        public void UpdatePvi1Display()
        {
            if (Pvi1Index >= 0)
            {
                string role = IsFixPvi1 ? "Cố định" : "Thay đổi";
                txtPvi1Info.Text = $"[PVI #{Pvi1Index}] Sta: {Pvi1Station:F2}m | Elev: {Pvi1Elevation:F3}m ({role})";
            }
            else
            {
                txtPvi1Info.Text = IsFixPvi1 ? "(Chưa chọn PVI 1 cố định)" : "(Chưa chọn PVI 1 thay đổi)";
            }
        }

        public void UpdatePvi2Display()
        {
            if (Pvi2Index >= 0)
            {
                string role = IsFixPvi1 ? "Thay đổi" : "Cố định";
                txtPvi2Info.Text = $"[PVI #{Pvi2Index}] Sta: {Pvi2Station:F2}m | Elev: {Pvi2Elevation:F3}m ({role})";
            }
            else
            {
                txtPvi2Info.Text = IsFixPvi1 ? "(Chưa chọn PVI 2 thay đổi)" : "(Chưa chọn PVI 2 cố định)";
            }
        }

        public void SetCenterPviData(int centerIdx, double centerSta, double centerElev,
            int prevIdx, double prevSta, double prevElev,
            int nextIdx, double nextSta, double nextElev,
            double slopeBefore, double slopeAfter)
        {
            PviCenterIndex = centerIdx;
            PviCenterStation = centerSta;
            PviCenterElevation = centerElev;

            PviPrevIndex = prevIdx;
            PviPrevStation = prevSta;
            PviPrevElevation = prevElev;

            PviNextIndex = nextIdx;
            PviNextStation = nextSta;
            PviNextElevation = nextElev;

            txtPrevPviInfo.Text = $"[PVI trước #{prevIdx}] Sta: {prevSta:F2}m | Elev: {prevElev:F3}m (CỐ ĐỊNH)";
            txtCenterPviInfo.Text = $"[PVI chọn #{centerIdx}] Sta: {centerSta:F2}m | Elev: {centerElev:F3}m (THAY ĐỔI)";
            txtNextPviInfo.Text = $"[PVI sau #{nextIdx}] Sta: {nextSta:F2}m | Elev: {nextElev:F3}m (CỐ ĐỊNH)";

            numSlopeBefore.Value = (decimal)Math.Round(slopeBefore, 4);
            numSlopeAfter.Value = (decimal)Math.Round(slopeAfter, 4);

            UpdateCenterPreview();
        }

        public void UpdateCenterPreview()
        {
            if (PviCenterIndex < 0 || PviPrevIndex < 0 || PviNextIndex < 0)
            {
                txtCenterPreviewInfo.Text = "Vị trí mới dự kiến: (Chưa chọn PVI)";
                return;
            }

            double sPrev = PviPrevStation;
            double ePrev = PviPrevElevation;
            double sNext = PviNextStation;
            double eNext = PviNextElevation;
            double sCurr = PviCenterStation;
            double eCurr = PviCenterElevation;

            double i1 = (double)numSlopeBefore.Value;
            double i2 = (double)numSlopeAfter.Value;

            if (rdoCalcIntersection.Checked)
            {
                if (Math.Abs(i1 - i2) < 0.00001)
                {
                    txtCenterPreviewInfo.Text = "⚠️ Dốc cánh trước và sau bằng nhau (song song), không thể tính giao điểm!";
                    return;
                }

                double sNew = (100.0 * (eNext - ePrev) - i2 * sNext + i1 * sPrev) / (i1 - i2);
                double eNew = ePrev + (i1 / 100.0) * (sNew - sPrev);
                double dS = sNew - sCurr;
                double dZ = eNew - eCurr;

                txtCenterPreviewInfo.Text = $"📌 GIAO ĐIỂM 2 DỐC (PVI #{PviCenterIndex}):\r\n" +
                    $" - Lý trình mới: {sNew:F2}m (ΔS = {dS:+0.00;-0.00;0.00}m)\r\n" +
                    $" - Cao độ mới: {eNew:F3}m (ΔZ = {dZ:+0.000;-0.000;0.000}m)";
            }
            else if (rdoCalcFixStationBefore.Checked)
            {
                double sNew = sCurr;
                double eNew = ePrev + (i1 / 100.0) * (sNew - sPrev);
                double i2Calc = (Math.Abs(sNext - sNew) > 0.0001) ? ((eNext - eNew) / (sNext - sNew)) * 100.0 : 0.0;
                double dZ = eNew - eCurr;

                txtCenterPreviewInfo.Text = $"📌 THEO DỐC TRƯỚC (Giữ Sta {sNew:F2}m):\r\n" +
                    $" - Cao độ mới PVI #{PviCenterIndex}: {eNew:F3}m (ΔZ = {dZ:+0.000;-0.000;0.000}m)\r\n" +
                    $" - Dốc cánh sau tự động tính lại = {i2Calc:F4}%";
            }
            else if (rdoCalcFixStationAfter.Checked)
            {
                double sNew = sCurr;
                double eNew = eNext - (i2 / 100.0) * (sNext - sNew);
                double i1Calc = (Math.Abs(sNew - sPrev) > 0.0001) ? ((eNew - ePrev) / (sNew - sPrev)) * 100.0 : 0.0;
                double dZ = eNew - eCurr;

                txtCenterPreviewInfo.Text = $"📌 THEO DỐC SAU (Giữ Sta {sNew:F2}m):\r\n" +
                    $" - Cao độ mới PVI #{PviCenterIndex}: {eNew:F3}m (ΔZ = {dZ:+0.000;-0.000;0.000}m)\r\n" +
                    $" - Dốc cánh trước tự động tính lại = {i1Calc:F4}%";
            }
        }

        public void PopulateProfiles(System.Collections.Generic.List<DieuChinhProfileItem> profiles, ObjectId selectedId)
        {
            cboProfile.SelectedIndexChanged -= CboProfile_SelectedIndexChanged;
            cboProfile.Items.Clear();

            DieuChinhProfileItem? toSelect = null;
            foreach (var item in profiles)
            {
                cboProfile.Items.Add(item);
                if (item.Id == selectedId)
                {
                    toSelect = item;
                }
            }

            if (toSelect != null)
            {
                cboProfile.SelectedItem = toSelect;
                ProfileId = toSelect.Id;
            }
            else if (cboProfile.Items.Count > 0)
            {
                cboProfile.SelectedIndex = 0;
                ProfileId = ((DieuChinhProfileItem)cboProfile.Items[0]).Id;
            }
            else
            {
                ProfileId = ObjectId.Null;
            }

            cboProfile.SelectedIndexChanged += CboProfile_SelectedIndexChanged;
        }

        private void CboProfile_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboProfile.SelectedItem is DieuChinhProfileItem item)
            {
                if (ProfileId != item.Id)
                {
                    ProfileId = item.Id;
                    // Reset PVI selections when profile changes
                    Pvi1Index = -1;
                    Pvi2Index = -1;
                    PviCenterIndex = -1;
                    PviPrevIndex = -1;
                    PviNextIndex = -1;
                    UpdatePvi1Display();
                    UpdatePvi2Display();
                    txtPrevPviInfo.Text = "[PVI trước]: (Chưa chọn PVI trung tâm)";
                    txtCenterPviInfo.Text = "[PVI chọn]: (Chưa chọn PVI trung tâm)";
                    txtNextPviInfo.Text = "[PVI sau]: (Chưa chọn PVI trung tâm)";
                    UpdateCenterPreview();
                }
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (ProfileId.IsNull || !ProfileId.IsValid)
            {
                MessageBox.Show("Vui lòng chọn Profile trên trắc dọc trước!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (SelectedTabMode == 0) // Tab 1: 2 PVI
            {
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
            }
            else // Tab 2: 1 PVI
            {
                if (PviCenterIndex < 0 || PviPrevIndex < 0 || PviNextIndex < 0)
                {
                    MessageBox.Show("Vui lòng pick 1 đỉnh PVI điều chỉnh trước!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (rdoCalcIntersection.Checked)
                {
                    double i1 = SlopeBefore;
                    double i2 = SlopeAfter;
                    if (Math.Abs(i1 - i2) < 0.00001)
                    {
                        MessageBox.Show("Dốc cánh trước và Dốc cánh sau bằng nhau (song song), không thể tính giao điểm PVI!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
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
