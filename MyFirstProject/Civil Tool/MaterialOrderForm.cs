using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyFirstProject.Civil_Tool
{
    public partial class MaterialOrderForm : Form
    {
        private DataGridView dataGridViewMaterials;
        private Button btnMoveUp;
        private Button btnMoveDown;
        private Button btnOK;
        private Button btnCancel;
        private Button btnReset;
        private Label lblInstructions;
        private Label lblDecimalPlaces;
        private Label lblContextInfo;
        private NumericUpDown numericUpDownDecimalPlaces;

        public List<string> MaterialTypes { get; set; } = new();
        public List<string> OrderedMaterialTypes { get; private set; } = new();
        public Dictionary<string, double> MaterialAdditionalValues { get; private set; } = new();
        public int DecimalPlaces { get; private set; } = 2;
        public bool DialogResult_OK { get; private set; } = false;

        private string AlignmentName { get; set; } = "";
        private string SampleLineGroupName { get; set; } = "";
        private int SampleLineGroupCount { get; set; } = 0;

        public MaterialOrderForm()
        {
            InitializeComponent();
        }

        public MaterialOrderForm(List<string> materialTypes)
        {
            MaterialTypes = new List<string>(materialTypes);
            InitializeComponent();
            LoadMaterialTypes();
        }

        public MaterialOrderForm(List<string> materialTypes, string alignmentName, string sampleLineGroupName)
        {
            MaterialTypes = new List<string>(materialTypes);
            AlignmentName = alignmentName;
            SampleLineGroupName = sampleLineGroupName;
            InitializeComponent();
            LoadMaterialTypes();
        }

        public MaterialOrderForm(List<string> materialTypes, string alignmentName, string sampleLineGroupName, int sampleLineGroupCount)
        {
            MaterialTypes = new List<string>(materialTypes);
            AlignmentName = alignmentName;
            SampleLineGroupName = sampleLineGroupName;
            SampleLineGroupCount = sampleLineGroupCount;
            InitializeComponent();
            LoadMaterialTypes();
        }

        private void InitializeComponent()
        {
            this.dataGridViewMaterials = new DataGridView();
            this.btnMoveUp = new Button();
            this.btnMoveDown = new Button();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.btnReset = new Button();
            this.lblInstructions = new Label();
            this.lblDecimalPlaces = new Label();
            this.lblContextInfo = new Label();
            this.numericUpDownDecimalPlaces = new NumericUpDown();
            this.SuspendLayout();

            // Form - Tăng kích thước để hiển thị tốt hơn
            this.Text = "Sắp xếp thứ tự cột vật liệu và giá trị cộng thêm";
            this.Size = new Size(750, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int currentY = 15;
            int leftMargin = 20;
            int rightMargin = 20;
            int contentWidth = 710;

            // lblContextInfo - Hiển thị thông tin Alignment và SampleLineGroup
            if (!string.IsNullOrEmpty(AlignmentName) || !string.IsNullOrEmpty(SampleLineGroupName))
            {
                this.lblContextInfo.Location = new Point(leftMargin, currentY);
                this.lblContextInfo.Size = new Size(contentWidth, 50);
                this.lblContextInfo.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                this.lblContextInfo.ForeColor = Color.FromArgb(0, 51, 153);
                this.lblContextInfo.AutoSize = false;
                
                string contextText = "";
                if (!string.IsNullOrEmpty(AlignmentName))
                {
                    // Hiển thị số lượng SampleLineGroup nếu có
                    if (SampleLineGroupCount > 0)
                    {
                        contextText += $"Alignment: {AlignmentName} ({SampleLineGroupCount} SampleLineGroup(s))";
                    }
                    else
                    {
                        contextText += $"Alignment: {AlignmentName}";
                    }
                }
                
                if (!string.IsNullOrEmpty(SampleLineGroupName))
                {
                    if (contextText.Length > 0) contextText += Environment.NewLine;
                    contextText += $"SampleLineGroup: {SampleLineGroupName}";
                }
                
                this.lblContextInfo.Text = contextText;
                this.Controls.Add(this.lblContextInfo);
                currentY += 55;
            }

            // lblInstructions - Tăng kích thước và spacing
            this.lblInstructions.Location = new Point(leftMargin, currentY);
            this.lblInstructions.Size = new Size(contentWidth, 65);
            this.lblInstructions.AutoSize = false;
            this.lblInstructions.Text = "Chọn vật liệu và sử dụng các nút để sắp xếp thứ tự hiển thị trong bảng." + Environment.NewLine + 
                                        "Vật liệu ở trên cùng sẽ hiển thị ở cột đầu tiên." + Environment.NewLine + 
                                        "Nhập giá trị cộng thêm cho mỗi vật liệu (có thể để trống = 0).";
            this.lblInstructions.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.lblInstructions.ForeColor = Color.FromArgb(51, 51, 51);
            currentY += 75;

            // dataGridViewMaterials - Tăng kích thước
            this.dataGridViewMaterials.Location = new Point(leftMargin, currentY);
            this.dataGridViewMaterials.Size = new Size(540, 320);
            this.dataGridViewMaterials.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.dataGridViewMaterials.AllowUserToAddRows = false;
            this.dataGridViewMaterials.AllowUserToDeleteRows = false;
            this.dataGridViewMaterials.RowHeadersVisible = false;
            this.dataGridViewMaterials.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewMaterials.MultiSelect = false;
            this.dataGridViewMaterials.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewMaterials.RowTemplate.Height = 28;
            this.dataGridViewMaterials.ColumnHeadersHeight = 32;
            this.dataGridViewMaterials.BackgroundColor = Color.White;
            this.dataGridViewMaterials.BorderStyle = BorderStyle.FixedSingle;

            // Add columns với padding tốt hơn
            DataGridViewTextBoxColumn colMaterial = new DataGridViewTextBoxColumn();
            colMaterial.Name = "Material";
            colMaterial.HeaderText = "Tên vật liệu";
            colMaterial.ReadOnly = true;
            colMaterial.FillWeight = 65;
            colMaterial.DefaultCellStyle.Padding = new Padding(5, 0, 5, 0);
            this.dataGridViewMaterials.Columns.Add(colMaterial);

            DataGridViewTextBoxColumn colAdditionalValue = new DataGridViewTextBoxColumn();
            colAdditionalValue.Name = "AdditionalValue";
            colAdditionalValue.HeaderText = "Giá trị cộng thêm";
            colAdditionalValue.ReadOnly = false;
            colAdditionalValue.FillWeight = 35;
            colAdditionalValue.DefaultCellStyle.Padding = new Padding(5, 0, 5, 0);
            colAdditionalValue.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.dataGridViewMaterials.Columns.Add(colAdditionalValue);

            int buttonLeft = leftMargin + 560;
            int buttonY = currentY + 30;

            // btnMoveUp - Tăng kích thước nút
            this.btnMoveUp.Location = new Point(buttonLeft, buttonY);
            this.btnMoveUp.Size = new Size(130, 38);
            this.btnMoveUp.Text = "▲ Lên trên";
            this.btnMoveUp.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.btnMoveUp.UseVisualStyleBackColor = true;
            this.btnMoveUp.Click += new EventHandler(this.BtnMoveUp_Click);

            // btnMoveDown
            this.btnMoveDown.Location = new Point(buttonLeft, buttonY + 50);
            this.btnMoveDown.Size = new Size(130, 38);
            this.btnMoveDown.Text = "▼ Xuống dưới";
            this.btnMoveDown.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.btnMoveDown.UseVisualStyleBackColor = true;
            this.btnMoveDown.Click += new EventHandler(this.BtnMoveDown_Click);

            // btnReset
            this.btnReset.Location = new Point(buttonLeft, buttonY + 120);
            this.btnReset.Size = new Size(130, 38);
            this.btnReset.Text = "Đặt lại";
            this.btnReset.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new EventHandler(this.BtnReset_Click);

            currentY += 330;

            // lblDecimalPlaces - Tăng khoảng cách
            this.lblDecimalPlaces.Location = new Point(leftMargin, currentY);
            this.lblDecimalPlaces.Size = new Size(180, 28);
            this.lblDecimalPlaces.AutoSize = false;
            this.lblDecimalPlaces.Text = "Số chữ số thập phân:";
            this.lblDecimalPlaces.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.lblDecimalPlaces.TextAlign = ContentAlignment.MiddleLeft;

            // numericUpDownDecimalPlaces - Tăng kích thước
            this.numericUpDownDecimalPlaces.Location = new Point(leftMargin + 185, currentY);
            this.numericUpDownDecimalPlaces.Size = new Size(70, 28);
            this.numericUpDownDecimalPlaces.Minimum = 0;
            this.numericUpDownDecimalPlaces.Maximum = 6;
            this.numericUpDownDecimalPlaces.Value = 2;
            this.numericUpDownDecimalPlaces.Font = new Font("Segoe UI", 10F, FontStyle.Regular);

            currentY += 45;

            // btnOK và btnCancel - Tăng kích thước và khoảng cách
            this.btnOK.Location = new Point(480, currentY);
            this.btnOK.Size = new Size(100, 40);
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnOK.BackColor = Color.FromArgb(0, 122, 204);
            this.btnOK.ForeColor = Color.White;
            this.btnOK.FlatStyle = FlatStyle.Flat;
            this.btnOK.FlatAppearance.BorderSize = 0;
            this.btnOK.Click += new EventHandler(this.BtnOK_Click);

            // btnCancel
            this.btnCancel.Location = new Point(600, currentY);
            this.btnCancel.Size = new Size(100, 40);
            this.btnCancel.Text = "Hủy";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.btnCancel.Click += new EventHandler(this.BtnCancel_Click);

            // Add controls to form
            this.Controls.Add(this.lblInstructions);
            this.Controls.Add(this.dataGridViewMaterials);
            this.Controls.Add(this.btnMoveUp);
            this.Controls.Add(this.btnMoveDown);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.lblDecimalPlaces);
            this.Controls.Add(this.numericUpDownDecimalPlaces);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);

            this.ResumeLayout(false);
        }

        private void LoadMaterialTypes()
        {
            dataGridViewMaterials.Rows.Clear();
            foreach (string material in MaterialTypes)
            {
                int rowIndex = dataGridViewMaterials.Rows.Add();
                dataGridViewMaterials.Rows[rowIndex].Cells["Material"].Value = material;
                dataGridViewMaterials.Rows[rowIndex].Cells["AdditionalValue"].Value = "0";
            }

            if (dataGridViewMaterials.Rows.Count > 0)
            {
                dataGridViewMaterials.Rows[0].Selected = true;
            }
        }

        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            if (dataGridViewMaterials.SelectedRows.Count > 0)
            {
                int selectedIndex = dataGridViewMaterials.SelectedRows[0].Index;
                if (selectedIndex > 0)
                {
                    // Get values from current row
                    string material = dataGridViewMaterials.Rows[selectedIndex].Cells["Material"].Value?.ToString() ?? "";
                    string additionalValue = dataGridViewMaterials.Rows[selectedIndex].Cells["AdditionalValue"].Value?.ToString() ?? "0";

                    // Get values from target row (row above)
                    string targetMaterial = dataGridViewMaterials.Rows[selectedIndex - 1].Cells["Material"].Value?.ToString() ?? "";
                    string targetAdditionalValue = dataGridViewMaterials.Rows[selectedIndex - 1].Cells["AdditionalValue"].Value?.ToString() ?? "0";

                    // Swap the values
                    dataGridViewMaterials.Rows[selectedIndex].Cells["Material"].Value = targetMaterial;
                    dataGridViewMaterials.Rows[selectedIndex].Cells["AdditionalValue"].Value = targetAdditionalValue;
                    dataGridViewMaterials.Rows[selectedIndex - 1].Cells["Material"].Value = material;
                    dataGridViewMaterials.Rows[selectedIndex - 1].Cells["AdditionalValue"].Value = additionalValue;

                    // Select the moved row
                    dataGridViewMaterials.ClearSelection();
                    dataGridViewMaterials.Rows[selectedIndex - 1].Selected = true;
                }
            }
        }

        private void BtnMoveDown_Click(object sender, EventArgs e)
        {
            if (dataGridViewMaterials.SelectedRows.Count > 0)
            {
                int selectedIndex = dataGridViewMaterials.SelectedRows[0].Index;
                if (selectedIndex >= 0 && selectedIndex < dataGridViewMaterials.Rows.Count - 1)
                {
                    // Get values from current row
                    string material = dataGridViewMaterials.Rows[selectedIndex].Cells["Material"].Value?.ToString() ?? "";
                    string additionalValue = dataGridViewMaterials.Rows[selectedIndex].Cells["AdditionalValue"].Value?.ToString() ?? "0";

                    // Get values from target row (row below)
                    string targetMaterial = dataGridViewMaterials.Rows[selectedIndex + 1].Cells["Material"].Value?.ToString() ?? "";
                    string targetAdditionalValue = dataGridViewMaterials.Rows[selectedIndex + 1].Cells["AdditionalValue"].Value?.ToString() ?? "0";

                    // Swap the values
                    dataGridViewMaterials.Rows[selectedIndex].Cells["Material"].Value = targetMaterial;
                    dataGridViewMaterials.Rows[selectedIndex].Cells["AdditionalValue"].Value = targetAdditionalValue;
                    dataGridViewMaterials.Rows[selectedIndex + 1].Cells["Material"].Value = material;
                    dataGridViewMaterials.Rows[selectedIndex + 1].Cells["AdditionalValue"].Value = additionalValue;

                    // Select the moved row
                    dataGridViewMaterials.ClearSelection();
                    dataGridViewMaterials.Rows[selectedIndex + 1].Selected = true;
                }
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            LoadMaterialTypes();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            OrderedMaterialTypes.Clear();
            MaterialAdditionalValues.Clear();

            foreach (DataGridViewRow row in dataGridViewMaterials.Rows)
            {
                string material = row.Cells["Material"].Value?.ToString() ?? "";
                string additionalValueStr = row.Cells["AdditionalValue"].Value?.ToString() ?? "0";
                
                if (!string.IsNullOrEmpty(material))
                {
                    OrderedMaterialTypes.Add(material);
                    
                    // Parse additional value, default to 0 if invalid
                    if (double.TryParse(additionalValueStr, out double additionalValue))
                    {
                        MaterialAdditionalValues[material] = additionalValue;
                    }
                    else
                    {
                        MaterialAdditionalValues[material] = 0.0;
                    }
                }
            }

            DecimalPlaces = (int)numericUpDownDecimalPlaces.Value;
            DialogResult_OK = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult_OK = false;
            this.Close();
        }
    }
}
