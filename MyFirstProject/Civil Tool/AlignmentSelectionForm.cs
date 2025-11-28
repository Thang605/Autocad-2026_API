using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MyFirstProject.Civil_Tool
{
    public partial class AlignmentSelectionForm : Form
    {
        private CheckedListBox checkedListBoxAlignments;
        private Button btnSelectAll;
        private Button btnDeselectAll;
        private Button btnOK;
        private Button btnCancel;
        private Label lblInstructions;
        private Label lblCount;
        private Label lblTitle;

        public List<AlignmentInfo> AvailableAlignments { get; set; } = new();
        public List<AlignmentInfo> SelectedAlignments { get; private set; } = new();
        public bool DialogResult_OK { get; private set; } = false;

        public AlignmentSelectionForm()
        {
            InitializeComponent();
        }

        public AlignmentSelectionForm(List<AlignmentInfo> alignments)
        {
            AvailableAlignments = new List<AlignmentInfo>(alignments);
            InitializeComponent();
            LoadAlignments();
        }

        private void InitializeComponent()
        {
            this.checkedListBoxAlignments = new CheckedListBox();
            this.btnSelectAll = new Button();
            this.btnDeselectAll = new Button();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.lblInstructions = new Label();
            this.lblCount = new Label();
            this.lblTitle = new Label();
            this.SuspendLayout();

            // Form - Tăng kích thước
            this.Text = "Chọn Alignment để xuất khối lượng";
            this.Size = new Size(700, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int currentY = 15;
            int leftMargin = 20;
            int contentWidth = 640;

            // lblTitle - Thêm tiêu đề nổi bật
            this.lblTitle.Location = new Point(leftMargin, currentY);
            this.lblTitle.Size = new Size(contentWidth, 35);
            this.lblTitle.Text = "CHỌN ALIGNMENT ĐỂ XUẤT KHỐI LƯỢNG";
            this.lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.FromArgb(0, 51, 153);
            this.lblTitle.AutoSize = false;
            this.lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            currentY += 40;

            // lblInstructions - Tăng kích thước và spacing
            this.lblInstructions.Location = new Point(leftMargin, currentY);
            this.lblInstructions.Size = new Size(contentWidth, 55);
            this.lblInstructions.AutoSize = false;
            this.lblInstructions.Text = "Chọn các Alignment có SampleLineGroup để xuất khối lượng." + Environment.NewLine + 
                                        "Mỗi SampleLineGroup sẽ được xuất ra một sheet riêng trong file Excel.";
            this.lblInstructions.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.lblInstructions.ForeColor = Color.FromArgb(51, 51, 51);
            currentY += 65;

            // checkedListBoxAlignments - Tăng kích thước
            this.checkedListBoxAlignments.Location = new Point(leftMargin, currentY);
            this.checkedListBoxAlignments.Size = new Size(contentWidth, 340);
            this.checkedListBoxAlignments.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.checkedListBoxAlignments.CheckOnClick = true;
            this.checkedListBoxAlignments.ItemHeight = 24;
            this.checkedListBoxAlignments.BorderStyle = BorderStyle.FixedSingle;
            this.checkedListBoxAlignments.ItemCheck += new ItemCheckEventHandler(this.CheckedListBoxAlignments_ItemCheck);
            currentY += 350;

            // lblCount - Tăng kích thước
            this.lblCount.Location = new Point(leftMargin, currentY);
            this.lblCount.Size = new Size(400, 28);
            this.lblCount.AutoSize = false;
            this.lblCount.Text = "Đã chọn: 0 alignment(s)";
            this.lblCount.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblCount.ForeColor = Color.FromArgb(0, 100, 0);
            this.lblCount.TextAlign = ContentAlignment.MiddleLeft;
            currentY += 35;

            // Buttons - Tăng kích thước và khoảng cách
            this.btnSelectAll.Location = new Point(leftMargin, currentY);
            this.btnSelectAll.Size = new Size(130, 38);
            this.btnSelectAll.Text = "Chọn tất cả";
            this.btnSelectAll.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.btnSelectAll.UseVisualStyleBackColor = true;
            this.btnSelectAll.Click += new EventHandler(this.BtnSelectAll_Click);

            this.btnDeselectAll.Location = new Point(leftMargin + 145, currentY);
            this.btnDeselectAll.Size = new Size(130, 38);
            this.btnDeselectAll.Text = "Bỏ chọn tất cả";
            this.btnDeselectAll.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.btnDeselectAll.UseVisualStyleBackColor = true;
            this.btnDeselectAll.Click += new EventHandler(this.BtnDeselectAll_Click);

            this.btnOK.Location = new Point(leftMargin + 435, currentY);
            this.btnOK.Size = new Size(100, 38);
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnOK.BackColor = Color.FromArgb(0, 122, 204);
            this.btnOK.ForeColor = Color.White;
            this.btnOK.FlatStyle = FlatStyle.Flat;
            this.btnOK.FlatAppearance.BorderSize = 0;
            this.btnOK.Click += new EventHandler(this.BtnOK_Click);

            this.btnCancel.Location = new Point(leftMargin + 550, currentY);
            this.btnCancel.Size = new Size(100, 38);
            this.btnCancel.Text = "Hủy";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.btnCancel.Click += new EventHandler(this.BtnCancel_Click);

            // Add controls to form
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblInstructions);
            this.Controls.Add(this.checkedListBoxAlignments);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.btnSelectAll);
            this.Controls.Add(this.btnDeselectAll);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);

            this.ResumeLayout(false);
        }

        private void LoadAlignments()
        {
            checkedListBoxAlignments.Items.Clear();
            
            foreach (var alignmentInfo in AvailableAlignments)
            {
                checkedListBoxAlignments.Items.Add(alignmentInfo, false);
                checkedListBoxAlignments.DisplayMember = "DisplayText";
            }

            UpdateCountLabel();
        }

        private void CheckedListBoxAlignments_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Update count after the check state changes
            this.BeginInvoke(new Action(() => UpdateCountLabel()));
        }

        private void UpdateCountLabel()
        {
            int checkedCount = checkedListBoxAlignments.CheckedItems.Count;
            lblCount.Text = $"Đã chọn: {checkedCount} alignment(s)";
        }

        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxAlignments.Items.Count; i++)
            {
                checkedListBoxAlignments.SetItemChecked(i, true);
            }
            UpdateCountLabel();
        }

        private void BtnDeselectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxAlignments.Items.Count; i++)
            {
                checkedListBoxAlignments.SetItemChecked(i, false);
            }
            UpdateCountLabel();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            SelectedAlignments.Clear();
            
            foreach (var item in checkedListBoxAlignments.CheckedItems)
            {
                if (item is AlignmentInfo alignmentInfo)
                {
                    SelectedAlignments.Add(alignmentInfo);
                }
            }

            if (SelectedAlignments.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một Alignment!", 
                    "Thông báo", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Warning);
                return;
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

    public class AlignmentInfo
    {
        public Autodesk.AutoCAD.DatabaseServices.ObjectId AlignmentId { get; set; }
        public string AlignmentName { get; set; } = "";
        public int SampleLineGroupCount { get; set; }
        public List<SampleLineGroupInfo> SampleLineGroups { get; set; } = new();

        public string DisplayText => $"{AlignmentName} ({SampleLineGroupCount} SampleLineGroup(s))";

        public override string ToString()
        {
            return DisplayText;
        }
    }

    public class SampleLineGroupInfo
    {
        public Autodesk.AutoCAD.DatabaseServices.ObjectId SampleLineGroupId { get; set; }
        public string SampleLineGroupName { get; set; } = "";
        public string AlignmentName { get; set; } = "";
    }
}
