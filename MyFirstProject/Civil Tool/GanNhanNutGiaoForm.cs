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

namespace MyFirstProject.Civil_Tool
{
    /// <summary>
    /// Form nhập thông số cho lệnh Gắn Nhãn Nút Giao Lên Trắc Dọc
    /// Cho phép chọn nhiều ProfileView cùng lúc
    /// </summary>
    public class GanNhanNutGiaoForm : Form
    {
        // Static variables to remember last input values
        private static string _lastPointGroupName = "";
        private static double _lastSaiSo = 0.02;
        private static List<string> _lastSelectedProfileViews = new List<string>();

        // Properties to return data
        public ObjectId SelectedPointGroupId { get; private set; } = ObjectId.Null;
        public double SaiSo { get; private set; } = 0.02;
        public List<ObjectId> SelectedProfileViewIds { get; private set; } = new List<ObjectId>();
        public bool FormAccepted { get; private set; } = false;

        // Info classes
        private class PointGroupInfo
        {
            public string Name { get; set; } = "";
            public ObjectId Id { get; set; }
            public override string ToString() => Name;
        }

        private class ProfileViewInfo
        {
            public string Name { get; set; } = "";
            public ObjectId Id { get; set; }
            public override string ToString() => Name;
        }

        // UI Controls
        private WinFormsLabel lblTitle = null!;
        private WinFormsLabel lblPointGroup = null!;
        private WinFormsLabel lblSaiSo = null!;
        private WinFormsLabel lblProfileViews = null!;
        private WinFormsLabel lblCount = null!;

        private ComboBox cmbPointGroup = null!;
        private NumericUpDown numSaiSo = null!;
        private ListBox lstProfileViews = null!;

        private Button btnSelectAll = null!;
        private Button btnDeselectAll = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;
        private GroupBox grpPointGroup = null!;
        private GroupBox grpProfileViews = null!;

        public GanNhanNutGiaoForm()
        {
            InitializeComponent();
            LoadPointGroups();
            LoadProfileViews();
            RestoreLastUsedValues();
            UpdateSelectedCount();
        }

        private void InitializeComponent()
        {
            // Initialize controls
            this.lblTitle = new WinFormsLabel();
            this.lblPointGroup = new WinFormsLabel();
            this.lblSaiSo = new WinFormsLabel();
            this.lblProfileViews = new WinFormsLabel();
            this.lblCount = new WinFormsLabel();

            this.cmbPointGroup = new ComboBox();
            this.numSaiSo = new NumericUpDown();
            this.lstProfileViews = new ListBox();

            this.btnSelectAll = new Button();
            this.btnDeselectAll = new Button();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.grpPointGroup = new GroupBox();
            this.grpProfileViews = new GroupBox();

            this.SuspendLayout();

            // Form
            this.Text = "Gắn Nhãn Nút Giao Lên Trắc Dọc";
            this.Size = new Size(500, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Title Label
            this.lblTitle.Text = "GẮN NHÃN NÚT GIAO LÊN TRẮC DỌC";
            this.lblTitle.Font = new WinFormsFont("Microsoft Sans Serif", 11F, FontStyle.Bold);
            this.lblTitle.Location = new WinFormsPoint(20, 15);
            this.lblTitle.Size = new Size(450, 25);
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.ForeColor = Color.DarkBlue;

            // Point Group GroupBox
            this.grpPointGroup.Text = "Cài đặt Point Group và Sai số";
            this.grpPointGroup.Location = new WinFormsPoint(12, 50);
            this.grpPointGroup.Size = new Size(460, 100);

            // Point Group selection
            this.lblPointGroup.Text = "Chọn Point Group:";
            this.lblPointGroup.Location = new WinFormsPoint(15, 30);
            this.lblPointGroup.Size = new Size(120, 23);

            this.cmbPointGroup.Location = new WinFormsPoint(140, 28);
            this.cmbPointGroup.Size = new Size(300, 23);
            this.cmbPointGroup.DropDownStyle = ComboBoxStyle.DropDownList;

            // Sai số
            this.lblSaiSo.Text = "Sai số (m):";
            this.lblSaiSo.Location = new WinFormsPoint(15, 65);
            this.lblSaiSo.Size = new Size(120, 23);

            this.numSaiSo.Location = new WinFormsPoint(140, 63);
            this.numSaiSo.Size = new Size(120, 23);
            this.numSaiSo.Minimum = 0.001M;
            this.numSaiSo.Maximum = 10M;
            this.numSaiSo.Value = 0.02M;
            this.numSaiSo.DecimalPlaces = 3;
            this.numSaiSo.Increment = 0.01M;

            // Add controls to point group groupbox
            this.grpPointGroup.Controls.AddRange(new Control[] {
                lblPointGroup, cmbPointGroup,
                lblSaiSo, numSaiSo
            });

            // ProfileViews GroupBox
            this.grpProfileViews.Text = "Chọn các ProfileView cần gắn nhãn";
            this.grpProfileViews.Location = new WinFormsPoint(12, 160);
            this.grpProfileViews.Size = new Size(460, 260);

            // ProfileViews label
            this.lblProfileViews.Text = "Danh sách ProfileView (giữ Ctrl để chọn nhiều):";
            this.lblProfileViews.Location = new WinFormsPoint(15, 25);
            this.lblProfileViews.Size = new Size(300, 23);

            // ProfileViews listbox - Multi-select
            this.lstProfileViews.Location = new WinFormsPoint(15, 50);
            this.lstProfileViews.Size = new Size(430, 160);
            this.lstProfileViews.SelectionMode = SelectionMode.MultiExtended;
            this.lstProfileViews.SelectedIndexChanged += LstProfileViews_SelectedIndexChanged;

            // Selected count label
            this.lblCount.Text = "Đã chọn: 0 ProfileView";
            this.lblCount.Location = new WinFormsPoint(15, 215);
            this.lblCount.Size = new Size(200, 23);
            this.lblCount.ForeColor = Color.DarkGreen;

            // Select All button
            this.btnSelectAll.Text = "Chọn tất cả";
            this.btnSelectAll.Location = new WinFormsPoint(250, 215);
            this.btnSelectAll.Size = new Size(90, 28);
            this.btnSelectAll.Click += BtnSelectAll_Click;

            // Deselect All button
            this.btnDeselectAll.Text = "Bỏ chọn";
            this.btnDeselectAll.Location = new WinFormsPoint(350, 215);
            this.btnDeselectAll.Size = new Size(90, 28);
            this.btnDeselectAll.Click += BtnDeselectAll_Click;

            // Add controls to profile views groupbox
            this.grpProfileViews.Controls.AddRange(new Control[] {
                lblProfileViews, lstProfileViews,
                lblCount, btnSelectAll, btnDeselectAll
            });

            // OK Button
            this.btnOK.Text = "OK";
            this.btnOK.Location = new WinFormsPoint(280, 435);
            this.btnOK.Size = new Size(90, 35);
            this.btnOK.Font = new WinFormsFont("Microsoft Sans Serif", 9F, FontStyle.Bold);
            this.btnOK.Click += BtnOK_Click;

            // Cancel Button
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new WinFormsPoint(380, 435);
            this.btnCancel.Size = new Size(90, 35);
            this.btnCancel.Click += BtnCancel_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                lblTitle,
                grpPointGroup,
                grpProfileViews,
                btnOK,
                btnCancel
            });

            this.ResumeLayout(false);
        }

        private void LoadPointGroups()
        {
            cmbPointGroup.Items.Clear();
            try
            {
                var civilDoc = CivilApplication.ActiveDocument;
                var pointGroupIds = civilDoc.PointGroups;

                using (var tr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    foreach (ObjectId pointGroupId in pointGroupIds)
                    {
                        var pointGroup = tr.GetObject(pointGroupId, OpenMode.ForRead) as PointGroup;
                        if (pointGroup != null)
                        {
                            var info = new PointGroupInfo
                            {
                                Name = pointGroup.Name,
                                Id = pointGroup.Id
                            };
                            cmbPointGroup.Items.Add(info);
                        }
                    }
                    tr.Commit();
                }

                if (cmbPointGroup.Items.Count > 0)
                {
                    cmbPointGroup.SelectedIndex = 0;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách Point Group: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadProfileViews()
        {
            lstProfileViews.Items.Clear();
            try
            {
                var civilDoc = CivilApplication.ActiveDocument;

                using (var tr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    // Get all alignments and their profile views
                    var alignmentIds = civilDoc.GetAlignmentIds();
                    foreach (ObjectId alignmentId in alignmentIds)
                    {
                        var alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                        if (alignment != null)
                        {
                            var profileViewIds = alignment.GetProfileViewIds();
                            foreach (ObjectId profileViewId in profileViewIds)
                            {
                                var profileView = tr.GetObject(profileViewId, OpenMode.ForRead) as ProfileView;
                                if (profileView != null)
                                {
                                    var info = new ProfileViewInfo
                                    {
                                        Name = $"{profileView.Name} ({alignment.Name})",
                                        Id = profileView.Id
                                    };
                                    lstProfileViews.Items.Add(info);
                                }
                            }
                        }
                    }
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách ProfileView: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestoreLastUsedValues()
        {
            // Restore sai so
            try
            {
                numSaiSo.Value = (decimal)_lastSaiSo;
            }
            catch
            {
                numSaiSo.Value = 0.02M;
            }

            // Restore Point Group selection
            if (!string.IsNullOrEmpty(_lastPointGroupName))
            {
                for (int i = 0; i < cmbPointGroup.Items.Count; i++)
                {
                    if (cmbPointGroup.Items[i].ToString() == _lastPointGroupName)
                    {
                        cmbPointGroup.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Restore ProfileView selection
            if (_lastSelectedProfileViews.Count > 0)
            {
                for (int i = 0; i < lstProfileViews.Items.Count; i++)
                {
                    if (_lastSelectedProfileViews.Contains(lstProfileViews.Items[i].ToString() ?? ""))
                    {
                        lstProfileViews.SetSelected(i, true);
                    }
                }
            }
        }

        private void SaveLastUsedValues()
        {
            _lastSaiSo = (double)numSaiSo.Value;

            if (cmbPointGroup.SelectedItem is PointGroupInfo selected)
            {
                _lastPointGroupName = selected.Name;
            }

            _lastSelectedProfileViews.Clear();
            foreach (var item in lstProfileViews.SelectedItems)
            {
                _lastSelectedProfileViews.Add(item.ToString() ?? "");
            }
        }

        private void UpdateSelectedCount()
        {
            lblCount.Text = $"Đã chọn: {lstProfileViews.SelectedItems.Count} ProfileView";
        }

        private void LstProfileViews_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateSelectedCount();
        }

        private void BtnSelectAll_Click(object? sender, EventArgs e)
        {
            for (int i = 0; i < lstProfileViews.Items.Count; i++)
            {
                lstProfileViews.SetSelected(i, true);
            }
            UpdateSelectedCount();
        }

        private void BtnDeselectAll_Click(object? sender, EventArgs e)
        {
            lstProfileViews.ClearSelected();
            UpdateSelectedCount();
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Validate Point Group selection
            if (cmbPointGroup.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn Point Group.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbPointGroup.Focus();
                return;
            }

            // Validate ProfileView selection
            if (lstProfileViews.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một ProfileView.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lstProfileViews.Focus();
                return;
            }

            // Get values
            var selectedPointGroup = (PointGroupInfo)cmbPointGroup.SelectedItem;
            SelectedPointGroupId = selectedPointGroup.Id;
            SaiSo = (double)numSaiSo.Value;

            SelectedProfileViewIds.Clear();
            foreach (var item in lstProfileViews.SelectedItems)
            {
                if (item is ProfileViewInfo pvInfo)
                {
                    SelectedProfileViewIds.Add(pvInfo.Id);
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
