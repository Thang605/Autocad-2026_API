using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject.Civil_Tool_2
{
    /// <summary>
    /// Form đặt tên cọc cho lệnh Bổ Sung Cọc Trên Trắc Dọc
    /// </summary>
    public class BoSungCocForm : Form
    {
        // Static variables to remember last input values
        private static string _lastStakeName = "H1";
        private static bool _lastAutoIncrement = true;
        private static string _lastSampleLineGroupName = "";

        // Properties to return data
        public string StakeName { get; private set; } = "H1";
        public bool AutoIncrement { get; private set; } = true;
        public ObjectId SelectedSampleLineGroupId { get; private set; } = ObjectId.Null;
        public bool FormAccepted { get; private set; } = false;

        // UI Controls
        private WinFormsLabel lblTitle = null!;
        private WinFormsLabel lblStakeName = null!;
        private WinFormsLabel lblSampleLineGroup = null!;
        private WinFormsLabel lblNote = null!;

        private TextBox txtStakeName = null!;
        private CheckBox chkAutoIncrement = null!;
        private ComboBox cmbSampleLineGroup = null!;

        private Button btnOK = null!;
        private Button btnCancel = null!;
        private GroupBox grpNaming = null!;
        private GroupBox grpGroup = null!;

        // Store group IDs
        private List<ObjectId> _sampleLineGroupIds = new List<ObjectId>();
        private ObjectId _alignmentId = ObjectId.Null;

        public BoSungCocForm(ObjectId alignmentId)
        {
            _alignmentId = alignmentId;
            InitializeComponent();
            LoadSampleLineGroups();
            RestoreLastUsedValues();
        }

        private void InitializeComponent()
        {
            // Standard Font
            var standardFont = new WinFormsFont("Segoe UI", 10F, FontStyle.Regular);
            var boldFont = new WinFormsFont("Segoe UI", 10F, FontStyle.Bold);
            var titleFont = new WinFormsFont("Segoe UI", 14F, FontStyle.Bold);
            var noteFont = new WinFormsFont("Segoe UI", 9F, FontStyle.Italic);

            // Initialize controls
            this.lblTitle = new WinFormsLabel();
            this.lblStakeName = new WinFormsLabel();
            this.lblSampleLineGroup = new WinFormsLabel();
            this.lblNote = new WinFormsLabel();

            this.txtStakeName = new TextBox();
            this.chkAutoIncrement = new CheckBox();
            this.cmbSampleLineGroup = new ComboBox();

            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.grpNaming = new GroupBox();
            this.grpGroup = new GroupBox();

            this.SuspendLayout();

            // Form
            this.Text = "Bổ Sung Cọc Trên Trắc Dọc";
            this.Size = new Size(500, 320);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = standardFont;

            // Title Label
            this.lblTitle.Text = "BỔ SUNG CỌC TRÊN TRẮC DỌC";
            this.lblTitle.Font = titleFont;
            this.lblTitle.Location = new WinFormsPoint(20, 15);
            this.lblTitle.Size = new Size(450, 30);
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.ForeColor = Color.FromArgb(0, 102, 204);

            // === Naming Group ===
            this.grpNaming.Text = "Đặt tên cọc";
            this.grpNaming.Font = boldFont;
            this.grpNaming.Location = new WinFormsPoint(20, 55);
            this.grpNaming.Size = new Size(445, 100);
            this.grpNaming.ForeColor = Color.Black;

            // Stake Name
            this.lblStakeName.Text = "Tên cọc:";
            this.lblStakeName.Font = standardFont;
            this.lblStakeName.Location = new WinFormsPoint(20, 30);
            this.lblStakeName.Size = new Size(80, 25);

            this.txtStakeName.Location = new WinFormsPoint(110, 28);
            this.txtStakeName.Size = new Size(150, 25);
            this.txtStakeName.Font = new WinFormsFont("Segoe UI", 11F, FontStyle.Bold);
            this.txtStakeName.Text = "H1";

            // Auto Increment
            this.chkAutoIncrement.Text = "Tự động tăng số";
            this.chkAutoIncrement.Font = standardFont;
            this.chkAutoIncrement.Location = new WinFormsPoint(280, 28);
            this.chkAutoIncrement.Size = new Size(150, 25);
            this.chkAutoIncrement.Checked = true;

            // Note
            this.lblNote.Text = "Ví dụ: H1 → H2 → H3...  hoặc  KC5 → KC6 → KC7...";
            this.lblNote.Font = noteFont;
            this.lblNote.Location = new WinFormsPoint(20, 65);
            this.lblNote.Size = new Size(410, 20);
            this.lblNote.ForeColor = Color.Gray;

            // Add controls to naming group
            this.grpNaming.Controls.AddRange(new Control[] {
                lblStakeName, txtStakeName, chkAutoIncrement, lblNote
            });

            // === Sample Line Group ===
            this.grpGroup.Text = "Nhóm cọc (SampleLine Group)";
            this.grpGroup.Font = boldFont;
            this.grpGroup.Location = new WinFormsPoint(20, 165);
            this.grpGroup.Size = new Size(445, 70);
            this.grpGroup.ForeColor = Color.Black;

            this.lblSampleLineGroup.Text = "Chọn nhóm:";
            this.lblSampleLineGroup.Font = standardFont;
            this.lblSampleLineGroup.Location = new WinFormsPoint(20, 30);
            this.lblSampleLineGroup.Size = new Size(100, 25);

            this.cmbSampleLineGroup.Location = new WinFormsPoint(130, 28);
            this.cmbSampleLineGroup.Size = new Size(290, 25);
            this.cmbSampleLineGroup.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbSampleLineGroup.Font = standardFont;

            this.grpGroup.Controls.AddRange(new Control[] {
                lblSampleLineGroup, cmbSampleLineGroup
            });

            // OK Button
            this.btnOK.Text = "OK";
            this.btnOK.Location = new WinFormsPoint(280, 245);
            this.btnOK.Size = new Size(85, 32);
            this.btnOK.Font = boldFont;
            this.btnOK.Click += BtnOK_Click;

            // Cancel Button
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new WinFormsPoint(380, 245);
            this.btnCancel.Size = new Size(85, 32);
            this.btnCancel.Font = standardFont;
            this.btnCancel.Click += BtnCancel_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                lblTitle,
                grpNaming,
                grpGroup,
                btnOK,
                btnCancel
            });

            this.ResumeLayout(false);
        }

        private void LoadSampleLineGroups()
        {
            cmbSampleLineGroup.Items.Clear();
            _sampleLineGroupIds.Clear();

            try
            {
                using (var tr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var alignment = tr.GetObject(_alignmentId, OpenMode.ForRead) as Alignment;
                    if (alignment != null)
                    {
                        var groupIds = alignment.GetSampleLineGroupIds();
                        foreach (ObjectId groupId in groupIds)
                        {
                            var group = tr.GetObject(groupId, OpenMode.ForRead) as SampleLineGroup;
                            if (group != null)
                            {
                                cmbSampleLineGroup.Items.Add(group.Name);
                                _sampleLineGroupIds.Add(groupId);
                            }
                        }
                    }
                    tr.Commit();
                }

                if (cmbSampleLineGroup.Items.Count > 0)
                {
                    cmbSampleLineGroup.SelectedIndex = 0;
                }
            }
            catch (System.Exception)
            {
                // Handle error silently
            }
        }

        private void RestoreLastUsedValues()
        {
            txtStakeName.Text = _lastStakeName;
            chkAutoIncrement.Checked = _lastAutoIncrement;

            // Restore sample line group selection
            if (!string.IsNullOrEmpty(_lastSampleLineGroupName))
            {
                int idx = cmbSampleLineGroup.Items.IndexOf(_lastSampleLineGroupName);
                if (idx >= 0)
                    cmbSampleLineGroup.SelectedIndex = idx;
            }
        }

        private void SaveLastUsedValues()
        {
            _lastStakeName = txtStakeName.Text;
            _lastAutoIncrement = chkAutoIncrement.Checked;
            if (cmbSampleLineGroup.SelectedItem != null)
                _lastSampleLineGroupName = cmbSampleLineGroup.SelectedItem.ToString() ?? "";
        }

        /// <summary>
        /// Lấy tên cọc hiện tại
        /// </summary>
        public string GetCurrentStakeName()
        {
            return StakeName;
        }

        /// <summary>
        /// Tự động tăng phần số cuối tên cọc.
        /// VD: H1 → H2, KC10 → KC11, ND5 → ND6
        /// </summary>
        public void IncrementNumber()
        {
            if (!AutoIncrement) return;

            // Tách phần chữ (prefix) và phần số (suffix)
            string name = StakeName;
            int i = name.Length - 1;
            while (i >= 0 && char.IsDigit(name[i]))
            {
                i--;
            }

            if (i < name.Length - 1)
            {
                // Có phần số ở cuối
                string prefix = name.Substring(0, i + 1);
                string numberPart = name.Substring(i + 1);
                if (int.TryParse(numberPart, out int num))
                {
                    StakeName = prefix + (num + 1).ToString();
                }
            }
            // Nếu không có phần số → giữ nguyên tên
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(txtStakeName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên cọc!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbSampleLineGroup.SelectedIndex < 0 || _sampleLineGroupIds.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn nhóm cọc (SampleLine Group)!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Get values
            StakeName = txtStakeName.Text.Trim();
            AutoIncrement = chkAutoIncrement.Checked;
            SelectedSampleLineGroupId = _sampleLineGroupIds[cmbSampleLineGroup.SelectedIndex];

            // Save for next time
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
