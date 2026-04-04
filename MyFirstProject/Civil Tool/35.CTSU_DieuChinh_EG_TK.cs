// CTSU_DieuChinh_EG_TK — 2 lệnh:
//   Lệnh 1: CTSU_DieuChinh_EG_TK       → chọn section, tạo polyline, xóa điểm cũ
//   Lệnh 2: CTSU_DieuChinh_EG_TK_Apply → đọc polyline đã chỉnh, add điểm mới vào surface
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Acad = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using Civil = Autodesk.Civil.ApplicationServices;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.Civil.Runtime;
using Autodesk.Civil.Settings;
using CivSurface = Autodesk.Civil.DatabaseServices.TinSurface;
using Section = Autodesk.Civil.DatabaseServices.Section;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Label = Autodesk.Civil.DatabaseServices.Label;
using MyFirstProject.Extensions;
using MyFirstProject;

[assembly: CommandClass(typeof(Civil3DCsharp.CTSU_DieuChinh_EG_TK_Commands))]

namespace Civil3DCsharp
{
    public class CTSU_DieuChinh_EG_TK_Commands
    {
        // Static fields — lưu trạng thái giữa 2 lệnh
        private static ObjectId _polylineId = ObjectId.Null;
        private static ObjectId _surfaceId = ObjectId.Null;
        private static ObjectId _alignmentId = ObjectId.Null;
        private static ObjectId _sectionViewId = ObjectId.Null;
        private static double _station = 0;
        private static bool _isWaitingForApply = false;
        private static List<Point3d> _oldWorldPoints = new();

        // ═══════════════════════════════════════════════════════════════
        // LỆNH 1: Chọn section → tạo polyline → xóa điểm cũ khỏi surface
        // Sau khi chạy xong, user tự do chỉnh sửa polyline
        // Rồi chạy lệnh CTSU_DieuChinh_EG_TK_Apply
        // ═══════════════════════════════════════════════════════════════
        [CommandMethod("CTSU_DieuChinh_EG_TK")]
        public static void CTSUDieuChinhEGTK()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                CivilDocument civDoc = CivilApplication.ActiveDocument;

                // Step 1: Chọn section
                PromptEntityOptions peo = new("\nChọn section trên trắc ngang: ");
                peo.SetRejectMessage("\nĐối tượng không phải section.");
                peo.AddAllowedClass(typeof(Section), true);
                PromptEntityResult per = A.Ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK) { A.Ed.WriteMessage("\nĐã hủy."); return; }

                ObjectId sectionId = per.ObjectId;
                Section? section = tr.GetObject(sectionId, OpenMode.ForWrite) as Section;
                if (section == null) { A.Ed.WriteMessage("\nKhông lấy được section."); return; }

                // Step 2: Tìm hierarchy (Alignment → SLG → SL → Surface)
                SampleLineGroup? sampleLineGroup = null;
                SampleLine? currentSampleLine = null;
                Alignment? alignment = null;
                ObjectId sourceId = ObjectId.Null;
                SectionView? sectionView = null;
                bool found = false;

                foreach (ObjectId alId in civDoc.GetAlignmentIds())
                {
                    if (found) break;
                    Alignment? al = tr.GetObject(alId, OpenMode.ForWrite) as Alignment;
                    if (al == null) continue;
                    foreach (ObjectId slgId in al.GetSampleLineGroupIds())
                    {
                        if (found) break;
                        SampleLineGroup? slg = tr.GetObject(slgId, OpenMode.ForWrite) as SampleLineGroup;
                        if (slg == null) continue;
                        SectionSourceCollection sources = slg.GetSectionSources();
                        foreach (ObjectId slId in slg.GetSampleLineIds())
                        {
                            if (found) break;
                            SampleLine? sl = tr.GetObject(slId, OpenMode.ForWrite) as SampleLine;
                            if (sl == null) continue;
                            foreach (SectionSource src in sources)
                            {
                                try
                                {
                                    if (sl.GetSectionId(src.SourceId) == sectionId)
                                    {
                                        currentSampleLine = sl;
                                        sampleLineGroup = slg;
                                        alignment = al;
                                        sourceId = src.SourceId;
                                        found = true;
                                        break;
                                    }
                                }
                                catch { continue; }
                            }
                        }
                    }
                }

                if (currentSampleLine == null || sampleLineGroup == null || alignment == null)
                { A.Ed.WriteMessage("\nKhông tìm thấy thông tin section."); return; }

                // Tìm section view
                foreach (SectionViewGroup svGroup in sampleLineGroup.SectionViewGroups)
                {
                    if (sectionView != null) break;
                    foreach (ObjectId svId in svGroup.GetSectionViewIds())
                    {
                        SectionView? sv = tr.GetObject(svId, OpenMode.ForWrite) as SectionView;
                        if (sv != null && sv.SampleLineId == currentSampleLine.ObjectId)
                        { sectionView = sv; break; }
                    }
                }
                if (sectionView == null) { A.Ed.WriteMessage("\nKhông tìm thấy section view."); return; }

                CivSurface? surface = tr.GetObject(sourceId, OpenMode.ForWrite) as CivSurface;
                if (surface == null) { A.Ed.WriteMessage("\nKhông tìm thấy surface nguồn."); return; }

                A.Ed.WriteMessage($"\n📋 Surface: '{surface.Name}' | Station: {currentSampleLine.Station:F3}");

                // Step 3: Tạo polyline từ section + tính world coords
                SectionPointCollection sectionPoints = section.SectionPoints;
                Point3d svLoc = sectionView.Location;
                bool wasAutoElev = sectionView.IsElevationRangeAutomatic;
                sectionView.IsElevationRangeAutomatic = false;
                double elevDatum = sectionView.ElevationMin;

                Polyline polyline = new();
                int vIdx = 0;
                List<Point3d> oldWorldPoints = new();

                foreach (SectionPoint sp in sectionPoints)
                {
                    Point3d loc = sp.Location;
                    double offset = loc.X;
                    double elevation = loc.Y;

                    polyline.AddVertexAt(vIdx++, new Point2d(
                        svLoc.X + offset,
                        svLoc.Y + (elevation - elevDatum)), 0, 0, 0);

                    double easting = 0, northing = 0;
                    alignment.PointLocation(currentSampleLine.Station, offset, ref easting, ref northing);
                    oldWorldPoints.Add(new Point3d(easting, northing, elevation));
                }

                sectionView.IsElevationRangeAutomatic = wasAutoElev;

                // Add polyline to model space (màu đỏ để dễ nhận biết)
                polyline.ColorIndex = 1;
                polyline.Layer = "0";
                BlockTable? bt = tr.GetObject(A.Db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord? ms = tr.GetObject(bt![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                ObjectId newPolyId = ms!.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline, true);

                A.Ed.WriteMessage($"\n✅ Đã tạo polyline ({vIdx} điểm, màu đỏ).");

                // Lưu trạng thái cho lệnh Apply (chưa xóa điểm — sẽ xóa + add cùng lúc trong Apply)
                _polylineId = newPolyId;
                _surfaceId = sourceId;
                _alignmentId = alignment.ObjectId;
                _sectionViewId = sectionView.ObjectId;
                _station = currentSampleLine.Station;
                _oldWorldPoints = oldWorldPoints;
                _isWaitingForApply = true;

                tr.Commit();

                A.Ed.WriteMessage("\n\n══════════════════════════════════════");
                A.Ed.WriteMessage("\n✏️  Bây giờ hãy chỉnh sửa polyline (màu đỏ).");
                A.Ed.WriteMessage("\n   Khi xong, chạy lệnh: CTSU_DieuChinh_EG_TK_Apply");
                A.Ed.WriteMessage("\n══════════════════════════════════════");
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\nLỗi AutoCAD: {e.Message}");
                tr.Abort();
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi: {ex.Message}\n{ex.StackTrace}");
                tr.Abort();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // LỆNH 2: Đọc polyline đã chỉnh sửa → add điểm mới vào surface
        // ═══════════════════════════════════════════════════════════════
        [CommandMethod("CTSU_DieuChinh_EG_TK_Apply")]
        public static void CTSUDieuChinhEGTKApply()
        {
            if (!_isWaitingForApply || _polylineId == ObjectId.Null)
            {
                A.Ed.WriteMessage("\n⚠ Chưa có polyline nào cần apply.");
                A.Ed.WriteMessage("\n   Hãy chạy lệnh CTSU_DieuChinh_EG_TK trước.");
                return;
            }

            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                Polyline? editedPoly = tr.GetObject(_polylineId, OpenMode.ForWrite) as Polyline;
                SectionView? sv = tr.GetObject(_sectionViewId, OpenMode.ForWrite) as SectionView;
                Alignment? al = tr.GetObject(_alignmentId, OpenMode.ForWrite) as Alignment;
                CivSurface? surface = tr.GetObject(_surfaceId, OpenMode.ForWrite) as CivSurface;

                if (editedPoly == null || sv == null || al == null || surface == null)
                {
                    A.Ed.WriteMessage("\nKhông truy cập được đối tượng. Polyline có thể đã bị xóa.");
                    _isWaitingForApply = false;
                    return;
                }

                // Transform polyline vertices → world coordinates
                List<Point3d> newWorldPoints = TransformPolylineVertices(sv, editedPoly, _station, al);

                if (newWorldPoints.Count < 2)
                {
                    A.Ed.WriteMessage("\nKhông đủ điểm (cần ≥ 2).");
                    tr.Abort();
                    return;
                }

                // Step 1: Remove điểm cũ
                int removedCount = 0;
                foreach (Point3d pt in _oldWorldPoints)
                {
                    try
                    {
                        TinSurfaceVertex vertex = surface.FindVertexAtXY(pt.X, pt.Y);
                        if (vertex != null)
                        {
                            surface.DeleteVertex(vertex);
                            removedCount++;
                        }
                    }
                    catch { }
                }
                A.Ed.WriteMessage($"\n🗑️ Đã xóa {removedCount}/{_oldWorldPoints.Count} điểm cũ.");

                // Step 2: Add điểm mới
                Point3dCollection pts = new();
                foreach (Point3d pt in newWorldPoints) pts.Add(pt);

                surface.AddVertices(pts);
                surface.Rebuild();

                A.Ed.WriteMessage($"\n✅ Đã thêm {newWorldPoints.Count} điểm mới vào surface '{surface.Name}'.");

                // Xóa polyline tạm
                editedPoly.Erase();
                A.Ed.WriteMessage("\n🗑️ Đã xóa polyline tạm.");

                // Reset trạng thái
                _isWaitingForApply = false;
                _polylineId = ObjectId.Null;

                tr.Commit();
                A.Ed.WriteMessage("\n✅ Hoàn thành điều chỉnh surface!");
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\nLỗi AutoCAD: {e.Message}");
                tr.Abort();
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi: {ex.Message}\n{ex.StackTrace}");
                tr.Abort();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Helper: Transform polyline vertices (section view space → world)
        // ═══════════════════════════════════════════════════════════════
        private static List<Point3d> TransformPolylineVertices(
            SectionView sectionView, Polyline polyline, double station, Alignment alignment)
        {
            var worldPoints = new List<Point3d>();
            Point3d svLoc = sectionView.Location;
            bool wasAutoElev = sectionView.IsElevationRangeAutomatic;

            try
            {
                sectionView.IsElevationRangeAutomatic = false;
                double elevDatum = sectionView.ElevationMin;

                for (int i = 0; i < polyline.NumberOfVertices; i++)
                {
                    Point2d vertex = polyline.GetPoint2dAt(i);
                    double offset = vertex.X - svLoc.X;
                    double elevation = vertex.Y - svLoc.Y + elevDatum;

                    double easting = 0, northing = 0;
                    alignment.PointLocation(station, offset, ref easting, ref northing);
                    worldPoints.Add(new Point3d(easting, northing, elevation));
                }
            }
            finally
            {
                sectionView.IsElevationRangeAutomatic = wasAutoElev;
            }

            return worldPoints;
        }
    }
}
