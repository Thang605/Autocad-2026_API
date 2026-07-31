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

            this.cmbStandard.SelectedIndex = 0; // TCVN 4054:2005
            this.cmbVtk.SelectedIndex = 1; // 60 km/h by default

            this.cmbStandard.SelectedIndexChanged += (s, e) => UpdateSuggestions();
            this.cmbVtk.SelectedIndexChanged += (s, e) => UpdateSuggestions();

            this.btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.btnApply.Click += BtnApply_Click;
        }

        public void UpdateSuggestions()
        {
            if (cmbVtk.SelectedItem == null) return;
            string vtk = cmbVtk.SelectedItem.ToString() ?? "60";
            
            double rMinBase = 125;
            double lsBase = 50;

            switch (vtk)
            {
                case "40":
                    rMinBase = 60;
                    lsBase = 0; // Khi Vtk < 60 km/h: Không bố trí đoạn chuyển tiếp Ls
                    break;
                case "60":
                    rMinBase = 125;
                    lsBase = 50;
                    break;
                case "80":
                    rMinBase = 250;
                    lsBase = 70;
                    break;
                case "100":
                    rMinBase = 400;
                    lsBase = 85;
                    break;
                case "120":
                    rMinBase = 600;
                    lsBase = 100;
                    break;
            }

            // Adjust for small deflection angle (alpha < 5 degrees)
            // Khi góc nhỏ -> Bán kính R rất lớn (vài nghìn m) -> Không cần bố trí Ls (Ls = 0)
            double rSuggest = rMinBase;

            if (DeflectionAngle > 0 && DeflectionAngle < 5.0)
            {
                lblAngleVal.Text = $"{DeflectionAngle:F1}° (<5°)";
                lblAngleVal.ForeColor = System.Drawing.Color.Red;

                // Lmin chuẩn từ 100m (40km/h) đến 175m (80-120km/h)
                double lMinCurve = vtk switch {
                    "40" => 100,
                    "60" => 140,
                    _ => 175
                };

                // R = (180 * Lmin) / (pi * alpha) -> xấp xỉ 10.000 / alpha (với Vtk >= 80km/h)
                double rAngleMin = (180.0 * lMinCurve) / (Math.PI * DeflectionAngle);
                rSuggest = Math.Max(rMinBase, Math.Ceiling(rAngleMin / 50.0) * 50.0);
                
                // Vì R rất lớn (vài nghìn m > R_không_siêu_cao) -> Không cần bố trí Ls
                lsBase = 0;
                string lsText = "Ls = 0m (R lớn, không Ls)";

                lblSuggest.Text = $"💡 TC ({vtk}km/h, α={DeflectionAngle:F1}°<5°): Rmin = {rSuggest}m | {lsText}";
            }
            else
            {
                lblAngleVal.Text = DeflectionAngle > 0 ? $"{DeflectionAngle:F1}°" : "--°";
                lblAngleVal.ForeColor = System.Drawing.Color.DarkGreen;
                string lsText = lsBase > 0 ? $"Ls = {lsBase}m" : "Ls = 0m (Không Ls)";
                lblSuggest.Text = $"💡 TC ({vtk}km/h): Rmin = {rMinBase}m | {lsText}";
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
