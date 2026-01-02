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
            this.Size = new Size(420, 320);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Title Label
            this.lblTitle.Text = "PHÁT SINH CỌC TỰ ĐỘNG";
            this.lblTitle.Font = new WinFormsFont("Microsoft Sans Serif", 11F, FontStyle.Bold);
            this.lblTitle.Location = new WinFormsPoint(20, 15);
            this.lblTitle.Size = new Size(370, 25);
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.ForeColor = Color.DarkBlue;

            // Cọc H và Km Group
            this.grpCocH.Text = "Cọc H và Km";
            this.grpCocH.Location = new WinFormsPoint(12, 50);
            this.grpCocH.Size = new Size(380, 80);

            // Checkbox phát sinh cọc H
            this.chkPhatSinhCocH.Text = "Phát sinh cọc H và Km";
            this.chkPhatSinhCocH.Location = new WinFormsPoint(15, 25);
            this.chkPhatSinhCocH.Size = new Size(200, 23);
            this.chkPhatSinhCocH.Checked = true;
            this.chkPhatSinhCocH.CheckedChanged += ChkPhatSinhCocH_CheckedChanged;

            // Km bắt đầu
            this.lblKmBatDau.Text = "Km bắt đầu:";
            this.lblKmBatDau.Location = new WinFormsPoint(15, 52);
            this.lblKmBatDau.Size = new Size(100, 23);

            this.numKmBatDau.Location = new WinFormsPoint(120, 50);
            this.numKmBatDau.Size = new Size(100, 23);
            this.numKmBatDau.Minimum = 0;
            this.numKmBatDau.Maximum = 999;
            this.numKmBatDau.Value = 0;

            // Add controls to cọc H group
            this.grpCocH.Controls.AddRange(new Control[] {
                chkPhatSinhCocH,
                lblKmBatDau, numKmBatDau
            });

            // Cọc chi tiết Group
            this.grpChiTiet.Text = "Cọc chi tiết";
            this.grpChiTiet.Location = new WinFormsPoint(12, 140);
            this.grpChiTiet.Size = new Size(380, 80);

            // Khoảng cách
            this.lblKhoangCach.Text = "Khoảng cách (m):";
            this.lblKhoangCach.Location = new WinFormsPoint(15, 25);
            this.lblKhoangCach.Size = new Size(120, 23);

            this.numKhoangCach.Location = new WinFormsPoint(140, 23);
            this.numKhoangCach.Size = new Size(100, 23);
            this.numKhoangCach.Minimum = 5;
            this.numKhoangCach.Maximum = 100;
            this.numKhoangCach.Value = 20;
            this.numKhoangCach.Increment = 5;

            // Label Style
            this.lblLabelStyle.Text = "Label Style:";
            this.lblLabelStyle.Location = new WinFormsPoint(15, 52);
            this.lblLabelStyle.Size = new Size(120, 23);

            this.cmbLabelStyle.Location = new WinFormsPoint(140, 50);
            this.cmbLabelStyle.Size = new Size(220, 23);
            this.cmbLabelStyle.DropDownStyle = ComboBoxStyle.DropDown;

            // Add controls to chi tiết group
            this.grpChiTiet.Controls.AddRange(new Control[] {
                lblKhoangCach, numKhoangCach,
                lblLabelStyle, cmbLabelStyle
            });

            // OK Button
            this.btnOK.Text = "OK";
            this.btnOK.Location = new WinFormsPoint(200, 235);
            this.btnOK.Size = new Size(90, 30);
            this.btnOK.Font = new WinFormsFont("Microsoft Sans Serif", 9F, FontStyle.Bold);
            this.btnOK.Click += BtnOK_Click;

            // Cancel Button
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new WinFormsPoint(300, 235);
            this.btnCancel.Size = new Size(90, 30);
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
