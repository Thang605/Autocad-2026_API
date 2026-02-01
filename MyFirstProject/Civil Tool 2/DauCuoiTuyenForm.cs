using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MyFirstProject
{
    public partial class DauCuoiTuyenForm : Form
    {
        /// <summary>
        /// ObjectId của Alignment Label Set Style được chọn
        /// </summary>
        public ObjectId SelectedLabelSetStyleId { get; private set; } = ObjectId.Null;

        /// <summary>
        /// Danh sách các Alignment được chọn
        /// </summary>
        public List<ObjectId> SelectedAlignmentIds { get; private set; } = new List<ObjectId>();

        /// <summary>
        /// Form có được chấp nhận không
        /// </summary>
        public bool FormAccepted { get; private set; } = false;

        private Dictionary<string, ObjectId> _labelSetDict = new Dictionary<string, ObjectId>();
        private Dictionary<string, ObjectId> _alignmentDict = new Dictionary<string, ObjectId>();

        public DauCuoiTuyenForm()
        {
            InitializeComponent();
            LoadAlignmentLabelSetStyles();
            LoadAlignments();
        }

        /// <summary>
        /// Load danh sách Alignment Label Set Styles từ Civil 3D document
        /// </summary>
        private void LoadAlignmentLabelSetStyles()
        {
            try
            {
                CivilDocument civilDoc = CivilApplication.ActiveDocument;
                Database db = Application.DocumentManager.MdiActiveDocument.Database;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Alignment Label Set Styles - iterate directly
                    foreach (ObjectId styleId in civilDoc.Styles.LabelSetStyles.AlignmentLabelSetStyles)
                    {
                        AlignmentLabelSetStyle? labelSetStyle = tr.GetObject(styleId, OpenMode.ForRead) as AlignmentLabelSetStyle;
                        if (labelSetStyle != null)
                        {
                            _labelSetDict[labelSetStyle.Name] = styleId;
                            cmbLabelSetStyle.Items.Add(labelSetStyle.Name);
                        }
                    }

                    tr.Commit();
                }

                // Tự động chọn style mặc định nếu có
                SelectDefaultStyle(cmbLabelSetStyle, "Điểm đầu cuối tuyến");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load Alignment Label Set Styles: {ex.Message}", 
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Load danh sách Alignments từ Civil 3D document
        /// </summary>
        private void LoadAlignments()
        {
            try
            {
                CivilDocument civilDoc = CivilApplication.ActiveDocument;
                Database db = Application.DocumentManager.MdiActiveDocument.Database;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ObjectIdCollection alignmentIds = civilDoc.GetAlignmentIds();

                    foreach (ObjectId alignmentId in alignmentIds)
                    {
                        Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                        if (alignment != null)
                        {
                            _alignmentDict[alignment.Name] = alignmentId;
                            chkListAlignments.Items.Add(alignment.Name, true); // Mặc định chọn tất cả
                        }
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load danh sách tuyến: {ex.Message}", 
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Chọn style mặc định trong ComboBox
        /// </summary>
        private void SelectDefaultStyle(ComboBox cmb, string defaultName)
        {
            // Tìm style có tên chứa defaultName
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                string? itemName = cmb.Items[i]?.ToString();
                if (itemName != null && itemName.Contains(defaultName, StringComparison.OrdinalIgnoreCase))
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }

            // Nếu không tìm thấy, chọn item đầu tiên
            if (cmb.Items.Count > 0)
            {
                cmb.SelectedIndex = 0;
            }
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < chkListAlignments.Items.Count; i++)
            {
                chkListAlignments.SetItemChecked(i, true);
            }
        }

        private void btnDeselectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < chkListAlignments.Items.Count; i++)
            {
                chkListAlignments.SetItemChecked(i, false);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (cmbLabelSetStyle.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một Label Set Style.", 
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (chkListAlignments.CheckedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một tuyến.", 
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Lấy Label Set Style đã chọn
            string selectedName = cmbLabelSetStyle.SelectedItem.ToString() ?? "";
            if (_labelSetDict.TryGetValue(selectedName, out ObjectId styleId))
            {
                SelectedLabelSetStyleId = styleId;
            }

            // Lấy danh sách Alignments đã chọn
            SelectedAlignmentIds.Clear();
            foreach (var item in chkListAlignments.CheckedItems)
            {
                string alignmentName = item.ToString() ?? "";
                if (_alignmentDict.TryGetValue(alignmentName, out ObjectId alignmentId))
                {
                    SelectedAlignmentIds.Add(alignmentId);
                }
            }

            FormAccepted = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            FormAccepted = false;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
