using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject.Civil_Tool_2
{
    /// <summary>
    /// Form nhập thông số cho lệnh Phát Sinh Cọc Thủ Công
    /// </summary>
    public class PhatSinhCocThuCongForm : Form
    {
        // Static variables to remember last input values
        private static string _lastPrefix = "H";
        private static int _lastStartNumber = 1;
        private static bool _lastAutoIncrement = true;
        private static string _lastSampleLineGroupName = "";
        private static bool _lastSelectOnProfileView = true;

        // Properties to return data
        public string StakeName { get; private set; } = "";
        public string Prefix { get; private set; } = "H";
        public int StartNumber { get; private set; } = 1;
        public bool AutoIncrement { get; private set; } = true;
        public ObjectId SelectedSampleLineGroupId { get; private set; } = ObjectId.Null;
        public bool SelectOnProfileView { get; private set; } = true;
        public bool FormAccepted { get; private set; } = false;

        // Current number for auto increment
        public int CurrentNumber { get; set; } = 1;

        // UI Controls
        private WinFormsLabel lblTitle = null!;
        private WinFormsLabel lblPrefix = null!;
        private WinFormsLabel lblStartNumber = null!;
        private WinFormsLabel lblStakeName = null!;
        private WinFormsLabel lblSampleLineGroup = null!;
        private WinFormsLabel lblSelectMode = null!;

        private TextBox txtPrefix = null!;
        private TextBox txtStakeName = null!;
        private NumericUpDown numStartNumber = null!;
        private CheckBox chkAutoIncrement = null!;
        private ComboBox cmbSampleLineGroup = null!;
        private RadioButton rbProfileView = null!;
        private RadioButton rbAlignment = null!;

        private Button btnOK = null!;
        private Button btnCancel = null!;
        private GroupBox grpNaming = null!;
        private GroupBox grpSelectMode = null!;
        private GroupBox grpGroup = null!;

        // Store group IDs
        private List<ObjectId> _sampleLineGroupIds = new List<ObjectId>();
        private ObjectId _alignmentId = ObjectId.Null;

        public PhatSinhCocThuCongForm(ObjectId alignmentId)
        {
            _alignmentId = alignmentId;
            InitializeComponent();
            LoadSampleLineGroups();
            RestoreLastUsedValues();
            UpdateStakeNamePreview();
        }

        private void InitializeComponent()
        {
            // Standard Font
            var standardFont = new WinFormsFont("Segoe UI", 10F, FontStyle.Regular);
            var boldFont = new WinFormsFont("Segoe UI", 10F, FontStyle.Bold);
            var titleFont = new WinFormsFont("Segoe UI", 14F, FontStyle.Bold);

            // Initialize controls
            this.lblTitle = new WinFormsLabel();
            this.lblPrefix = new WinFormsLabel();
            this.lblStartNumber = new WinFormsLabel();
            this.lblStakeName = new WinFormsLabel();
            this.lblSampleLineGroup = new WinFormsLabel();
            this.lblSelectMode = new WinFormsLabel();

            this.txtPrefix = new TextBox();
            this.txtStakeName = new TextBox();
            this.numStartNumber = new NumericUpDown();
            this.chkAutoIncrement = new CheckBox();
            this.cmbSampleLineGroup = new ComboBox();
            this.rbProfileView = new RadioButton();
            this.rbAlignment = new RadioButton();

            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.grpNaming = new GroupBox();
            this.grpSelectMode = new GroupBox();
            this.grpGroup = new GroupBox();

            this.SuspendLayout();

            // Form
            this.Text = "Phát Sinh Cọc Thủ Công";
            this.Size = new Size(650, 470);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = standardFont;

            // Title Label
            this.lblTitle.Text = "PHÁT SINH CỌC THỦ CÔNG";
            this.lblTitle.Font = titleFont;
            this.lblTitle.Location = new WinFormsPoint(20, 15);
            this.lblTitle.Size = new Size(600, 30);
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.ForeColor = Color.FromArgb(0, 102, 204);

            // Naming Group
            this.grpNaming.Text = "Đặt tên cọc";
            this.grpNaming.Font = boldFont;
            this.grpNaming.Location = new WinFormsPoint(20, 55);
            this.grpNaming.Size = new Size(595, 140);
            this.grpNaming.ForeColor = Color.Black;

            // Prefix
            this.lblPrefix.Text = "Prefix (tiền tố):";
            this.lblPrefix.Font = standardFont;
            this.lblPrefix.Location = new WinFormsPoint(20, 30);
            this.lblPrefix.Size = new Size(120, 25);

            this.txtPrefix.Location = new WinFormsPoint(180, 28);
            this.txtPrefix.Size = new Size(80, 25);
            this.txtPrefix.Font = standardFont;
            this.txtPrefix.Text = "H";
            this.txtPrefix.TextChanged += TxtPrefix_TextChanged;

            // Start Number
            this.lblStartNumber.Text = "Số bắt đầu:";
            this.lblStartNumber.Font = standardFont;
            this.lblStartNumber.Location = new WinFormsPoint(20, 65);
            this.lblStartNumber.Size = new Size(120, 25);

            this.numStartNumber.Location = new WinFormsPoint(180, 63);
            this.numStartNumber.Size = new Size(80, 25);
            this.numStartNumber.Minimum = 1;
            this.numStartNumber.Maximum = 9999;
            this.numStartNumber.Value = 1;
            this.numStartNumber.Font = standardFont;
            this.numStartNumber.ValueChanged += NumStartNumber_ValueChanged;

            // Auto Increment
            this.chkAutoIncrement.Text = "Tự động tăng số";
            this.chkAutoIncrement.Font = standardFont;
            this.chkAutoIncrement.Location = new WinFormsPoint(280, 63);
            this.chkAutoIncrement.Size = new Size(140, 25);
            this.chkAutoIncrement.Checked = true;

            // Stake Name Preview
            this.lblStakeName.Text = "Tên cọc: H1";
            this.lblStakeName.Font = boldFont;
            this.lblStakeName.Location = new WinFormsPoint(20, 100);
            this.lblStakeName.Size = new Size(380, 25);
            this.lblStakeName.ForeColor = Color.FromArgb(0, 128, 0);

            // Add controls to naming group
            this.grpNaming.Controls.AddRange(new Control[] {
                lblPrefix, txtPrefix,
                lblStartNumber, numStartNumber, chkAutoIncrement,
                lblStakeName
            });

            // Select Mode Group
            this.grpSelectMode.Text = "Chế độ chọn điểm";
            this.grpSelectMode.Font = boldFont;
            this.grpSelectMode.Location = new WinFormsPoint(20, 205);
            this.grpSelectMode.Size = new Size(595, 70);
            this.grpSelectMode.ForeColor = Color.Black;

            this.rbProfileView.Text = "Chọn trên Profile View (trắc dọc)";
            this.rbProfileView.Font = standardFont;
            this.rbProfileView.Location = new WinFormsPoint(20, 30);
            this.rbProfileView.Size = new Size(280, 25);
            this.rbProfileView.Checked = true;

            this.rbAlignment.Text = "Chọn trên bình đồ";
            this.rbAlignment.Font = standardFont;
            this.rbAlignment.Location = new WinFormsPoint(310, 30);
            this.rbAlignment.Size = new Size(250, 25);

            this.grpSelectMode.Controls.AddRange(new Control[] {
                rbProfileView, rbAlignment
            });

            // Sample Line Group
            this.grpGroup.Text = "Nhóm cọc (SampleLine Group)";
            this.grpGroup.Font = boldFont;
            this.grpGroup.Location = new WinFormsPoint(20, 285);
            this.grpGroup.Size = new Size(595, 70);
            this.grpGroup.ForeColor = Color.Black;

            this.lblSampleLineGroup.Text = "Chọn nhóm:";
            this.lblSampleLineGroup.Font = standardFont;
            this.lblSampleLineGroup.Location = new WinFormsPoint(20, 30);
            this.lblSampleLineGroup.Size = new Size(100, 25);

            this.cmbSampleLineGroup.Location = new WinFormsPoint(130, 28);
            this.cmbSampleLineGroup.Size = new Size(440, 25);
            this.cmbSampleLineGroup.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbSampleLineGroup.Font = standardFont;

            this.grpGroup.Controls.AddRange(new Control[] {
                lblSampleLineGroup, cmbSampleLineGroup
            });

            // OK Button
            this.btnOK.Text = "OK";
            this.btnOK.Location = new WinFormsPoint(420, 365);
            this.btnOK.Size = new Size(90, 32);
            this.btnOK.Font = boldFont;
            this.btnOK.Click += BtnOK_Click;

            // Cancel Button
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new WinFormsPoint(525, 365);
            this.btnCancel.Size = new Size(90, 32);
            this.btnCancel.Font = standardFont;
            this.btnCancel.Click += BtnCancel_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                lblTitle,
                grpNaming,
                grpSelectMode,
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

        private void TxtPrefix_TextChanged(object? sender, EventArgs e)
        {
            UpdateStakeNamePreview();
        }

        private void NumStartNumber_ValueChanged(object? sender, EventArgs e)
        {
            UpdateStakeNamePreview();
        }

        private void UpdateStakeNamePreview()
        {
            string prefix = txtPrefix.Text;
            int number = (int)numStartNumber.Value;
            lblStakeName.Text = $"Tên cọc: {prefix}{number}";
        }

        private void RestoreLastUsedValues()
        {
            txtPrefix.Text = _lastPrefix;
            numStartNumber.Value = _lastStartNumber;
            chkAutoIncrement.Checked = _lastAutoIncrement;
            rbProfileView.Checked = _lastSelectOnProfileView;
            rbAlignment.Checked = !_lastSelectOnProfileView;

            // Restore sample line group selection
            if (!string.IsNullOrEmpty(_lastSampleLineGroupName))
            {
                int idx = cmbSampleLineGroup.Items.IndexOf(_lastSampleLineGroupName);
                if (idx >= 0)
                    cmbSampleLineGroup.SelectedIndex = idx;
            }

            UpdateStakeNamePreview();
        }

        private void SaveLastUsedValues()
        {
            _lastPrefix = txtPrefix.Text;
            _lastStartNumber = (int)numStartNumber.Value;
            _lastAutoIncrement = chkAutoIncrement.Checked;
            _lastSelectOnProfileView = rbProfileView.Checked;
            if (cmbSampleLineGroup.SelectedItem != null)
                _lastSampleLineGroupName = cmbSampleLineGroup.SelectedItem.ToString() ?? "";
        }

        public string GetCurrentStakeName()
        {
            return $"{Prefix}{CurrentNumber}";
        }

        public void IncrementNumber()
        {
            if (AutoIncrement)
            {
                CurrentNumber++;
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Validate
            if (cmbSampleLineGroup.SelectedIndex < 0 || _sampleLineGroupIds.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn nhóm cọc (SampleLine Group)!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Get values
            Prefix = txtPrefix.Text;
            StartNumber = (int)numStartNumber.Value;
            CurrentNumber = StartNumber;
            AutoIncrement = chkAutoIncrement.Checked;
            SelectOnProfileView = rbProfileView.Checked;
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
