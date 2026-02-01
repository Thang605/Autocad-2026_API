using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MyFirstProject
{
    public partial class ArcLabelForm : Form
    {
        /// <summary>
        /// ObjectId của Curve Label Style được chọn
        /// </summary>
        public ObjectId SelectedCurveLabelStyleId { get; private set; } = ObjectId.Null;

        /// <summary>
        /// Tỷ lệ vị trí label (0 = đầu, 0.5 = giữa, 1 = cuối)
        /// </summary>
        public double LabelRatio { get; private set; } = 0.5;

        /// <summary>
        /// Form có được chấp nhận không
        /// </summary>
        public bool FormAccepted { get; private set; } = false;

        private Dictionary<string, ObjectId> _styleDict = new Dictionary<string, ObjectId>();

        public ArcLabelForm()
        {
            InitializeComponent();
            LoadCurveLabelStyles();
        }

        /// <summary>
        /// Load danh sách General Curve Label Styles từ Civil 3D document
        /// </summary>
        private void LoadCurveLabelStyles()
        {
            try
            {
                CivilDocument civilDoc = CivilApplication.ActiveDocument;
                Database db = Application.DocumentManager.MdiActiveDocument.Database;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LabelStyleCollection curveLabelStyles = civilDoc.Styles.LabelStyles.GeneralCurveLabelStyles;

                    foreach (ObjectId styleId in curveLabelStyles)
                    {
                        LabelStyle? labelStyle = tr.GetObject(styleId, OpenMode.ForRead) as LabelStyle;
                        if (labelStyle != null)
                        {
                            _styleDict[labelStyle.Name] = styleId;
                            cmbCurveLabelStyle.Items.Add(labelStyle.Name);
                        }
                    }

                    tr.Commit();
                }

                // Chọn item đầu tiên nếu có
                if (cmbCurveLabelStyle.Items.Count > 0)
                {
                    cmbCurveLabelStyle.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load Curve Label Styles: {ex.Message}", 
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (cmbCurveLabelStyle.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một Curve Label Style.", 
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedName = cmbCurveLabelStyle.SelectedItem.ToString() ?? "";
            if (_styleDict.TryGetValue(selectedName, out ObjectId styleId))
            {
                SelectedCurveLabelStyleId = styleId;
            }

            LabelRatio = (double)numRatio.Value;
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
