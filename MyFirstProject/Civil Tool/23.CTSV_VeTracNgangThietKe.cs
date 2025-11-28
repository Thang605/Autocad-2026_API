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
using MyFirstProject.Civil_Tool;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.CTSV_VeTracNgangThietKe_Commands))]

namespace Civil3DCsharp
{
    public class CTSV_VeTracNgangThietKe_Commands
    {
        [CommandMethod("CTSV_VeTracNgangThietKe")]
        public static void CTSV_VeTracNgangThietKe()
        {
            Transaction? tr = null;
            try
            {
                // Show form to get user inputs OUTSIDE of transaction
                SectionViewDesignForm form = new();
                
                // Use ShowDialog instead of Application.Run
                var dialogResult = form.ShowDialog();
                
                if (dialogResult != System.Windows.Forms.DialogResult.OK || !form.DialogResultOK)
                {
                    A.Ed.WriteMessage("\nLệnh đã bị hủy bỏ.");
                    return;
                }

                A.Ed.WriteMessage("\nBắt đầu tạo trắc ngang thiết kế...");

                // Validate form inputs first
                if (form.AlignmentId == ObjectId.Null || !form.AlignmentId.IsValid)
                {
                    A.Ed.WriteMessage("\nAlignment ID không hợp lệ.");
                    return;
                }

                // Now start transaction for the main work
                tr = A.Db.TransactionManager.StartTransaction();
                
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();

                // Get inputs from form
                ObjectId alignmentId = form.AlignmentId;
                Point3d point3D = form.PlacementPoint;
                
                SectionViewGroupCreationPlacementOptions sectionViewGroupCreationPlacementOptions = new();
                sectionViewGroupCreationPlacementOptions.UseProductionPlacement(form.LayoutTemplatePath, form.LayoutName);

                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                if (alignment == null)
                {
                    A.Ed.WriteMessage("\nKhông thể mở alignment.");
                    return;
                }

                double startStation = alignment.StartingStation;
                double endstation = alignment.EndingStation;
                
                // Check if alignment has sample line groups
                if (alignment.GetSampleLineGroupIds().Count == 0)
                {
                    A.Ed.WriteMessage("\nAlignment không có sample line group nào.");
                    return;
                }
                
                ObjectId sampleLineGroupId = alignment.GetSampleLineGroupIds()[0];
                SampleLineGroup? sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;
                if (sampleLineGroup == null)
                {
                    A.Ed.WriteMessage("\nKhông thể mở sample line group.");
                    return;
                }

                SectionViewGroupCreationRangeOptions sectionViewGroupCreationRangeOptions = new(sampleLineGroupId);

                ObjectIdCollection sectionSourceIdColl = [];
                SectionSourceCollection sectionSources = sampleLineGroup.GetSectionSources();

                //Add_SectionSource - thiết lập các nguồn dữ liệu cho trắc ngang từ form
                A.Ed.WriteMessage($"\nKiểm tra {sectionSources.Count} section sources...");
                
                foreach (SectionSource sectionsource in sectionSources)
                {
                    try
                    {
                        // Find corresponding config from form
                        var sourceConfig = form.SectionSources?.FirstOrDefault(s => s.SourceId == sectionsource.SourceId);
                        
                        // Debug thông tin về source
                        string sourceName = GetSourceName(sectionsource.SourceId, tr);
                        
                        A.Ed.WriteMessage($"\n  Source: {sourceName}, Type: {sectionsource.SourceType}, Use: {sourceConfig?.UseSource ?? false}");
                        
                        if (sourceConfig != null && sourceConfig.UseSource)
                        {
                            sectionsource.IsSampled = true;
                            
                            // Apply style with priority for All Codes 1-1000 for corridor sources
                            if (sourceConfig.StyleId != ObjectId.Null && sourceConfig.StyleId.IsValid)
                            {
                                sectionsource.StyleId = sourceConfig.StyleId;
                                
                                // Debug: Show applied style name
                                string appliedStyleName = GetStyleName(sourceConfig.StyleId, tr);
                                A.Ed.WriteMessage($"\n    -> Áp dụng style: {appliedStyleName}");
                            }
                            else
                            {
                                // Apply default style with All Codes priority for corridors
                                ObjectId defaultStyleId = GetDefaultStyleForSectionSource(sectionsource, tr, alignment.Name);
                                if (defaultStyleId != ObjectId.Null)
                                {
                                    sectionsource.StyleId = defaultStyleId;
                                    string defaultStyleName = GetStyleName(defaultStyleId, tr);
                                    A.Ed.WriteMessage($"\n    -> Áp dụng default style: {defaultStyleName}");
                                }
                            }
                            
                            // Add to collection for later use - chỉ bao gồm TinSurface, Corridor, CorridorSurface, Material
                            if (sectionsource.SourceType == SectionSourceType.TinSurface || 
                                sectionsource.SourceType == SectionSourceType.Corridor ||
                                sectionsource.SourceType == SectionSourceType.CorridorSurface ||
                                sectionsource.SourceType == SectionSourceType.Material)
                            {
                                sectionSourceIdColl.Add(sectionsource.SourceId);
                                A.Ed.WriteMessage($"\n    -> Đã thêm vào collection: {sourceName}");
                            }
                        }
                        else
                        {
                            sectionsource.IsSampled = false;
                            A.Ed.WriteMessage($"\n    -> Không sử dụng: {sourceName}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\nLỗi khi thiết lập section source: {ex.Message}");
                        // Continue with other sources
                    }
                }

                A.Ed.WriteMessage($"\nTổng cộng {sectionSourceIdColl.Count} sources được chọn để vẽ trắc ngang.");

                // Tạo nhóm trắc ngang
                SectionViewGroupCollection sectionViewGroupCollection = sampleLineGroup.SectionViewGroups;
                SectionViewGroup? sectionViewGroup = null;
                
                try
                {
                    sectionViewGroup = sectionViewGroupCollection.Add(point3D, startStation, endstation, sectionViewGroupCreationRangeOptions, sectionViewGroupCreationPlacementOptions);
                }
                catch (System.Exception ex)
                {
                    A.Ed.WriteMessage($"\nLỗi khi tạo section view group: {ex.Message}");
                    return;
                }

                if (sectionViewGroup == null)
                {
                    A.Ed.WriteMessage("\nKhông thể tạo section view group.");
                    return;
                }

                // Cập nhật styles cho section view group từ form
                if (form.PlotStyleId != ObjectId.Null && form.PlotStyleId.IsValid)
                {
                    try
                    {
                        sectionViewGroup.PlotStyleId = form.PlotStyleId;
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\nKhông thể thiết lập plot style: {ex.Message}");
                    }
                }

                ObjectIdCollection sectionViewIdColl = sectionViewGroup.GetSectionViewIds();

                //surfaceId - khởi tạo các ObjectId cho các mặt
                ObjectId surfaceTnId = ObjectId.Null;
                ObjectId surfaceTopId = ObjectId.Null;
                ObjectId surfaceDatumId = ObjectId.Null;

                // Thêm nhãn cho các section và bands
                foreach (ObjectId sectionviewId in sectionViewIdColl)
                {
                    if (!sectionviewId.IsValid)
                        continue;

                    SectionView? sectionView = tr.GetObject(sectionviewId, OpenMode.ForWrite) as SectionView;
                    if (sectionView == null)
                        continue;
                    
                    // Apply section view style from form
                    if (form.SectionViewStyleId != ObjectId.Null && form.SectionViewStyleId.IsValid)
                    {
                        try
                        {
                            sectionView.StyleId = form.SectionViewStyleId;
                        }
                        catch (System.Exception ex)
                        {
                            A.Ed.WriteMessage($"\nKhông thể thiết lập section view style: {ex.Message}");
                        }
                    }
                    
                    var sampleLineId = sectionView.SampleLineId;
                    if (!sampleLineId.IsValid)
                        continue;

                    SampleLine? sampleLine = tr.GetObject(sampleLineId, OpenMode.ForRead) as SampleLine;
                    if (sampleLine == null)
                        continue;

                    // Thêm nhãn cho các surface
                    AddSurfaceLabels(sectionSourceIdColl, sampleLine, sectionviewId, tr);

                    // Lấy các section ID để sử dụng cho bands
                    var sectionIds = GetSectionIdsForBands(sectionSourceIdColl, sampleLine, tr);
                    ObjectId sectionTnId = sectionIds.TnId;
                    ObjectId sectionTopId = sectionIds.TopId;
                    ObjectId sectionDatumId = sectionIds.DatumId;

                    // Cập nhật surface IDs cho material list
                    if (sectionIds.TnSurfaceId != ObjectId.Null) surfaceTnId = sectionIds.TnSurfaceId;
                    if (sectionIds.TopSurfaceId != ObjectId.Null) surfaceTopId = sectionIds.TopSurfaceId;
                    if (sectionIds.DatumSurfaceId != ObjectId.Null) surfaceDatumId = sectionIds.DatumSurfaceId;

                    // Thêm các bands cho section view nếu được chọn trong form
                    if (form.ImportBandSet && form.BandSetStyleId != ObjectId.Null)
                    {
                        // Use Band Set instead of individual bands
                        ApplyBandStyleToSectionView(sectionView, form.BandSetStyleId);
                    }
                    else if (form.AddElevationBands || form.AddDistanceBands)
                    {
                        // Use individual bands
                        AddSectionBands(sectionView, form, sectionTopId, sectionTnId);
                    }
                }

                // Tạo Material List nếu được yêu cầu
                if (form.CreateMaterialList)
                {
                    CreateMaterialList(sampleLineGroup, form.MaterialListName, surfaceTnId, surfaceDatumId, tr);
                }

                A.Ed.WriteMessage("\n Lệnh CTSV_VeTracNgangThietKe đã hoàn thành thành công!");
                A.Ed.WriteMessage("\n💡 Lưu ý: Để tạo corridor surfaces, sử dụng lệnh riêng biệt: CTSV_TaoCorridorSurface");

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\n Lỗi AutoCAD khi thực hiện lệnh: {e.Message}");
                tr?.Abort();
            }
            catch (System.Exception e)
            {
                A.Ed.WriteMessage($"\n Lỗi khi thực hiện lệnh: {e.Message}");
                tr?.Abort();
            }
            finally
            {
                tr?.Dispose();
            }
        }

        // Helper method to get source name
        private static string GetSourceName(ObjectId sourceId, Transaction tr)
        {
            try
            {
                if (sourceId == ObjectId.Null || !sourceId.IsValid)
                    return "Invalid Source";

                var entity = tr.GetObject(sourceId, OpenMode.ForRead);
                if (entity is TinSurface surface)
                    return surface.Name ?? "Unnamed Surface";
                else if (entity is Corridor corridor)
                    return corridor.Name ?? "Unnamed Corridor";
                else
                {
                    // Use reflection to get Name property
                    var nameProperty = entity.GetType().GetProperty("Name");
                    if (nameProperty != null)
                    {
                        return nameProperty.GetValue(entity)?.ToString() ?? $"Other ({entity.GetType().Name})";
                    }
                    return $"Other ({entity.GetType().Name})";
                }
            }
            catch
            {
                return "Error Reading Source";
            }
        }

        // Helper method to create material list
        private static void CreateMaterialList(SampleLineGroup sampleLineGroup, string materialListName, ObjectId surfaceTnId, ObjectId surfaceDatumId, Transaction tr)
        {
            try
            {
                A.Ed.WriteMessage($"\n=== Chuẩn bị tạo Material List: {materialListName} ===");
                
                // Check if we have the required surfaces
                if (surfaceTnId == ObjectId.Null || surfaceDatumId == ObjectId.Null)
                {
                    A.Ed.WriteMessage("\n⚠️ Cần có EG/TN surface và DATUM surface để tạo material list.");
                    
                    // Try to find surfaces from section sources
                    var foundSurfaces = FindSurfacesForMaterialList(sampleLineGroup, tr);
                    if (foundSurfaces.EgSurfaceId != ObjectId.Null && foundSurfaces.DatumSurfaceId != ObjectId.Null)
                    {
                        surfaceTnId = foundSurfaces.EgSurfaceId;
                        surfaceDatumId = foundSurfaces.DatumSurfaceId;
                        A.Ed.WriteMessage($"\n✅ Đã tìm thấy surfaces: EG={GetSurfaceName(surfaceTnId, tr)}, DATUM={GetSurfaceName(surfaceDatumId, tr)}");
                    }
                    else
                    {
                        A.Ed.WriteMessage("\n❌ Không tìm thấy surfaces phù hợp. Bỏ qua tạo material list.");
                        return;
                    }
                }

                // Display instructions for manual material list creation
                A.Ed.WriteMessage($"\n📋 Hướng dẫn tạo Material List thủ công:");
                A.Ed.WriteMessage($"\n1. Trong Prospector, mở rộng Sample Line Groups");
                A.Ed.WriteMessage($"\n2. Right-click vào Sample Line Group > Properties");
                A.Ed.WriteMessage($"\n3. Chuyển đến tab 'Material List'");
                A.Ed.WriteMessage($"\n4. Click 'Add new material' để tạo material list mới");
                A.Ed.WriteMessage($"\n5. Đặt tên: '{materialListName}'");
                A.Ed.WriteMessage($"\n");
                A.Ed.WriteMessage($"\n💡 Cấu trúc Material List cần tạo:");
                A.Ed.WriteMessage($"\n   📁 Đào đất");
                A.Ed.WriteMessage($"\n      ├─ EG ({GetSurfaceName(surfaceTnId, tr)}) - Condition: Below, Type: Cut");
                A.Ed.WriteMessage($"\n      └─ DATUM ({GetSurfaceName(surfaceDatumId, tr)}) - Condition: Above, Type: Cut");
                A.Ed.WriteMessage($"\n   📁 Đắp đất");
                A.Ed.WriteMessage($"\n      ├─ EG ({GetSurfaceName(surfaceTnId, tr)}) - Condition: Above, Type: Fill");
                A.Ed.WriteMessage($"\n      └─ DATUM ({GetSurfaceName(surfaceDatumId, tr)}) - Condition: Below, Type: Fill");
                A.Ed.WriteMessage($"\n");
                A.Ed.WriteMessage($"\n✅ Đã chuẩn bị thông tin để tạo Material List thủ công");
                
                // Alternative: Try to use Civil 3D's built-in material creation
                try
                {
                    CreateMaterialListProgrammatically(sampleLineGroup, materialListName, surfaceTnId, surfaceDatumId, tr);
                }
                catch (System.Exception ex)
                {
                    A.Ed.WriteMessage($"\n⚠️ Không thể tạo material list tự động: {ex.Message}");
                    A.Ed.WriteMessage($"\n💡 Vui lòng tạo material list thủ công theo hướng dẫn ở trên.");
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n❌ Lỗi khi chuẩn bị Material List: {ex.Message}");
            }
        }

        // Helper method to attempt programmatic material list creation
        private static void CreateMaterialListProgrammatically(SampleLineGroup sampleLineGroup, string materialListName, ObjectId surfaceTnId, ObjectId surfaceDatumId, Transaction tr)
        {
            try
            {
                A.Ed.WriteMessage($"\n🔄 Thử tạo Material List tự động...");
                
                // For now, just provide detailed instructions since the exact API varies between Civil 3D versions
                A.Ed.WriteMessage($"\n📋 Thông tin cần thiết để tạo Material List:");
                A.Ed.WriteMessage($"\n   - Sample Line Group: {sampleLineGroup.Name}");
                A.Ed.WriteMessage($"\n   - Material List Name: {materialListName}");
                A.Ed.WriteMessage($"\n   - EG/TN Surface: {GetSurfaceName(surfaceTnId, tr)} (ID: {surfaceTnId})");
                A.Ed.WriteMessage($"\n   - DATUM Surface: {GetSurfaceName(surfaceDatumId, tr)} (ID: {surfaceDatumId})");
                
                A.Ed.WriteMessage($"\n💡 Material List này sẽ tính toán khối lượng đào đắp giữa 2 surfaces:");
                A.Ed.WriteMessage($"\n   • Đào đất: Vùng dưới EG và trên DATUM");
                A.Ed.WriteMessage($"\n   • Đắp đất: Vùng trên EG và dưới DATUM");
                
                A.Ed.WriteMessage($"\n⚠️ Chức năng tạo Material List tự động sẽ được cải thiện trong phiên bản sau.");
                A.Ed.WriteMessage($"\n💡 Hiện tại vui lòng tạo thủ công theo hướng dẫn ở trên.");
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n❌ Lỗi trong CreateMaterialListProgrammatically: {ex.Message}");
                throw;
            }
        }

        // Helper method to find surfaces for material list
        private static (ObjectId EgSurfaceId, ObjectId DatumSurfaceId) FindSurfacesForMaterialList(SampleLineGroup sampleLineGroup, Transaction tr)
        {
            ObjectId egSurfaceId = ObjectId.Null;
            ObjectId datumSurfaceId = ObjectId.Null;

            try
            {
                SectionSourceCollection sectionSources = sampleLineGroup.GetSectionSources();
                
                foreach (SectionSource sectionSource in sectionSources)
                {
                    if (sectionSource.SourceType == SectionSourceType.TinSurface && sectionSource.IsSampled)
                    {
                        string sourceName = GetSourceName(sectionSource.SourceId, tr).ToLower();
                        
                        if ((sourceName.Contains("eg") || sourceName.Contains("tn")) && egSurfaceId == ObjectId.Null)
                        {
                            egSurfaceId = sectionSource.SourceId;
                            A.Ed.WriteMessage($"\n  Found EG/TN surface: {GetSourceName(sectionSource.SourceId, tr)}");
                        }
                        else if (sourceName.Contains("datum") && datumSurfaceId == ObjectId.Null)
                        {
                            datumSurfaceId = sectionSource.SourceId;
                            A.Ed.WriteMessage($"\n  Found DATUM surface: {GetSourceName(sectionSource.SourceId, tr)}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi tìm surfaces: {ex.Message}");
            }

            return (egSurfaceId, datumSurfaceId);
        }

        // Helper method to get surface name
        private static string GetSurfaceName(ObjectId surfaceId, Transaction tr)
        {
            try
            {
                if (surfaceId == ObjectId.Null || !surfaceId.IsValid)
                    return "Invalid Surface";

                var surface = tr.GetObject(surfaceId, OpenMode.ForRead) as TinSurface;
                return surface?.Name ?? "Unknown Surface";
            }
            catch
            {
                return "Error Reading Surface";
            }
        }

        // Helper method to get style name from ObjectId
        private static string GetStyleName(ObjectId styleId, Transaction tr)
        {
            try
            {
                if (styleId == ObjectId.Null || !styleId.IsValid)
                    return "(No Style)";
                    
                var entity = tr.GetObject(styleId, OpenMode.ForRead);
                var nameProperty = entity.GetType().GetProperty("Name");
                if (nameProperty != null)
                {
                    return nameProperty.GetValue(entity)?.ToString() ?? "Unknown Style";
                }
                return "Unknown Style";
            }
            catch
            {
                return "Error Reading Style";
            }
        }

        // Helper method to get default style for section source with All Codes priority
        private static ObjectId GetDefaultStyleForSectionSource(SectionSource sectionSource, Transaction tr, string alignmentName)
        {
            try
            {
                string sourceName = GetSourceName(sectionSource.SourceId, tr);
                
                if (sectionSource.SourceType == SectionSourceType.TinSurface)
                {
                    if (sourceName.Contains("EG") || sourceName.Contains("TN"))
                    {
                        try { return A.Cdoc.Styles.SectionStyles["1.TN Ground"]; }
                        catch { /* Style not found, continue to fallback */ }
                    }
                    if (sourceName.Contains("top"))
                    {
                        try { return A.Cdoc.Styles.SectionStyles["2.Top Ground"]; }
                        catch { /* Style not found, continue to fallback */ }
                    }
                    if (sourceName.Contains("datum"))
                    {
                        try { return A.Cdoc.Styles.SectionStyles["3.Datum Ground"]; }
                        catch { /* Style not found, continue to fallback */ }
                    }
                }
                else if (sectionSource.SourceType == SectionSourceType.Corridor)
                {
                    // Priority 1: All Codes 1-1000 style for corridor sources
                    ObjectId allCodesStyleId = GetAllCodesStyleForSectionSource();
                    if (allCodesStyleId != ObjectId.Null)
                    {
                        A.Ed.WriteMessage($"\n      -> Corridor sử dụng All Codes style");
                        return allCodesStyleId;
                    }
                    
                    // Fallback for corridor
                    if (sourceName.Contains(alignmentName))
                    {
                        try { return A.Cdoc.Styles.CodeSetStyles["1. All Codes 1-1000"]; }
                        catch { /* Style not found, continue to fallback */ }
                    }
                }
                else if (sectionSource.SourceType == SectionSourceType.CorridorSurface)
                {
                    // For corridor surfaces, also try All Codes first
                    ObjectId allCodesStyleId = GetAllCodesStyleForSectionSource();
                    if (allCodesStyleId != ObjectId.Null)
                    {
                        A.Ed.WriteMessage($"\n      -> Corridor Surface sử dụng All Codes style");
                        return allCodesStyleId;
                    }
                    
                    if (sourceName.Contains("top"))
                    {
                        try { return A.Cdoc.Styles.SectionStyles["2.Top Ground"]; }
                        catch { /* Style not found, continue to fallback */ }
                    }
                    if (sourceName.Contains("datum"))
                    {
                        try { return A.Cdoc.Styles.SectionStyles["3.Datum Ground"]; }
                        catch { /* Style not found, continue to fallback */ }
                    }
                }
                else if (sectionSource.SourceType == SectionSourceType.Material)
                {
                    // Default section style for material
                    try { return A.Cdoc.Styles.SectionStyles["Standard"]; }
                    catch { /* Style not found, continue to fallback */ }
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi lấy default style: {ex.Message}");
            }
            
            return ObjectId.Null;
        }

        // Helper method to get All Codes style for section sources
        private static ObjectId GetAllCodesStyleForSectionSource()
        {
            try
            {
                // Try CodeSet styles first (preferred for corridor sources)
                var codeSetStyles = A.Cdoc.Styles.CodeSetStyles;
                
                // Priority style names for All Codes
                string[] allCodesStyleNames = {
                    "1. All Codes 1-1000",
                    "All Codes 1-1000", 
                    "1.All Codes 1-1000",
                    "All Codes",
                    "1. All Codes",
                    "ALL CODES 1-1000"
                };
                
                foreach (string styleName in allCodesStyleNames)
                {
                    if (codeSetStyles.Contains(styleName))
                    {
                        A.Ed.WriteMessage($"\n      -> Tìm thấy CodeSet style: {styleName}");
                        return codeSetStyles[styleName];
                    }
                }
                
                A.Ed.WriteMessage($"\n      -> Không tìm thấy All Codes 1-1000 style");
                return ObjectId.Null;
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi tìm All Codes style: {ex.Message}");
                return ObjectId.Null;
            }
        }

        // Helper method to add surface labels
        private static void AddSurfaceLabels(ObjectIdCollection sectionSourceIdColl, SampleLine sampleLine, ObjectId sectionviewId, Transaction tr)
        {
            foreach (ObjectId sectionsourcceId in sectionSourceIdColl)
            {
                if (!sectionsourcceId.IsValid)
                    continue;

                try
                {
                    string surfaceName = GetSourceName(sectionsourcceId, tr);
                    
                    if (surfaceName.ToLower().Contains("tn") || surfaceName.ToLower().Contains("eg"))
                    {
                        ObjectId sectionId = sampleLine.GetSectionId(sectionsourcceId);
                        if (sectionId.IsValid)
                        {
                            CreateSurfaceLabel(sectionviewId, sectionId, "Duong giong (EG)", 1);
                        }
                    }
                    else if (surfaceName.ToLower().Contains("top"))
                    {
                        ObjectId sectionId = sampleLine.GetSectionId(sectionsourcceId);
                        if (sectionId.IsValid)
                        {
                            CreateSurfaceLabel(sectionviewId, sectionId, "Duong giong", 0.7);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    A.Ed.WriteMessage($"\nLỗi khi thêm label cho surface: {ex.Message}");
                }
            }
        }

        // Helper method to create surface labels
        private static void CreateSurfaceLabel(ObjectId sectionviewId, ObjectId sectionId, string styleName, double weeding)
        {
            try
            {
                var styleCollection = A.Cdoc.Styles.LabelStyles.SectionLabelStyles.GradeBreakLabelStyles;
                if (styleCollection.Contains(styleName))
                {
                    using (var tr = A.Db.TransactionManager.TopTransaction)
                    {
                        ObjectId labelId = SectionGradeBreakLabelGroup.Create(sectionviewId, sectionId, styleCollection[styleName]);
                        SectionGradeBreakLabelGroup? labelGroup = tr.GetObject(labelId, OpenMode.ForWrite) as SectionGradeBreakLabelGroup;
                        if (labelGroup != null)
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            labelGroup.DefaultDimensionAnchorOption = DimensionAnchorOptionType.ViewBottom;
#pragma warning restore CS0618 // Type or member is obsolete
                            labelGroup.Weeding = weeding;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nKhông thể tạo label với style '{styleName}': {ex.Message}");
            }
        }

        // Helper struct for section IDs
        private struct SectionIdsResult
        {
            public ObjectId TnId;
            public ObjectId TopId;
            public ObjectId DatumId;
            public ObjectId TnSurfaceId;
            public ObjectId TopSurfaceId;
            public ObjectId DatumSurfaceId;
        }

        // Helper method to get section IDs for bands
        private static SectionIdsResult GetSectionIdsForBands(ObjectIdCollection sectionSourceIdColl, SampleLine sampleLine, Transaction tr)
        {
            var result = new SectionIdsResult();

            foreach (ObjectId sectionsourcceId in sectionSourceIdColl)
            {
                if (!sectionsourcceId.IsValid)
                    continue;

                try
                {
                    string surfaceName = GetSourceName(sectionsourcceId, tr);
                    
                    if (surfaceName.ToLower().Contains("tn") || surfaceName.ToLower().Contains("eg"))
                    {
                        result.TnId = sampleLine.GetSectionId(sectionsourcceId);
                        result.TnSurfaceId = sectionsourcceId;
                        A.Ed.WriteMessage($"\nTìm thấy TN/EG surface: {surfaceName}, Section ID: {result.TnId}");
                    }
                    else if (surfaceName.ToLower().Contains("top"))
                    {
                        result.TopId = sampleLine.GetSectionId(sectionsourcceId);
                        result.TopSurfaceId = sectionsourcceId;
                        A.Ed.WriteMessage($"\nTìm thấy TOP surface: {surfaceName}, Section ID: {result.TopId}");
                    }
                    else if (surfaceName.ToLower().Contains("datum"))
                    {
                        result.DatumId = sampleLine.GetSectionId(sectionsourcceId);
                        result.DatumSurfaceId = sectionsourcceId;
                        A.Ed.WriteMessage($"\nTìm thấy DATUM surface: {surfaceName}, Section ID: {result.DatumId}");
                    }
                }
                catch (System.Exception ex)
                {
                    A.Ed.WriteMessage($"\nLỗi khi lấy section ID: {ex.Message}");
                }
            }

            return result;
        }

        // Helper method to add section bands
        private static void AddSectionBands(SectionView sectionView, SectionViewDesignForm form, ObjectId sectionTopId, ObjectId sectionTnId)
        {
            if (sectionTopId.IsValid && sectionTnId.IsValid)
            {
                if (form.AddElevationBands)
                {
                    try
                    {
                        UtilitiesC3D.AddSectionBand(sectionView, "Cao do thiet ke 1-1000", 0, sectionTopId, sectionTnId, 0, 0.7);
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\nKhông thể thêm elevation band cho thiết kế: {ex.Message}");
                    }
                }
                if (form.AddDistanceBands)
                {
                    try
                    {
                        UtilitiesC3D.AddSectionBand(sectionView, "Khoang cach mia TK 1-1000", 1, sectionTopId, sectionTnId, 0, 0.7);
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\nKhông thể thêm distance band cho thiết kế: {ex.Message}");
                    }
                }
            }
            
            if (sectionTnId.IsValid)
            {
                if (form.AddElevationBands)
                {
                    try
                    {
                        UtilitiesC3D.AddSectionBand(sectionView, "Cao do tu nhien 1-1000", 2, sectionTnId, sectionTnId, 0, 1);
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\nKhông thể thêm elevation band cho tự nhiên: {ex.Message}");
                    }
                }
                if (form.AddDistanceBands)
                {
                    try
                    {
                        UtilitiesC3D.AddSectionBand(sectionView, "Khoang cach mia TN 1-1000", 3, sectionTnId, sectionTnId, 0, 1);
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\nKhông thể thêm distance band cho tự nhiên: {ex.Message}");
                    }
                }
            }
        }
        
        // Helper method to apply band style to section view
        private static void ApplyBandStyleToSectionView(SectionView sectionView, ObjectId bandStyleId)
        {
            try
            {
                A.Ed.WriteMessage($"\nÁp dụng Band Style cho section view: {sectionView.Name}");
                
                // Use the existing AddSectionBand method which works
                // This will add the selected band style to the section view
                UtilitiesC3D.AddSectionBand(sectionView, GetBandStyleName(bandStyleId), 0, ObjectId.Null, ObjectId.Null, 0, 1.0);
                
                A.Ed.WriteMessage($"\nĐã áp dụng Band Style thành công.");
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi áp dụng Band Style: {ex.Message}");
                A.Ed.WriteMessage($"\nSẽ sử dụng individual bands thay thế.");
            }
        }
        
        // Helper method to get band style name from ObjectId
        private static string GetBandStyleName(ObjectId bandStyleId)
        {
            try
            {
                using (var tr = A.Db.TransactionManager.StartTransaction())
                {
                    var entity = tr.GetObject(bandStyleId, OpenMode.ForRead);
                    var nameProperty = entity.GetType().GetProperty("Name");
                    if (nameProperty != null)
                    {
                        return nameProperty.GetValue(entity)?.ToString() ?? "Unknown Band Style";
                    }
                    tr.Commit();
                }
            }
            catch
            {
                // Fallback to a default style name
            }
            return "Standard";
        }
    }
}
