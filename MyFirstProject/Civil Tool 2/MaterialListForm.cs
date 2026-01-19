using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Form đơn giản chọn các Surface và Shape Style để tạo Material List cho SampleLineGroup
    /// Chỉ nhận data đã được load sẵn, không mở transaction
    /// </summary>
    public class MaterialListFormSimple : Form
    {
        // Static variables to remember last selections
        private static string _lastCutMaterialName = "Đào đất";
        private static string _lastFillMaterialName = "Đắp đất";
        private static string _lastCutShapeStyleName = "";
        private static string _lastFillShapeStyleName = "";

        // Properties to return data
        public string MaterialListName { get; private set; } = "";
        public ObjectId EgSurfaceId { get; private set; } = ObjectId.Null;
        public ObjectId DatumSurfaceId { get; private set; } = ObjectId.Null;
        public string CutMaterialName { get; private set; } = "Đào đất";
        public string FillMaterialName { get; private set; } = "Đắp đất";
        public ObjectId CutShapeStyleId { get; private set; } = ObjectId.Null;
        public ObjectId FillShapeStyleId { get; private set; } = ObjectId.Null;
        public bool FormAccepted { get; private set; } = false;

        // UI Controls
        private WinFormsLabel lblTitle = null!;
        private WinFormsLabel lblMaterialListName = null!;
        private WinFormsLabel lblSampleLineGroup = null!;
        private WinFormsLabel lblEgSurface = null!;
        private WinFormsLabel lblDatumSurface = null!;
        private WinFormsLabel lblCutMaterial = null!;
        private WinFormsLabel lblFillMaterial = null!;
        private WinFormsLabel lblCutShapeStyle = null!;
        private WinFormsLabel lblFillShapeStyle = null!;

        private TextBox txtMaterialListName = null!;
        private TextBox txtSampleLineGroup = null!;
        private TextBox txtCutMaterial = null!;
        private TextBox txtFillMaterial = null!;
        private ComboBox cmbEgSurface = null!;
        private ComboBox cmbDatumSurface = null!;
        private ComboBox cmbCutShapeStyle = null!;
        private ComboBox cmbFillShapeStyle = null!;

        private Button btnCreate = null!;
        private Button btnCancel = null!;
        private GroupBox grpSurfaces = null!;
        private GroupBox grpMaterialNames = null!;
        private GroupBox grpShapeStyles = null!;

        // Data
        private string _sampleLineGroupName;
        private List<KeyValuePair<string, ObjectId>> _surfaceList;
        private List<KeyValuePair<string, ObjectId>> _shapeStyleList;

        public MaterialListFormSimple(string sampleLineGroupName, 
                                       List<KeyValuePair<string, ObjectId>> surfaceList,
                                       List<KeyValuePair<string, ObjectId>> shapeStyleList)
        {
            _sampleLineGroupName = sampleLineGroupName;
            _surfaceList = surfaceList;
            _shapeStyleList = shapeStyleList;
            InitializeComponent();
            LoadSurfaces();
            LoadShapeStyles();
        }

        private void InitializeComponent()
        {
            // Standard Font
            var standardFont = new WinFormsFont("Segoe UI", 10F, FontStyle.Regular);
            var boldFont = new WinFormsFont("Segoe UI", 10F, FontStyle.Bold);
            var titleFont = new WinFormsFont("Segoe UI", 14F, FontStyle.Bold);

            // Initialize controls
            this.lblTitle = new WinFormsLabel();
            this.lblMaterialListName = new WinFormsLabel();
            this.lblSampleLineGroup = new WinFormsLabel();
            this.lblEgSurface = new WinFormsLabel();
            this.lblDatumSurface = new WinFormsLabel();
            this.lblCutMaterial = new WinFormsLabel();
            this.lblFillMaterial = new WinFormsLabel();
            this.lblCutShapeStyle = new WinFormsLabel();
            this.lblFillShapeStyle = new WinFormsLabel();

            this.txtMaterialListName = new TextBox();
            this.txtSampleLineGroup = new TextBox();
            this.txtCutMaterial = new TextBox();
            this.txtFillMaterial = new TextBox();
            this.cmbEgSurface = new ComboBox();
            this.cmbDatumSurface = new ComboBox();
            this.cmbCutShapeStyle = new ComboBox();
            this.cmbFillShapeStyle = new ComboBox();

            this.btnCreate = new Button();
            this.btnCancel = new Button();
            this.grpSurfaces = new GroupBox();
            this.grpMaterialNames = new GroupBox();
            this.grpShapeStyles = new GroupBox();

            this.SuspendLayout();

            // Form
            this.Text = "Tạo Material List - Đào Đắp";
            this.Size = new Size(520, 530);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = standardFont;

            // Title Label
            this.lblTitle.Text = "TẠO MATERIAL LIST - ĐÀO ĐẮP";
            this.lblTitle.Font = titleFont;
            this.lblTitle.Location = new WinFormsPoint(20, 15);
            this.lblTitle.Size = new Size(460, 30);
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.ForeColor = Color.FromArgb(0, 102, 204);

            // SampleLineGroup Info
            this.lblSampleLineGroup.Text = "Sample Line Group:";
            this.lblSampleLineGroup.Font = boldFont;
            this.lblSampleLineGroup.Location = new WinFormsPoint(20, 55);
            this.lblSampleLineGroup.Size = new Size(150, 25);

            this.txtSampleLineGroup.Location = new WinFormsPoint(175, 53);
            this.txtSampleLineGroup.Size = new Size(310, 25);
            this.txtSampleLineGroup.ReadOnly = true;
            this.txtSampleLineGroup.BackColor = Color.WhiteSmoke;
            this.txtSampleLineGroup.Text = _sampleLineGroupName;
            this.txtSampleLineGroup.Font = standardFont;

            // Material List Name
            this.lblMaterialListName.Text = "Tên Material List:";
            this.lblMaterialListName.Font = boldFont;
            this.lblMaterialListName.Location = new WinFormsPoint(20, 85);
            this.lblMaterialListName.Size = new Size(150, 25);

            this.txtMaterialListName.Location = new WinFormsPoint(175, 83);
            this.txtMaterialListName.Size = new Size(310, 25);
            this.txtMaterialListName.Text = _sampleLineGroupName;
            this.txtMaterialListName.Font = standardFont;

            // Surfaces Group
            this.grpSurfaces.Text = "Chọn Surface";
            this.grpSurfaces.Font = boldFont;
            this.grpSurfaces.Location = new WinFormsPoint(20, 120);
            this.grpSurfaces.Size = new Size(465, 100);
            this.grpSurfaces.ForeColor = Color.Black;

            // EG Surface Label
            this.lblEgSurface.Text = "EG Surface (Tự nhiên):";
            this.lblEgSurface.Font = standardFont;
            this.lblEgSurface.Location = new WinFormsPoint(15, 30);
            this.lblEgSurface.Size = new Size(160, 25);

            // EG Surface ComboBox
            this.cmbEgSurface.Location = new WinFormsPoint(180, 28);
            this.cmbEgSurface.Size = new Size(265, 25);
            this.cmbEgSurface.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbEgSurface.Font = standardFont;

            // Datum Surface Label
            this.lblDatumSurface.Text = "Datum Surface (Thiết kế):";
            this.lblDatumSurface.Font = standardFont;
            this.lblDatumSurface.Location = new WinFormsPoint(15, 65);
            this.lblDatumSurface.Size = new Size(160, 25);

            // Datum Surface ComboBox
            this.cmbDatumSurface.Location = new WinFormsPoint(180, 63);
            this.cmbDatumSurface.Size = new Size(265, 25);
            this.cmbDatumSurface.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbDatumSurface.Font = standardFont;

            // Add controls to Surfaces group
            this.grpSurfaces.Controls.AddRange(new Control[] {
                lblEgSurface, cmbEgSurface,
                lblDatumSurface, cmbDatumSurface
            });

            // Material Names Group
            this.grpMaterialNames.Text = "Tên vật liệu";
            this.grpMaterialNames.Font = boldFont;
            this.grpMaterialNames.Location = new WinFormsPoint(20, 230);
            this.grpMaterialNames.Size = new Size(465, 100);
            this.grpMaterialNames.ForeColor = Color.Black;

            // Cut Material Label
            this.lblCutMaterial.Text = "Tên vật liệu Đào (Cut):";
            this.lblCutMaterial.Font = standardFont;
            this.lblCutMaterial.Location = new WinFormsPoint(15, 30);
            this.lblCutMaterial.Size = new Size(160, 25);

            // Cut Material TextBox
            this.txtCutMaterial.Location = new WinFormsPoint(180, 28);
            this.txtCutMaterial.Size = new Size(265, 25);
            this.txtCutMaterial.Text = _lastCutMaterialName;
            this.txtCutMaterial.Font = standardFont;

            // Fill Material Label
            this.lblFillMaterial.Text = "Tên vật liệu Đắp (Fill):";
            this.lblFillMaterial.Font = standardFont;
            this.lblFillMaterial.Location = new WinFormsPoint(15, 65);
            this.lblFillMaterial.Size = new Size(160, 25);

            // Fill Material TextBox
            this.txtFillMaterial.Location = new WinFormsPoint(180, 63);
            this.txtFillMaterial.Size = new Size(265, 25);
            this.txtFillMaterial.Text = _lastFillMaterialName;
            this.txtFillMaterial.Font = standardFont;

            // Add controls to Material Names group
            this.grpMaterialNames.Controls.AddRange(new Control[] {
                lblCutMaterial, txtCutMaterial,
                lblFillMaterial, txtFillMaterial
            });

            // Shape Styles Group
            this.grpShapeStyles.Text = "Shape Style";
            this.grpShapeStyles.Font = boldFont;
            this.grpShapeStyles.Location = new WinFormsPoint(20, 340);
            this.grpShapeStyles.Size = new Size(465, 100);
            this.grpShapeStyles.ForeColor = Color.Black;

            // Cut Shape Style Label
            this.lblCutShapeStyle.Text = "Cut Shape Style:";
            this.lblCutShapeStyle.Font = standardFont;
            this.lblCutShapeStyle.Location = new WinFormsPoint(15, 30);
            this.lblCutShapeStyle.Size = new Size(160, 25);

            // Cut Shape Style ComboBox
            this.cmbCutShapeStyle.Location = new WinFormsPoint(180, 28);
            this.cmbCutShapeStyle.Size = new Size(265, 25);
            this.cmbCutShapeStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbCutShapeStyle.Font = standardFont;

            // Fill Shape Style Label
            this.lblFillShapeStyle.Text = "Fill Shape Style:";
            this.lblFillShapeStyle.Font = standardFont;
            this.lblFillShapeStyle.Location = new WinFormsPoint(15, 65);
            this.lblFillShapeStyle.Size = new Size(160, 25);

            // Fill Shape Style ComboBox
            this.cmbFillShapeStyle.Location = new WinFormsPoint(180, 63);
            this.cmbFillShapeStyle.Size = new Size(265, 25);
            this.cmbFillShapeStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbFillShapeStyle.Font = standardFont;

            // Add controls to Shape Styles group
            this.grpShapeStyles.Controls.AddRange(new Control[] {
                lblCutShapeStyle, cmbCutShapeStyle,
                lblFillShapeStyle, cmbFillShapeStyle
            });

            // Create Button
            this.btnCreate.Text = "Tạo Material List";
            this.btnCreate.Location = new WinFormsPoint(270, 455);
            this.btnCreate.Size = new Size(120, 35);
            this.btnCreate.Font = boldFont;
            this.btnCreate.BackColor = Color.FromArgb(0, 122, 204);
            this.btnCreate.ForeColor = Color.White;
            this.btnCreate.FlatStyle = FlatStyle.Flat;
            this.btnCreate.Click += BtnCreate_Click;

            // Cancel Button
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new WinFormsPoint(400, 455);
            this.btnCancel.Size = new Size(85, 35);
            this.btnCancel.Font = standardFont;
            this.btnCancel.Click += BtnCancel_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                lblTitle,
                lblSampleLineGroup,
                txtSampleLineGroup,
                lblMaterialListName,
                txtMaterialListName,
                grpSurfaces,
                grpMaterialNames,
                grpShapeStyles,
                btnCreate,
                btnCancel
            });

            this.ResumeLayout(false);
        }

        private void LoadSurfaces()
        {
            cmbEgSurface.Items.Clear();
            cmbDatumSurface.Items.Clear();

            foreach (var kvp in _surfaceList)
            {
                cmbEgSurface.Items.Add(kvp.Key);
                cmbDatumSurface.Items.Add(kvp.Key);
            }

            // Auto-select EG based on name patterns (eg, tn)
            for (int i = 0; i < cmbEgSurface.Items.Count; i++)
            {
                string name = cmbEgSurface.Items[i]?.ToString()?.ToLower() ?? "";
                if (name.Contains("eg") || name == "tn")
                {
                    cmbEgSurface.SelectedIndex = i;
                    break;
                }
            }

            // Auto-select Datum based on SampleLineGroup name
            string groupNameLower = _sampleLineGroupName.ToLower().Trim();
            int bestDatumIndex = -1;
            int bestScore = 0;

            for (int i = 0; i < cmbDatumSurface.Items.Count; i++)
            {
                string name = cmbDatumSurface.Items[i]?.ToString() ?? "";
                string nameLower = name.ToLower();
                int score = 0;

                // Kiểm tra xem surface có chứa tên SampleLineGroup không
                if (nameLower.Contains(groupNameLower) || 
                    nameLower.Contains(groupNameLower.Replace("-", "_")) ||
                    nameLower.Contains(groupNameLower.Replace("_", "-")))
                {
                    score += 100;
                }

                if (nameLower.Contains("corridor"))
                    score += 20;
                if (nameLower.Contains("top"))
                    score += 15;
                if (nameLower.Contains("datum"))
                    score += 10;

                if (nameLower.Contains("eg") || nameLower == "tn")
                    score -= 50;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDatumIndex = i;
                }
            }

            if (bestDatumIndex >= 0)
            {
                cmbDatumSurface.SelectedIndex = bestDatumIndex;
            }

            // Fallback selection
            if (cmbEgSurface.SelectedIndex < 0 && cmbEgSurface.Items.Count > 0)
                cmbEgSurface.SelectedIndex = 0;
            if (cmbDatumSurface.SelectedIndex < 0 && cmbDatumSurface.Items.Count > 1)
                cmbDatumSurface.SelectedIndex = 1;
            else if (cmbDatumSurface.SelectedIndex < 0 && cmbDatumSurface.Items.Count > 0)
                cmbDatumSurface.SelectedIndex = 0;
        }

        private void LoadShapeStyles()
        {
            cmbCutShapeStyle.Items.Clear();
            cmbFillShapeStyle.Items.Clear();

            // Thêm option "(Không chọn)"
            cmbCutShapeStyle.Items.Add("(Không chọn)");
            cmbFillShapeStyle.Items.Add("(Không chọn)");

            foreach (var kvp in _shapeStyleList)
            {
                cmbCutShapeStyle.Items.Add(kvp.Key);
                cmbFillShapeStyle.Items.Add(kvp.Key);
            }

            // Auto-select based on name patterns or last selection
            int cutIndex = 0;
            int fillIndex = 0;

            for (int i = 1; i < cmbCutShapeStyle.Items.Count; i++)
            {
                string name = cmbCutShapeStyle.Items[i]?.ToString() ?? "";
                
                // Kiểm tra last selection
                if (!string.IsNullOrEmpty(_lastCutShapeStyleName) && name == _lastCutShapeStyleName)
                {
                    cutIndex = i;
                    break;
                }
                
                // Auto-select nếu chứa "Cut" và "HATCH"
                if (name.Contains("Cut") && name.ToUpper().Contains("HATCH"))
                {
                    cutIndex = i;
                }
            }

            for (int i = 1; i < cmbFillShapeStyle.Items.Count; i++)
            {
                string name = cmbFillShapeStyle.Items[i]?.ToString() ?? "";
                
                // Kiểm tra last selection
                if (!string.IsNullOrEmpty(_lastFillShapeStyleName) && name == _lastFillShapeStyleName)
                {
                    fillIndex = i;
                    break;
                }
                
                // Auto-select nếu chứa "Fill" và "HATCH"
                if (name.Contains("Fill") && name.ToUpper().Contains("HATCH"))
                {
                    fillIndex = i;
                }
            }

            cmbCutShapeStyle.SelectedIndex = cutIndex;
            cmbFillShapeStyle.SelectedIndex = fillIndex;
        }

        private void BtnCreate_Click(object? sender, EventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtMaterialListName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên Material List!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMaterialListName.Focus();
                return;
            }

            if (cmbEgSurface.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn EG Surface!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbDatumSurface.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn Datum Surface!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbEgSurface.SelectedItem.ToString() == cmbDatumSurface.SelectedItem.ToString())
            {
                MessageBox.Show("EG Surface và Datum Surface không được trùng nhau!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string egName = cmbEgSurface.SelectedItem.ToString() ?? "";
            string datumName = cmbDatumSurface.SelectedItem.ToString() ?? "";

            // Find ObjectIds from surface list
            ObjectId egId = ObjectId.Null;
            ObjectId datumId = ObjectId.Null;

            foreach (var kvp in _surfaceList)
            {
                if (kvp.Key == egName)
                    egId = kvp.Value;
                if (kvp.Key == datumName)
                    datumId = kvp.Value;
            }

            // Get Shape Style IDs
            ObjectId cutStyleId = ObjectId.Null;
            ObjectId fillStyleId = ObjectId.Null;

            if (cmbCutShapeStyle.SelectedIndex > 0) // Skip "(Không chọn)"
            {
                string cutStyleName = cmbCutShapeStyle.SelectedItem?.ToString() ?? "";
                foreach (var kvp in _shapeStyleList)
                {
                    if (kvp.Key == cutStyleName)
                    {
                        cutStyleId = kvp.Value;
                        break;
                    }
                }
            }

            if (cmbFillShapeStyle.SelectedIndex > 0) // Skip "(Không chọn)"
            {
                string fillStyleName = cmbFillShapeStyle.SelectedItem?.ToString() ?? "";
                foreach (var kvp in _shapeStyleList)
                {
                    if (kvp.Key == fillStyleName)
                    {
                        fillStyleId = kvp.Value;
                        break;
                    }
                }
            }

            if (egId != ObjectId.Null && datumId != ObjectId.Null)
            {
                MaterialListName = txtMaterialListName.Text.Trim();
                EgSurfaceId = egId;
                DatumSurfaceId = datumId;
                CutMaterialName = txtCutMaterial.Text.Trim();
                FillMaterialName = txtFillMaterial.Text.Trim();
                CutShapeStyleId = cutStyleId;
                FillShapeStyleId = fillStyleId;

                // Save for next time
                _lastCutMaterialName = CutMaterialName;
                _lastFillMaterialName = FillMaterialName;
                _lastCutShapeStyleName = cmbCutShapeStyle.SelectedIndex > 0 ? cmbCutShapeStyle.SelectedItem?.ToString() ?? "" : "";
                _lastFillShapeStyleName = cmbFillShapeStyle.SelectedIndex > 0 ? cmbFillShapeStyle.SelectedItem?.ToString() ?? "" : "";

                FormAccepted = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Không tìm thấy Surface đã chọn!", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            FormAccepted = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
