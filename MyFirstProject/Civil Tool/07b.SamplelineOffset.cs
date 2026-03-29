// Nhóm lệnh: Offset bề rộng SampleLine
// Tách từ 07.Sampleline.cs
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.Civil.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using MyFirstProject.Extensions;

[assembly: CommandClass(typeof(Civil3DCsharp.SamplelineOffset))]

namespace Civil3DCsharp
{
    public class SamplelineOffset
    {
        [CommandMethod("CTS_Copy_BeRong_sampleLine")]
        public static void CTSCopyBeRongSampleLine()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here

                ObjectId sampleLineId = UserInput.GSampleLineId("Chọn sampleLine GỐC cần sao chép bề rộng:");
                double offsetLeft = new();
                double offsetRight = new();
                Point3d point3dLeft = new();
                Point3d point3dRight = new();
                Point3d point3dCenter = new();
                SampleLine? sampleLine = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8604 // Possible null reference argument.
                ObjectId alignmentId = sampleLine.GetParentAlignmentId();
#pragma warning restore CS8604 // Possible null reference argument.
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
                if (sampleLine.Vertices.Count > 3)
                {
                    A.Ok("Kiểm tra lại sampleline có nhiều hơn 3 điểm");
                }
                foreach (SampleLineVertex vertex in sampleLine.Vertices)
                {

                    if (vertex.Side == SampleLineVertexSideType.Left)
                    {
                        point3dLeft = vertex.Location;
                    }
                    if (vertex.Side == SampleLineVertexSideType.Right)
                    {
                        point3dRight = vertex.Location;
                    }
                    if (vertex.Side == SampleLineVertexSideType.Center)
                    {
                        point3dCenter = vertex.Location;
                    }
                    offsetLeft = point3dCenter.DistanceTo(point3dLeft);
                    offsetRight = point3dCenter.DistanceTo(point3dRight);
                }
                double station = new();
                double offset = new();
                double easting = new();
                double northing = new();
                ObjectIdCollection sampleLineIds = UserInput.GSelectionSetWithType("Chọn các sample cần copy bề rộng: \n", "AECC_SAMPLE_LINE");
                A.OkHere();
                foreach (ObjectId SLineId in sampleLineIds)
                {
                    SampleLine? sampleLine1 = tr.GetObject(SLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (sampleLine1.Vertices.Count > 3)
                    {
                        A.Ok("Kiểm tra lại sampleline có nhiều hơn 3 điểm");
                    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    foreach (SampleLineVertex vertex in sampleLine1.Vertices)
                    {
                        if (vertex.Side == SampleLineVertexSideType.Center)
                        {
                            point3dCenter = vertex.Location;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            alignment.StationOffset(point3dCenter.X, point3dCenter.Y, ref station, ref offset);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        }
                    }
                    foreach (SampleLineVertex vertex in sampleLine1.Vertices)
                    {
                        if (vertex.Side == SampleLineVertexSideType.Left)
                        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            alignment.PointLocation(station, -offsetLeft, ref easting, ref northing);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                            vertex.Location = new Point3d(easting, northing, 0);
                        }
                    }
                    foreach (SampleLineVertex vertex in sampleLine1.Vertices)
                    {
                        if (vertex.Side == SampleLineVertexSideType.Right)
                        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            alignment.PointLocation(station, offsetRight, ref easting, ref northing);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                            vertex.Location = new Point3d(easting, northing, 0);
                        }
                    }
                }




                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_Offset_BeRong_sampleLine")]
        public static void CTSOffsetBeRongSampleLine()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var db = doc.Database;
            var ed = doc.Editor;

            try
            {
                // Bước 1: Chọn 1 sampleline để xác định group
                ObjectId pickedSlId = UserInput.GSampleLineId("\nChọn 1 SampleLine (sẽ lấy cả group): ");
                if (pickedSlId == ObjectId.Null) return;

                // Show form chọn polyline biên
                var form = new SampleLineOffsetPolylineForm();
                if (Application.ShowModalDialog(form) != DialogResult.OK) return;

                using var tr = db.TransactionManager.StartTransaction();

                // Lấy group từ sampleline đã chọn
                var pickedSl = tr.GetObject(pickedSlId, OpenMode.ForRead) as SampleLine;
                if (pickedSl == null) { ed.WriteMessage("\nKhông thể đọc SampleLine."); return; }

                ObjectId groupId = pickedSl.GroupId;
                var slGroup = tr.GetObject(groupId, OpenMode.ForRead) as SampleLineGroup;
                if (slGroup == null) { ed.WriteMessage("\nKhông tìm thấy SampleLine Group."); return; }

                ObjectIdCollection slIds = slGroup.GetSampleLineIds();
                ed.WriteMessage($"\nĐã tìm thấy {slIds.Count} SampleLine trong group \"{slGroup.Name}\".");

                // Flatten polyline về Z=0 để tính giao điểm chính xác
                Polyline? plLeftSrc = form.LeftPolylineId != ObjectId.Null
                    ? (Polyline)tr.GetObject(form.LeftPolylineId, OpenMode.ForRead) : null;
                Polyline? plRightSrc = form.RightPolylineId != ObjectId.Null
                    ? (Polyline)tr.GetObject(form.RightPolylineId, OpenMode.ForRead) : null;

                Polyline? plLeftFlat = plLeftSrc != null ? FlattenPolyline(plLeftSrc) : null;
                Polyline? plRightFlat = plRightSrc != null ? FlattenPolyline(plRightSrc) : null;

                double leftExcess = form.LeftExcess;
                double rightExcess = form.RightExcess;

                int updated = 0, noCenter = 0, noLeft = 0, noRight = 0, missLeft = 0, missRight = 0;

                foreach (ObjectId id in slIds)
                {
                    var sl = tr.GetObject(id, OpenMode.ForWrite) as SampleLine;
                    if (sl == null) continue;

                    var centerV = sl.Vertices.Cast<SampleLineVertex>()
                        .FirstOrDefault(v => v.Side == SampleLineVertexSideType.Center);
                    if (centerV == null) { noCenter++; continue; }

                    // Flatten center point về Z=0
                    Point3d centerPt = new Point3d(centerV.Location.X, centerV.Location.Y, 0);
                    bool sampleLineUpdated = false;

                    // Xử lý bên trái
                    if (plLeftFlat != null)
                    {
                        var leftV = sl.Vertices.Cast<SampleLineVertex>()
                            .FirstOrDefault(v => v.Side == SampleLineVertexSideType.Left);
                        if (leftV != null)
                        {
                            Point3d leftPt = new Point3d(leftV.Location.X, leftV.Location.Y, 0);
                            Vector3d dirLeft = leftPt - centerPt;
                            if (dirLeft.Length < 1e-6) dirLeft = new Vector3d(-1, 0, 0);
                            dirLeft = dirLeft.GetNormal();

                            Point3d? hitLeft = IntersectOneDirection(centerPt, dirLeft, plLeftFlat);
                            if (hitLeft != null)
                            {
                                Point3d newLoc = hitLeft.Value + dirLeft * leftExcess;
                                leftV.Location = new Point3d(newLoc.X, newLoc.Y, 0);
                                sampleLineUpdated = true;
                            }
                            else { missLeft++; }
                        }
                        else { noLeft++; }
                    }

                    // Xử lý bên phải
                    if (plRightFlat != null)
                    {
                        var rightV = sl.Vertices.Cast<SampleLineVertex>()
                            .FirstOrDefault(v => v.Side == SampleLineVertexSideType.Right);
                        if (rightV != null)
                        {
                            Point3d rightPt = new Point3d(rightV.Location.X, rightV.Location.Y, 0);
                            Vector3d dirRight = rightPt - centerPt;
                            if (dirRight.Length < 1e-6) dirRight = new Vector3d(1, 0, 0);
                            dirRight = dirRight.GetNormal();

                            Point3d? hitRight = IntersectOneDirection(centerPt, dirRight, plRightFlat);
                            if (hitRight != null)
                            {
                                Point3d newLoc = hitRight.Value + dirRight * rightExcess;
                                rightV.Location = new Point3d(newLoc.X, newLoc.Y, 0);
                                sampleLineUpdated = true;
                            }
                            else { missRight++; }
                        }
                        else { noRight++; }
                    }

                    if (sampleLineUpdated) updated++;
                }

                // Dispose polyline clones
                plLeftFlat?.Dispose();
                plRightFlat?.Dispose();

                tr.Commit();
                ed.WriteMessage($"\nCập nhật xong: {updated}/{slIds.Count} sample lines.");
                if (noCenter > 0) ed.WriteMessage($" Không center: {noCenter}.");
                if (noLeft > 0) ed.WriteMessage($" Không left vtx: {noLeft}.");
                if (noRight > 0) ed.WriteMessage($" Không right vtx: {noRight}.");
                if (missLeft > 0) ed.WriteMessage($" Không giao trái: {missLeft}.");
                if (missRight > 0) ed.WriteMessage($" Không giao phải: {missRight}.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nLỗi: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Clone polyline và gán tất cả đỉnh về Z=0, elevation=0
        /// Giúp intersection hoạt động đúng khi polyline có cao độ khác 0
        /// </summary>
        private static Polyline FlattenPolyline(Polyline source)
        {
            Polyline flat = (Polyline)source.Clone();
            flat.Elevation = 0;
            for (int i = 0; i < flat.NumberOfVertices; i++)
            {
                var pt2d = flat.GetPoint2dAt(i);
                flat.SetPointAt(i, pt2d); // giữ nguyên X,Y
            }
            // Đặt normal thẳng đứng để đảm bảo polyline nằm trên mặt phẳng XY
            flat.Normal = Vector3d.ZAxis;
            return flat;
        }

        /// <summary>
        /// Tìm giao điểm theo 1 hướng từ origin với polyline boundary (đã flatten Z=0)
        /// </summary>
        private static Point3d? IntersectOneDirection(Point3d origin, Vector3d dir, Polyline boundary)
        {
            const double maxLen = 100000.0;
            // Tạo line tại Z=0
            Point3d start = new Point3d(origin.X, origin.Y, 0);
            Point3d end = start + dir * maxLen;
            using var ray = new Line(start, end);

            var pts = new Point3dCollection();
            try
            {
                boundary.IntersectWith(ray, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
            }
            catch { return null; }

            if (pts.Count == 0) return null;

            // Tìm giao điểm gần nhất theo hướng dir
            double best = double.MaxValue;
            Point3d bestPt = Point3d.Origin;
            for (int i = 0; i < pts.Count; i++)
            {
                Point3d flatPt = new Point3d(pts[i].X, pts[i].Y, 0);
                double d = (flatPt - start).DotProduct(dir);
                if (d > 1e-6 && d < best) { best = d; bestPt = flatPt; }
            }
            return best == double.MaxValue ? null : bestPt;
        }
    }

    // Form for selecting polylines and extra offsets
    public class SampleLineOffsetPolylineForm : Form
    {
        public ObjectId LeftPolylineId { get; private set; } = ObjectId.Null;
        public ObjectId RightPolylineId { get; private set; } = ObjectId.Null;
        public double LeftExcess { get; private set; } = 0.0;
        public double RightExcess { get; private set; } = 0.0;

        private System.Windows.Forms.Label lblLeftPl;
        private System.Windows.Forms.Label lblRightPl;
        private TextBox txtLeftExcess;
        private TextBox txtRightExcess;
        private Button btnPickLeft;
        private Button btnPickRight;
        private Button btnOK;
        private Button btnCancel;

        public SampleLineOffsetPolylineForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Offset Sample Line theo Polyline";
            this.Size = new System.Drawing.Size(400, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var gbLeft = new GroupBox { Text = "Bên Trái", Location = new System.Drawing.Point(10, 10), Size = new System.Drawing.Size(360, 75) };
            lblLeftPl = new System.Windows.Forms.Label { Text = "Chưa chọn Polyline", Location = new System.Drawing.Point(10, 20), Size = new System.Drawing.Size(200, 20) };
            btnPickLeft = new Button { Text = "Chọn Model", Location = new System.Drawing.Point(220, 15), Size = new System.Drawing.Size(130, 25) };
            btnPickLeft.Click += (s, e) => PickPolyline(true);
            var lblL2 = new System.Windows.Forms.Label { Text = "Khoảng vượt:", Location = new System.Drawing.Point(10, 48), Size = new System.Drawing.Size(100, 20) };
            txtLeftExcess = new TextBox { Text = "0.0", Location = new System.Drawing.Point(110, 45), Size = new System.Drawing.Size(100, 20) };

            gbLeft.Controls.AddRange(new Control[] { lblLeftPl, btnPickLeft, lblL2, txtLeftExcess });

            var gbRight = new GroupBox { Text = "Bên Phải", Location = new System.Drawing.Point(10, 90), Size = new System.Drawing.Size(360, 75) };
            lblRightPl = new System.Windows.Forms.Label { Text = "Chưa chọn Polyline", Location = new System.Drawing.Point(10, 20), Size = new System.Drawing.Size(200, 20) };
            btnPickRight = new Button { Text = "Chọn Model", Location = new System.Drawing.Point(220, 15), Size = new System.Drawing.Size(130, 25) };
            btnPickRight.Click += (s, e) => PickPolyline(false);
            var lblR2 = new System.Windows.Forms.Label { Text = "Khoảng vượt:", Location = new System.Drawing.Point(10, 48), Size = new System.Drawing.Size(100, 20) };
            txtRightExcess = new TextBox { Text = "0.0", Location = new System.Drawing.Point(110, 45), Size = new System.Drawing.Size(100, 20) };

            gbRight.Controls.AddRange(new Control[] { lblRightPl, btnPickRight, lblR2, txtRightExcess });

            btnOK = new Button { Text = "OK", Location = new System.Drawing.Point(210, 175), Size = new System.Drawing.Size(75, 25) };
            btnOK.Click += BtnOK_Click;
            btnCancel = new Button { Text = "Hủy", Location = new System.Drawing.Point(295, 175), Size = new System.Drawing.Size(75, 25) };
            btnCancel.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] { gbLeft, gbRight, btnOK, btnCancel });
        }

        private void PickPolyline(bool isLeft)
        {
            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            this.Visible = false;
            try
            {
                var peo = new PromptEntityOptions($"\nChọn Polyline biên {(isLeft ? "TRÁI" : "PHẢI")}: ");
                peo.SetRejectMessage("\nPhải là Polyline.");
                peo.AddAllowedClass(typeof(Polyline), true);
                var per = ed.GetEntity(peo);
                if (per.Status == PromptStatus.OK)
                {
                    if (isLeft)
                    {
                        LeftPolylineId = per.ObjectId;
                        lblLeftPl.Text = $"Đã chọn: {per.ObjectId.Handle}";
                    }
                    else
                    {
                        RightPolylineId = per.ObjectId;
                        lblRightPl.Text = $"Đã chọn: {per.ObjectId.Handle}";
                    }
                }
            }
            finally
            {
                this.Visible = true;
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (double.TryParse(txtLeftExcess.Text, out double l)) LeftExcess = l;
            if (double.TryParse(txtRightExcess.Text, out double r)) RightExcess = r;

            if (LeftPolylineId == ObjectId.Null && RightPolylineId == ObjectId.Null)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một Polyline biên.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
