using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil;
using Autodesk.Civil.DatabaseServices;
using Civil3DCsharp;
using MyFirstProject.Extensions;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Acad = Autodesk.AutoCAD.ApplicationServices;

namespace MyFirstProject.Menu_form
{
    public partial class KiemTraTimTuyenForm : Form
    {
        private List<ObjectId> _alignmentIds = new List<ObjectId>();
        private IDesignStandard _currentStandard;
        public List<CheckResult> ErrorResults { get; private set; } = new List<CheckResult>();

        public KiemTraTimTuyenForm()
        {
            InitializeComponent();
        }

        private void KiemTraTimTuyenForm_Load(object sender, EventArgs e)
        {
            LoadAlignments();
            
            var standards = StandardFactory.GetAllStandards();
            cbbStandard.DataSource = standards;
            cbbStandard.DisplayMember = "StandardName";
            if (standards.Count > 0)
                cbbStandard.SelectedIndex = 0;
        }

        private void cbbStandard_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbbStandard.SelectedIndex < 0) return;
            _currentStandard = (IDesignStandard)cbbStandard.SelectedItem;

            cbbDesignSpeed.Items.Clear();
            foreach (var speed in _currentStandard.SupportedSpeeds)
            {
                cbbDesignSpeed.Items.Add(speed.ToString());
            }

            if (cbbDesignSpeed.Items.Count > 0)
            {
                int idx = cbbDesignSpeed.Items.IndexOf("60");
                if (idx >= 0) cbbDesignSpeed.SelectedIndex = idx;
                else cbbDesignSpeed.SelectedIndex = 0;
            }
        }

        private void LoadAlignments()
        {
            cbbAlignments.Items.Clear();
            _alignmentIds.Clear();

            try
            {
                using (var tr = A.Db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(A.Db.BlockTableId, OpenMode.ForRead);
                    var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    foreach (ObjectId id in btr)
                    {
                        if (id.ObjectClass.Name == "AeccDbAlignment")
                        {
                            var align = tr.GetObject(id, OpenMode.ForRead) as Alignment;
                            if (align != null && !align.IsConnectedAlignment)
                            {
                                cbbAlignments.Items.Add(align.Name);
                                _alignmentIds.Add(id);
                            }
                        }
                    }
                    tr.Commit();
                }

                if (cbbAlignments.Items.Count > 0)
                    cbbAlignments.SelectedIndex = 0;
            }
            catch (Exception)
            {
                A.Ed.WriteMessage($"\nLỗi tải Alignment.");
            }
        }

        private void btnPickAlignment_Click(object sender, EventArgs e)
        {
            using (A.Doc.LockDocument())
            {
                // Hide form temporarily to pick on screen
                var prevVisible = this.Visible;
                if (prevVisible) this.Hide();

                try
                {
                    ObjectId alignId = UserInput.GAlignmentId("\nChọn tim tuyến: ");
                    if (!alignId.IsNull)
                    {
                        using (var tr = A.Db.TransactionManager.StartTransaction())
                        {
                            var align = tr.GetObject(alignId, OpenMode.ForRead) as Alignment;
                            if (align != null)
                            {
                                if (!_alignmentIds.Contains(alignId))
                                {
                                    _alignmentIds.Add(alignId);
                                    cbbAlignments.Items.Add(align.Name);
                                }
                                cbbAlignments.SelectedIndex = _alignmentIds.IndexOf(alignId);
                            }
                            tr.Commit();
                        }
                    }
                }
                catch (Exception)
                {
                    A.Ed.WriteMessage("\nHủy chọn.");
                }
                finally
                {
                    if (prevVisible) this.Show();
                }
            }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            if (cbbAlignments.SelectedIndex < 0)
            {
                MessageBox.Show("Vui lòng chọn Alignment.");
                return;
            }

            if (cbbStandard.SelectedIndex < 0 || cbbDesignSpeed.SelectedIndex < 0)
            {
                MessageBox.Show("Vui lòng chọn tiêu chuẩn và vận tốc thiết kế.");
                return;
            }

            int speed = int.Parse(cbbDesignSpeed.SelectedItem.ToString());
            var parameters = _currentStandard.GetParameters(speed);
            ObjectId alignId = _alignmentIds[cbbAlignments.SelectedIndex];

            ErrorResults.Clear();
            dgvResults.Rows.Clear();

            using (A.Doc.LockDocument())
            {
                using (var tr = A.Db.TransactionManager.StartTransaction())
                {
                    var align = tr.GetObject(alignId, OpenMode.ForRead) as Alignment;
                    if (align != null)
                    {
                        var entities = align.Entities;
                        
                        // Lấy các AlignmentEntity để kiểm tra tuần tự
                        // entities trong Civil 3D được lưu trữ dưới dạng collection
                        
                        AlignmentEntity prevEntity = null;
                        
                        foreach (AlignmentEntity entity in entities)
                        {
                            // 1. Kiểm tra cong tròn (Arc)
                            if (entity.EntityType == AlignmentEntityType.Arc)
                            {
                                CheckArc((AlignmentArc)entity, parameters);
                            }
                            
                            // 2. Kiểm tra cong chuyển tiếp (Spiral)
                            else if (entity.EntityType == AlignmentEntityType.Spiral)
                            {
                                CheckSpiral((AlignmentSpiral)entity, parameters);
                            }

                            // 3. Kiểm tra đoạn thẳng (Line)
                            else if (entity.EntityType == AlignmentEntityType.Line)
                            {
                                CheckLine((AlignmentLine)entity, parameters);
                            }
                            
                            // 4. Các loại cong phức tạp
                            else if (entity is AlignmentSCS scs)
                            {
                                CheckSpiral(scs.SpiralIn, parameters);
                                CheckArc(scs.Arc, parameters);
                                CheckSpiral(scs.SpiralOut, parameters);
                            }
                        }

                        // 5. Kiểm tra siêu cao
                        if (chkCheckSuperelevation.Checked)
                        {
                            if (align.SuperelevationCurves.Count == 0)
                            {
                                AddResult(false, "Siêu cao", align.StartingStation, "Tuyến chưa được tính toán siêu cao (Superelevation)");
                            }
                            else
                            {
                                foreach (SuperelevationCurve sc in align.SuperelevationCurves)
                                {
                                    bool hasFullSuper = false;
                                    double localMaxSlope = 0;
                                    double localMaxSlopeStation = sc.StartStation;
                                    
                                    // Xác định độ dốc lớn nhất trong đường cong siêu cao này
                                    foreach (SuperelevationCriticalStation cs in sc.CriticalStations)
                                    {
                                        if (cs.StationType == SuperelevationCriticalStationType.BeginFullSuper || cs.StationType == SuperelevationCriticalStationType.EndFullSuper)
                                        {
                                            hasFullSuper = true;
                                            try 
                                            {
                                                double leftOut = Math.Abs(cs.GetSlope(SuperelevationCrossSegmentType.LeftOutLaneCrossSlope));
                                                double rightOut = Math.Abs(cs.GetSlope(SuperelevationCrossSegmentType.RightOutLaneCrossSlope));
                                                double maxLanes = Math.Max(leftOut, rightOut) * 100.0; // Chuyển sang %
                                                
                                                if (maxLanes > localMaxSlope)
                                                {
                                                    localMaxSlope = maxLanes;
                                                    localMaxSlopeStation = cs.Station;
                                                }
                                            }
                                            catch { /* Bỏ qua nếu không tồn tại lane */ }
                                        }
                                    }

                                    if (hasFullSuper && localMaxSlope > 0)
                                    {
                                        if (localMaxSlope > parameters.MaxSuperelevation)
                                        {
                                            AddResult(false, "Siêu cao max", localMaxSlopeStation, $"Isc_max = {localMaxSlope:F2}% > {parameters.MaxSuperelevation}% (Giới hạn)");
                                        }
                                        else
                                        {
                                            AddResult(true, "Siêu cao max", localMaxSlopeStation, $"Isc_max = {localMaxSlope:F2}% <= {parameters.MaxSuperelevation}%");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    tr.Commit();
                }
            }

            UpdateGrid();

            if (!ErrorResults.Any(r => !r.IsPassed))
            {
                MessageBox.Show("Tim tuyến đạt tất cả tiêu chuẩn!");
            }
        }

        private void chkFilter_CheckedChanged(object sender, EventArgs e)
        {
            UpdateGrid();
        }

        private void UpdateGrid()
        {
            dgvResults.Rows.Clear();
            foreach (var res in ErrorResults)
            {
                if (res.IsPassed && !chkShowPassed.Checked) continue;
                if (!res.IsPassed && !chkShowFailed.Checked) continue;

                dgvResults.Rows.Add(res.IsPassed ? "Đạt" : "Không đạt", res.Type, $"Km{Math.Floor(res.Station / 1000):0}+{res.Station % 1000:000.00}", res.Detail);
            }
        }

        private void AddResult(bool isPassed, string type, double station, string detail)
        {
            ErrorResults.Add(new CheckResult { IsPassed = isPassed, Type = type, Station = station, Detail = detail });
        }

        private void btnDrawErrors_Click(object sender, EventArgs e)
        {
            if (ErrorResults.Count == 0 || cbbAlignments.SelectedIndex < 0) return;

            ObjectId alignId = _alignmentIds[cbbAlignments.SelectedIndex];

            using (A.Doc.LockDocument())
            {
                using (var tr = A.Db.TransactionManager.StartTransaction())
                {
                    var align = tr.GetObject(alignId, OpenMode.ForRead) as Alignment;
                    var btr = (BlockTableRecord)tr.GetObject(A.Db.CurrentSpaceId, OpenMode.ForWrite);

                    foreach (var error in ErrorResults.Where(r => !r.IsPassed))
                    {
                        double x = 0, y = 0;
                        try
                        {
                            align.PointLocation(error.Station, 0, ref x, ref y);
                            
                            // Vẽ đường tròn đánh dấu
                            Circle circle = new Circle();
                            circle.Center = new Autodesk.AutoCAD.Geometry.Point3d(x, y, 0);
                            circle.Radius = 20.0;
                            circle.ColorIndex = 1; // Đỏ
                            btr.AppendEntity(circle);
                            tr.AddNewlyCreatedDBObject(circle, true);
                        }
                        catch { }
                    }
                    tr.Commit();
                }
            }
            MessageBox.Show("Đã đánh dấu lỗi trên bản vẽ!");
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cbbDesignSpeed_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbbDesignSpeed.SelectedIndex < 0) return;
            
            int speed = int.Parse(cbbDesignSpeed.SelectedItem.ToString());
            var parameters = _currentStandard.GetParameters(speed);

            chkMinRadius.Text = $"Bán kính đường cong tối thiểu (R >= {parameters.MinRadiusNormal}m)";
            chkMinCurveLength.Text = $"Chiều dài đường cong tối thiểu (K >= {parameters.MinCurveLength}m)";
            chkMaxStraightLength.Text = $"Chiều dài đường thẳng tối đa (L <= {parameters.MaxStraightLength}m)";
            chkStraightBetweenCurves.Text = $"Chiều dài đoạn thẳng giữa 2 đường cong (L >= {parameters.MinStraightLengthSameDirection}m)";
            chkTransitionLength.Text = $"Chiều dài cong chuyển tiếp tối thiểu (Ls >= {parameters.MinTransitionCurveLength}m)";
        }

        private void btnViewTable_Click(object sender, EventArgs e)
        {
            if (cbbDesignSpeed.SelectedIndex < 0) return;

            int speed = int.Parse(cbbDesignSpeed.SelectedItem.ToString());
            
            var allStandards = StandardFactory.GetAllStandards();
            
            using (var form = new StandardComparisonForm(speed, allStandards))
            {
                form.ShowDialog();
            }
        }

        private void CheckArc(dynamic arc, DesignParameters parameters)
        {
            if (arc == null) return;
            if (chkMinRadius.Checked)
            {
                double radius = Math.Round(arc.Radius, 2);
                if (radius < parameters.MinRadiusLimit)
                {
                    AddResult(false, "Bán kính cong", arc.StartStation, $"R = {arc.Radius:F2} < {parameters.MinRadiusLimit} (Giới hạn)");
                }
                else if (radius < parameters.MinRadiusNormal)
                {
                    AddResult(false, "Bán kính cong", arc.StartStation, $"R = {arc.Radius:F2} < {parameters.MinRadiusNormal} (Thông thường)");
                }
                else
                {
                    AddResult(true, "Bán kính cong", arc.StartStation, $"R = {arc.Radius:F2} >= {parameters.MinRadiusNormal}");
                }
            }

            if (chkMinCurveLength.Checked)
            {
                double length = Math.Round(arc.Length, 2);
                if (length < parameters.MinCurveLength)
                {
                    AddResult(false, "Chiều dài cong", arc.StartStation, $"L = {arc.Length:F2} < {parameters.MinCurveLength}");
                }
                else
                {
                    AddResult(true, "Chiều dài cong", arc.StartStation, $"L = {arc.Length:F2} >= {parameters.MinCurveLength}");
                }
            }
        }

        private void CheckSpiral(dynamic spiral, DesignParameters parameters)
        {
            if (spiral == null) return;
            if (chkTransitionLength.Checked)
            {
                double length = Math.Round(spiral.Length, 2);
                if (length < parameters.MinTransitionCurveLength)
                {
                    AddResult(false, "Cong chuyển tiếp", spiral.StartStation, $"Ls = {spiral.Length:F2} < {parameters.MinTransitionCurveLength}");
                }
                else
                {
                    AddResult(true, "Cong chuyển tiếp", spiral.StartStation, $"Ls = {spiral.Length:F2} >= {parameters.MinTransitionCurveLength}");
                }
            }
        }

        private void CheckLine(dynamic line, DesignParameters parameters)
        {
            if (line == null) return;
            if (chkMaxStraightLength.Checked)
            {
                double length = Math.Round(line.Length, 2);
                if (length > parameters.MaxStraightLength)
                {
                    AddResult(false, "Đường thẳng tối đa", line.StartStation, $"L = {line.Length:F2} > {parameters.MaxStraightLength}");
                }
                else
                {
                    AddResult(true, "Đường thẳng tối đa", line.StartStation, $"L = {line.Length:F2} <= {parameters.MaxStraightLength}");
                }
            }
        }
    }

    public class CheckResult
    {
        public string Type { get; set; }
        public double Station { get; set; }
        public string Detail { get; set; }
        public bool IsPassed { get; set; }
    }
}
