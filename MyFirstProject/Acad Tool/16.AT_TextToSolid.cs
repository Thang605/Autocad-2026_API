// (C) Copyright 2015 by  
//
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;
using System.IO;

using Autodesk.AutoCAD.Runtime;
using Acad = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using MyFirstProject.Extensions;
using WinFormsLabel = System.Windows.Forms.Label;
using AcadColor = Autodesk.AutoCAD.Colors.Color;
using AcadRegion = Autodesk.AutoCAD.DatabaseServices.Region;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.AT_TextToSolid_Commands))]

namespace Civil3DCsharp
{
    public class AT_TextToSolid_Commands
    {
        // Biến lưu trữ kết quả TXTEXP
        private static HashSet<long> _beforeHandles = new HashSet<long>();
        private static List<ObjectId> _textIdsToProcess = new List<ObjectId>();
        private static bool _useCustomColor = false;
        private static AcadColor? _solidColor = null;
        private static bool _create3DSolid = false;
        private static double _extrusionHeight = 1.0;
        
        // Lưu thông tin text gốc để scale và move
        private static Point3d _originalTextPosition = Point3d.Origin;
        private static double _originalTextHeight = 1.0;
        private static double _originalTextRotation = 0.0;
        private static string _originalTextLayer = "0";
        private static AcadColor _originalTextColor = AcadColor.FromColorIndex(ColorMethod.ByAci, 7);
        private static Extents3d _originalTextExtents;

        /// <summary>
        /// Lệnh chính: Chuyển Text (DBText/MText) thành Solid Hatch
        /// Quy trình 2 bước: Step 1 chạy TXTEXP, Step 2 tạo hatch với scale/move
        /// </summary>
        [CommandMethod("AT_TextToSolid")]
        public static void AT_TextToSolid()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n═══════════════════════════════════════════════════════════════");
            ed.WriteMessage("\n  AT_TextToSolid - Chuyển Text thành Solid Hatch");
            ed.WriteMessage("\n═══════════════════════════════════════════════════════════════");

            // Hiển thị form để cấu hình
            using (var form = new TextToSolidForm())
            {
                // Chọn text trước
                List<ObjectId> textIds = SelectTexts(ed);
                if (textIds.Count == 0)
                {
                    ed.WriteMessage("\nKhông có text nào được chọn. Hủy lệnh.");
                    return;
                }

                form.SelectedTextCount = textIds.Count;

                if (form.ShowDialog() != DialogResult.OK)
                {
                    ed.WriteMessage("\nĐã hủy lệnh.");
                    return;
                }

                // Lưu cấu hình
                _useCustomColor = form.UseCustomColor;
                _solidColor = form.SolidColor;
                _create3DSolid = form.Create3DSolid;
                _extrusionHeight = form.ExtrusionHeight;
                _textIdsToProcess = textIds;

                // Lấy thông tin text gốc TRƯỚC KHI chạy TXTEXP
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity textEnt = (Entity)tr.GetObject(textIds[0], OpenMode.ForRead);
                    _originalTextLayer = textEnt.Layer;
                    _originalTextColor = textEnt.Color;
                    _originalTextExtents = textEnt.GeometricExtents;

                    if (textEnt is DBText dbText)
                    {
                        _originalTextPosition = dbText.Position;
                        _originalTextHeight = dbText.Height;
                        _originalTextRotation = dbText.Rotation;
                        ed.WriteMessage($"\n  Text gốc: Height={_originalTextHeight:F2}, Position=({_originalTextPosition.X:F2}, {_originalTextPosition.Y:F2})");
                    }
                    else if (textEnt is MText mText)
                    {
                        _originalTextPosition = mText.Location;
                        _originalTextHeight = mText.TextHeight;
                        _originalTextRotation = mText.Rotation;
                        ed.WriteMessage($"\n  MText gốc: Height={_originalTextHeight:F2}, Position=({_originalTextPosition.X:F2}, {_originalTextPosition.Y:F2})");
                    }

                    tr.Commit();
                }

                // Lấy handles trước khi chạy TXTEXP
                _beforeHandles = GetAllHandles(db);

                ed.WriteMessage($"\n\n→ Đang chạy TXTEXP...");

                // Chọn tất cả text cần xử lý
                ed.SetImpliedSelection(textIds.ToArray());

                // Chạy TXTEXP rồi tự động gọi Step2
                doc.SendStringToExecute("_.TXTEXP\n_AT_TextToSolid_Step2\n", true, false, false);
            }
        }

        /// <summary>
        /// Bước 2: Tạo hatch từ polylines đã được tạo bởi TXTEXP, với scale và move
        /// </summary>
        [CommandMethod("AT_TextToSolid_Step2")]
        public static void AT_TextToSolid_Step2()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n\n→ Đang xử lý polylines từ TXTEXP...");

            if (_beforeHandles.Count == 0)
            {
                ed.WriteMessage("\n✗ Lỗi: Không có dữ liệu từ Bước 1. Vui lòng chạy lại AT_TextToSolid.");
                return;
            }

            // Tìm các objects mới được tạo bởi TXTEXP
            List<ObjectId> newCurveIds = new List<ObjectId>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId objId in btr)
                {
                    if (!_beforeHandles.Contains(objId.Handle.Value))
                    {
                        try
                        {
                            Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                            if (ent != null && !ent.IsErased)
                            {
                                if (ent is Polyline || ent is Polyline2d || ent is Line ||
                                    ent is Arc || ent is Spline || ent is Circle || ent is Ellipse)
                                {
                                    newCurveIds.Add(objId);
                                }
                            }
                        }
                        catch { }
                    }
                }
                tr.Commit();
            }

            ed.WriteMessage($"\n  Tìm thấy {newCurveIds.Count} đường nét từ TXTEXP.");

            if (newCurveIds.Count == 0)
            {
                ed.WriteMessage("\n✗ Không tìm thấy polylines. TXTEXP có thể đã thất bại.");
                ResetState();
                return;
            }

            // Tính toán scale factor và offset
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Lấy extents của polylines mới tạo
                Extents3d newExtents = new Extents3d();
                bool firstExtent = true;

                foreach (ObjectId id in newCurveIds)
                {
                    try
                    {
                        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                        if (firstExtent)
                        {
                            newExtents = ent.GeometricExtents;
                            firstExtent = false;
                        }
                        else
                        {
                            newExtents.AddExtents(ent.GeometricExtents);
                        }
                    }
                    catch { }
                }

                // Tính scale factor
                double newHeight = newExtents.MaxPoint.Y - newExtents.MinPoint.Y;
                double originalHeight = _originalTextExtents.MaxPoint.Y - _originalTextExtents.MinPoint.Y;
                double scaleFactor = originalHeight / newHeight;

                // Tính center của polylines mới (TXTEXP output)
                Point3d newCenter = new Point3d(
                    (newExtents.MinPoint.X + newExtents.MaxPoint.X) / 2,
                    (newExtents.MinPoint.Y + newExtents.MaxPoint.Y) / 2,
                    0);
                
                // Tính center của text gốc
                Point3d originalCenter = new Point3d(
                    (_originalTextExtents.MinPoint.X + _originalTextExtents.MaxPoint.X) / 2,
                    (_originalTextExtents.MinPoint.Y + _originalTextExtents.MaxPoint.Y) / 2,
                    0);

                ed.WriteMessage($"\n  Scale factor: {scaleFactor:F4}");
                ed.WriteMessage($"\n  New center: ({newCenter.X:F2}, {newCenter.Y:F2})");
                ed.WriteMessage($"\n  Original center: ({originalCenter.X:F2}, {originalCenter.Y:F2})");

                // Scale và Move các polylines
                ed.WriteMessage("\n  Đang scale và move polylines...");

                // Scale quanh center của polylines mới
                Matrix3d scaleMatrix = Matrix3d.Scaling(scaleFactor, newCenter);
                
                // Sau khi scale, center vẫn giữ nguyên (vì scale quanh chính nó)
                // Chỉ cần move từ newCenter đến originalCenter
                Vector3d moveVector = originalCenter - newCenter;
                moveVector = new Vector3d(moveVector.X, moveVector.Y, 0);
                
                // Thực hiện transformation: Scale trước, rồi Move
                Matrix3d transformMatrix = Matrix3d.Displacement(moveVector) * scaleMatrix;

                foreach (ObjectId id in newCurveIds)
                {
                    try
                    {
                        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                        ent.TransformBy(transformMatrix);
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n  Lỗi transform: {ex.Message}");
                    }
                }

                tr.Commit();
            }

            // Tạo output từ curves đã được scale/move
            bool success = false;
            
            if (_create3DSolid)
            {
                // Tạo 3D Solid
                success = Create3DSolidFromCurves(newCurveIds, db, ed, _originalTextLayer,
                    _useCustomColor, _solidColor, _originalTextColor, _extrusionHeight);
                    
                if (success)
                {
                    ed.WriteMessage($"\n\n✓ Đã tạo 3D Solid thành công (chiều cao: {_extrusionHeight})!");
                }
                else
                {
                    ed.WriteMessage("\n\n✗ Không thể tạo 3D Solid.");
                }
            }
            else
            {
                // Tạo Solid Hatch
                success = CreateHatchFromCurves(newCurveIds, db, ed, _originalTextLayer,
                    _useCustomColor, _solidColor, _originalTextColor);
                    
                if (success)
                {
                    ed.WriteMessage("\n\n✓ Đã tạo Solid Hatch thành công!");
                }
                else
                {
                    ed.WriteMessage("\n\n✗ Không thể tạo Solid Hatch.");
                }
            }

            if (success)
            {
                // Xóa các polylines sau khi tạo
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId id in newCurveIds)
                    {
                        try
                        {
                            Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                            if (!ent.IsErased)
                                ent.Erase();
                        }
                        catch { }
                    }
                    tr.Commit();
                }
            }

            // Reset state
            ResetState();
        }

        /// <summary>
        /// Lệnh đơn giản: Chọn polylines và tạo hatch (manual mode)
        /// </summary>
        [CommandMethod("AT_PolysToSolid")]
        public static void AT_PolysToSolid()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Chọn polylines
            TypedValue[] filterList = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start, "POLYLINE"),
                new TypedValue((int)DxfCode.Start, "LINE"),
                new TypedValue((int)DxfCode.Start, "ARC"),
                new TypedValue((int)DxfCode.Start, "CIRCLE"),
                new TypedValue((int)DxfCode.Start, "ELLIPSE"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            };
            SelectionFilter filter = new SelectionFilter(filterList);

            PromptSelectionOptions opts = new PromptSelectionOptions();
            opts.MessageForAdding = "\nChọn các polylines/curves: ";

            PromptSelectionResult result = ed.GetSelection(opts, filter);

            if (result.Status != PromptStatus.OK || result.Value.Count == 0)
            {
                ed.WriteMessage("\nKhông có đối tượng nào được chọn.");
                return;
            }

            List<ObjectId> curveIds = result.Value.GetObjectIds().ToList();
            ed.WriteMessage($"\nĐã chọn {curveIds.Count} đối tượng.");

            // Hỏi màu
            PromptKeywordOptions colorOpts = new PromptKeywordOptions("\nSử dụng màu tùy chỉnh? ");
            colorOpts.Keywords.Add("Yes");
            colorOpts.Keywords.Add("No");
            colorOpts.Keywords.Default = "No";
            PromptResult colorResult = ed.GetKeywords(colorOpts);

            bool useCustomColor = colorResult.StringResult == "Yes";
            AcadColor? solidColor = null;

            if (useCustomColor)
            {
                PromptIntegerOptions colorIndexOpts = new PromptIntegerOptions("\nNhập ACI color index (1-255): ");
                colorIndexOpts.LowerLimit = 1;
                colorIndexOpts.UpperLimit = 255;
                colorIndexOpts.DefaultValue = 1;
                PromptIntegerResult colorIndexResult = ed.GetInteger(colorIndexOpts);
                if (colorIndexResult.Status == PromptStatus.OK)
                {
                    solidColor = AcadColor.FromColorIndex(ColorMethod.ByAci, (short)colorIndexResult.Value);
                }
            }

            // Lấy layer và màu từ đối tượng đầu tiên
            string layer = "0";
            AcadColor originalColor = AcadColor.FromColorIndex(ColorMethod.ByAci, 7);
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity firstEnt = (Entity)tr.GetObject(curveIds[0], OpenMode.ForRead);
                layer = firstEnt.Layer;
                originalColor = firstEnt.Color;
                tr.Commit();
            }

            // Tạo hatch
            bool success = CreateHatchFromCurves(curveIds, db, ed, layer, useCustomColor, solidColor, originalColor);

            if (success)
            {
                // Hỏi có xóa curves không
                PromptKeywordOptions deleteOpts = new PromptKeywordOptions("\nXóa các curves đã chọn? ");
                deleteOpts.Keywords.Add("Yes");
                deleteOpts.Keywords.Add("No");
                deleteOpts.Keywords.Default = "Yes";
                PromptResult deleteResult = ed.GetKeywords(deleteOpts);

                if (deleteResult.StringResult == "Yes")
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId id in curveIds)
                        {
                            try
                            {
                                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                                if (!ent.IsErased)
                                    ent.Erase();
                            }
                            catch { }
                        }
                        tr.Commit();
                    }
                    ed.WriteMessage("\n✓ Đã xóa curves.");
                }

                ed.WriteMessage("\n✓ Hoàn thành!");
            }
        }

        private static void ResetState()
        {
            _beforeHandles.Clear();
            _textIdsToProcess.Clear();
            _useCustomColor = false;
            _solidColor = null;
            _create3DSolid = false;
            _extrusionHeight = 1.0;
            _originalTextPosition = Point3d.Origin;
            _originalTextHeight = 1.0;
            _originalTextRotation = 0.0;
        }

        /// <summary>
        /// Chọn các đối tượng Text (DBText hoặc MText)
        /// </summary>
        private static List<ObjectId> SelectTexts(Editor ed)
        {
            List<ObjectId> textIds = new List<ObjectId>();

            TypedValue[] filterList = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "TEXT"),
                new TypedValue((int)DxfCode.Start, "MTEXT"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            };
            SelectionFilter filter = new SelectionFilter(filterList);

            PromptSelectionOptions opts = new PromptSelectionOptions();
            opts.MessageForAdding = "\nChọn các Text hoặc MText để chuyển thành Solid: ";
            opts.AllowDuplicates = false;

            PromptSelectionResult result = ed.GetSelection(opts, filter);

            if (result.Status == PromptStatus.OK)
            {
                textIds.AddRange(result.Value.GetObjectIds());
                ed.WriteMessage($"\nĐã chọn {textIds.Count} text.");
            }

            return textIds;
        }

        /// <summary>
        /// Lấy tất cả handles trong ModelSpace
        /// </summary>
        private static HashSet<long> GetAllHandles(Database db)
        {
            HashSet<long> handles = new HashSet<long>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId objId in btr)
                {
                    handles.Add(objId.Handle.Value);
                }
                tr.Commit();
            }
            return handles;
        }

        /// <summary>
        /// Tạo Hatch từ các curves (Line, Arc, Polyline...)
        /// </summary>
        private static bool CreateHatchFromCurves(List<ObjectId> curveIds, Database db, Editor ed,
            string layer, bool useCustomColor, AcadColor? solidColor, AcadColor originalColor)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // Thu thập tất cả các curves
                    DBObjectCollection curves = new DBObjectCollection();
                    foreach (ObjectId id in curveIds)
                    {
                        try
                        {
                            Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                            if (ent is Curve curve)
                            {
                                curves.Add(curve);
                            }
                        }
                        catch { }
                    }

                    if (curves.Count == 0)
                    {
                        ed.WriteMessage("\n  Không có curves hợp lệ.");
                        tr.Abort();
                        return false;
                    }

                    ed.WriteMessage($"\n  Đang xử lý {curves.Count} curves...");

                    // Thử tạo Region từ các curves
                    DBObjectCollection regions = new DBObjectCollection();
                    try
                    {
                        regions = AcadRegion.CreateFromCurves(curves);
                        ed.WriteMessage($"\n  Đã tạo {regions.Count} Region từ curves.");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n  Không thể tạo Region: {ex.Message}");
                    }

                    if (regions.Count > 0)
                    {
                        // Tạo Hatch từ mỗi Region
                        int hatchCount = 0;
                        foreach (DBObject obj in regions)
                        {
                            if (obj is AcadRegion region)
                            {
                                region.SetDatabaseDefaults();
                                ObjectId regionId = btr.AppendEntity(region);
                                tr.AddNewlyCreatedDBObject(region, true);

                                // Tạo Hatch
                                Hatch hatch = new Hatch();
                                hatch.SetDatabaseDefaults();
                                hatch.Layer = layer;

                                if (useCustomColor && solidColor != null)
                                    hatch.Color = solidColor;
                                else
                                    hatch.Color = originalColor;

                                ObjectId hatchId = btr.AppendEntity(hatch);
                                tr.AddNewlyCreatedDBObject(hatch, true);

                                hatch.PatternScale = 1.0;
                                hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");

                                ObjectIdCollection boundaries = new ObjectIdCollection();
                                boundaries.Add(regionId);

                                try
                                {
                                    hatch.AppendLoop(HatchLoopTypes.Default, boundaries);
                                    hatch.EvaluateHatch(true);
                                    hatchCount++;
                                }
                                catch (System.Exception ex)
                                {
                                    ed.WriteMessage($"\n  Lỗi thêm boundary: {ex.Message}");
                                    hatch.Erase();
                                }

                                // Xóa region sau khi tạo hatch
                                region.Erase();
                            }
                        }

                        ed.WriteMessage($"\n  Đã tạo {hatchCount} Solid Hatch.");
                        tr.Commit();
                        return hatchCount > 0;
                    }
                    else
                    {
                        // Fallback: Tìm closed polylines và tạo hatch trực tiếp
                        ed.WriteMessage("\n  Tìm closed polylines...");

                        List<ObjectId> closedPolyIds = new List<ObjectId>();
                        foreach (ObjectId id in curveIds)
                        {
                            try
                            {
                                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                                if (ent is Polyline pline && pline.Closed)
                                {
                                    closedPolyIds.Add(id);
                                }
                                else if (ent is Circle || ent is Ellipse)
                                {
                                    closedPolyIds.Add(id);
                                }
                            }
                            catch { }
                        }

                        ed.WriteMessage($"\n  Tìm thấy {closedPolyIds.Count} đường kín.");

                        if (closedPolyIds.Count > 0)
                        {
                            int hatchCount = 0;
                            foreach (ObjectId polyId in closedPolyIds)
                            {
                                try
                                {
                                    Hatch hatch = new Hatch();
                                    hatch.SetDatabaseDefaults();
                                    hatch.Layer = layer;

                                    if (useCustomColor && solidColor != null)
                                        hatch.Color = solidColor;
                                    else
                                        hatch.Color = originalColor;

                                    ObjectId hatchId = btr.AppendEntity(hatch);
                                    tr.AddNewlyCreatedDBObject(hatch, true);

                                    hatch.PatternScale = 1.0;
                                    hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");

                                    ObjectIdCollection boundaries = new ObjectIdCollection();
                                    boundaries.Add(polyId);
                                    hatch.AppendLoop(HatchLoopTypes.Default, boundaries);
                                    hatch.EvaluateHatch(true);
                                    hatchCount++;
                                }
                                catch { }
                            }

                            ed.WriteMessage($"\n  Đã tạo {hatchCount} Solid Hatch từ closed polylines.");
                            tr.Commit();
                            return hatchCount > 0;
                        }

                        ed.WriteMessage("\n  Không tìm thấy đường kín.");
                        tr.Abort();
                        return false;
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n  Lỗi tạo hatch: {ex.Message}");
                    tr.Abort();
                    return false;
                }
            }
        }

        /// <summary>
        /// Tạo 3D Solid từ các curves bằng cách extruding Region
        /// </summary>
        private static bool Create3DSolidFromCurves(List<ObjectId> curveIds, Database db, Editor ed,
            string layer, bool useCustomColor, AcadColor? solidColor, AcadColor originalColor, double extrusionHeight)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // Thu thập tất cả các curves
                    DBObjectCollection curves = new DBObjectCollection();
                    foreach (ObjectId id in curveIds)
                    {
                        try
                        {
                            Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                            if (ent is Curve curve)
                            {
                                curves.Add(curve);
                            }
                        }
                        catch { }
                    }

                    if (curves.Count == 0)
                    {
                        ed.WriteMessage("\n  Không có curves hợp lệ.");
                        tr.Abort();
                        return false;
                    }

                    ed.WriteMessage($"\n  Đang tạo 3D Solid từ {curves.Count} curves...");

                    // Tạo Region từ các curves
                    DBObjectCollection regions = new DBObjectCollection();
                    try
                    {
                        regions = AcadRegion.CreateFromCurves(curves);
                        ed.WriteMessage($"\n  Đã tạo {regions.Count} Region từ curves.");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n  Không thể tạo Region: {ex.Message}");
                    }

                    if (regions.Count > 0)
                    {
                        int solidCount = 0;
                        foreach (DBObject obj in regions)
                        {
                            if (obj is AcadRegion region)
                            {
                                try
                                {
                                    // Tạo 3D Solid bằng cách extrude Region
                                    Solid3d solid = new Solid3d();
                                    solid.SetDatabaseDefaults();
                                    solid.Layer = layer;

                                    if (useCustomColor && solidColor != null)
                                        solid.Color = solidColor;
                                    else
                                        solid.Color = originalColor;

                                    // Extrude region theo chiều cao
                                    solid.Extrude(region, extrusionHeight, 0.0);

                                    // Thêm solid vào database
                                    btr.AppendEntity(solid);
                                    tr.AddNewlyCreatedDBObject(solid, true);

                                    solidCount++;
                                }
                                catch (System.Exception ex)
                                {
                                    ed.WriteMessage($"\n  Lỗi extrude: {ex.Message}");
                                }

                                // Region không cần add vào database
                                region.Dispose();
                            }
                        }

                        ed.WriteMessage($"\n  Đã tạo {solidCount} 3D Solid.");
                        tr.Commit();
                        return solidCount > 0;
                    }
                    else
                    {
                        // Fallback: Tìm closed polylines
                        ed.WriteMessage("\n  Tìm closed polylines để tạo 3D Solid...");

                        int solidCount = 0;
                        foreach (ObjectId id in curveIds)
                        {
                            try
                            {
                                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                                Curve closedCurve = null;

                                if (ent is Polyline pline && pline.Closed)
                                    closedCurve = pline;
                                else if (ent is Circle)
                                    closedCurve = ent as Curve;
                                else if (ent is Ellipse)
                                    closedCurve = ent as Curve;

                                if (closedCurve != null)
                                {
                                    // Tạo region từ single curve
                                    DBObjectCollection singleCurve = new DBObjectCollection();
                                    singleCurve.Add(closedCurve);
                                    
                                    DBObjectCollection singleRegion = AcadRegion.CreateFromCurves(singleCurve);
                                    
                                    if (singleRegion.Count > 0 && singleRegion[0] is AcadRegion reg)
                                    {
                                        Solid3d solid = new Solid3d();
                                        solid.SetDatabaseDefaults();
                                        solid.Layer = layer;

                                        if (useCustomColor && solidColor != null)
                                            solid.Color = solidColor;
                                        else
                                            solid.Color = originalColor;

                                        solid.Extrude(reg, extrusionHeight, 0.0);

                                        btr.AppendEntity(solid);
                                        tr.AddNewlyCreatedDBObject(solid, true);

                                        reg.Dispose();
                                        solidCount++;
                                    }
                                }
                            }
                            catch { }
                        }

                        if (solidCount > 0)
                        {
                            ed.WriteMessage($"\n  Đã tạo {solidCount} 3D Solid từ closed polylines.");
                            tr.Commit();
                            return true;
                        }

                        ed.WriteMessage("\n  Không thể tạo 3D Solid.");
                        tr.Abort();
                        return false;
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n  Lỗi tạo 3D Solid: {ex.Message}");
                    tr.Abort();
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// Form cấu hình cho lệnh AT_TextToSolid
    /// </summary>
    public class TextToSolidForm : Form
    {
        private WinFormsLabel? lblInfo;
        private WinFormsLabel? lblNote;
        private WinFormsLabel? lblOutputType;
        private RadioButton? rbHatch;
        private RadioButton? rb3DSolid;
        private WinFormsLabel? lblExtrusionHeight;
        private TextBox? txtExtrusionHeight;
        private CheckBox? chkUseCustomColor;
        private Button? btnSelectColor;
        private Panel? pnlColorPreview;
        private Button? btnOK;
        private Button? btnCancel;

        public int SelectedTextCount { get; set; } = 0;
        public bool DeleteOriginalText => true; // TXTEXP always replaces
        public bool UseCustomColor => chkUseCustomColor?.Checked ?? false;
        public AcadColor? SolidColor { get; private set; }
        public bool Create3DSolid => rb3DSolid?.Checked ?? false;
        public double ExtrusionHeight 
        { 
            get 
            { 
                if (double.TryParse(txtExtrusionHeight?.Text, out double h) && h > 0)
                    return h;
                return 1.0;
            } 
        }

        private System.Drawing.Color _selectedColor = System.Drawing.Color.Red;

        public TextToSolidForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "AT_TextToSolid - Chuyển Text thành Solid";
            this.Size = new System.Drawing.Size(420, 360);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.ForeColor = System.Drawing.Color.White;

            lblInfo = new WinFormsLabel()
            {
                Text = "Cấu hình chuyển đổi Text thành Solid",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(380, 25),
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.LightSkyBlue
            };

            lblNote = new WinFormsLabel()
            {
                Text = "✓ Sử dụng TXTEXP Express Tools + Scale/Move tự động\n" +
                       "✓ Hỗ trợ cả TrueType và SHX fonts",
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size(380, 40),
                Font = new System.Drawing.Font("Segoe UI", 8),
                ForeColor = System.Drawing.Color.LightGreen
            };

            // Output type selection
            lblOutputType = new WinFormsLabel()
            {
                Text = "Loại output:",
                Location = new System.Drawing.Point(20, 100),
                Size = new System.Drawing.Size(100, 25),
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };

            rbHatch = new RadioButton()
            {
                Text = "Solid Hatch (2D)",
                Location = new System.Drawing.Point(130, 98),
                Size = new System.Drawing.Size(130, 25),
                Checked = true,
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9)
            };

            rb3DSolid = new RadioButton()
            {
                Text = "3D Solid",
                Location = new System.Drawing.Point(270, 98),
                Size = new System.Drawing.Size(100, 25),
                Checked = false,
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9)
            };
            rb3DSolid.CheckedChanged += Rb3DSolid_CheckedChanged;

            // Extrusion height
            lblExtrusionHeight = new WinFormsLabel()
            {
                Text = "Chiều cao extrude:",
                Location = new System.Drawing.Point(40, 130),
                Size = new System.Drawing.Size(130, 25),
                Font = new System.Drawing.Font("Segoe UI", 9),
                ForeColor = System.Drawing.Color.Gray,
                Enabled = false
            };

            txtExtrusionHeight = new TextBox()
            {
                Text = "1.0",
                Location = new System.Drawing.Point(180, 128),
                Size = new System.Drawing.Size(80, 25),
                BackColor = System.Drawing.Color.FromArgb(62, 62, 66),
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = false
            };

            // Color options
            chkUseCustomColor = new CheckBox()
            {
                Text = "Sử dụng màu tùy chỉnh (mặc định: giữ màu text)",
                Location = new System.Drawing.Point(20, 170),
                Size = new System.Drawing.Size(350, 25),
                Checked = false,
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9)
            };
            chkUseCustomColor.CheckedChanged += ChkUseCustomColor_CheckedChanged;

            btnSelectColor = new Button()
            {
                Text = "Chọn màu...",
                Location = new System.Drawing.Point(40, 200),
                Size = new System.Drawing.Size(120, 30),
                Enabled = false,
                BackColor = System.Drawing.Color.FromArgb(62, 62, 66),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSelectColor.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(100, 100, 100);
            btnSelectColor.Click += BtnSelectColor_Click;

            pnlColorPreview = new Panel()
            {
                Location = new System.Drawing.Point(170, 200),
                Size = new System.Drawing.Size(80, 30),
                BackColor = _selectedColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnOK = new Button()
            {
                Text = "Bắt đầu",
                Location = new System.Drawing.Point(200, 260),
                Size = new System.Drawing.Size(90, 35),
                DialogResult = DialogResult.OK,
                BackColor = System.Drawing.Color.FromArgb(0, 122, 204),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
            };
            btnOK.FlatAppearance.BorderSize = 0;

            btnCancel = new Button()
            {
                Text = "Hủy",
                Location = new System.Drawing.Point(300, 260),
                Size = new System.Drawing.Size(90, 35),
                DialogResult = DialogResult.Cancel,
                BackColor = System.Drawing.Color.FromArgb(62, 62, 66),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9)
            };
            btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(100, 100, 100);

            this.Controls.AddRange(new Control[]
            {
                lblInfo, lblNote, 
                lblOutputType, rbHatch, rb3DSolid,
                lblExtrusionHeight, txtExtrusionHeight,
                chkUseCustomColor, btnSelectColor, pnlColorPreview,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (lblInfo != null)
            {
                lblInfo.Text = $"Đã chọn {SelectedTextCount} text để chuyển đổi";
            }
        }

        private void Rb3DSolid_CheckedChanged(object? sender, EventArgs e)
        {
            bool enabled = rb3DSolid?.Checked ?? false;
            if (lblExtrusionHeight != null)
            {
                lblExtrusionHeight.Enabled = enabled;
                lblExtrusionHeight.ForeColor = enabled ? System.Drawing.Color.White : System.Drawing.Color.Gray;
            }
            if (txtExtrusionHeight != null)
            {
                txtExtrusionHeight.Enabled = enabled;
                txtExtrusionHeight.ForeColor = enabled ? System.Drawing.Color.White : System.Drawing.Color.Gray;
            }
        }

        private void ChkUseCustomColor_CheckedChanged(object? sender, EventArgs e)
        {
            bool enabled = chkUseCustomColor?.Checked ?? false;
            if (btnSelectColor != null)
                btnSelectColor.Enabled = enabled;
        }

        private void BtnSelectColor_Click(object? sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = _selectedColor;
                colorDialog.FullOpen = true;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedColor = colorDialog.Color;
                    if (pnlColorPreview != null)
                        pnlColorPreview.BackColor = _selectedColor;

                    SolidColor = AcadColor.FromRgb(_selectedColor.R, _selectedColor.G, _selectedColor.B);
                }
            }
        }
    }
}
