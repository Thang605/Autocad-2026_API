using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Form để hiển thị và chỉnh sửa bán kính các đường cong trong Alignment
    /// </summary>
    public partial class DieuChinhBanKinhForm : Form
    {
        private DataGridView dataGridView;
        private Button btnOK;
        private Button btnCancel;
        private Button btnReset;
        private Label lblAlignmentName;
        private Label lblTitle;

        public string AlignmentName { get; set; } = "";
        public List<ArcInfo> ArcList { get; set; } = new();
        public bool DialogResult_OK { get; private set; } = false;
        
        // Lưu bán kính đã sử dụng cho lần chạy sau
        private static double _lastUsedRadius = 0;

        public DieuChinhBanKinhForm()
        {
            InitializeComponent();
        }

        public DieuChinhBanKinhForm(string alignmentName, List<ArcInfo> arcList)
        {
            AlignmentName = alignmentName;
            ArcList = new List<ArcInfo>(arcList);
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.dataGridView = new DataGridView();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.btnReset = new Button();
            this.lblAlignmentName = new Label();
            this.lblTitle = new Label();
            this.SuspendLayout();

            // Form
            this.Text = "Điều chỉnh bán kính đường cong";
            this.Size = new Size(850, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int currentY = 15;
            int leftMargin = 20;
            int contentWidth = 790;

            // lblTitle
            this.lblTitle.Location = new Point(leftMargin, currentY);
            this.lblTitle.Size = new Size(contentWidth, 35);
            this.lblTitle.Text = "ĐIỀU CHỈNH BÁN KÍNH ĐƯỜNG CONG";
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.FromArgb(0, 51, 153);
            this.lblTitle.AutoSize = false;
            this.lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            currentY += 40;

            // lblAlignmentName
            this.lblAlignmentName.Location = new Point(leftMargin, currentY);
            this.lblAlignmentName.Size = new Size(contentWidth, 30);
            this.lblAlignmentName.Text = $"Alignment: {AlignmentName}";
            this.lblAlignmentName.Font = new System.Drawing.Font("Segoe UI", 10F, FontStyle.Regular);
            this.lblAlignmentName.ForeColor = Color.FromArgb(51, 51, 51);
            this.lblAlignmentName.AutoSize = false;
            currentY += 40;

            // DataGridView
            this.dataGridView.Location = new Point(leftMargin, currentY);
            this.dataGridView.Size = new Size(contentWidth, 350);
            this.dataGridView.Font = new System.Drawing.Font("Segoe UI", 10F, FontStyle.Regular);
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView.MultiSelect = false;
            this.dataGridView.RowHeadersVisible = false;
            this.dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView.BackgroundColor = Color.White;
            this.dataGridView.BorderStyle = BorderStyle.FixedSingle;

            // Add columns
            var colSTT = new DataGridViewTextBoxColumn
            {
                Name = "STT",
                HeaderText = "STT",
                Width = 50,
                ReadOnly = true
            };

            var colEntityId = new DataGridViewTextBoxColumn
            {
                Name = "EntityId",
                HeaderText = "Entity ID",
                Width = 80,
                ReadOnly = true
            };

            var colStartStation = new DataGridViewTextBoxColumn
            {
                Name = "StartStation",
                HeaderText = "Lý trình đầu (m)",
                Width = 120,
                ReadOnly = true
            };

            var colEndStation = new DataGridViewTextBoxColumn
            {
                Name = "EndStation",
                HeaderText = "Lý trình cuối (m)",
                Width = 120,
                ReadOnly = true
            };

            var colCurrentRadius = new DataGridViewTextBoxColumn
            {
                Name = "CurrentRadius",
                HeaderText = "Bán kính hiện tại (m)",
                Width = 150,
                ReadOnly = true
            };

            var colNewRadius = new DataGridViewTextBoxColumn
            {
                Name = "NewRadius",
                HeaderText = "Bán kính mới (m)",
                Width = 150,
                ReadOnly = false
            };

            this.dataGridView.Columns.AddRange(new DataGridViewColumn[] {
                colSTT, colEntityId, colStartStation, colEndStation, colCurrentRadius, colNewRadius
            });

            currentY += 360;

            // Buttons
            this.btnReset.Location = new Point(leftMargin, currentY);
            this.btnReset.Size = new Size(130, 38);
            this.btnReset.Text = "Khôi phục";
            this.btnReset.Font = new System.Drawing.Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new EventHandler(this.BtnReset_Click);

            this.btnOK.Location = new Point(leftMargin + 530, currentY);
            this.btnOK.Size = new Size(120, 38);
            this.btnOK.Text = "Áp dụng";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Font = new System.Drawing.Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnOK.BackColor = Color.FromArgb(0, 122, 204);
            this.btnOK.ForeColor = Color.White;
            this.btnOK.FlatStyle = FlatStyle.Flat;
            this.btnOK.FlatAppearance.BorderSize = 0;
            this.btnOK.Click += new EventHandler(this.BtnOK_Click);

            this.btnCancel.Location = new Point(leftMargin + 665, currentY);
            this.btnCancel.Size = new Size(120, 38);
            this.btnCancel.Text = "Hủy";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F, FontStyle.Regular);
            this.btnCancel.Click += new EventHandler(this.BtnCancel_Click);

            // Add controls
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblAlignmentName);
            this.Controls.Add(this.dataGridView);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);

            this.ResumeLayout(false);
        }

        private void LoadData()
        {
            dataGridView.Rows.Clear();
            lblAlignmentName.Text = $"Alignment: {AlignmentName}";

            int stt = 1;
            foreach (var arc in ArcList)
            {
                int rowIndex = dataGridView.Rows.Add();
                var row = dataGridView.Rows[rowIndex];

                row.Cells["STT"].Value = stt++;
                row.Cells["EntityId"].Value = arc.EntityId;
                row.Cells["StartStation"].Value = arc.StartStation.ToString("F2");
                row.Cells["EndStation"].Value = arc.EndStation.ToString("F2");
                row.Cells["CurrentRadius"].Value = arc.CurrentRadius.ToString("F2");
                
                // Sử dụng bán kính đã dùng lần trước nếu có, ngược lại dùng bán kính hiện tại
                double newRadiusValue = _lastUsedRadius > 0 ? _lastUsedRadius : arc.CurrentRadius;
                row.Cells["NewRadius"].Value = newRadiusValue.ToString("F2");

                row.Tag = arc;
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.Tag is ArcInfo arc)
                {
                    row.Cells["NewRadius"].Value = arc.CurrentRadius.ToString("F2");
                }
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Validate and update ArcList with new radius values
            bool hasChanges = false;

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.Tag is ArcInfo arc)
                {
                    string newRadiusStr = row.Cells["NewRadius"].Value?.ToString() ?? "";
                    if (double.TryParse(newRadiusStr, out double newRadius))
                    {
                        if (newRadius <= 0)
                        {
                            MessageBox.Show($"Bán kính phải lớn hơn 0!\nVị trí: Dòng {row.Index + 1}",
                                "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        if (Math.Abs(newRadius - arc.CurrentRadius) > 0.001)
                        {
                            arc.NewRadius = newRadius;
                            hasChanges = true;
                        }
                        else
                        {
                            arc.NewRadius = arc.CurrentRadius;
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Giá trị bán kính không hợp lệ!\nVị trí: Dòng {row.Index + 1}",
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            if (!hasChanges)
            {
                var result = MessageBox.Show("Không có thay đổi nào được thực hiện.\nBạn có muốn đóng form?",
                    "Thông báo", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No) return;
            }

            // Lưu bán kính đã nhập cho lần chạy sau
            foreach (var arc in ArcList)
            {
                if (arc.NewRadius > 0 && Math.Abs(arc.NewRadius - arc.CurrentRadius) > 0.001)
                {
                    _lastUsedRadius = arc.NewRadius;
                    break;
                }
            }

            DialogResult_OK = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult_OK = false;
            this.Close();
        }
    }

    /// <summary>
    /// Class chứa thông tin về một cung tròn trong Alignment
    /// </summary>
    public class ArcInfo
    {
        public int EntityId { get; set; }
        public double StartStation { get; set; }
        public double EndStation { get; set; }
        public double CurrentRadius { get; set; }
        public double NewRadius { get; set; }
        public ObjectId AlignmentArcObjectId { get; set; }
    }
}
