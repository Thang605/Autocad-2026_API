using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Civil3DCsharp;

namespace MyFirstProject.Civil_Tool
{
    public class PviComboItem
    {
        public int Index { get; set; }
        public double Station { get; set; }
        public double Elevation { get; set; }
        public bool IsEndpoint { get; set; }

        public PviComboItem(int index, double station, double elevation, bool isEndpoint = false)
        {
            Index = index;
            Station = station;
            Elevation = elevation;
            IsEndpoint = isEndpoint;
        }

        public override string ToString()
        {
            if (IsEndpoint)
            {
                string endType = (Index == 0) ? "ĐẦU TUYẾN" : "CUỐI TUYẾN";
                return $"PVI #{Index} ({endType} - Không bố trí cong được)";
            }
            return $"PVI #{Index} (Lý trình: {Station:F2}m, Cao độ: {Elevation:F3}m)";
        }
    }

    public partial class BoTriCongDungForm : Form
    {
        public ObjectId ProfileId { get; set; } = ObjectId.Null;
        public ObjectId ProfileViewId { get; set; } = ObjectId.Null;
        public int PviIndex { get; set; } = -1;
        public int TotalPviCount { get; set; } = 0;
        public double PviStation { get; set; } = 0;
        public double PviElevation { get; set; } = 0;
        public double GradeIn { get; set; } = 0; // %
        public double GradeOut { get; set; } = 0; // %
        public double AlgebraicGradeDiff { get; set; } = 0; // % = |GradeIn - GradeOut|
        public bool IsConvex { get; set; } = true; // true = Cong Lồi (i1 > i2), false = Cong Lõm (i1 < i2)

        public bool FormAccepted { get; set; } = false;
        public bool PickNextAfterApply { get; set; } = false;

        // Custom events for non-modal instant apply and pick next
        public event EventHandler? OnApplyClicked;
        public event EventHandler? OnPickPviNextClicked;
        public event EventHandler? OnApplyAllClicked;

        // Computed parameters from standard
        public double MinRadiusLimit { get; private set; } = 0;
        public double MinRadiusNormal { get; private set; } = 0;
        public double MinCurveLengthStandard { get; private set; } = 0;
        public double ThresholdAlgebraicDiff { get; private set; } = 1.0;

        // Bộ nhớ tĩnh lưu thông số đã chọn
        private static string _lastStandardName = "";
        private static string _lastVtk = "";
        private static string _lastTerrain = "";
        private static bool _lastIsRadiusMode = true;

        public BoTriCongDungForm()
        {
            InitializeComponent();

            PopulateStandards();
            this.cmbTerrain.SelectedIndex = 0; // Đồng bằng

            this.cmbStandard.SelectedIndexChanged += (s, e) => { UpdateVtkList(); UpdateSuggestions(); };
            this.cmbVtk.SelectedIndexChanged += (s, e) => UpdateSuggestions();
            this.cmbTerrain.SelectedIndexChanged += (s, e) => UpdateSuggestions();

            this.radRadius.CheckedChanged += RadMode_CheckedChanged;
            this.radLength.CheckedChanged += RadMode_CheckedChanged;

            this.txtRadius.TextChanged += (s, e) => RecalculateFromRadius();
            this.txtLength.TextChanged += (s, e) => RecalculateFromLength();

            this.btnApply.Click += BtnApply_Click;
            this.btnPickPviNext.Click += (s, e) => OnPickPviNextClicked?.Invoke(this, EventArgs.Empty);
            this.btnApplyAndClose.Click += BtnApplyAndClose_Click;
            this.btnApplyAll.Click += BtnApplyAll_Click;
            this.btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.FormClosing += (s, e) => SaveLastUsedValues();

            RestoreLastUsedValues();
        }

        private void RestoreLastUsedValues()
        {
            try
            {
                if (!string.IsNullOrEmpty(_lastStandardName))
                {
                    int idx = cmbStandard.Items.IndexOf(_lastStandardName);
                    if (idx >= 0) cmbStandard.SelectedIndex = idx;
                }

                if (!string.IsNullOrEmpty(_lastVtk))
                {
                    int idx = cmbVtk.Items.IndexOf(_lastVtk);
                    if (idx >= 0) cmbVtk.SelectedIndex = idx;
                }

                if (!string.IsNullOrEmpty(_lastTerrain))
                {
                    int idx = cmbTerrain.Items.IndexOf(_lastTerrain);
                    if (idx >= 0) cmbTerrain.SelectedIndex = idx;
                }

                if (_lastIsRadiusMode) radRadius.Checked = true;
                else radLength.Checked = true;
            }
            catch { }
        }

        private void SaveLastUsedValues()
        {
            try
            {
                if (cmbStandard.SelectedItem != null)
                    _lastStandardName = cmbStandard.SelectedItem.ToString() ?? "";

                if (cmbVtk.SelectedItem != null)
                    _lastVtk = cmbVtk.SelectedItem.ToString() ?? "";

                if (cmbTerrain.SelectedItem != null)
                    _lastTerrain = cmbTerrain.SelectedItem.ToString() ?? "";

                _lastIsRadiusMode = radRadius.Checked;
            }
            catch { }
        }

        private void PopulateStandards()
        {
            this.cmbStandard.Items.Clear();
            var standards = StandardFactory.GetAllStandards();
            foreach (var std in standards)
            {
                this.cmbStandard.Items.Add(std.StandardName);
            }
            if (this.cmbStandard.Items.Count > 0)
                this.cmbStandard.SelectedIndex = 0;

            UpdateVtkList();
        }

        private void UpdateVtkList()
        {
            var standards = StandardFactory.GetAllStandards();
            int stdIndex = cmbStandard.SelectedIndex;
            if (stdIndex < 0 || stdIndex >= standards.Count) return;

            var standard = standards[stdIndex];
            string? currentVtk = cmbVtk.SelectedItem?.ToString();

            cmbVtk.Items.Clear();
            foreach (int spd in standard.SupportedSpeeds)
            {
                cmbVtk.Items.Add(spd.ToString());
            }

            if (currentVtk != null && cmbVtk.Items.Contains(currentVtk))
            {
                cmbVtk.SelectedItem = currentVtk;
            }
            else if (cmbVtk.Items.Count > 0)
            {
                cmbVtk.SelectedIndex = Math.Min(2, cmbVtk.Items.Count - 1);
            }
        }

        public void PopulateProfiles(List<DieuChinhProfileItem> items, ObjectId selectedId)
        {
            cmbProfile.Items.Clear();
            int selectIdx = -1;
            for (int i = 0; i < items.Count; i++)
            {
                cmbProfile.Items.Add(items[i]);
                if (items[i].Id == selectedId) selectIdx = i;
            }

            if (selectIdx >= 0) cmbProfile.SelectedIndex = selectIdx;
            else if (cmbProfile.Items.Count > 0) cmbProfile.SelectedIndex = 0;
        }

        public void PopulatePvis(List<PviComboItem> items, int selectIdx = -1)
        {
            cmbPvi.Items.Clear();
            TotalPviCount = items.Count;

            for (int i = 0; i < items.Count; i++)
            {
                cmbPvi.Items.Add(items[i]);
            }

            if (selectIdx >= 0 && selectIdx < cmbPvi.Items.Count)
            {
                cmbPvi.SelectedIndex = selectIdx;
            }
            else if (cmbPvi.Items.Count > 2)
            {
                cmbPvi.SelectedIndex = 1;
            }
            else if (cmbPvi.Items.Count > 0)
            {
                cmbPvi.SelectedIndex = 0;
            }
        }

        public void SetPviInformation(string profileName, int pviIndex, double station, double elevation, double gradeIn, double gradeOut, double currentRadius = 0, double currentLength = 0)
        {
            PviIndex = pviIndex;
            PviStation = station;
            PviElevation = elevation;
            GradeIn = gradeIn;
            GradeOut = gradeOut;

            bool isStartPoint = (pviIndex == 0);
            bool isEndPoint = (TotalPviCount > 0 && pviIndex == TotalPviCount - 1);

            if (isStartPoint || isEndPoint)
            {
                AlgebraicGradeDiff = 0;
                lblPviDetails.Text = $"PVI #{pviIndex} (Điểm đầu/cuối tuyến)  |  Lý trình: {station:F2} m  |  Cao độ: {elevation:F3} m";
                string note = isStartPoint ? "Đầu tuyến (chỉ có dốc đi i2)" : "Cuối tuyến (chỉ có dốc vào i1)";
                lblGrades.Text = $"[!] PVI #{pviIndex} là đỉnh {note} -> Không bố trí cong đứng được";
                lblGrades.ForeColor = Color.Maroon;
            }
            else
            {
                AlgebraicGradeDiff = Math.Abs(gradeIn - gradeOut);
                IsConvex = gradeIn > gradeOut;

                lblPviDetails.Text = $"PVI #{pviIndex}  |  Lý trình: {station:F2} m  |  Cao độ: {elevation:F3} m";
                string curveTypeStr = IsConvex ? "Đường cong LỒI" : "Đường cong LÕM";
                lblGrades.Text = $"Dốc i1 = {gradeIn:F2}%  |  Dốc i2 = {gradeOut:F2}%  |  Hiệu dốc Δi = {AlgebraicGradeDiff:F2}% ({curveTypeStr})";
                lblGrades.ForeColor = Color.DarkGreen;
            }

            UpdateSuggestions();

            // Setup default or current radius/length
            if (currentRadius > 0)
            {
                txtRadius.Text = Math.Round(currentRadius, 1).ToString();
                radRadius.Checked = true;
            }
            else if (currentLength > 0)
            {
                txtLength.Text = Math.Round(currentLength, 2).ToString();
                radLength.Checked = true;
            }
            else
            {
                double defaultR = MinRadiusNormal > 0 ? MinRadiusNormal : (MinRadiusLimit > 0 ? MinRadiusLimit : 6000.0);
                txtRadius.Text = Math.Round(defaultR, 0).ToString();
                radRadius.Checked = true;
            }

            RecalculateFromRadius();
        }

        public void UpdateSuggestions()
        {
            if (cmbVtk.SelectedItem == null) return;
            string vtkStr = cmbVtk.SelectedItem.ToString() ?? "60";
            int vtk = int.TryParse(vtkStr, out int v) ? v : 60;
            string terrain = cmbTerrain.SelectedItem?.ToString() ?? "Đồng bằng";

            var standards = StandardFactory.GetAllStandards();
            int stdIndex = cmbStandard.SelectedIndex;
            if (stdIndex < 0 || stdIndex >= standards.Count) stdIndex = 0;
            var standard = standards[stdIndex];

            ProfileDesignParameters profileParams = standard.GetProfileParameters(vtk, terrain);
            ThresholdAlgebraicDiff = profileParams.AlgebraicGradeDiffThreshold;
            MinCurveLengthStandard = profileParams.MinVerticalCurveLength;

            if (IsConvex)
            {
                MinRadiusLimit = profileParams.MinConvexRadiusLimit;
                MinRadiusNormal = profileParams.MinConvexRadiusNormal;
            }
            else
            {
                MinRadiusLimit = profileParams.MinConcaveRadiusLimit;
                MinRadiusNormal = profileParams.MinConcaveRadiusNormal;
            }

            double deltaI = AlgebraicGradeDiff;
            string curveKind = IsConvex ? "Cong LỒI" : "Cong LÕM";

            lblSuggestTitle.Text = $"Yêu cầu tiêu chuẩn cho {curveKind} (Vtk = {vtk} km/h, {terrain}):";
            lblSuggestRadius.Text = $"• Bán kính R: Giới hạn ≥ {MinRadiusLimit:F0} m  |  Thông thường ≥ {MinRadiusNormal:F0} m";
            lblSuggestLength.Text = $"• Chiều dài L tối thiểu theo tiêu chuẩn: ≥ {MinCurveLengthStandard:F1} m";

            if (deltaI > 0 && deltaI < ThresholdAlgebraicDiff)
            {
                lblWarningThreshold.Text = $"[!] Hiệu dốc Δi = {deltaI:F2}% < {ThresholdAlgebraicDiff:F1}% (Chưa đạt ngưỡng bắt buộc cắm cong đứng)";
                lblWarningThreshold.Visible = true;
            }
            else
            {
                lblWarningThreshold.Visible = false;
            }

            if (radRadius.Checked) RecalculateFromRadius();
            else RecalculateFromLength();
        }

        private void RadMode_CheckedChanged(object? sender, EventArgs e)
        {
            txtRadius.Enabled = radRadius.Checked;
            txtLength.Enabled = radLength.Checked;

            if (radRadius.Checked) RecalculateFromRadius();
            else RecalculateFromLength();
        }

        private void RecalculateFromRadius()
        {
            if (!radRadius.Checked) return;

            if (double.TryParse(txtRadius.Text, out double R) && R > 0)
            {
                if (AlgebraicGradeDiff > 0)
                {
                    double L = R * (AlgebraicGradeDiff / 100.0);
                    lblCalculatedVal.Text = $"-> Tương ứng chiều dài L = {L:F2} m";
                    ValidateInputs(R, L);
                }
                else
                {
                    lblCalculatedVal.Text = "-> Tương ứng L = --- m (Vui lòng chọn PVI hợp lệ để tính L)";
                    ValidateInputs(R, 0);
                }
            }
            else
            {
                lblCalculatedVal.Text = "-> Tương ứng chiều dài L = --- m";
                lblValidation.Text = "[X] Bán kính R nhập không hợp lệ";
                lblValidation.ForeColor = Color.Red;
            }
        }

        private void RecalculateFromLength()
        {
            if (!radLength.Checked) return;

            if (double.TryParse(txtLength.Text, out double L) && L > 0)
            {
                if (AlgebraicGradeDiff > 0)
                {
                    double R = (100.0 * L) / AlgebraicGradeDiff;
                    lblCalculatedVal.Text = $"-> Tương ứng bán kính R = {R:F1} m";
                    ValidateInputs(R, L);
                }
                else
                {
                    lblCalculatedVal.Text = "-> Tương ứng R = --- m (Vui lòng chọn PVI hợp lệ để tính R)";
                    ValidateInputs(0, L);
                }
            }
            else
            {
                lblCalculatedVal.Text = "-> Tương ứng bán kính R = --- m";
                lblValidation.Text = "[X] Chiều dài L nhập không hợp lệ";
                lblValidation.ForeColor = Color.Red;
            }
        }

        private void ValidateInputs(double R, double L)
        {
            if (PviIndex == 0 || (TotalPviCount > 0 && PviIndex == TotalPviCount - 1))
            {
                lblValidation.Text = "[!] PVI đầu/cuối tuyến không thể cắm cong đứng";
                lblValidation.ForeColor = Color.Red;
                return;
            }

            double reqMinLength = MinCurveLengthStandard;

            if (radRadius.Checked)
            {
                if (L > 0 && reqMinLength > 0 && L < reqMinLength)
                {
                    double reqMinRForL = (AlgebraicGradeDiff > 0) ? (100.0 * reqMinLength) / AlgebraicGradeDiff : 0;
                    lblValidation.Text = $"[X] R = {R:F0}m tính ra L = {L:F2}m < Lmin ({reqMinLength:F1}m)! Hãy chọn 'Theo Chiều dài L' (>= {reqMinLength:F1}m) hoặc tăng R >= {reqMinRForL:F0}m.";
                    lblValidation.ForeColor = Color.Red;
                    return;
                }

                if (MinRadiusNormal > 0 && R >= MinRadiusNormal)
                {
                    lblValidation.Text = $"[OK] Đạt tiêu chuẩn THÔNG THƯỜNG (R = {R:F0}m >= {MinRadiusNormal:F0}m, L = {L:F2}m >= {reqMinLength:F1}m)";
                    lblValidation.ForeColor = Color.DarkGreen;
                }
                else if (MinRadiusLimit > 0 && R >= MinRadiusLimit)
                {
                    lblValidation.Text = $"[!] Đạt tiêu chuẩn GIỚI HẠN (R = {R:F0}m >= {MinRadiusLimit:F0}m, L = {L:F2}m >= {reqMinLength:F1}m)";
                    lblValidation.ForeColor = Color.DarkOrange;
                }
                else if (MinRadiusLimit > 0)
                {
                    lblValidation.Text = $"[X] KHÔNG ĐẠT tiêu chuẩn tối thiểu (R = {R:F0}m < Giới hạn {MinRadiusLimit:F0}m)";
                    lblValidation.ForeColor = Color.Red;
                }
                else
                {
                    lblValidation.Text = $"[OK] Bán kính R = {R:F0}m, L = {L:F2}m";
                    lblValidation.ForeColor = Color.DarkGreen;
                }
            }
            else if (radLength.Checked)
            {
                if (reqMinLength > 0 && L < reqMinLength)
                {
                    lblValidation.Text = $"[X] KHÔNG ĐẠT chiều dài tối thiểu (L = {L:F1}m < {reqMinLength:F1}m)";
                    lblValidation.ForeColor = Color.Red;
                }
                else
                {
                    lblValidation.Text = $"[OK] Đạt chiều dài tối thiểu (L = {L:F1}m >= {reqMinLength:F1}m, R tương ứng ≈ {R:F0}m)";
                    lblValidation.ForeColor = Color.DarkGreen;
                }
            }
        }

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            if (!ValidateBeforeApply()) return;
            OnApplyClicked?.Invoke(this, EventArgs.Empty);

            lblValidation.Text = $"[OK] ĐÃ ÁP DỤNG THÀNH CÔNG cong đứng R = {Radius:F0}m (L = {CurveLength:F2}m) cho PVI #{PviIndex}!";
            lblValidation.ForeColor = Color.DarkGreen;
        }

        private void BtnApplyAndClose_Click(object? sender, EventArgs e)
        {
            if (!ValidateBeforeApply()) return;
            FormAccepted = true;
            PickNextAfterApply = false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnApplyAll_Click(object? sender, EventArgs e)
        {
            if (ProfileId.IsNull || !ProfileId.IsValid)
            {
                MessageBox.Show("Vui lòng chọn Profile đường đỏ trước khi áp dụng hàng loạt!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int internalPviCount = Math.Max(0, TotalPviCount - 2);
            DialogResult answer = MessageBox.Show(
                $"Tự động bố trí cong đứng cho {internalPviCount} PVI giữa tuyến?\n\n" +
                "Mỗi PVI sẽ dùng bán kính thông thường theo loại cong lồi/lõm. " +
                "Nếu chiều dài tính theo bán kính nhỏ hơn L tối thiểu thì chương trình sẽ dùng L tối thiểu.",
                "Xác nhận áp dụng tất cả PVI",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer == DialogResult.Yes)
            {
                OnApplyAllClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        public ProfileDesignParameters GetCurrentProfileDesignParameters()
        {
            var standards = StandardFactory.GetAllStandards();
            int standardIndex = cmbStandard.SelectedIndex;
            if (standardIndex < 0 || standardIndex >= standards.Count) standardIndex = 0;

            int designSpeed = int.TryParse(cmbVtk.SelectedItem?.ToString(), out int speed) ? speed : 60;
            string terrain = cmbTerrain.SelectedItem?.ToString() ?? "Đồng bằng";
            return standards[standardIndex].GetProfileParameters(designSpeed, terrain);
        }

        public void SetApplyAllResult(int successCount, int failureCount, int skippedCount, int minLengthCount)
        {
            lblValidation.Text =
                $"Hàng loạt: thành công {successCount}, lỗi {failureCount}, bỏ qua {skippedCount}; " +
                $"{minLengthCount} PVI dùng L tối thiểu.";
            lblValidation.ForeColor = failureCount == 0 ? Color.DarkGreen : Color.DarkOrange;
        }
        private bool ValidateBeforeApply()
        {
            if (PviIndex < 0 || ProfileId.IsNull)
            {
                MessageBox.Show("Vui lòng chọn đỉnh PVI trước khi thực hiện!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (PviIndex == 0 || (TotalPviCount > 0 && PviIndex == TotalPviCount - 1))
            {
                MessageBox.Show("Đỉnh PVI đầu hoặc cuối tuyến không thể bố trí đường cong đứng!\nVui lòng chọn các đỉnh PVI ở giữa (từ PVI #1 trở đi).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            double reqMinLength = MinCurveLengthStandard;

            if (radRadius.Checked)
            {
                if (!double.TryParse(txtRadius.Text, out double r) || r <= 0)
                {
                    MessageBox.Show("Bán kính R không hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                double l = CurveLength;
                if (l > 0 && reqMinLength > 0 && l < reqMinLength)
                {
                    double reqMinR = (AlgebraicGradeDiff > 0) ? (100.0 * reqMinLength) / AlgebraicGradeDiff : 0;
                    DialogResult ask = MessageBox.Show(
                        $"Bán kính R = {r:F0}m ứng với chiều dài L = {l:F2}m chưa đạt chiều dài tối thiểu Lmin = {reqMinLength:F1}m theo tiêu chuẩn!\n\n" +
                        $"Bạn có muốn tự động chuyển sang chọn 'Theo Chiều dài L' (gán L = {reqMinLength:F1}m, R tương ứng ≈ {reqMinR:F0}m) không?",
                        "Cảnh báo Chiều dài L chưa đạt tiêu chuẩn",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (ask == DialogResult.Yes)
                    {
                        radLength.Checked = true;
                        txtLength.Text = Math.Round(reqMinLength, 1).ToString();
                        RecalculateFromLength();
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!double.TryParse(txtLength.Text, out double l) || l <= 0)
                {
                    MessageBox.Show("Chiều dài L không hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (reqMinLength > 0 && l < reqMinLength)
                {
                    DialogResult ask = MessageBox.Show(
                        $"Chiều dài L = {l:F1}m chưa đạt chiều dài tối thiểu Lmin = {reqMinLength:F1}m theo tiêu chuẩn!\n\n" +
                        $"Bạn có muốn tự động gán chiều dài L = {reqMinLength:F1}m không?",
                        "Cảnh báo Chiều dài L chưa đạt tiêu chuẩn",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (ask == DialogResult.Yes)
                    {
                        txtLength.Text = Math.Round(reqMinLength, 1).ToString();
                        RecalculateFromLength();
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public double Radius
        {
            get
            {
                if (radRadius.Checked && double.TryParse(txtRadius.Text, out double r)) return r;
                if (radLength.Checked && double.TryParse(txtLength.Text, out double l) && AlgebraicGradeDiff > 0)
                    return (100.0 * l) / AlgebraicGradeDiff;
                return 0;
            }
        }

        public double CurveLength
        {
            get
            {
                if (radLength.Checked && double.TryParse(txtLength.Text, out double l)) return l;
                if (radRadius.Checked && double.TryParse(txtRadius.Text, out double r) && AlgebraicGradeDiff > 0)
                    return r * (AlgebraicGradeDiff / 100.0);
                return 0;
            }
        }

        public int StandardIndex
        {
            get => cmbStandard.SelectedIndex;
            set { if (value >= 0 && value < cmbStandard.Items.Count) cmbStandard.SelectedIndex = value; }
        }

        public int VtkIndex
        {
            get => cmbVtk.SelectedIndex;
            set { if (value >= 0 && value < cmbVtk.Items.Count) cmbVtk.SelectedIndex = value; }
        }

        public int TerrainIndex
        {
            get => cmbTerrain.SelectedIndex;
            set { if (value >= 0 && value < cmbTerrain.Items.Count) cmbTerrain.SelectedIndex = value; }
        }

        public Button BtnPickProfileView => btnPickProfileView;
        public Button BtnPickProfile => btnPickProfile;
        public Button BtnPickPvi => btnPickPvi;
        public Button BtnPickPviNext => btnPickPviNext;
        public TextBox TxtProfileViewName => txtProfileViewName;
        public ComboBox CmbProfile => cmbProfile;
        public ComboBox CmbPvi => cmbPvi;
    }
}
