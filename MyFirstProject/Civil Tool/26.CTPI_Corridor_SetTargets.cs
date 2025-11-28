using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using CivSurface = Autodesk.Civil.DatabaseServices.Surface;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using MyFirstProject.Extensions;
using Label = System.Windows.Forms.Label;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.CTPI_Corridor_SetTargets_Commands))]

namespace Civil3DCsharp
{
    public class CTPI_Corridor_SetTargets_Commands
    {
        [CommandMethod("CTPI_Corridor_SetTargets")]
        public static void CTPI_Corridor_SetTargets()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                // Get all corridors in the drawing
                List<CorridorInfo> corridors = GetAllCorridors(tr);
                
                if (corridors.Count == 0)
                {
                    A.Ed.WriteMessage("\nKhông tìm thấy corridor nào trong bản vẽ.");
                    return;
                }

                // Get all surfaces
                List<TargetInfo> surfaces = GetAllSurfaces(tr);

                // Get all alignments
                List<TargetInfo> alignments = GetAllAlignments(tr);

                // Get all feature lines
                List<TargetInfo> featureLines = GetAllFeatureLines(tr);

                // Show the form
                using (var form = new CorridorTargetSetupForm())
                {
                    form.SetData(corridors, surfaces, alignments, featureLines);

                    DialogResult result = Application.ShowModalDialog(form);

                    if (result != DialogResult.OK)
                    {
                        A.Ed.WriteMessage("\nThao tác bị hủy bỏ.");
                        return;
                    }

                    // Get selected corridors and targets
                    var selectedCorridors = form.SelectedCorridors;
                    var selectedSurfaces = form.SelectedSurfaces;
                    var selectedAlignments = form.SelectedAlignments;
                    var selectedFeatureLines = form.SelectedFeatureLines;

                    if (selectedCorridors.Count == 0)
                    {
                        A.Ed.WriteMessage("\nKhông có corridor nào được chọn.");
                        return;
                    }

                    // Apply targets to selected corridors
                    int successCount = 0;
                    foreach (var corridorInfo in selectedCorridors)
                    {
                        try
                        {
                            Corridor corridor = tr.GetObject(corridorInfo.ObjectId, OpenMode.ForWrite) as Corridor;
                            if (corridor != null)
                            {
                                ApplyTargetsToCoridor(corridor, selectedSurfaces, selectedAlignments, selectedFeatureLines);
                                successCount++;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            A.Ed.WriteMessage($"\nLỗi khi áp dụng target cho corridor '{corridorInfo.Name}': {ex.Message}");
                        }
                    }

                    A.Ed.WriteMessage($"\nĐã áp dụng targets cho {successCount}/{selectedCorridors.Count} corridors.");
                }

                tr.Commit();
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi: {ex.Message}");
            }
        }

        private static List<CorridorInfo> GetAllCorridors(Transaction tr)
        {
            List<CorridorInfo> corridors = new List<CorridorInfo>();

            // Use selection filter to find all corridors in the drawing
            TypedValue[] filterList = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "AECC_CORRIDOR")
            };

            SelectionFilter filter = new SelectionFilter(filterList);
            PromptSelectionResult result = A.Ed.SelectAll(filter);

            if (result.Status == PromptStatus.OK)
            {
                SelectionSet selSet = result.Value;
                foreach (SelectedObject selObj in selSet)
                {
                    if (selObj != null)
                    {
                        Corridor corridor = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Corridor;
                        if (corridor != null)
                        {
                            corridors.Add(new CorridorInfo
                            {
                                ObjectId = selObj.ObjectId,
                                Name = corridor.Name,
                                DisplayName = $"{corridor.Name} [{corridor.Baselines.Count} baseline(s)]"
                            });
                        }
                    }
                }
            }

            return corridors;
        }

        private static List<TargetInfo> GetAllSurfaces(Transaction tr)
        {
            List<TargetInfo> surfaces = new List<TargetInfo>();

            var civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
            var surfaceIds = civilDoc.GetSurfaceIds();

            foreach (ObjectId surfaceId in surfaceIds)
            {
                CivSurface surface = tr.GetObject(surfaceId, OpenMode.ForRead) as CivSurface;
                if (surface != null)
                {
                    surfaces.Add(new TargetInfo
                    {
                        ObjectId = surfaceId,
                        Name = surface.Name,
                        Type = TargetType.Surface
                    });
                }
            }

            return surfaces;
        }

        private static List<TargetInfo> GetAllAlignments(Transaction tr)
        {
            List<TargetInfo> alignments = new List<TargetInfo>();

            var civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
            var alignmentIds = civilDoc.GetAlignmentIds();

            foreach (ObjectId alignmentId in alignmentIds)
            {
                Alignment alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                if (alignment != null)
                {
                    alignments.Add(new TargetInfo
                    {
                        ObjectId = alignmentId,
                        Name = alignment.Name,
                        Type = TargetType.Alignment
                    });
                }
            }

            return alignments;
        }

        private static List<TargetInfo> GetAllFeatureLines(Transaction tr)
        {
            List<TargetInfo> featureLines = new List<TargetInfo>();

            // Get all feature lines from the drawing
            TypedValue[] filterList = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "AECC_FEATURE_LINE")
            };

            SelectionFilter filter = new SelectionFilter(filterList);
            PromptSelectionResult result = A.Ed.SelectAll(filter);

            if (result.Status == PromptStatus.OK)
            {
                SelectionSet selSet = result.Value;
                foreach (SelectedObject selObj in selSet)
                {
                    if (selObj != null)
                    {
                        FeatureLine featureLine = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as FeatureLine;
                        if (featureLine != null)
                        {
                            featureLines.Add(new TargetInfo
                            {
                                ObjectId = selObj.ObjectId,
                                Name = featureLine.Name,
                                Type = TargetType.FeatureLine
                            });
                        }
                    }
                }
            }

            return featureLines;
        }

        private static void ApplyTargetsToCoridor(Corridor corridor, 
            List<TargetInfo> surfaces, List<TargetInfo> alignments, List<TargetInfo> featureLines)
        {
            // Note: This is a simplified implementation
            // In reality, you would need to specify which subassembly needs which target
            // This requires more complex logic based on the corridor's assembly structure

            A.Ed.WriteMessage($"\nÁp dụng targets cho corridor: {corridor.Name}");
            A.Ed.WriteMessage($"\n  - Surfaces: {surfaces.Count}");
            A.Ed.WriteMessage($"\n  - Alignments: {alignments.Count}");
            A.Ed.WriteMessage($"\n  - Feature Lines: {featureLines.Count}");

            // TODO: Implement actual target assignment logic
            // This would involve iterating through baselines, regions, and applied assemblies
            // to set appropriate targets for each subassembly that requires them
        }
    }

    public class CorridorInfo
    {
        public ObjectId ObjectId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }

    public enum TargetType
    {
        Surface,
        Alignment,
        FeatureLine
    }

    public class TargetInfo
    {
        public ObjectId ObjectId { get; set; }
        public string Name { get; set; }
        public TargetType Type { get; set; }
    }

    public partial class CorridorTargetSetupForm : Form
    {
        public List<CorridorInfo> SelectedCorridors { get; private set; }
        public List<TargetInfo> SelectedSurfaces { get; private set; }
        public List<TargetInfo> SelectedAlignments { get; private set; }
        public List<TargetInfo> SelectedFeatureLines { get; private set; }

        private CheckedListBox lstCorridors;
        private CheckedListBox lstSurfaces;
        private CheckedListBox lstAlignments;
        private CheckedListBox lstFeatureLines;
        private Button btnOK;
        private Button btnCancel;
        private Button btnSelectAllCorridors;
        private Button btnDeselectAllCorridors;
        private Button btnSelectAllSurfaces;
        private Button btnDeselectAllSurfaces;
        private Button btnSelectAllAlignments;
        private Button btnDeselectAllAlignments;
        private Button btnSelectAllFeatureLines;
        private Button btnDeselectAllFeatureLines;
        private GroupBox grpCorridors;
        private GroupBox grpSurfaces;
        private GroupBox grpAlignments;
        private GroupBox grpFeatureLines;
        private Label lblInfo;

        private List<CorridorInfo> _allCorridors;
        private List<TargetInfo> _allSurfaces;
        private List<TargetInfo> _allAlignments;
        private List<TargetInfo> _allFeatureLines;

        public CorridorTargetSetupForm()
        {
            InitializeComponent();
            SelectedCorridors = new List<CorridorInfo>();
            SelectedSurfaces = new List<TargetInfo>();
            SelectedAlignments = new List<TargetInfo>();
            SelectedFeatureLines = new List<TargetInfo>();
        }

        private void InitializeComponent()
        {
            this.lstCorridors = new CheckedListBox();
            this.lstSurfaces = new CheckedListBox();
            this.lstAlignments = new CheckedListBox();
            this.lstFeatureLines = new CheckedListBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.btnSelectAllCorridors = new Button();
            this.btnDeselectAllCorridors = new Button();
            this.btnSelectAllSurfaces = new Button();
            this.btnDeselectAllSurfaces = new Button();
            this.btnSelectAllAlignments = new Button();
            this.btnDeselectAllAlignments = new Button();
            this.btnSelectAllFeatureLines = new Button();
            this.btnDeselectAllFeatureLines = new Button();
            this.grpCorridors = new GroupBox();
            this.grpSurfaces = new GroupBox();
            this.grpAlignments = new GroupBox();
            this.grpFeatureLines = new GroupBox();
            this.lblInfo = new Label();

            this.SuspendLayout();

            // Form
            this.Text = "Thiết lập Target cho Corridors";
            this.Size = new System.Drawing.Size(900, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Info Label
            this.lblInfo.Text = "Chọn corridors và targets (surfaces, alignments, feature lines) để áp dụng:";
            this.lblInfo.Location = new System.Drawing.Point(12, 12);
            this.lblInfo.Size = new System.Drawing.Size(860, 20);
            this.lblInfo.Font = new System.Drawing.Font(this.lblInfo.Font, System.Drawing.FontStyle.Bold);

            // Corridors Group
            this.grpCorridors.Text = "Corridors";
            this.grpCorridors.Location = new System.Drawing.Point(12, 40);
            this.grpCorridors.Size = new System.Drawing.Size(420, 280);

            this.lstCorridors.Location = new System.Drawing.Point(10, 25);
            this.lstCorridors.Size = new System.Drawing.Size(400, 210);
            this.lstCorridors.CheckOnClick = true;

            this.btnSelectAllCorridors.Text = "Chọn tất cả";
            this.btnSelectAllCorridors.Location = new System.Drawing.Point(10, 245);
            this.btnSelectAllCorridors.Size = new System.Drawing.Size(120, 25);
            this.btnSelectAllCorridors.Click += new EventHandler(this.btnSelectAllCorridors_Click);

            this.btnDeselectAllCorridors.Text = "Bỏ chọn tất cả";
            this.btnDeselectAllCorridors.Location = new System.Drawing.Point(140, 245);
            this.btnDeselectAllCorridors.Size = new System.Drawing.Size(120, 25);
            this.btnDeselectAllCorridors.Click += new EventHandler(this.btnDeselectAllCorridors_Click);

            // Surfaces Group
            this.grpSurfaces.Text = "Surfaces (Mặt đất)";
            this.grpSurfaces.Location = new System.Drawing.Point(452, 40);
            this.grpSurfaces.Size = new System.Drawing.Size(420, 160);

            this.lstSurfaces.Location = new System.Drawing.Point(10, 25);
            this.lstSurfaces.Size = new System.Drawing.Size(400, 90);
            this.lstSurfaces.CheckOnClick = true;

            this.btnSelectAllSurfaces.Text = "Chọn tất cả";
            this.btnSelectAllSurfaces.Location = new System.Drawing.Point(10, 125);
            this.btnSelectAllSurfaces.Size = new System.Drawing.Size(120, 25);
            this.btnSelectAllSurfaces.Click += new EventHandler(this.btnSelectAllSurfaces_Click);

            this.btnDeselectAllSurfaces.Text = "Bỏ chọn tất cả";
            this.btnDeselectAllSurfaces.Location = new System.Drawing.Point(140, 125);
            this.btnDeselectAllSurfaces.Size = new System.Drawing.Size(120, 25);
            this.btnDeselectAllSurfaces.Click += new EventHandler(this.btnDeselectAllSurfaces_Click);

            // Alignments Group
            this.grpAlignments.Text = "Alignments (Trục tuyến)";
            this.grpAlignments.Location = new System.Drawing.Point(452, 210);
            this.grpAlignments.Size = new System.Drawing.Size(420, 160);

            this.lstAlignments.Location = new System.Drawing.Point(10, 25);
            this.lstAlignments.Size = new System.Drawing.Size(400, 90);
            this.lstAlignments.CheckOnClick = true;

            this.btnSelectAllAlignments.Text = "Chọn tất cả";
            this.btnSelectAllAlignments.Location = new System.Drawing.Point(10, 125);
            this.btnSelectAllAlignments.Size = new System.Drawing.Size(120, 25);
            this.btnSelectAllAlignments.Click += new EventHandler(this.btnSelectAllAlignments_Click);

            this.btnDeselectAllAlignments.Text = "Bỏ chọn tất cả";
            this.btnDeselectAllAlignments.Location = new System.Drawing.Point(140, 125);
            this.btnDeselectAllAlignments.Size = new System.Drawing.Size(120, 25);
            this.btnDeselectAllAlignments.Click += new EventHandler(this.btnDeselectAllAlignments_Click);

            // Feature Lines Group
            this.grpFeatureLines.Text = "Feature Lines (Đường đặc trưng)";
            this.grpFeatureLines.Location = new System.Drawing.Point(12, 330);
            this.grpFeatureLines.Size = new System.Drawing.Size(860, 280);

            this.lstFeatureLines.Location = new System.Drawing.Point(10, 25);
            this.lstFeatureLines.Size = new System.Drawing.Size(840, 210);
            this.lstFeatureLines.CheckOnClick = true;

            this.btnSelectAllFeatureLines.Text = "Chọn tất cả";
            this.btnSelectAllFeatureLines.Location = new System.Drawing.Point(10, 245);
            this.btnSelectAllFeatureLines.Size = new System.Drawing.Size(120, 25);
            this.btnSelectAllFeatureLines.Click += new EventHandler(this.btnSelectAllFeatureLines_Click);

            this.btnDeselectAllFeatureLines.Text = "Bỏ chọn tất cả";
            this.btnDeselectAllFeatureLines.Location = new System.Drawing.Point(140, 245);
            this.btnDeselectAllFeatureLines.Size = new System.Drawing.Size(120, 25);
            this.btnDeselectAllFeatureLines.Click += new EventHandler(this.btnDeselectAllFeatureLines_Click);

            // OK Button
            this.btnOK.Text = "OK";
            this.btnOK.Location = new System.Drawing.Point(712, 620);
            this.btnOK.Size = new System.Drawing.Size(75, 30);
            this.btnOK.Click += new EventHandler(this.btnOK_Click);

            // Cancel Button
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Location = new System.Drawing.Point(797, 620);
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);

            // Add controls to groups
            this.grpCorridors.Controls.Add(this.lstCorridors);
            this.grpCorridors.Controls.Add(this.btnSelectAllCorridors);
            this.grpCorridors.Controls.Add(this.btnDeselectAllCorridors);

            this.grpSurfaces.Controls.Add(this.lstSurfaces);
            this.grpSurfaces.Controls.Add(this.btnSelectAllSurfaces);
            this.grpSurfaces.Controls.Add(this.btnDeselectAllSurfaces);

            this.grpAlignments.Controls.Add(this.lstAlignments);
            this.grpAlignments.Controls.Add(this.btnSelectAllAlignments);
            this.grpAlignments.Controls.Add(this.btnDeselectAllAlignments);

            this.grpFeatureLines.Controls.Add(this.lstFeatureLines);
            this.grpFeatureLines.Controls.Add(this.btnSelectAllFeatureLines);
            this.grpFeatureLines.Controls.Add(this.btnDeselectAllFeatureLines);

            // Add controls to form
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.grpCorridors);
            this.Controls.Add(this.grpSurfaces);
            this.Controls.Add(this.grpAlignments);
            this.Controls.Add(this.grpFeatureLines);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);

            this.ResumeLayout(false);
        }

        public void SetData(List<CorridorInfo> corridors, List<TargetInfo> surfaces, 
            List<TargetInfo> alignments, List<TargetInfo> featureLines)
        {
            _allCorridors = corridors;
            _allSurfaces = surfaces;
            _allAlignments = alignments;
            _allFeatureLines = featureLines;

            // Populate lists
            lstCorridors.Items.Clear();
            foreach (var corridor in corridors)
            {
                lstCorridors.Items.Add(corridor.DisplayName);
            }

            lstSurfaces.Items.Clear();
            foreach (var surface in surfaces)
            {
                lstSurfaces.Items.Add(surface.Name);
            }

            lstAlignments.Items.Clear();
            foreach (var alignment in alignments)
            {
                lstAlignments.Items.Add(alignment.Name);
            }

            lstFeatureLines.Items.Clear();
            foreach (var featureLine in featureLines)
            {
                lstFeatureLines.Items.Add(featureLine.Name);
            }
        }

        private void btnSelectAllCorridors_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstCorridors.Items.Count; i++)
            {
                lstCorridors.SetItemChecked(i, true);
            }
        }

        private void btnDeselectAllCorridors_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstCorridors.Items.Count; i++)
            {
                lstCorridors.SetItemChecked(i, false);
            }
        }

        private void btnSelectAllSurfaces_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstSurfaces.Items.Count; i++)
            {
                lstSurfaces.SetItemChecked(i, true);
            }
        }

        private void btnDeselectAllSurfaces_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstSurfaces.Items.Count; i++)
            {
                lstSurfaces.SetItemChecked(i, false);
            }
        }

        private void btnSelectAllAlignments_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstAlignments.Items.Count; i++)
            {
                lstAlignments.SetItemChecked(i, true);
            }
        }

        private void btnDeselectAllAlignments_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstAlignments.Items.Count; i++)
            {
                lstAlignments.SetItemChecked(i, false);
            }
        }

        private void btnSelectAllFeatureLines_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstFeatureLines.Items.Count; i++)
            {
                lstFeatureLines.SetItemChecked(i, true);
            }
        }

        private void btnDeselectAllFeatureLines_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstFeatureLines.Items.Count; i++)
            {
                lstFeatureLines.SetItemChecked(i, false);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            SelectedCorridors.Clear();
            for (int i = 0; i < lstCorridors.CheckedIndices.Count; i++)
            {
                int index = lstCorridors.CheckedIndices[i];
                SelectedCorridors.Add(_allCorridors[index]);
            }

            SelectedSurfaces.Clear();
            for (int i = 0; i < lstSurfaces.CheckedIndices.Count; i++)
            {
                int index = lstSurfaces.CheckedIndices[i];
                SelectedSurfaces.Add(_allSurfaces[index]);
            }

            SelectedAlignments.Clear();
            for (int i = 0; i < lstAlignments.CheckedIndices.Count; i++)
            {
                int index = lstAlignments.CheckedIndices[i];
                SelectedAlignments.Add(_allAlignments[index]);
            }

            SelectedFeatureLines.Clear();
            for (int i = 0; i < lstFeatureLines.CheckedIndices.Count; i++)
            {
                int index = lstFeatureLines.CheckedIndices[i];
                SelectedFeatureLines.Add(_allFeatureLines[index]);
            }

            if (SelectedCorridors.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một corridor.", "Cảnh báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (SelectedSurfaces.Count == 0 && SelectedAlignments.Count == 0 && SelectedFeatureLines.Count == 0)
            {
                var result = MessageBox.Show(
                    "Bạn chưa chọn target nào. Bạn có muốn tiếp tục?", 
                    "Xác nhận", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.No)
                {
                    return;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
