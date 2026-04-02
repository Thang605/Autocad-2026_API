// (C) Copyright 2015 by  
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Acad = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using ATable = Autodesk.AutoCAD.DatabaseServices.Table;

using Civil = Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.Civil.Runtime;
using Autodesk.Civil.Settings;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Civil.ApplicationServices;
using CivSurface = Autodesk.Civil.DatabaseServices.TinSurface;
using Section = Autodesk.Civil.DatabaseServices.Section;
using Autodesk.Civil;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using MyFirstProject.Extensions;
using MyFirstProject;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.CTSV_DieuChinh_DuongEG_Commands))]

namespace Civil3DCsharp
{
    public class CTSV_DieuChinh_DuongEG_Commands
    {
        [CommandMethod("CTSV_DieuChinh_DuongEG")]
        public static void CTSVDieuChinhDuongEG()
        {
            // start transaction
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                CivilDocument civilDoc = CivilApplication.ActiveDocument;

                // Step 1: Choose section directly
                A.Ed.WriteMessage("\nChọn section của cọc cần hiệu chỉnh:");
                PromptEntityOptions peo = new("\nChọn section của cọc cần hiệu chỉnh: ");
                peo.SetRejectMessage("\nĐối tượng được chọn không phải là section.");
                peo.AddAllowedClass(typeof(Section), true);
                
                PromptEntityResult per = A.Ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                {
                    A.Ed.WriteMessage("\nĐã hủy lệnh.");
                    return;
                }

                ObjectId sectionId = per.ObjectId;
                Section? section = tr.GetObject(sectionId, OpenMode.ForWrite) as Section;
                if (section == null)
                {
                    A.Ed.WriteMessage("\nKhông thể lấy thông tin section.");
                    return;
                }

                // Step 2: Get section view from section and find source surface, sample line group
                SectionView? sectionView = null;
                CivSurface? sourceSurface = null;
                SampleLineGroup? sampleLineGroup = null;
                SampleLine? currentSampleLine = null;
                
                BlockTable? bt = tr.GetObject(A.Db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                if (bt == null)
                {
                    A.Ed.WriteMessage("\nKhông thể truy cập Block Table.");
                    return;
                }

                BlockTableRecord? btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                if (btr == null)
                {
                    A.Ed.WriteMessage("\nKhông thể truy cập Model Space.");
                    return;
                }
                
                foreach (ObjectId entId in btr)
                {
                    try
                    {
                        if (tr.GetObject(entId, OpenMode.ForWrite) is SectionView sv)
                        {
                            ObjectId svSampleLineId = sv.SampleLineId;
                            SampleLine? sl = tr.GetObject(svSampleLineId, OpenMode.ForWrite) as SampleLine;
                            if (sl != null)
                            {
                                ObjectId slGroupId = sl.GroupId;
                                SampleLineGroup? slGroup = tr.GetObject(slGroupId, OpenMode.ForWrite) as SampleLineGroup;
                                if (slGroup != null)
                                {
                                    SectionSourceCollection sectionSources = slGroup.GetSectionSources();
                                    foreach (SectionSource source in sectionSources)
                                    {
                                        try
                                        {
                                            ObjectId testSectionId = sl.GetSectionId(source.SourceId);
                                            if (testSectionId == sectionId)
                                            {
                                                sectionView = sv;
                                                currentSampleLine = sl;
                                                sampleLineGroup = slGroup;
                                                
                                                try
                                                {
                                                    CivSurface? surf = tr.GetObject(source.SourceId, OpenMode.ForWrite) as CivSurface;
                                                    if (surf != null)
                                                    {
                                                        sourceSurface = surf;
                                                        A.Ed.WriteMessage($"\nĐã tìm thấy surface nguồn: '{surf.Name}'");
                                                    }
                                                }
                                                catch { }
                                                break;
                                            }
                                        }
                                        catch { continue; }
                                    }
                                    if (sectionView != null) break;
                                }
                            }
                        }
                    }
                    catch { continue; }
                }

                if (sectionView == null || currentSampleLine == null || sampleLineGroup == null)
                {
                    A.Ed.WriteMessage("\nKhông tìm thấy section view chứa section này.");
                    return;
                }

                // Get parent alignment
                ObjectId alignmentId = currentSampleLine.GetParentAlignmentId();
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
                if (alignment == null)
                {
                    A.Ed.WriteMessage("\nKhông thể lấy thông tin alignment.");
                    return;
                }

                // Find previous and next sample lines
                double currentStation = currentSampleLine.Station;
                SampleLine? previousSampleLine = null;
                SampleLine? nextSampleLine = null;

                ObjectIdCollection sampleLineIds = sampleLineGroup.GetSampleLineIds();
                var sampleLinesWithStations = new List<(ObjectId id, double station, SampleLine sampleLine)>();

                foreach (ObjectId slId in sampleLineIds)
                {
                    try
                    {
                        SampleLine? sl = tr.GetObject(slId, OpenMode.ForRead) as SampleLine;
                        if (sl != null)
                            sampleLinesWithStations.Add((slId, sl.Station, sl));
                    }
                    catch { continue; }
                }

                sampleLinesWithStations.Sort((a, b) => a.station.CompareTo(b.station));

                for (int i = 0; i < sampleLinesWithStations.Count; i++)
                {
                    if (Math.Abs(sampleLinesWithStations[i].station - currentStation) < 0.001)
                    {
                        if (i > 0)
                            previousSampleLine = sampleLinesWithStations[i - 1].sampleLine;
                        if (i < sampleLinesWithStations.Count - 1)
                            nextSampleLine = sampleLinesWithStations[i + 1].sampleLine;
                        break;
                    }
                }

                // ===== Show Form =====
                DieuChinhDuongEGForm form = new();
                form.SetContextInfo(
                    alignmentName: alignment.Name,
                    sampleLineName: currentSampleLine.Name,
                    station: currentStation,
                    sourceSurfaceName: sourceSurface?.Name ?? "",
                    sourceSurfaceId: sourceSurface?.ObjectId ?? ObjectId.Null,
                    previousStation: previousSampleLine?.Station,
                    nextStation: nextSampleLine?.Station
                );

                var result = Application.ShowModalDialog(form);
                if (result != System.Windows.Forms.DialogResult.OK)
                {
                    A.Ed.WriteMessage("\nĐã hủy lệnh.");
                    return;
                }

                // Get form results
                ObjectId targetSurfaceId = form.SelectedTargetSurfaceId;
                bool processAdjacent = form.ProcessAdjacentSections;

                CivSurface targetSurface = tr.GetObject(targetSurfaceId, OpenMode.ForWrite) as CivSurface
                    ?? throw new System.Exception("Không thể mở surface đích để ghi.");
                A.Ed.WriteMessage($"\nSurface đích: '{targetSurface.Name}'");

                // Get or create Site named after alignment
                string siteName = alignment.Name;
                ObjectId siteId = GetOrCreateSite(tr, civilDoc, siteName);
                A.Ed.WriteMessage($"\nSite: '{siteName}'");

                // Process adjacent sections if enabled
                if (processAdjacent)
                {
                    Section? previousSection = null;
                    Section? nextSection = null;

                    if (sourceSurface != null && (previousSampleLine != null || nextSampleLine != null))
                    {
                        SectionSourceCollection sources = sampleLineGroup.GetSectionSources();
                        foreach (SectionSource source in sources)
                        {
                            try
                            {
                                if (tr.GetObject(source.SourceId, OpenMode.ForRead) is CivSurface surf && surf.ObjectId == sourceSurface.ObjectId)
                                {
                                    if (previousSampleLine != null)
                                    {
                                        try
                                        {
                                            ObjectId prevSectionId = previousSampleLine.GetSectionId(source.SourceId);
                                            previousSection = tr.GetObject(prevSectionId, OpenMode.ForRead) as Section;
                                        }
                                        catch { }
                                    }

                                    if (nextSampleLine != null)
                                    {
                                        try
                                        {
                                            ObjectId nextSectionId = nextSampleLine.GetSectionId(source.SourceId);
                                            nextSection = tr.GetObject(nextSectionId, OpenMode.ForRead) as Section;
                                        }
                                        catch { }
                                    }
                                    break;
                                }
                            }
                            catch { continue; }
                        }
                    }

                    if (previousSection != null && previousSampleLine != null)
                    {
                        A.Ed.WriteMessage("\n=== Xử lý section trước ===");
                        ProcessSectionToFeatureLine(tr, previousSection, previousSampleLine, sectionView,
                            targetSurface, alignment, bt, siteId);
                    }

                    if (nextSection != null && nextSampleLine != null)
                    {
                        A.Ed.WriteMessage("\n=== Xử lý section sau ===");
                        ProcessSectionToFeatureLine(tr, nextSection, nextSampleLine, sectionView,
                            targetSurface, alignment, bt, siteId);
                    }

                    A.Ed.WriteMessage("\n=== Hoàn thành xử lý section tham chiếu ===");
                }

                // Step 3: Choose existing polyline (created by AT_PolylineFromSection)
                A.Ed.WriteMessage("\nChọn polyline đã chỉnh sửa (tạo bởi lệnh AT_PolylineFromSection):");
                PromptEntityOptions polylinePeo = new("\nChọn polyline: ");
                polylinePeo.SetRejectMessage("\nĐối tượng được chọn không phải là polyline.");
                polylinePeo.AddAllowedClass(typeof(Polyline), true);
                
                PromptEntityResult polylinePer = A.Ed.GetEntity(polylinePeo);
                if (polylinePer.Status != PromptStatus.OK)
                {
                    A.Ed.WriteMessage("\nĐã hủy lệnh.");
                    return;
                }

                ObjectId polylineId = polylinePer.ObjectId;
                Polyline? selectedPolyline = tr.GetObject(polylineId, OpenMode.ForWrite) as Polyline;
                if (selectedPolyline == null)
                {
                    A.Ed.WriteMessage("\nKhông thể lấy thông tin polyline.");
                    return;
                }

                A.Ed.WriteMessage($"\nĐã chọn polyline với {selectedPolyline.NumberOfVertices} điểm.");

                // Step 4: Transform polyline vertices to world coordinates
                double station = currentSampleLine.Station;
                List<Point3d> adjustedPoints = TransformPolylineVertices(sectionView, selectedPolyline, station, alignment);

                if (adjustedPoints.Count < 2)
                {
                    A.Ed.WriteMessage("\nKhông đủ điểm để tạo Feature Line (cần ít nhất 2 điểm).");
                    return;
                }

                // Step 5: Create 3D Polyline as source for Feature Line
                Point3dCollection point3dCollection = new Point3dCollection();
                foreach (Point3d point in adjustedPoints)
                {
                    point3dCollection.Add(point);
                }
                
                Polyline3d polyline3d = new Polyline3d(Poly3dType.SimplePoly, point3dCollection, false);
                polyline3d.Layer = "0";

                BlockTableRecord? modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                if (modelSpace == null)
                {
                    A.Ed.WriteMessage("\nKhông thể truy cập Model Space để ghi.");
                    return;
                }

                ObjectId polyline3dId = modelSpace.AppendEntity(polyline3d);
                tr.AddNewlyCreatedDBObject(polyline3d, true);

                // Step 6: Create Feature Line from 3D Polyline
                string featureLineName = $"{alignment.Name}-{currentSampleLine.Name}";
                A.Ed.WriteMessage($"\nTên Feature Line: '{featureLineName}'");

                // Delete existing feature line with same name if exists
                DeleteExistingFeatureLine(tr, siteId, featureLineName);

                try
                {
                    ObjectId featureLineId = FeatureLine.Create(featureLineName, polyline3dId, siteId);
                    
                    A.Ed.WriteMessage($"\nĐã tạo Feature Line '{featureLineName}' với {adjustedPoints.Count} điểm.");

                    // Step 7: Add Feature Line as breakline to target surface
                    try
                    {
                        ObjectIdCollection breaklineIds = new ObjectIdCollection();
                        breaklineIds.Add(featureLineId);
                        
                        targetSurface.BreaklinesDefinition.AddStandardBreaklines(breaklineIds, 15.0, 100.0, 1.0, 0.0);
                        
                        A.Ed.WriteMessage($"\nĐã thêm Feature Line làm breakline vào surface '{targetSurface.Name}'.");
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\nLỗi khi thêm breakline vào surface: {ex.Message}");
                    }
                }
                catch (System.Exception ex)
                {
                    A.Ed.WriteMessage($"\nLỗi khi tạo Feature Line: {ex.Message}");
                    return;
                }

                A.Ed.WriteMessage($"\nĐã hoàn thành điều chỉnh đường EG cho surface '{targetSurface.Name}'.");

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\nLỗi AutoCAD: {e.Message}");
                A.Ed.WriteMessage($"\nError Code: {e.ErrorStatus}");
                tr.Abort();
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi hệ thống: {ex.Message}");
                A.Ed.WriteMessage($"\nStack trace: {ex.StackTrace}");
                tr.Abort();
            }
        }

        /// <summary>
        /// Get existing Site by name, or create a new one
        /// </summary>
        private static ObjectId GetOrCreateSite(Transaction tr, CivilDocument civilDoc, string siteName)
        {
            ObjectIdCollection siteIds = civilDoc.GetSiteIds();
            foreach (ObjectId id in siteIds)
            {
                try
                {
                    Site? site = tr.GetObject(id, OpenMode.ForRead) as Site;
                    if (site != null && site.Name == siteName)
                        return id;
                }
                catch { continue; }
            }

            ObjectId newSiteId = Site.Create(civilDoc, siteName);
            A.Ed.WriteMessage($"\nĐã tạo Site mới: '{siteName}'");
            return newSiteId;
        }

        /// <summary>
        /// Delete existing Feature Line with the same name in the specified Site
        /// </summary>
        private static void DeleteExistingFeatureLine(Transaction tr, ObjectId siteId, string featureLineName)
        {
            try
            {
                Site? site = tr.GetObject(siteId, OpenMode.ForRead) as Site;
                if (site == null) return;

                ObjectIdCollection flIds = site.GetFeatureLineIds();
                foreach (ObjectId flId in flIds)
                {
                    try
                    {
                        FeatureLine? fl = tr.GetObject(flId, OpenMode.ForWrite) as FeatureLine;
                        if (fl != null && fl.Name == featureLineName)
                        {
                            A.Ed.WriteMessage($"\nĐã tìm thấy Feature Line trùng tên '{featureLineName}', đang xóa...");
                            fl.Erase();
                            A.Ed.WriteMessage($"\nĐã xóa Feature Line cũ '{featureLineName}'.");
                            break;
                        }
                    }
                    catch { continue; }
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nCảnh báo khi kiểm tra Feature Line trùng tên: {ex.Message}");
            }
        }

        private static void ProcessSectionToFeatureLine(Transaction tr, Section section, SampleLine sampleLine, 
            SectionView sectionView, CivSurface targetSurface, Alignment alignment, BlockTable bt, ObjectId siteId)
        {
            try
            {
                SectionPointCollection sectionPoints = section.SectionPoints;
                if (sectionPoints.Count < 2)
                {
                    A.Ed.WriteMessage($"\nSection tại station {sampleLine.Station:F3} không có đủ điểm.");
                    return;
                }

                Polyline? sectionPolyline = CreatePolylineFromSection(sectionView, sectionPoints);
                if (sectionPolyline == null)
                {
                    A.Ed.WriteMessage($"\nKhông thể tạo polyline từ section tại station {sampleLine.Station:F3}.");
                    return;
                }

                double station = sampleLine.Station;
                List<Point3d> adjustedPoints = TransformPolylineVertices(sectionView, sectionPolyline, station, alignment);

                if (adjustedPoints.Count < 2)
                {
                    A.Ed.WriteMessage($"\nKhông đủ điểm để tạo Feature Line tại station {station:F3}.");
                    sectionPolyline.Dispose();
                    return;
                }

                Point3dCollection point3dCollection = new Point3dCollection();
                foreach (Point3d point in adjustedPoints)
                    point3dCollection.Add(point);
                
                Polyline3d polyline3d = new Polyline3d(Poly3dType.SimplePoly, point3dCollection, false);
                polyline3d.Layer = "0";

                BlockTableRecord? modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                if (modelSpace == null)
                {
                    sectionPolyline.Dispose();
                    polyline3d.Dispose();
                    return;
                }

                ObjectId polyline3dId = modelSpace.AppendEntity(polyline3d);
                tr.AddNewlyCreatedDBObject(polyline3d, true);

                string featureLineName = $"{alignment.Name}-{sampleLine.Name}";

                DeleteExistingFeatureLine(tr, siteId, featureLineName);

                try
                {
                    ObjectId featureLineId = FeatureLine.Create(featureLineName, polyline3dId, siteId);
                    A.Ed.WriteMessage($"\nĐã tạo Feature Line '{featureLineName}' tại station {station:F3}.");

                    try
                    {
                        ObjectIdCollection breaklineIds = new ObjectIdCollection();
                        breaklineIds.Add(featureLineId);
                        targetSurface.BreaklinesDefinition.AddStandardBreaklines(breaklineIds, 15.0, 100.0, 1.0, 0.0);
                        A.Ed.WriteMessage($"\nĐã thêm Feature Line '{featureLineName}' làm breakline vào surface '{targetSurface.Name}'.");
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\nLỗi khi thêm breakline tại station {station:F3}: {ex.Message}");
                    }
                }
                catch (System.Exception ex)
                {
                    A.Ed.WriteMessage($"\nLỗi khi tạo Feature Line tại station {station:F3}: {ex.Message}");
                }

                sectionPolyline.Dispose();
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi xử lý section tại station {sampleLine.Station:F3}: {ex.Message}");
            }
        }

        private static Polyline? CreatePolylineFromSection(SectionView sectionView, SectionPointCollection sectionPoints)
        {
            try
            {
                Point3d sectionViewLocation = sectionView.Location;
                bool wasElevationAutomatic = sectionView.IsElevationRangeAutomatic;
                try
                {
                    sectionView.IsElevationRangeAutomatic = false;
                    double elevationDatum = sectionView.ElevationMin;

                    Polyline polyline = new();
                    int vertexIndex = 0;

                    foreach (SectionPoint sectionPoint in sectionPoints)
                    {
                        Point3d sectionLocation = sectionPoint.Location;
                        double x = sectionViewLocation.X + sectionLocation.X;
                        double y = sectionViewLocation.Y + (sectionLocation.Y - elevationDatum);
                        polyline.AddVertexAt(vertexIndex, new Autodesk.AutoCAD.Geometry.Point2d(x, y), 0, 0, 0);
                        vertexIndex++;
                    }
                    return polyline;
                }
                finally
                {
                    sectionView.IsElevationRangeAutomatic = wasElevationAutomatic;
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi tạo polyline từ section: {ex.Message}");
                return null;
            }
        }

        private static List<Point3d> TransformPolylineVertices(SectionView sectionView, Polyline polyline, 
            double station, Alignment alignment)
        {
            if (sectionView == null || polyline == null || alignment == null)
                throw new ArgumentNullException("Invalid parameters for coordinate transformation");

            var worldPoints = new List<Point3d>();
            
            try
            {
                Point3d sectionViewLocation = sectionView.Location;
                bool wasElevationAutomatic = sectionView.IsElevationRangeAutomatic;
                
                try
                {
                    sectionView.IsElevationRangeAutomatic = false;
                    double elevationDatum = sectionView.ElevationMin;

                    for (int i = 0; i < polyline.NumberOfVertices; i++)
                    {
                        Autodesk.AutoCAD.Geometry.Point2d vertex = polyline.GetPoint2dAt(i);
                        double offset = vertex.X - sectionViewLocation.X;
                        double elevation = vertex.Y - sectionViewLocation.Y + elevationDatum;

                        double easting = 0, northing = 0;
                        alignment.PointLocation(station, offset, ref easting, ref northing);
                        worldPoints.Add(new Point3d(easting, northing, elevation));
                    }
                }
                finally
                {
                    sectionView.IsElevationRangeAutomatic = wasElevationAutomatic;
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi trong quá trình chuyển đổi tọa độ: {ex.Message}");
                throw;
            }
            
            return worldPoints;
        }
    }
}
