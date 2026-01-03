using System;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices.Styles;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Form nhập thông số cho lệnh Phát Sinh Cọc
    /// </summary>
    public class PhatSinhCocForm : Form
    {
        // Static variables to remember last input values
        private static bool _lastPhatSinhCocH = true;
        private static int _lastKmBatDau = 0;
        private static int _lastKhoangCachChiTiet = 20;
        private static string _lastSampleLineLabelStyle = "Tên cọc";

        // Properties to return data
        public bool PhatSinhCocH { get; private set; } = true;
        public int KmBatDau { get; private set; } = 0;
        public int KhoangCachChiTiet { get; private set; } = 20;
        public string SampleLineLabelStyleName { get; private set; } = "Tên cọc";
        public bool FormAccepted { get; private set; } = false;

        // UI Controls
        private WinFormsLabel lblTitle = null!;
        private WinFormsLabel lblKmBatDau = null!;
        private WinFormsLabel lblKhoangCach = null!;
        private WinFormsLabel lblLabelStyle = null!;

        private CheckBox chkPhatSinhCocH = null!;
        private NumericUpDown numKmBatDau = null!;
        private NumericUpDown numKhoangCach = null!;
        private ComboBox cmbLabelStyle = null!;

        private Button btnOK = null!;
        private Button btnCancel = null!;
        private GroupBox grpCocH = null!;
        private GroupBox grpChiTiet = null!;

        public PhatSinhCocForm()
        {
            InitializeComponent();
            LoadLabelStyles();
            RestoreLastUsedValues();
        }

        private void InitializeComponent()
        {
            // Standard Font
            var standardFont = new WinFormsFont("Segoe UI", 10F, FontStyle.Regular);
            var boldFont = new WinFormsFont("Segoe UI", 10F, FontStyle.Bold);
            var titleFont = new WinFormsFont("Segoe UI", 14F, FontStyle.Bold);

            // Initialize controls
            this.lblTitle = new WinFormsLabel();
            this.lblKmBatDau = new WinFormsLabel();
            this.lblKhoangCach = new WinFormsLabel();
            this.lblLabelStyle = new WinFormsLabel();

            this.chkPhatSinhCocH = new CheckBox();
            this.numKmBatDau = new NumericUpDown();
            this.numKhoangCach = new NumericUpDown();
            this.cmbLabelStyle = new ComboBox();

            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.grpCocH = new GroupBox();
            this.grpChiTiet = new GroupBox();

            this.SuspendLayout();

            // Form
            this.Text = "Phát Sinh Cọc";
            this.Size = new Size(460, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = standardFont;

            // Title Label
            this.lblTitle.Text = "PHÁT SINH CỌC TỰ ĐỘNG";
            this.lblTitle.Font = titleFont;
            this.lblTitle.Location = new WinFormsPoint(20, 20);
            this.lblTitle.Size = new Size(400, 30);
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.ForeColor = Color.FromArgb(0, 102, 204);

            // Cọc H và Km Group
            this.grpCocH.Text = "Cọc H và Km";
            this.grpCocH.Font = boldFont; // Group Title Bold
            this.grpCocH.Location = new WinFormsPoint(20, 70);
            this.grpCocH.Size = new Size(405, 90);
            this.grpCocH.ForeColor = Color.Black;

            // Checkbox phát sinh cọc H
            this.chkPhatSinhCocH.Text = "Phát sinh cọc H và Km";
            this.chkPhatSinhCocH.Font = standardFont; // Content Regular
            this.chkPhatSinhCocH.Location = new WinFormsPoint(20, 25);
            this.chkPhatSinhCocH.Size = new Size(250, 25);
            this.chkPhatSinhCocH.Checked = true;
            this.chkPhatSinhCocH.CheckedChanged += ChkPhatSinhCocH_CheckedChanged;

            // Km bắt đầu
            this.lblKmBatDau.Text = "Km bắt đầu:";
            this.lblKmBatDau.Font = standardFont; // Content Regular
            this.lblKmBatDau.Location = new WinFormsPoint(20, 55);
            this.lblKmBatDau.Size = new Size(120, 25);

            this.numKmBatDau.Location = new WinFormsPoint(180, 53); // Corrected X
            this.numKmBatDau.Size = new Size(120, 25);
            this.numKmBatDau.Minimum = 0;
            this.numKmBatDau.Maximum = 999;
            this.numKmBatDau.Value = 0;
            this.numKmBatDau.Font = standardFont; // Content Regular

            // Add controls to cọc H group
            this.grpCocH.Controls.AddRange(new Control[] {
                chkPhatSinhCocH,
                lblKmBatDau, numKmBatDau
            });

            // Cọc chi tiết Group
            this.grpChiTiet.Text = "Cọc chi tiết";
            this.grpChiTiet.Font = boldFont; // Group Title Bold
            this.grpChiTiet.Location = new WinFormsPoint(20, 180);
            this.grpChiTiet.Size = new Size(405, 100);
            this.grpChiTiet.ForeColor = Color.Black;

            // Khoảng cách
            this.lblKhoangCach.Text = "Khoảng cách (m):";
            this.lblKhoangCach.Font = standardFont; // Content Regular
            this.lblKhoangCach.Location = new WinFormsPoint(20, 30);
            this.lblKhoangCach.Size = new Size(150, 25);

            this.numKhoangCach.Location = new WinFormsPoint(180, 28); // Corrected X
            this.numKhoangCach.Size = new Size(120, 25);
            this.numKhoangCach.Minimum = 5;
            this.numKhoangCach.Maximum = 100;
            this.numKhoangCach.Value = 20;
            this.numKhoangCach.Increment = 5;
            this.numKhoangCach.Font = standardFont; // Content Regular

            // Label Style
            this.lblLabelStyle.Text = "Label Style:";
            this.lblLabelStyle.Font = standardFont; // Content Regular
            this.lblLabelStyle.Location = new WinFormsPoint(20, 60);
            this.lblLabelStyle.Size = new Size(150, 25);

            this.cmbLabelStyle.Location = new WinFormsPoint(180, 58); // Corrected X
            this.cmbLabelStyle.Size = new Size(200, 25); // Adjusted width
            this.cmbLabelStyle.DropDownStyle = ComboBoxStyle.DropDown;
            this.cmbLabelStyle.Font = standardFont; // Content Regular

            // Add controls to chi tiết group
            this.grpChiTiet.Controls.AddRange(new Control[] {
                lblKhoangCach, numKhoangCach,
                lblLabelStyle, cmbLabelStyle
            });

            // OK Button
            this.btnOK.Text = "OK";
            this.btnOK.Location = new WinFormsPoint(230, 300);
            this.btnOK.Size = new Size(90, 32);
            this.btnOK.Font = boldFont;
            this.btnOK.Click += BtnOK_Click;

            // Cancel Button
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new WinFormsPoint(335, 300);
            this.btnCancel.Size = new Size(90, 32);
            this.btnCancel.Font = standardFont;
            this.btnCancel.Click += BtnCancel_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                lblTitle,
                grpCocH,
                grpChiTiet,
                btnOK,
                btnCancel
            });

            this.ResumeLayout(false);
        }

        private void LoadLabelStyles()
        {
            cmbLabelStyle.Items.Clear();
            try
            {
                var civilDoc = CivilApplication.ActiveDocument;

                foreach (ObjectId styleId in civilDoc.Styles.LabelStyles.SampleLineLabelStyles.LabelStyles)
                {
                    using (var tr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var style = tr.GetObject(styleId, OpenMode.ForRead) as LabelStyle;
                        if (style != null)
                        {
                            cmbLabelStyle.Items.Add(style.Name);
                        }
                        tr.Commit();
                    }
                }

                // Set default
                if (cmbLabelStyle.Items.Count > 0)
                {
                    int idx = cmbLabelStyle.Items.IndexOf("Tên cọc");
                    if (idx >= 0)
                        cmbLabelStyle.SelectedIndex = idx;
                    else
                        cmbLabelStyle.SelectedIndex = 0;
                }
            }
            catch (System.Exception ex)
            {
                // Fallback - add default item
                cmbLabelStyle.Items.Add("Tên cọc");
                cmbLabelStyle.SelectedIndex = 0;
            }
        }

        private void ChkPhatSinhCocH_CheckedChanged(object? sender, EventArgs e)
        {
            numKmBatDau.Enabled = chkPhatSinhCocH.Checked;
            lblKmBatDau.Enabled = chkPhatSinhCocH.Checked;
        }

        private void RestoreLastUsedValues()
        {
            chkPhatSinhCocH.Checked = _lastPhatSinhCocH;
            numKmBatDau.Value = _lastKmBatDau;
            numKhoangCach.Value = _lastKhoangCachChiTiet;

            // Restore label style
            if (!string.IsNullOrEmpty(_lastSampleLineLabelStyle))
            {
                int idx = cmbLabelStyle.Items.IndexOf(_lastSampleLineLabelStyle);
                if (idx >= 0)
                    cmbLabelStyle.SelectedIndex = idx;
                else
                    cmbLabelStyle.Text = _lastSampleLineLabelStyle;
            }

            // Update enabled state
            numKmBatDau.Enabled = chkPhatSinhCocH.Checked;
            lblKmBatDau.Enabled = chkPhatSinhCocH.Checked;
        }

        private void SaveLastUsedValues()
        {
            _lastPhatSinhCocH = chkPhatSinhCocH.Checked;
            _lastKmBatDau = (int)numKmBatDau.Value;
            _lastKhoangCachChiTiet = (int)numKhoangCach.Value;
            _lastSampleLineLabelStyle = cmbLabelStyle.Text;
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Get values
            PhatSinhCocH = chkPhatSinhCocH.Checked;
            KmBatDau = (int)numKmBatDau.Value;
            KhoangCachChiTiet = (int)numKhoangCach.Value;
            SampleLineLabelStyleName = cmbLabelStyle.Text;

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
