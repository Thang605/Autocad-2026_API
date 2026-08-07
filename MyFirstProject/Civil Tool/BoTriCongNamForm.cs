using System;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;

namespace MyFirstProject.Civil_Tool
{
    public partial class BoTriCongNamForm : Form
    {
        public ObjectId AlignmentId { get; set; } = ObjectId.Null;
        public int Entity1Id { get; set; } = -1;
        public int Entity2Id { get; set; } = -1;
        
        private double _deflectionAngle = 0;
        public double DeflectionAngle
        {
            get => _deflectionAngle;
            set
            {
                _deflectionAngle = value;
                UpdateSuggestions();
            }
        }

        public BoTriCongNamForm()
        {
            InitializeComponent();

            this.cmbStandard.Items.Clear();
            var standards = Civil3DCsharp.StandardFactory.GetAllStandards();
            foreach (var std in standards)
            {
                this.cmbStandard.Items.Add(std.StandardName);
            }

            if (this.cmbStandard.Items.Count > 0)
                this.cmbStandard.SelectedIndex = 0;

            UpdateVtkList();

            this.cmbStandard.SelectedIndexChanged += (s, e) => { UpdateVtkList(); UpdateSuggestions(); };
            this.cmbVtk.SelectedIndexChanged += (s, e) => UpdateSuggestions();

            this.btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.btnApply.Click += BtnApply_Click;
        }

        private void UpdateVtkList()
        {
            var standards = Civil3DCsharp.StandardFactory.GetAllStandards();
            int stdIndex = cmbStandard.SelectedIndex;
            if (stdIndex < 0 || stdIndex >= standards.Count) return;

            var standard = standards[stdIndex];
            string currentVtk = cmbVtk.SelectedItem?.ToString();

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
                // Mặc định chọn tốc độ ở vị trí thứ 1 (hoặc 0)
                cmbVtk.SelectedIndex = Math.Min(1, cmbVtk.Items.Count - 1);
            }
        }

        public void UpdateSuggestions()
        {
            if (cmbVtk.SelectedItem == null) return;
            string vtkStr = cmbVtk.SelectedItem.ToString();
            int vtk = int.TryParse(vtkStr, out int v) ? v : 60;
            
            var standards = Civil3DCsharp.StandardFactory.GetAllStandards();
            int stdIndex = cmbStandard.SelectedIndex;
            if (stdIndex < 0 || stdIndex >= standards.Count) stdIndex = 0;
            var standard = standards[stdIndex];
            
            var p = standard.GetParameters(vtk);
            double rMinBase = p.MinRadiusLimit;
            double rNormal = p.MinRadiusNormal;
            double rNoSuperelevation = p.MinRadiusNoSuperelevation;
            double lsBase = p.MinTransitionCurveLength;

            // Adjust for small deflection angle (alpha < 5 degrees)
            // Khi góc nhỏ -> Bán kính R rất lớn (vài nghìn m) -> Không cần bố trí Ls (Ls = 0)
            double rSuggest = rMinBase;

            if (DeflectionAngle > 0 && DeflectionAngle < 5.0)
            {
                lblAngleVal.Text = $"{DeflectionAngle:F1}° (<5°)";
                lblAngleVal.ForeColor = System.Drawing.Color.Red;

                // Lmin chuẩn từ 100m (40km/h) đến 175m (80-120km/h)
                double lMinCurve = vtk switch {
                    40 => 100,
                    60 => 140,
                    _ => 175
                };

                // R = (180 * Lmin) / (pi * alpha) -> xấp xỉ 10.000 / alpha (với Vtk >= 80km/h)
                double rAngleMin = (180.0 * lMinCurve) / (Math.PI * DeflectionAngle);
                rSuggest = Math.Max(rMinBase, Math.Ceiling(rAngleMin / 50.0) * 50.0);
                
                // Vì R rất lớn (vài nghìn m > R_không_siêu_cao) -> Không cần bố trí Ls
                lsBase = 0;
                string lsText = "Ls = 0m (R lớn, không Ls)";

                lblSuggest.Text = $"💡 TC ({vtk}km/h, α={DeflectionAngle:F1}°<5°): Rmin={rSuggest}m | Rtt={rNormal}m | Rksc={rNoSuperelevation}m | {lsText}";
            }
            else
            {
                lblAngleVal.Text = DeflectionAngle > 0 ? $"{DeflectionAngle:F1}°" : "--°";
                lblAngleVal.ForeColor = System.Drawing.Color.DarkGreen;
                string lsText = lsBase > 0 ? $"Ls = {lsBase}m" : "Ls = 0m (Không Ls)";
                lblSuggest.Text = $"💡 TC ({vtk}km/h): Rmin = {rMinBase}m | Rtt = {rNormal}m | Rksc = {rNoSuperelevation}m | {lsText}";
            }

            txtR.Text = rSuggest.ToString();
            txtLs.Text = lsBase.ToString();
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (AlignmentId.IsNull)
            {
                MessageBox.Show("Vui lòng chọn Alignment.");
                return;
            }
            if (Entity1Id == -1 || Entity2Id == -1)
            {
                MessageBox.Show("Vui lòng chọn 2 cánh tuyến.");
                return;
            }

            if (!double.TryParse(txtR.Text, out double r) || r <= 0)
            {
                MessageBox.Show("Bán kính R không hợp lệ.");
                return;
            }

            if (!double.TryParse(txtLs.Text, out double ls) || ls < 0)
            {
                MessageBox.Show("Chiều dài Ls không hợp lệ.");
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // Public properties to access inputs
        public double Radius => double.Parse(txtR.Text);
        public double SpiralLength => double.Parse(txtLs.Text);
        
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

        public Button BtnPickAlignment => btnPickAlignment;
        public Button BtnPickEnt1 => btnPickEnt1;
        public Button BtnPickEnt2 => btnPickEnt2;
        public TextBox TxtAlignName => txtAlignName;
        public Label LblEnt1 => lblEnt1;
        public Label LblEnt2 => lblEnt2;
    }
}
