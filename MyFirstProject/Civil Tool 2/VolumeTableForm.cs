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
    /// Form cấu hình và thêm Volume Tables (Bảng Khối Lượng) vào Section View Group
    /// </summary>
    public class VolumeTableForm : Form
    {
        // Static variables to remember last selections
        private static string _lastTableType = "Material";
        private static string _lastTableStyle = "";
        private static string _lastSectionViewAnchor = "Top Left";
        private static string _lastTableAnchor = "Top Left";
        private static string _lastTableLayout = "Horizontal";
        private static double _lastXOffset = 0.0;
        private static double _lastYOffset = 0.0;

        // Properties to return data
        public bool FormAccepted { get; private set; } = false;
        public List<VolumeTableConfig> VolumeTables { get; private set; } = new List<VolumeTableConfig>();
        public string SectionViewAnchor => cmbSectionViewAnchor.SelectedItem?.ToString() ?? "Top Left";
        public string TableAnchor => cmbTableAnchor.SelectedItem?.ToString() ?? "Top Left";
        public string TableLayout => cmbTableLayout.SelectedItem?.ToString() ?? "Horizontal";
        public double XOffset => (double)nudXOffset.Value;
        public double YOffset => (double)nudYOffset.Value;

        // UI Controls - Top section
        private WinFormsLabel lblWarning = null!;
        private WinFormsLabel lblType = null!;
        private WinFormsLabel lblTableStyle = null!;
        private ComboBox cmbType = null!;
        private ComboBox cmbTableStyle = null!;
        private Button btnAdd = null!;

        // UI Controls - Volume Tables List
        private GroupBox grpVolumeTables = null!;
        private DataGridView dgvVolumeTables = null!;
        private Button btnMoveUp = null!;
        private Button btnMoveDown = null!;
        private Button btnDelete = null!;

        // UI Controls - Position Settings
        private GroupBox grpPosition = null!;
        private WinFormsLabel lblSectionViewAnchor = null!;
        private WinFormsLabel lblTableAnchorLabel = null!;
        private WinFormsLabel lblTableLayoutLabel = null!;
        private WinFormsLabel lblXOffset = null!;
        private WinFormsLabel lblYOffset = null!;
        private ComboBox cmbSectionViewAnchor = null!;
        private ComboBox cmbTableAnchor = null!;
        private ComboBox cmbTableLayout = null!;
        private NumericUpDown nudXOffset = null!;
        private NumericUpDown nudYOffset = null!;

        // UI Controls - Buttons
        private Button btnOK = null!;
        private Button btnCancel = null!;
        private Button btnApply = null!;
        private Button btnHelp = null!;

        // Data
        private string _sectionViewGroupName;
        private List<KeyValuePair<string, ObjectId>> _materialListList;
        private List<KeyValuePair<string, ObjectId>> _tableStyleList;

        public VolumeTableForm(string sectionViewGroupName,
                               List<KeyValuePair<string, ObjectId>> materialListList,
                               List<KeyValuePair<string, ObjectId>> tableStyleList)
        {
            _sectionViewGroupName = sectionViewGroupName;
            _materialListList = materialListList;
            _tableStyleList = tableStyleList;
            InitializeComponent();
            LoadData();
            RestoreLastUsedValues();
        }

        private void InitializeComponent()
        {
            // Standard Fonts
            var standardFont = new WinFormsFont("Segoe UI", 9F, FontStyle.Regular);
            var boldFont = new WinFormsFont("Segoe UI", 9F, FontStyle.Bold);

            // Initialize controls
            this.lblWarning = new WinFormsLabel();
            this.lblType = new WinFormsLabel();
            this.lblTableStyle = new WinFormsLabel();
            this.cmbType = new ComboBox();
            this.cmbTableStyle = new ComboBox();
            this.btnAdd = new Button();

            this.grpVolumeTables = new GroupBox();
            this.dgvVolumeTables = new DataGridView();
            this.btnMoveUp = new Button();
            this.btnMoveDown = new Button();
            this.btnDelete = new Button();

            this.grpPosition = new GroupBox();
            this.lblSectionViewAnchor = new WinFormsLabel();
            this.lblTableAnchorLabel = new WinFormsLabel();
            this.lblTableLayoutLabel = new WinFormsLabel();
            this.lblXOffset = new WinFormsLabel();
            this.lblYOffset = new WinFormsLabel();
            this.cmbSectionViewAnchor = new ComboBox();
            this.cmbTableAnchor = new ComboBox();
            this.cmbTableLayout = new ComboBox();
            this.nudXOffset = new NumericUpDown();
            this.nudYOffset = new NumericUpDown();

            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.btnApply = new Button();
            this.btnHelp = new Button();

            this.SuspendLayout();

            // Form settings
            this.Text = $"Change Volume Tables - {_sectionViewGroupName}";
            this.Size = new Size(900, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = standardFont;

            // Warning Label
            this.lblWarning.Text = "⚠ The section view(s) include volume tables. Please select volume table type(s) to draw.";
            this.lblWarning.Font = standardFont;
            this.lblWarning.Location = new WinFormsPoint(20, 15);
            this.lblWarning.Size = new Size(650, 20);
            this.lblWarning.ForeColor = Color.DarkOrange;

            // Type Label and ComboBox
            this.lblType.Text = "Type:";
            this.lblType.Font = standardFont;
            this.lblType.Location = new WinFormsPoint(20, 45);
            this.lblType.Size = new Size(50, 20);

            this.cmbType.Location = new WinFormsPoint(70, 42);
            this.cmbType.Size = new Size(280, 25);
            this.cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbType.Font = standardFont;
            this.cmbType.Items.AddRange(new object[] { "Material", "Cut and Fill", "Earthwork" });
            this.cmbType.SelectedIndex = 0;

            // Table Style Label and ComboBox
            this.lblTableStyle.Text = "Select table style:";
            this.lblTableStyle.Font = standardFont;
            this.lblTableStyle.Location = new WinFormsPoint(380, 45);
            this.lblTableStyle.Size = new Size(100, 20);

            this.cmbTableStyle.Location = new WinFormsPoint(485, 42);
            this.cmbTableStyle.Size = new Size(280, 25);
            this.cmbTableStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbTableStyle.Font = standardFont;

            // Add Button
            this.btnAdd.Text = "Add>>";
            this.btnAdd.Font = standardFont;
            this.btnAdd.Location = new WinFormsPoint(785, 40);
            this.btnAdd.Size = new Size(75, 28);
            this.btnAdd.Click += BtnAdd_Click;

            // ===== Volume Tables GroupBox =====
            this.grpVolumeTables.Text = "List of volume tables";
            this.grpVolumeTables.Font = boldFont;
            this.grpVolumeTables.Location = new WinFormsPoint(20, 75);
            this.grpVolumeTables.Size = new Size(840, 180);

            // DataGridView
            this.dgvVolumeTables.Location = new WinFormsPoint(10, 22);
            this.dgvVolumeTables.Size = new Size(780, 145);
            this.dgvVolumeTables.AllowUserToAddRows = false;
            this.dgvVolumeTables.AllowUserToDeleteRows = false;
            this.dgvVolumeTables.ReadOnly = false;
            this.dgvVolumeTables.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvVolumeTables.MultiSelect = false;
            this.dgvVolumeTables.RowHeadersVisible = false;
            this.dgvVolumeTables.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvVolumeTables.Font = standardFont;
            this.dgvVolumeTables.BackgroundColor = Color.White;

            // Add columns
            this.dgvVolumeTables.Columns.Add("TableType", "Table type");
            this.dgvVolumeTables.Columns.Add("Style", "Style");
            
            var cmbMaterialListColumn = new DataGridViewComboBoxColumn();
            cmbMaterialListColumn.Name = "MaterialList";
            cmbMaterialListColumn.HeaderText = "Material list";
            cmbMaterialListColumn.FlatStyle = FlatStyle.Flat;
            this.dgvVolumeTables.Columns.Add(cmbMaterialListColumn);
            
            this.dgvVolumeTables.Columns.Add("Materials", "Materials");
            this.dgvVolumeTables.Columns.Add("Layer", "Layer");
            
            var cmbSplitColumn = new DataGridViewComboBoxColumn();
            cmbSplitColumn.Name = "Split";
            cmbSplitColumn.HeaderText = "Split";
            cmbSplitColumn.Items.AddRange("Yes", "No");
            cmbSplitColumn.FlatStyle = FlatStyle.Flat;
            this.dgvVolumeTables.Columns.Add(cmbSplitColumn);
            
            this.dgvVolumeTables.Columns.Add("Gap", "Gap");
            
            var cmbReactivityColumn = new DataGridViewComboBoxColumn();
            cmbReactivityColumn.Name = "ReactivityMode";
            cmbReactivityColumn.HeaderText = "Reactivity mode";
            cmbReactivityColumn.Items.AddRange("Static", "Dynamic");
            cmbReactivityColumn.FlatStyle = FlatStyle.Flat;
            this.dgvVolumeTables.Columns.Add(cmbReactivityColumn);

            // Set column widths
            this.dgvVolumeTables.Columns["TableType"].Width = 80;
            this.dgvVolumeTables.Columns["Style"].Width = 120;
            this.dgvVolumeTables.Columns["MaterialList"].Width = 100;
            this.dgvVolumeTables.Columns["Materials"].Width = 80;
            this.dgvVolumeTables.Columns["Layer"].Width = 60;
            this.dgvVolumeTables.Columns["Split"].Width = 50;
            this.dgvVolumeTables.Columns["Gap"].Width = 60;
            this.dgvVolumeTables.Columns["ReactivityMode"].Width = 100;

            // Readonly columns
            this.dgvVolumeTables.Columns["TableType"].ReadOnly = true;
            this.dgvVolumeTables.Columns["Style"].ReadOnly = true;

            // Move Up Button
            this.btnMoveUp.Text = "▲";
            this.btnMoveUp.Font = standardFont;
            this.btnMoveUp.Location = new WinFormsPoint(800, 25);
            this.btnMoveUp.Size = new Size(30, 30);
            this.btnMoveUp.Click += BtnMoveUp_Click;

            // Move Down Button
            this.btnMoveDown.Text = "▼";
            this.btnMoveDown.Font = standardFont;
            this.btnMoveDown.Location = new WinFormsPoint(800, 60);
            this.btnMoveDown.Size = new Size(30, 30);
            this.btnMoveDown.Click += BtnMoveDown_Click;

            // Delete Button
            this.btnDelete.Text = "✕";
            this.btnDelete.Font = new WinFormsFont("Segoe UI", 10F, FontStyle.Bold);
            this.btnDelete.ForeColor = Color.Red;
            this.btnDelete.Location = new WinFormsPoint(800, 130);
            this.btnDelete.Size = new Size(30, 30);
            this.btnDelete.Click += BtnDelete_Click;

            this.grpVolumeTables.Controls.AddRange(new Control[] {
                dgvVolumeTables, btnMoveUp, btnMoveDown, btnDelete
            });

            // ===== Position GroupBox =====
            this.grpPosition.Text = "Position of table(s) relative to section view";
            this.grpPosition.Font = boldFont;
            this.grpPosition.Location = new WinFormsPoint(20, 265);
            this.grpPosition.Size = new Size(670, 140);

            // Section View Anchor
            this.lblSectionViewAnchor.Text = "Section view anchor:";
            this.lblSectionViewAnchor.Font = standardFont;
            this.lblSectionViewAnchor.Location = new WinFormsPoint(15, 30);
            this.lblSectionViewAnchor.Size = new Size(120, 20);

            this.cmbSectionViewAnchor.Location = new WinFormsPoint(15, 52);
            this.cmbSectionViewAnchor.Size = new Size(160, 25);
            this.cmbSectionViewAnchor.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbSectionViewAnchor.Font = standardFont;
            this.cmbSectionViewAnchor.Items.AddRange(new object[] {
                "Top Left", "Top Right", "Bottom Left", "Bottom Right",
                "Middle Left", "Middle Right"
            });

            // Table Anchor
            this.lblTableAnchorLabel.Text = "Table anchor:";
            this.lblTableAnchorLabel.Font = standardFont;
            this.lblTableAnchorLabel.Location = new WinFormsPoint(200, 30);
            this.lblTableAnchorLabel.Size = new Size(100, 20);

            this.cmbTableAnchor.Location = new WinFormsPoint(200, 52);
            this.cmbTableAnchor.Size = new Size(160, 25);
            this.cmbTableAnchor.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbTableAnchor.Font = standardFont;
            this.cmbTableAnchor.Items.AddRange(new object[] {
                "Top Left", "Top Right", "Bottom Left", "Bottom Right",
                "Middle Left", "Middle Right"
            });

            // Table Layout
            this.lblTableLayoutLabel.Text = "Table Layout:";
            this.lblTableLayoutLabel.Font = standardFont;
            this.lblTableLayoutLabel.Location = new WinFormsPoint(385, 30);
            this.lblTableLayoutLabel.Size = new Size(100, 20);

            this.cmbTableLayout.Location = new WinFormsPoint(385, 52);
            this.cmbTableLayout.Size = new Size(160, 25);
            this.cmbTableLayout.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbTableLayout.Font = standardFont;
            this.cmbTableLayout.Items.AddRange(new object[] { "Horizontal", "Vertical" });

            // X Offset
            this.lblXOffset.Text = "X offset:";
            this.lblXOffset.Font = standardFont;
            this.lblXOffset.Location = new WinFormsPoint(15, 90);
            this.lblXOffset.Size = new Size(60, 20);

            this.nudXOffset.Location = new WinFormsPoint(15, 110);
            this.nudXOffset.Size = new Size(120, 25);
            this.nudXOffset.Font = standardFont;
            this.nudXOffset.DecimalPlaces = 2;
            this.nudXOffset.Minimum = -100000;
            this.nudXOffset.Maximum = 100000;
            this.nudXOffset.Value = 0;
            this.nudXOffset.Increment = 0.1M;

            // Y Offset
            this.lblYOffset.Text = "Y offset:";
            this.lblYOffset.Font = standardFont;
            this.lblYOffset.Location = new WinFormsPoint(200, 90);
            this.lblYOffset.Size = new Size(60, 20);

            this.nudYOffset.Location = new WinFormsPoint(200, 110);
            this.nudYOffset.Size = new Size(120, 25);
            this.nudYOffset.Font = standardFont;
            this.nudYOffset.DecimalPlaces = 2;
            this.nudYOffset.Minimum = -100000;
            this.nudYOffset.Maximum = 100000;
            this.nudYOffset.Value = 0;
            this.nudYOffset.Increment = 0.1M;

            this.grpPosition.Controls.AddRange(new Control[] {
                lblSectionViewAnchor, cmbSectionViewAnchor,
                lblTableAnchorLabel, cmbTableAnchor,
                lblTableLayoutLabel, cmbTableLayout,
                lblXOffset, nudXOffset,
                lblYOffset, nudYOffset
            });

            // ===== Buttons =====
            this.btnOK.Text = "OK";
            this.btnOK.Font = standardFont;
            this.btnOK.Location = new WinFormsPoint(605, 420);
            this.btnOK.Size = new Size(75, 28);
            this.btnOK.Click += BtnOK_Click;

            this.btnCancel.Text = "Cancel";
            this.btnCancel.Font = standardFont;
            this.btnCancel.Location = new WinFormsPoint(685, 420);
            this.btnCancel.Size = new Size(75, 28);
            this.btnCancel.Click += BtnCancel_Click;

            this.btnApply.Text = "Apply";
            this.btnApply.Font = standardFont;
            this.btnApply.Location = new WinFormsPoint(765, 420);
            this.btnApply.Size = new Size(75, 28);
            this.btnApply.Click += BtnApply_Click;

            this.btnHelp.Text = "Help";
            this.btnHelp.Font = standardFont;
            this.btnHelp.Location = new WinFormsPoint(845, 420);
            this.btnHelp.Size = new Size(55, 28);

            // Add all controls to form
            this.Controls.AddRange(new Control[] {
                lblWarning, lblType, cmbType, lblTableStyle, cmbTableStyle, btnAdd,
                grpVolumeTables, grpPosition,
                btnOK, btnCancel, btnApply, btnHelp
            });

            this.ResumeLayout(false);
        }

        private void LoadData()
        {
            // Load table styles into combo box
            cmbTableStyle.Items.Clear();
            foreach (var kvp in _tableStyleList)
            {
                cmbTableStyle.Items.Add(kvp.Key);
            }
            if (cmbTableStyle.Items.Count > 0)
                cmbTableStyle.SelectedIndex = 0;

            // Load material lists into DataGridView combo column
            var materialListColumn = dgvVolumeTables.Columns["MaterialList"] as DataGridViewComboBoxColumn;
            if (materialListColumn != null)
            {
                materialListColumn.Items.Clear();
                foreach (var kvp in _materialListList)
                {
                    materialListColumn.Items.Add(kvp.Key);
                }
            }
        }

        private void RestoreLastUsedValues()
        {
            // Restore Type
            for (int i = 0; i < cmbType.Items.Count; i++)
            {
                if (cmbType.Items[i]?.ToString() == _lastTableType)
                {
                    cmbType.SelectedIndex = i;
                    break;
                }
            }

            // Restore Table Style
            if (!string.IsNullOrEmpty(_lastTableStyle))
            {
                for (int i = 0; i < cmbTableStyle.Items.Count; i++)
                {
                    if (cmbTableStyle.Items[i]?.ToString() == _lastTableStyle)
                    {
                        cmbTableStyle.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Restore Position settings
            SelectComboItem(cmbSectionViewAnchor, _lastSectionViewAnchor);
            SelectComboItem(cmbTableAnchor, _lastTableAnchor);
            SelectComboItem(cmbTableLayout, _lastTableLayout);
            
            nudXOffset.Value = (decimal)_lastXOffset;
            nudYOffset.Value = (decimal)_lastYOffset;
        }

        private void SelectComboItem(ComboBox cmb, string value)
        {
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                if (cmb.Items[i]?.ToString() == value)
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }
            if (cmb.Items.Count > 0)
                cmb.SelectedIndex = 0;
        }

        private void SaveLastUsedValues()
        {
            _lastTableType = cmbType.SelectedItem?.ToString() ?? "Material";
            _lastTableStyle = cmbTableStyle.SelectedItem?.ToString() ?? "";
            _lastSectionViewAnchor = cmbSectionViewAnchor.SelectedItem?.ToString() ?? "Top Left";
            _lastTableAnchor = cmbTableAnchor.SelectedItem?.ToString() ?? "Top Left";
            _lastTableLayout = cmbTableLayout.SelectedItem?.ToString() ?? "Horizontal";
            _lastXOffset = (double)nudXOffset.Value;
            _lastYOffset = (double)nudYOffset.Value;
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            if (cmbType.SelectedItem == null || cmbTableStyle.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn Type và Table Style!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableType = cmbType.SelectedItem.ToString() ?? "Material";
            string tableStyle = cmbTableStyle.SelectedItem.ToString() ?? "";

            // Add new row to DataGridView
            int rowIndex = dgvVolumeTables.Rows.Add();
            var row = dgvVolumeTables.Rows[rowIndex];

            row.Cells["TableType"].Value = tableType;
            row.Cells["Style"].Value = tableStyle;
            
            // Set default values for Material list if available
            var materialListColumn = dgvVolumeTables.Columns["MaterialList"] as DataGridViewComboBoxColumn;
            if (materialListColumn != null && materialListColumn.Items.Count > 0)
            {
                row.Cells["MaterialList"].Value = materialListColumn.Items[0];
            }
            
            row.Cells["Materials"].Value = "";
            row.Cells["Layer"].Value = "0";
            row.Cells["Split"].Value = "Yes";
            row.Cells["Gap"].Value = "";
            row.Cells["ReactivityMode"].Value = "Dynamic";
        }

        private void BtnMoveUp_Click(object? sender, EventArgs e)
        {
            if (dgvVolumeTables.SelectedRows.Count == 0) return;
            
            int selectedIndex = dgvVolumeTables.SelectedRows[0].Index;
            if (selectedIndex <= 0) return;

            // Swap rows
            DataGridViewRow row = dgvVolumeTables.Rows[selectedIndex];
            dgvVolumeTables.Rows.RemoveAt(selectedIndex);
            dgvVolumeTables.Rows.Insert(selectedIndex - 1, row);
            dgvVolumeTables.Rows[selectedIndex - 1].Selected = true;
        }

        private void BtnMoveDown_Click(object? sender, EventArgs e)
        {
            if (dgvVolumeTables.SelectedRows.Count == 0) return;
            
            int selectedIndex = dgvVolumeTables.SelectedRows[0].Index;
            if (selectedIndex >= dgvVolumeTables.Rows.Count - 1) return;

            // Swap rows
            DataGridViewRow row = dgvVolumeTables.Rows[selectedIndex];
            dgvVolumeTables.Rows.RemoveAt(selectedIndex);
            dgvVolumeTables.Rows.Insert(selectedIndex + 1, row);
            dgvVolumeTables.Rows[selectedIndex + 1].Selected = true;
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvVolumeTables.SelectedRows.Count == 0) return;
            
            int selectedIndex = dgvVolumeTables.SelectedRows[0].Index;
            dgvVolumeTables.Rows.RemoveAt(selectedIndex);
        }

        private void CollectData()
        {
            VolumeTables.Clear();

            foreach (DataGridViewRow row in dgvVolumeTables.Rows)
            {
                if (row.IsNewRow) continue;

                var config = new VolumeTableConfig
                {
                    TableType = row.Cells["TableType"].Value?.ToString() ?? "",
                    Style = row.Cells["Style"].Value?.ToString() ?? "",
                    MaterialList = row.Cells["MaterialList"].Value?.ToString() ?? "",
                    Materials = row.Cells["Materials"].Value?.ToString() ?? "",
                    Layer = row.Cells["Layer"].Value?.ToString() ?? "0",
                    Split = row.Cells["Split"].Value?.ToString() == "Yes",
                    Gap = row.Cells["Gap"].Value?.ToString() ?? "",
                    ReactivityMode = row.Cells["ReactivityMode"].Value?.ToString() ?? "Dynamic"
                };

                // Find ObjectIds
                foreach (var kvp in _tableStyleList)
                {
                    if (kvp.Key == config.Style)
                    {
                        config.StyleId = kvp.Value;
                        break;
                    }
                }

                foreach (var kvp in _materialListList)
                {
                    if (kvp.Key == config.MaterialList)
                    {
                        config.MaterialListId = kvp.Value;
                        break;
                    }
                }

                VolumeTables.Add(config);
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (dgvVolumeTables.Rows.Count == 0)
            {
                MessageBox.Show("Vui lòng thêm ít nhất một Volume Table!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveLastUsedValues();
            CollectData();
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

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            if (dgvVolumeTables.Rows.Count == 0)
            {
                MessageBox.Show("Vui lòng thêm ít nhất một Volume Table!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveLastUsedValues();
            CollectData();
            
            // Raise event or callback for Apply action
            OnApplyClicked?.Invoke(this, EventArgs.Empty);
        }

        // Event for Apply button
        public event EventHandler? OnApplyClicked;
    }

    /// <summary>
    /// Helper class to store Volume Table configuration
    /// </summary>
    public class VolumeTableConfig
    {
        public string TableType { get; set; } = "";
        public string Style { get; set; } = "";
        public ObjectId StyleId { get; set; } = ObjectId.Null;
        public string MaterialList { get; set; } = "";
        public ObjectId MaterialListId { get; set; } = ObjectId.Null;
        public string Materials { get; set; } = "";
        public string Layer { get; set; } = "0";
        public bool Split { get; set; } = true;
        public string Gap { get; set; } = "";
        public string ReactivityMode { get; set; } = "Dynamic";
    }
}
