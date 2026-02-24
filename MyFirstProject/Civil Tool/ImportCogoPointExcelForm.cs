using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;
using Autodesk.AutoCAD.DatabaseServices;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsPoint = System.Drawing.Point;

namespace Civil3DCsharp
{
    public class ImportCogoPointExcelForm : Form
    {
        // UI Controls
        private TextBox txtFilePath = null!;
        private Button btnBrowse = null!;
        private GroupBox grpColumnMapping = null!;
        private ComboBox cmbColX = null!, cmbColY = null!, cmbColZ = null!, cmbColName = null!, cmbColDesc = null!;
        private GroupBox grpOptions = null!;
        private TextBox txtDefaultDesc = null!;
        private CheckBox chkAddToPointGroup = null!;
        private TextBox txtPointGroupName = null!;
        private Button btnImport = null!;
        private Button btnCancel = null!;

        // Data
        public string FilePath => txtFilePath.Text;
        public int ColXIndex => cmbColX.SelectedIndex;
        public int ColYIndex => cmbColY.SelectedIndex;
        public int ColZIndex => cmbColZ.SelectedIndex;
        public int ColNameIndex => cmbColName.SelectedIndex;
        public int ColDescIndex => cmbColDesc.SelectedIndex;
        public string DefaultDescription => txtDefaultDesc.Text;
        public bool AddToPointGroup => chkAddToPointGroup.Checked;
        public string PointGroupName => txtPointGroupName.Text;

        private List<string> _headers = new List<string>();

        public ImportCogoPointExcelForm()
        {
            InitializeComponent();
            SetupMappingComboBoxes();
        }

        private void InitializeComponent()
        {
            this.txtFilePath = new TextBox();
            this.btnBrowse = new Button();
            this.grpColumnMapping = new GroupBox();
            this.cmbColX = new ComboBox();
            this.cmbColY = new ComboBox();
            this.cmbColZ = new ComboBox();
            this.cmbColName = new ComboBox();
            this.cmbColDesc = new ComboBox();
            this.grpOptions = new GroupBox();
            this.txtDefaultDesc = new TextBox();
            this.chkAddToPointGroup = new CheckBox();
            this.txtPointGroupName = new TextBox();
            this.btnImport = new Button();
            this.btnCancel = new Button();

            this.SuspendLayout();

            // Form settings
            this.Text = "Import COGO Points from Excel";
            this.Size = new Size(450, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            // File selection
            WinFormsLabel lblFile = new WinFormsLabel { Text = "Excel File:", Location = new WinFormsPoint(10, 15), Size = new Size(100, 20) };
            txtFilePath.Location = new WinFormsPoint(10, 35);
            txtFilePath.Size = new Size(330, 23);
            txtFilePath.ReadOnly = true;
            btnBrowse.Text = "...";
            btnBrowse.Location = new WinFormsPoint(350, 34);
            btnBrowse.Size = new Size(70, 25);
            btnBrowse.Click += BtnBrowse_Click;

            // Column Mapping Group
            grpColumnMapping.Text = "Column Mapping (Header row: 1)";
            grpColumnMapping.Location = new WinFormsPoint(10, 70);
            grpColumnMapping.Size = new Size(410, 180);

            int startY = 25;
            int spacing = 30;

            AddMappingRow(grpColumnMapping, "Tọa độ X (East):", cmbColX, startY);
            AddMappingRow(grpColumnMapping, "Tọa độ Y (North):", cmbColY, startY + spacing);
            AddMappingRow(grpColumnMapping, "Cao độ (Z):", cmbColZ, startY + spacing * 2);
            AddMappingRow(grpColumnMapping, "Tên điểm (Name):", cmbColName, startY + spacing * 3);
            AddMappingRow(grpColumnMapping, "Mô tả (Desc):", cmbColDesc, startY + spacing * 4);

            // Options Group
            grpOptions.Text = "Options";
            grpOptions.Location = new WinFormsPoint(10, 260);
            grpOptions.Size = new Size(410, 120);

            WinFormsLabel lblDefDesc = new WinFormsLabel { Text = "Default Desc:", Location = new WinFormsPoint(10, 25), Size = new Size(100, 20) };
            txtDefaultDesc.Location = new WinFormsPoint(120, 22);
            txtDefaultDesc.Size = new Size(150, 23);
            txtDefaultDesc.Text = "EG";

            chkAddToPointGroup.Text = "Add to Point Group:";
            chkAddToPointGroup.Location = new WinFormsPoint(10, 55);
            chkAddToPointGroup.Size = new Size(150, 20);
            chkAddToPointGroup.Checked = true;
            chkAddToPointGroup.CheckedChanged += (s, e) => txtPointGroupName.Enabled = chkAddToPointGroup.Checked;

            txtPointGroupName.Location = new WinFormsPoint(120, 80);
            txtPointGroupName.Size = new Size(270, 23);
            txtPointGroupName.Text = "Import_" + DateTime.Now.ToString("yyyyMMdd_HHmm");

            grpOptions.Controls.AddRange(new Control[] { lblDefDesc, txtDefaultDesc, chkAddToPointGroup, txtPointGroupName });

            // Buttons
            btnImport.Text = "Import";
            btnImport.Location = new WinFormsPoint(240, 400);
            btnImport.Size = new Size(90, 30);
            btnImport.Enabled = false;
            btnImport.Click += BtnImport_Click;

            btnCancel.Text = "Cancel";
            btnCancel.Location = new WinFormsPoint(340, 400);
            btnCancel.Size = new Size(80, 30);
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] { lblFile, txtFilePath, btnBrowse, grpColumnMapping, grpOptions, btnImport, btnCancel });

            this.ResumeLayout(false);
        }

        private void AddMappingRow(GroupBox parent, string labelText, ComboBox combo, int y)
        {
            WinFormsLabel lbl = new WinFormsLabel { Text = labelText, Location = new WinFormsPoint(10, y + 3), Size = new Size(120, 20) };
            combo.Location = new WinFormsPoint(140, y);
            combo.Size = new Size(250, 23);
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            parent.Controls.Add(lbl);
            parent.Controls.Add(combo);
        }

        private void SetupMappingComboBoxes()
        {
            ComboBox[] combos = { cmbColX, cmbColY, cmbColZ, cmbColName, cmbColDesc };
            foreach (var cb in combos)
            {
                cb.Items.Add("(None)");
                cb.SelectedIndex = 0;
            }
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls",
                Title = "Select Coordinate File"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = ofd.FileName;
                LoadExcelHeaders(ofd.FileName);
                btnImport.Enabled = true;
            }
        }

        private void LoadExcelHeaders(string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);
                var firstRow = worksheet.FirstRowUsed();

                _headers.Clear();
                if (firstRow != null)
                {
                    foreach (var cell in firstRow.Cells())
                    {
                        _headers.Add(cell.GetString().Trim());
                    }
                }

                UpdateComboBoxes();
                AutoMapColumns();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading Excel headers: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateComboBoxes()
        {
            ComboBox[] combos = { cmbColX, cmbColY, cmbColZ, cmbColName, cmbColDesc };
            foreach (var cb in combos)
            {
                cb.Items.Clear();
                cb.Items.Add("(None)");
                foreach (var header in _headers)
                {
                    cb.Items.Add(header);
                }
                cb.SelectedIndex = 0;
            }
        }

        private void AutoMapColumns()
        {
            for (int i = 0; i < _headers.Count; i++)
            {
                string h = _headers[i].ToLower();
                int idx = i + 1; // 1-based index for dropdown (0 is (None))

                if (h.Contains("x") || h.Contains("east")) cmbColX.SelectedIndex = idx;
                else if (h.Contains("y") || h.Contains("north")) cmbColY.SelectedIndex = idx;
                else if (h.Contains("z") || h.Contains("elev") || h.Contains("cao")) cmbColZ.SelectedIndex = idx;
                else if (h.Contains("ten") || h.Contains("name")) cmbColName.SelectedIndex = idx;
                else if (h.Contains("desc") || h.Contains("mo ta") || h.Contains("ghi chu")) cmbColDesc.SelectedIndex = idx;
            }
        }

        private void BtnImport_Click(object? sender, EventArgs e)
        {
            if (cmbColX.SelectedIndex <= 0 || cmbColY.SelectedIndex <= 0)
            {
                MessageBox.Show("You must map at least X and Y columns.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
