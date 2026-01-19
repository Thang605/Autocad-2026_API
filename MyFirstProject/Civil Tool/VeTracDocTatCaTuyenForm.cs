using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsFont = System.Drawing.Font;
using WinFormsPoint = System.Drawing.Point;
using CivSurface = Autodesk.Civil.DatabaseServices.TinSurface;

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Form nhập thông số cho lệnh Vẽ Trắc Dọc Tự Nhiên Tất Cả Tuyến
    /// </summary>
    public class VeTracDocTatCaTuyenForm : Form
    {
        // Static variables to remember last input values
        private static int _lastKhoangCach = 300;
        private static string _lastProfileStyle = "0.TN";
        private static string _lastProfileLabelSet = "_No Labels";
        private static string _lastProfileViewStyle = "TDTN GT 1-1000";
        private static string _lastProfileViewBandSet = "TRẮC DỌC DO THI";
        private static string _lastSurfaceName = "";
        private static List<string> _lastSelectedAlignments = new List<string>();

        // Properties to return data
        public ObjectId SelectedSurfaceId { get; private set; } = ObjectId.Null;
        public int KhoangCach { get; private set; } = 300;
        public string ProfileStyleName { get; private set; } = "0.TN";
        public string ProfileLabelSetName { get; private set; } = "_No Labels";
        public string ProfileViewStyleName { get; private set; } = "TDTN GT 1-1000";
        public string ProfileViewBandSetName { get; private set; } = "TRẮC DỌC DO THI";
        public bool FormAccepted { get; private set; } = false;
        public List<ObjectId> SelectedAlignmentIds { get; private set; } = new List<ObjectId>();

        // Alignment info class
        private class AlignmentInfo
        {
            public string Name { get; set; } = "";
            public ObjectId Id { get; set; }
            public override string ToString() => Name;
        }

        // Surface info class
        private class SurfaceInfo
        {
            public string Name { get; set; } = "";
            public ObjectId Id { get; set; }
            public override string ToString() => Name;
        }

        // UI Controls
        private WinFormsLabel lblTitle = null!;
        private WinFormsLabel lblSurface = null!;
        private WinFormsLabel lblKhoangCach = null!;
        private WinFormsLabel lblProfileStyle = null!;
        private WinFormsLabel lblProfileLabelSet = null!;
        private WinFormsLabel lblProfileViewStyle = null!;
        private WinFormsLabel lblProfileViewBandSet = null!;
        private WinFormsLabel lblAlignments = null!;

        private ComboBox cmbSurface = null!;
        private NumericUpDown numKhoangCach = null!;
        private ComboBox cmbProfileStyle = null!;
        private ComboBox cmbProfileLabelSet = null!;
        private ComboBox cmbProfileViewStyle = null!;
        private ComboBox cmbProfileViewBandSet = null!;
        private CheckedListBox chkListAlignments = null!;

        private Button btnOK = null!;
        private Button btnCancel = null!;
        private Button btnSelectAll = null!;
        private Button btnDeselectAll = null!;
        private GroupBox grpSurface = null!;
        private GroupBox grpStyles = null!;
        private GroupBox grpAlignments = null!;

        public VeTracDocTatCaTuyenForm()
        {
            InitializeComponent();
            LoadSurfaces();
            LoadAlignments();
            LoadStyles();
            RestoreLastUsedValues();
        }

        private void InitializeComponent()
        {
            // Initialize controls
            this.lblTitle = new WinFormsLabel();
            this.lblSurface = new WinFormsLabel();
            this.lblKhoangCach = new WinFormsLabel();
            this.lblProfileStyle = new WinFormsLabel();
            this.lblProfileLabelSet = new WinFormsLabel();
            this.lblProfileViewStyle = new WinFormsLabel();
            this.lblProfileViewBandSet = new WinFormsLabel();
            this.lblAlignments = new WinFormsLabel();

            this.cmbSurface = new ComboBox();
            this.numKhoangCach = new NumericUpDown();
            this.cmbProfileStyle = new ComboBox();
            this.cmbProfileLabelSet = new ComboBox();
            this.cmbProfileViewStyle = new ComboBox();
            this.cmbProfileViewBandSet = new ComboBox();
            this.chkListAlignments = new CheckedListBox();

            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.btnSelectAll = new Button();
            this.btnDeselectAll = new Button();
            this.grpSurface = new GroupBox();
            this.grpStyles = new GroupBox();
            this.grpAlignments = new GroupBox();

            this.SuspendLayout();

            // Form - tăng kích thước để hiển thị danh sách alignments
            this.Text = "Vẽ Trắc Dọc Tự Nhiên - Tất Cả Tuyến";
            this.Size = new Size(700, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Title Label
            this.lblTitle.Text = "VẼ TRẮC DỌC TỰ NHIÊN - TẤT CẢ TUYẾN";
            this.lblTitle.Font = new WinFormsFont("Microsoft Sans Serif", 11F, FontStyle.Bold);
            this.lblTitle.Location = new WinFormsPoint(20, 15);
            this.lblTitle.Size = new Size(650, 25);
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.ForeColor = Color.DarkBlue;

            // Surface Group
            this.grpSurface.Text = "Cài đặt Surface và Khoảng cách";
            this.grpSurface.Location = new WinFormsPoint(12, 50);
            this.grpSurface.Size = new Size(330, 100);

            // Surface selection
            this.lblSurface.Text = "Chọn Surface:";
            this.lblSurface.Location = new WinFormsPoint(15, 30);
            this.lblSurface.Size = new Size(100, 23);

            this.cmbSurface.Location = new WinFormsPoint(115, 28);
            this.cmbSurface.Size = new Size(200, 23);
            this.cmbSurface.DropDownStyle = ComboBoxStyle.DropDownList;

            // Khoảng cách
            this.lblKhoangCach.Text = "Khoảng cách (mm):";
            this.lblKhoangCach.Location = new WinFormsPoint(15, 65);
            this.lblKhoangCach.Size = new Size(100, 23);

            this.numKhoangCach.Location = new WinFormsPoint(115, 63);
            this.numKhoangCach.Size = new Size(100, 23);
            this.numKhoangCach.Minimum = 50;
            this.numKhoangCach.Maximum = 2000;
            this.numKhoangCach.Value = 300;
            this.numKhoangCach.Increment = 50;

            // Add controls to surface group
            this.grpSurface.Controls.AddRange(new Control[] {
                lblSurface, cmbSurface,
                lblKhoangCach, numKhoangCach
            });

            // Alignments Group
            this.grpAlignments.Text = "Danh sách tim đường (Centerline)";
            this.grpAlignments.Location = new WinFormsPoint(350, 50);
            this.grpAlignments.Size = new Size(320, 330);

            // Alignments list header label
            this.lblAlignments.Text = "Chọn các tuyến cần vẽ trắc dọc:";
            this.lblAlignments.Location = new WinFormsPoint(10, 20);
            this.lblAlignments.Size = new Size(200, 20);

            // Alignments CheckedListBox
            this.chkListAlignments.Location = new WinFormsPoint(10, 45);
            this.chkListAlignments.Size = new Size(300, 240);
            this.chkListAlignments.CheckOnClick = true;

            // Select All Button
            this.btnSelectAll.Text = "Chọn tất cả";
            this.btnSelectAll.Location = new WinFormsPoint(10, 290);
            this.btnSelectAll.Size = new Size(90, 28);
            this.btnSelectAll.Click += BtnSelectAll_Click;

            // Deselect All Button
            this.btnDeselectAll.Text = "Bỏ chọn tất cả";
            this.btnDeselectAll.Location = new WinFormsPoint(105, 290);
            this.btnDeselectAll.Size = new Size(100, 28);
            this.btnDeselectAll.Click += BtnDeselectAll_Click;

            // Add controls to alignments group
            this.grpAlignments.Controls.AddRange(new Control[] {
                lblAlignments, chkListAlignments, btnSelectAll, btnDeselectAll
            });

            // Styles Group
            this.grpStyles.Text = "Cài đặt Styles";
            this.grpStyles.Location = new WinFormsPoint(12, 160);
            this.grpStyles.Size = new Size(330, 170);

            // Profile Style
            this.lblProfileStyle.Text = "Profile Style:";
            this.lblProfileStyle.Location = new WinFormsPoint(15, 30);
            this.lblProfileStyle.Size = new Size(100, 23);

            this.cmbProfileStyle.Location = new WinFormsPoint(115, 28);
            this.cmbProfileStyle.Size = new Size(200, 23);
            this.cmbProfileStyle.DropDownStyle = ComboBoxStyle.DropDown;

            // Profile Label Set
            this.lblProfileLabelSet.Text = "Profile Label Set:";
            this.lblProfileLabelSet.Location = new WinFormsPoint(15, 60);
            this.lblProfileLabelSet.Size = new Size(100, 23);

            this.cmbProfileLabelSet.Location = new WinFormsPoint(115, 58);
            this.cmbProfileLabelSet.Size = new Size(200, 23);
            this.cmbProfileLabelSet.DropDownStyle = ComboBoxStyle.DropDown;

            // ProfileView Style
            this.lblProfileViewStyle.Text = "ProfileView Style:";
            this.lblProfileViewStyle.Location = new WinFormsPoint(15, 95);
            this.lblProfileViewStyle.Size = new Size(100, 23);

            this.cmbProfileViewStyle.Location = new WinFormsPoint(115, 93);
            this.cmbProfileViewStyle.Size = new Size(200, 23);
            this.cmbProfileViewStyle.DropDownStyle = ComboBoxStyle.DropDown;

            // ProfileView Band Set
            this.lblProfileViewBandSet.Text = "ProfileView Band:";
            this.lblProfileViewBandSet.Location = new WinFormsPoint(15, 130);
            this.lblProfileViewBandSet.Size = new Size(100, 23);

            this.cmbProfileViewBandSet.Location = new WinFormsPoint(115, 128);
            this.cmbProfileViewBandSet.Size = new Size(200, 23);
            this.cmbProfileViewBandSet.DropDownStyle = ComboBoxStyle.DropDown;

            // Add controls to styles group
            this.grpStyles.Controls.AddRange(new Control[] {
                lblProfileStyle, cmbProfileStyle,
                lblProfileLabelSet, cmbProfileLabelSet,
                lblProfileViewStyle, cmbProfileViewStyle,
                lblProfileViewBandSet, cmbProfileViewBandSet
            });

            // OK Button
            this.btnOK.Text = "OK";
            this.btnOK.Location = new WinFormsPoint(480, 500);
            this.btnOK.Size = new Size(90, 30);
            this.btnOK.Font = new WinFormsFont("Microsoft Sans Serif", 9F, FontStyle.Bold);
            this.btnOK.Click += BtnOK_Click;

            // Cancel Button
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new WinFormsPoint(580, 500);
            this.btnCancel.Size = new Size(90, 30);
            this.btnCancel.Click += BtnCancel_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                lblTitle,
                grpSurface,
                grpAlignments,
                grpStyles,
                btnOK,
                btnCancel
            });

            this.ResumeLayout(false);
        }

        private void LoadSurfaces()
        {
            cmbSurface.Items.Clear();
            try
            {
                var civilDoc = CivilApplication.ActiveDocument;
                var surfaceIds = civilDoc.GetSurfaceIds();

                using (var tr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    foreach (ObjectId surfaceId in surfaceIds)
                    {
                        var surface = tr.GetObject(surfaceId, OpenMode.ForRead) as CivSurface;
                        if (surface != null)
                        {
                            var surfaceInfo = new SurfaceInfo
                            {
                                Name = surface.Name,
                                Id = surface.Id
                            };
                            cmbSurface.Items.Add(surfaceInfo);
                        }
                    }
                    tr.Commit();
                }

                if (cmbSurface.Items.Count > 0)
                {
                    cmbSurface.SelectedIndex = 0;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách surface: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAlignments()
        {
            chkListAlignments.Items.Clear();
            try
            {
                var civilDoc = CivilApplication.ActiveDocument;
                var alignmentIds = civilDoc.GetAlignmentIds();

                var alignmentList = new List<AlignmentInfo>();

                using (var tr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    foreach (ObjectId alignmentId in alignmentIds)
                    {
                        var alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                        if (alignment != null && alignment.AlignmentType == AlignmentType.Centerline)
                        {
                            alignmentList.Add(new AlignmentInfo
                            {
                                Name = alignment.Name,
                                Id = alignment.Id
                            });
                        }
                    }
                    tr.Commit();
                }

                // Sort by name
                alignmentList = alignmentList.OrderBy(a => a.Name).ToList();

                // Add to CheckedListBox
                foreach (var alignmentInfo in alignmentList)
                {
                    chkListAlignments.Items.Add(alignmentInfo, true); // Default checked
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách tim đường: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSelectAll_Click(object? sender, EventArgs e)
        {
            for (int i = 0; i < chkListAlignments.Items.Count; i++)
            {
                chkListAlignments.SetItemChecked(i, true);
            }
        }

        private void BtnDeselectAll_Click(object? sender, EventArgs e)
        {
            for (int i = 0; i < chkListAlignments.Items.Count; i++)
            {
                chkListAlignments.SetItemChecked(i, false);
            }
        }

        private void LoadStyles()
        {
            try
            {
                var civilDoc = CivilApplication.ActiveDocument;

                // Load Profile Styles
                cmbProfileStyle.Items.Clear();
                foreach (ObjectId styleId in civilDoc.Styles.ProfileStyles)
                {
                    using (var tr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var style = tr.GetObject(styleId, OpenMode.ForRead) as ProfileStyle;
                        if (style != null)
                        {
                            cmbProfileStyle.Items.Add(style.Name);
                        }
                        tr.Commit();
                    }
                }

                // Load Profile Label Set Styles
                cmbProfileLabelSet.Items.Clear();
                foreach (ObjectId styleId in civilDoc.Styles.LabelSetStyles.ProfileLabelSetStyles)
                {
                    using (var tr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var style = tr.GetObject(styleId, OpenMode.ForRead) as ProfileLabelSetStyle;
                        if (style != null)
                        {
                            cmbProfileLabelSet.Items.Add(style.Name);
                        }
                        tr.Commit();
                    }
                }

                // Load ProfileView Styles
                cmbProfileViewStyle.Items.Clear();
                foreach (ObjectId styleId in civilDoc.Styles.ProfileViewStyles)
                {
                    using (var tr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var style = tr.GetObject(styleId, OpenMode.ForRead) as ProfileViewStyle;
                        if (style != null)
                        {
                            cmbProfileViewStyle.Items.Add(style.Name);
                        }
                        tr.Commit();
                    }
                }

                // Load ProfileView Band Set Styles
                cmbProfileViewBandSet.Items.Clear();
                foreach (ObjectId styleId in civilDoc.Styles.ProfileViewBandSetStyles)
                {
                    using (var tr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var style = tr.GetObject(styleId, OpenMode.ForRead) as ProfileViewBandSetStyle;
                        if (style != null)
                        {
                            cmbProfileViewBandSet.Items.Add(style.Name);
                        }
                        tr.Commit();
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải styles: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RestoreLastUsedValues()
        {
            numKhoangCach.Value = _lastKhoangCach;

            // Restore surface selection
            if (!string.IsNullOrEmpty(_lastSurfaceName))
            {
                for (int i = 0; i < cmbSurface.Items.Count; i++)
                {
                    if (cmbSurface.Items[i].ToString() == _lastSurfaceName)
                    {
                        cmbSurface.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Restore styles
            RestoreComboBoxValue(cmbProfileStyle, _lastProfileStyle);
            RestoreComboBoxValue(cmbProfileLabelSet, _lastProfileLabelSet);
            RestoreComboBoxValue(cmbProfileViewStyle, _lastProfileViewStyle);
            RestoreComboBoxValue(cmbProfileViewBandSet, _lastProfileViewBandSet);

            // Restore alignment selection (if previously saved)
            if (_lastSelectedAlignments.Count > 0)
            {
                for (int i = 0; i < chkListAlignments.Items.Count; i++)
                {
                    var item = chkListAlignments.Items[i] as AlignmentInfo;
                    if (item != null)
                    {
                        bool shouldBeChecked = _lastSelectedAlignments.Contains(item.Name);
                        chkListAlignments.SetItemChecked(i, shouldBeChecked);
                    }
                }
            }
        }

        private void RestoreComboBoxValue(ComboBox cmb, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            int idx = cmb.Items.IndexOf(value);
            if (idx >= 0)
            {
                cmb.SelectedIndex = idx;
            }
            else
            {
                cmb.Text = value;
            }
        }

        private void SaveLastUsedValues()
        {
            _lastKhoangCach = (int)numKhoangCach.Value;
            _lastProfileStyle = cmbProfileStyle.Text;
            _lastProfileLabelSet = cmbProfileLabelSet.Text;
            _lastProfileViewStyle = cmbProfileViewStyle.Text;
            _lastProfileViewBandSet = cmbProfileViewBandSet.Text;

            if (cmbSurface.SelectedItem is SurfaceInfo selected)
            {
                _lastSurfaceName = selected.Name;
            }

            // Save selected alignments
            _lastSelectedAlignments.Clear();
            foreach (var item in chkListAlignments.CheckedItems)
            {
                if (item is AlignmentInfo alignmentInfo)
                {
                    _lastSelectedAlignments.Add(alignmentInfo.Name);
                }
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Validate surface selection
            if (cmbSurface.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn Surface.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbSurface.Focus();
                return;
            }

            // Validate styles
            if (string.IsNullOrWhiteSpace(cmbProfileStyle.Text))
            {
                MessageBox.Show("Vui lòng chọn Profile Style.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbProfileStyle.Focus();
                return;
            }

            // Validate alignment selection
            if (chkListAlignments.CheckedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một tim đường.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                chkListAlignments.Focus();
                return;
            }

            // Get values
            var selectedSurface = (SurfaceInfo)cmbSurface.SelectedItem;
            SelectedSurfaceId = selectedSurface.Id;
            KhoangCach = (int)numKhoangCach.Value;
            ProfileStyleName = cmbProfileStyle.Text;
            ProfileLabelSetName = cmbProfileLabelSet.Text;
            ProfileViewStyleName = cmbProfileViewStyle.Text;
            ProfileViewBandSetName = cmbProfileViewBandSet.Text;

            // Get selected alignments
            SelectedAlignmentIds.Clear();
            foreach (var item in chkListAlignments.CheckedItems)
            {
                if (item is AlignmentInfo alignmentInfo)
                {
                    SelectedAlignmentIds.Add(alignmentInfo.Id);
                }
            }

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
