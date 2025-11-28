// (C) Copyright 2024 by  
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.DatabaseServices;
using MyFirstProject.Extensions;
using ClosedXML.Excel;
using MyFirstProject.Civil_Tool;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.CTSV_XuatKhoiLuongRaExcel_Commands))]

namespace Civil3DCsharp
{
    public class CTSV_XuatKhoiLuongRaExcel_Commands
    {
        private static string? _lastExportDirectory;
        
        [CommandMethod("CTSV_XuatKhoiLuongRaExcel")]
        public static void CTSVXuatKhoiLuongRaExcel()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                A.Ed.WriteMessage("\n📊 Lệnh xuất bảng khối lượng vật liệu ra Excel...");

                // Step 1: Lấy tất cả Alignments có SampleLineGroup
                A.Ed.WriteMessage("\n\n🎯 BƯỚC 1: Tìm tất cả Alignment có SampleLineGroup");
                List<AlignmentInfo> alignmentsWithSLG = GetAllAlignmentsWithSampleLineGroups(tr);

                if (alignmentsWithSLG.Count == 0)
                {
                    A.Ed.WriteMessage("\n❌ Không tìm thấy Alignment nào có SampleLineGroup.");
                    tr.Abort();
                    return;
                }

                A.Ed.WriteMessage($"\n✓ Tìm thấy {alignmentsWithSLG.Count} alignment(s) có SampleLineGroup");

                // Step 2: Hiển thị form để chọn alignments
                A.Ed.WriteMessage("\n\n🎯 BƯỚC 2: Chọn Alignment để xuất khối lượng");
                var selectionForm = new AlignmentSelectionForm(alignmentsWithSLG);
                Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(selectionForm);

                if (!selectionForm.DialogResult_OK || selectionForm.SelectedAlignments.Count == 0)
                {
                    A.Ed.WriteMessage("\n❌ Đã hủy lệnh.");
                    tr.Abort();
                    return;
                }

                A.Ed.WriteMessage($"\n✓ Đã chọn {selectionForm.SelectedAlignments.Count} alignment(s)");

                // Step 3: Thu thập thông tin từ tất cả các SampleLineGroups
                A.Ed.WriteMessage("\n\n🎯 BƯỚC 3: Thu thập thông tin khối lượng vật liệu");
                List<SheetData> allSheetData = new();
                
                foreach (var alignmentInfo in selectionForm.SelectedAlignments)
                {
                    A.Ed.WriteMessage($"\n\n📍 Xử lý Alignment: {alignmentInfo.AlignmentName}");
                    
                    foreach (var slgInfo in alignmentInfo.SampleLineGroups)
                    {
                        A.Ed.WriteMessage($"\n  - SampleLineGroup: {slgInfo.SampleLineGroupName}");
                        
                        SampleLineGroup? sampleLineGroup = tr.GetObject(slgInfo.SampleLineGroupId, OpenMode.ForRead) as SampleLineGroup;
                        if (sampleLineGroup == null) continue;

                        List<MaterialVolumeInfo> materialInfoList = CollectMaterialVolumeInformation(sampleLineGroup, tr);
                        
                        if (materialInfoList.Count > 0)
                        {
                            var sheetData = new SheetData
                            {
                                SheetName = GenerateSheetName(alignmentInfo.AlignmentName, slgInfo.SampleLineGroupName),
                                AlignmentName = alignmentInfo.AlignmentName,
                                SampleLineGroupName = slgInfo.SampleLineGroupName,
                                SampleLineGroupCount = alignmentInfo.SampleLineGroupCount,
                                MaterialInfoList = materialInfoList
                            };
                            allSheetData.Add(sheetData);
                            A.Ed.WriteMessage($"\n  ✓ Thu thập được {materialInfoList.Count} mục khối lượng");
                        }
                    }
                }

                if (allSheetData.Count == 0)
                {
                    A.Ed.WriteMessage("\n❌ Không tìm thấy thông tin material section nào.");
                    tr.Abort();
                    return;
                }

                A.Ed.WriteMessage($"\n\n✓ Tổng cộng: {allSheetData.Count} sheet sẽ được tạo");

                // Step 4: Chọn vị trí lưu file Excel
                A.Ed.WriteMessage("\n\n🎯 BƯỚC 4: Chọn vị trí lưu file Excel");
                string suggestedName = $"KhoiLuong_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string initialDir = _lastExportDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                using SaveFileDialog sfd = new()
                {
                    Title = "Chọn nơi lưu file Excel",
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = suggestedName,
                    InitialDirectory = initialDir,
                    AddExtension = true,
                    DefaultExt = "xlsx",
                    OverwritePrompt = true
                };

                if (sfd.ShowDialog() != DialogResult.OK)
                {
                    A.Ed.WriteMessage("\n❌ Đã hủy lưu file.");
                    tr.Abort();
                    return;
                }

                string exportPath = sfd.FileName;
                _lastExportDirectory = Path.GetDirectoryName(exportPath);

                // Step 5: Xuất ra Excel với nhiều sheet
                A.Ed.WriteMessage("\n\n🎯 BƯỚC 5: Xuất dữ liệu ra Excel");
                ExportMultipleSheetsToExcel(allSheetData, exportPath);

                A.Ed.WriteMessage($"\n\n✅ ===== HOÀN THÀNH =====");
                A.Ed.WriteMessage($"\n📁 File đã được lưu tại: {exportPath}");
                A.Ed.WriteMessage($"\n📊 Đã tạo {allSheetData.Count} sheet(s) trong file Excel");
                
                // Hỏi người dùng có muốn mở file không
                PromptKeywordOptions pko = new("\nBạn có muốn mở file Excel không?")
                {
                    Keywords = { "Yes", "No" },
                    AllowNone = false
                };
                pko.Keywords.Default = "Yes";
                
                PromptResult pr = A.Ed.GetKeywords(pko);
                if (pr.Status == PromptStatus.OK && pr.StringResult == "Yes")
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exportPath,
                        UseShellExecute = true
                    });
                }

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\n❌ Lỗi AutoCAD: {e.Message}");
                A.Ed.WriteMessage($"\nError Code: {e.ErrorStatus}");
                tr.Abort();
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n❌ Lỗi hệ thống: {ex.Message}");
                A.Ed.WriteMessage($"\nStack Trace: {ex.StackTrace}");
                tr.Abort();
            }
        }

        private static List<AlignmentInfo> GetAllAlignmentsWithSampleLineGroups(Transaction tr)
        {
            List<AlignmentInfo> result = new();

            try
            {
                BlockTable? bt = tr.GetObject(A.Db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (bt == null) return result;

                BlockTableRecord? btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                if (btr == null) return result;

                foreach (ObjectId entityId in btr)
                {
                    try
                    {
                        if (tr.GetObject(entityId, OpenMode.ForRead) is Alignment alignment)
                        {
                            ObjectIdCollection sampleLineGroupIds = alignment.GetSampleLineGroupIds();
                            
                            if (sampleLineGroupIds.Count > 0)
                            {
                                var alignmentInfo = new AlignmentInfo
                                {
                                    AlignmentId = entityId,
                                    AlignmentName = alignment.Name ?? "Unknown",
                                    SampleLineGroupCount = sampleLineGroupIds.Count
                                };

                                // Lấy thông tin từng SampleLineGroup
                                foreach (ObjectId slgId in sampleLineGroupIds)
                                {
                                    try
                                    {
                                        SampleLineGroup? slg = tr.GetObject(slgId, OpenMode.ForRead) as SampleLineGroup;
                                        if (slg != null)
                                        {
                                            alignmentInfo.SampleLineGroups.Add(new SampleLineGroupInfo
                                            {
                                                SampleLineGroupId = slgId,
                                                SampleLineGroupName = slg.Name ?? "Unknown",
                                                AlignmentName = alignmentInfo.AlignmentName
                                            });
                                        }
                                    }
                                    catch
                                    {
                                        continue;
                                    }
                                }

                                result.Add(alignmentInfo);
                                A.Ed.WriteMessage($"\n  - {alignmentInfo.AlignmentName}: {sampleLineGroupIds.Count} SampleLineGroup(s)");
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n⚠️  Lỗi khi tìm alignment: {ex.Message}");
            }

            return result;
        }

        private static string GenerateSheetName(string alignmentName, string sampleLineGroupName)
        {
            // Excel sheet name có giới hạn 31 ký tự
            string sheetName = $"{alignmentName}_{sampleLineGroupName}";
            
            // Loại bỏ các ký tự không hợp lệ
            char[] invalidChars = { '\\', '/', '*', '?', ':', '[', ']' };
            foreach (char c in invalidChars)
            {
                sheetName = sheetName.Replace(c, '_');
            }

            // Cắt ngắn nếu quá dài
            if (sheetName.Length > 31)
            {
                sheetName = sheetName.Substring(0, 31);
            }

            return sheetName;
        }

        private static void ExportMultipleSheetsToExcel(List<SheetData> allSheetData, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();

                foreach (var sheetData in allSheetData)
                {
                    A.Ed.WriteMessage($"\n  📄 Tạo sheet: {sheetData.SheetName}");
                    
                    // Xử lý dữ liệu cho sheet này - truyền thêm thông tin alignment, SampleLineGroup và số lượng SLG
                    var pivotData = CreatePivotTableData(sheetData.MaterialInfoList, sheetData.AlignmentName, sheetData.SampleLineGroupName, sheetData.SampleLineGroupCount);
                    
                    // Tạo worksheet
                    var worksheet = workbook.Worksheets.Add(sheetData.SheetName);
                    
                    // Xuất dữ liệu vào sheet
                    ExportSheetData(worksheet, pivotData, sheetData.AlignmentName, sheetData.SampleLineGroupName);
                    
                    A.Ed.WriteMessage($"\n  ✓ Sheet '{sheetData.SheetName}': {pivotData.StakeInfos.Count} cọc, {pivotData.MaterialTypes.Count} vật liệu");
                }

                // Save workbook
                workbook.SaveAs(filePath);
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n❌ Lỗi xuất Excel: {ex.Message}");
                throw;
            }
        }

        private static void ExportSheetData(IXLWorksheet worksheet, PivotTableData pivotData, string alignmentName, string sampleLineGroupName)
        {
            try
            {
                int currentRow = 1;
                // Luôn sử dụng 3 chữ số thập phân
                int decimalPlaces = 3;

                // ===== TITLE =====
                string title = $"BẢNG KHỐI LƯỢNG VẬT LIỆU - {alignmentName}";
                int totalCols = 3 + pivotData.MaterialTypes.Count;
                
                worksheet.Cell(currentRow, 1).Value = title;
                worksheet.Range(currentRow, 1, currentRow, totalCols).Merge();
                var titleCell = worksheet.Cell(currentRow, 1);
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 14;
                titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                titleCell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                worksheet.Row(currentRow).Height = 30;
                currentRow++;

                // Bỏ dòng Subtitle (SampleLineGroup)

                // ===== HEADER =====
                worksheet.Cell(currentRow, 1).Value = "Lý trình";
                worksheet.Cell(currentRow, 2).Value = "Tên cọc";
                worksheet.Cell(currentRow, 3).Value = "Khoảng cách lẻ (m)";

                for (int i = 0; i < pivotData.MaterialTypes.Count; i++)
                {
                    worksheet.Cell(currentRow, 4 + i).Value = $"{pivotData.MaterialTypes[i]} (m²)";
                }

                // Style cho header
                for (int col = 1; col <= totalCols; col++)
                {
                    var headerCell = worksheet.Cell(currentRow, col);
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Font.FontSize = 11;
                    headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    headerCell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    headerCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                worksheet.Row(currentRow).Height = 20;
                currentRow++;

                // ===== DATA ROWS =====
                int dataStartRow = currentRow;
                foreach (var stakeInfo in pivotData.StakeInfos)
                {
                    worksheet.Cell(currentRow, 1).Value = stakeInfo.Station;
                    worksheet.Cell(currentRow, 2).Value = stakeInfo.StakeName;
                    
                    // Làm tròn khoảng cách đến 3 chữ số thập phân
                    double spacingRounded = Math.Round(stakeInfo.SpacingPrev, 3);
                    worksheet.Cell(currentRow, 3).Value = spacingRounded;
                    worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "0.000";

                    for (int i = 0; i < pivotData.MaterialTypes.Count; i++)
                    {
                        string materialType = pivotData.MaterialTypes[i];
                        double area = stakeInfo.MaterialAreas.ContainsKey(materialType) ? stakeInfo.MaterialAreas[materialType] : 0.0;
                        
                        if (pivotData.MaterialAdditionalValues.ContainsKey(materialType))
                        {
                            area += pivotData.MaterialAdditionalValues[materialType];
                        }

                        // Làm tròn đến 3 chữ số thập phân
                        double areaRounded = Math.Round(area, 3);
                        
                        // Luôn hiển thị giá trị số (0 thay vì "-")
                        worksheet.Cell(currentRow, 4 + i).Value = areaRounded;
                        worksheet.Cell(currentRow, 4 + i).Style.NumberFormat.Format = "0.000";
                    }

                    // Style cho data rows
                    for (int col = 1; col <= totalCols; col++)
                    {
                        var dataCell = worksheet.Cell(currentRow, col);
                        dataCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        dataCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        dataCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }

                    currentRow++;
                }
                int dataEndRow = currentRow - 1;

                // ===== TOTAL ROW =====
                worksheet.Cell(currentRow, 1).Value = "TỔNG CỘNG";
                worksheet.Range(currentRow, 1, currentRow, 2).Merge();
                
                double totalSpacing = pivotData.StakeInfos.Sum(s => s.SpacingPrev);
                double totalSpacingRounded = Math.Round(totalSpacing, 3);
                worksheet.Cell(currentRow, 3).Value = totalSpacingRounded;
                worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "0.000";

                for (int i = 0; i < pivotData.MaterialTypes.Count; i++)
                {
                    string materialType = pivotData.MaterialTypes[i];
                    double sum = 0.0;
                    
                    foreach (var stake in pivotData.StakeInfos)
                    {
                        double stakeArea = 0.0;
                        if (stake.MaterialAreas.ContainsKey(materialType))
                            stakeArea = stake.MaterialAreas[materialType];
                        
                        if (pivotData.MaterialAdditionalValues.ContainsKey(materialType))
                            stakeArea += pivotData.MaterialAdditionalValues[materialType];
                        
                        sum += stakeArea;
                    }
                    
                    // Làm tròn tổng đến 3 chữ số thập phân
                    double sumRounded = Math.Round(sum, 3);
                    worksheet.Cell(currentRow, 4 + i).Value = sumRounded;
                    worksheet.Cell(currentRow, 4 + i).Style.NumberFormat.Format = "0.000";
                }

                // Style cho total row - Đổi từ Medium sang Thin
                for (int col = 1; col <= totalCols; col++)
                {
                    var totalCell = worksheet.Cell(currentRow, col);
                    totalCell.Style.Font.Bold = true;
                    totalCell.Style.Font.FontSize = 11;
                    totalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    totalCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    totalCell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                    totalCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                worksheet.Row(currentRow).Height = 22;

                // ===== COLUMN WIDTHS =====
                worksheet.Column(1).Width = 15;
                worksheet.Column(2).Width = 12;
                worksheet.Column(3).Width = 15;
                for (int i = 4; i <= totalCols; i++)
                {
                    worksheet.Column(i).Width = 15;
                }

                // ===== BỎ AUTO FILTER =====
                // Đã bỏ dòng: worksheet.Range(2, 1, dataEndRow, totalCols).SetAutoFilter();
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n❌ Lỗi xuất sheet: {ex.Message}");
                throw;
            }
        }

        private static List<MaterialVolumeInfo> CollectMaterialVolumeInformation(SampleLineGroup sampleLineGroup, Transaction tr)
        {
            List<MaterialVolumeInfo> materialInfoList = new();

            try
            {
                // Lấy tất cả sample lines trong group
                ObjectIdCollection sampleLineIds = sampleLineGroup.GetSampleLineIds();
                A.Ed.WriteMessage($"\n  - Tìm thấy {sampleLineIds.Count} sample lines trong group.");

                // Duyệt qua từng SampleLine
                foreach (ObjectId sampleLineId in sampleLineIds)
                {
                    try
                    {
                        SampleLine? sampleLine = tr.GetObject(sampleLineId, OpenMode.ForRead) as SampleLine;
                        if (sampleLine == null) continue;

                        double station = sampleLine.Station;
                        string stakeName = sampleLine.Name ?? FormatStation(station);
                        
                        A.Ed.WriteMessage($"\n  📍 Xử lý cọc: {stakeName} (Station: {FormatStation(station)})");

                        // Lấy tất cả Section IDs từ SampleLine
                        ObjectIdCollection sectionIds = sampleLine.GetSectionIds();
                        A.Ed.WriteMessage($"\n     - Tìm thấy {sectionIds.Count} sections");

                        // Dictionary để lưu MaterialSection duy nhất cho mỗi material name
                        Dictionary<string, (double Area, string SourceName)> uniqueMaterials = new();

                        // Duyệt qua từng Section để tìm MaterialSection
                        foreach (ObjectId sectionId in sectionIds)
                        {
                            try
                            {
                                Autodesk.AutoCAD.DatabaseServices.DBObject dbObj = tr.GetObject(sectionId, OpenMode.ForRead);
                                
                                // Kiểm tra nếu là MaterialSection
                                if (dbObj is MaterialSection materialSection)
                                {
                                    string sourceName = materialSection.SourceName ?? "Không có tên";
                                    string materialName = ExtractMaterialNameFromSource(sourceName);
                                    double area = CalculateAreaFromSectionPoints(materialSection.SectionPoints);

                                    // Chỉ thêm hoặc cập nhật nếu chưa có hoặc diện tích lớn hơn
                                    if (!uniqueMaterials.ContainsKey(materialName))
                                    {
                                        uniqueMaterials[materialName] = (area, sourceName);
                                        A.Ed.WriteMessage($"\n     ✓ {materialName}: {area:F3} m²");
                                    }
                                    else if (area > uniqueMaterials[materialName].Area)
                                    {
                                        A.Ed.WriteMessage($"\n     ⚠️  Cập nhật {materialName}: {uniqueMaterials[materialName].Area:F3} → {area:F3} m²");
                                        uniqueMaterials[materialName] = (area, sourceName);
                                    }
                                    else
                                    {
                                        A.Ed.WriteMessage($"\n     ℹ️  Bỏ qua duplicate {materialName}: {area:F3} m² (đã có {uniqueMaterials[materialName].Area:F3} m²)");
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                A.Ed.WriteMessage($"\n     ⚠️  Lỗi xử lý section: {ex.Message}");
                                continue;
                            }
                        }

                        // Thêm tất cả material duy nhất vào list
                        foreach (var kvp in uniqueMaterials)
                        {
                            MaterialVolumeInfo info = new()
                            {
                                StakeName = stakeName,
                                Station = FormatStation(station),
                                StationValue = station,
                                MaterialName = kvp.Key,
                                Area = kvp.Value.Area,
                                SourceName = kvp.Value.SourceName
                            };
                            materialInfoList.Add(info);
                        }

                        A.Ed.WriteMessage($"\n     → Tổng: {uniqueMaterials.Count} loại vật liệu duy nhất");
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\n  ❌ Lỗi xử lý sample line: {ex.Message}");
                        continue;
                    }
                }

                // Sắp xếp theo lý trình
                materialInfoList = materialInfoList.OrderBy(x => x.StationValue).ToList();

                A.Ed.WriteMessage($"\n\n  ✅ Tổng cộng: {materialInfoList.Count} mục khối lượng vật liệu đã được thu thập.");
                
                // Hiển thị thống kê
                var materialStats = materialInfoList
                    .GroupBy(x => x.MaterialName)
                    .Select(g => new { Material = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count);
                
                A.Ed.WriteMessage("\n  📊 Thống kê vật liệu:");
                foreach (var stat in materialStats)
                {
                    A.Ed.WriteMessage($"\n     - {stat.Material}: {stat.Count} cọc");
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n❌ Lỗi khi thu thập thông tin material: {ex.Message}");
            }

            return materialInfoList;
        }

        private static string ExtractMaterialNameFromSource(string sourceName)
        {
            try
            {
                if (string.IsNullOrEmpty(sourceName))
                    return "Không xác định";

                int dashIndex = sourceName.LastIndexOf('-');
                if (dashIndex >= 0 && dashIndex < sourceName.Length - 1)
                {
                    string materialName = sourceName.Substring(dashIndex + 1).Trim();
                    return string.IsNullOrEmpty(materialName) ? "Không xác định" : materialName;
                }

                return sourceName.Trim();
            }
            catch
            {
                return "Không xác định";
            }
        }

        private static double CalculateAreaFromSectionPoints(SectionPointCollection sectionPoints)
        {
            try
            {
                if (sectionPoints == null || sectionPoints.Count < 3)
                    return 0.0;

                List<Autodesk.AutoCAD.Geometry.Point2d> points = new();
                
                for (int i = 0; i < sectionPoints.Count; i++)
                {
                    SectionPoint sectionPoint = sectionPoints[i];
                    Autodesk.AutoCAD.Geometry.Point3d location = sectionPoint.Location;
                    points.Add(new Autodesk.AutoCAD.Geometry.Point2d(location.X, location.Y));
                }

                return CalculatePolygonArea(points);
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n⚠️  Lỗi tính diện tích từ section points: {ex.Message}");
                return 0.0;
            }
        }

        private static double CalculatePolygonArea(List<Autodesk.AutoCAD.Geometry.Point2d> points)
        {
            if (points.Count < 3) return 0.0;

            double area = 0.0;
            int n = points.Count;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += points[i].X * points[j].Y;
                area -= points[j].X * points[i].Y;
            }

            return Math.Abs(area) / 2.0;
        }

        private static string FormatStation(double station)
        {
            int km = (int)(station / 1000);
            double meters = station % 1000;
            return $"Km{km}+{meters:F3}";
        }

        private static PivotTableData CreatePivotTableData(List<MaterialVolumeInfo> materialInfoList, string alignmentName = "", string sampleLineGroupName = "", int sampleLineGroupCount = 0)
        {
            var pivotData = new PivotTableData();
            
            var allMaterialTypes = materialInfoList
                .Select(x => x.MaterialName)
                .Distinct()
                .ToList();

            var (orderedMaterials, decimalPlaces, additionalValues) = GetUserOrderedMaterialsAndDecimalPlaces(allMaterialTypes, alignmentName, sampleLineGroupName);
            pivotData.MaterialTypes = orderedMaterials;
            pivotData.DecimalPlaces = decimalPlaces;
            pivotData.MaterialAdditionalValues = additionalValues;

            var groupedByStation = materialInfoList
                .GroupBy(x => x.StationValue)
                .OrderBy(g => g.Key);

            foreach (var group in groupedByStation)
            {
                var firstItem = group.First();
                var stakeInfo = new StakeInfo
                {
                    Station = firstItem.Station,
                    StakeName = firstItem.StakeName,
                    StationValue = firstItem.StationValue,
                    MaterialAreas = new Dictionary<string, double>()
                };

                foreach (var item in group)
                {
                    if (stakeInfo.MaterialAreas.ContainsKey(item.MaterialName))
                    {
                        stakeInfo.MaterialAreas[item.MaterialName] += item.Area;
                    }
                    else
                    {
                        stakeInfo.MaterialAreas[item.MaterialName] = item.Area;
                    }
                }

                pivotData.StakeInfos.Add(stakeInfo);
            }

            for (int i = 0; i < pivotData.StakeInfos.Count; i++)
            {
                if (i == 0)
                {
                    pivotData.StakeInfos[i].SpacingPrev = 0.0;
                }
                else
                {
                    pivotData.StakeInfos[i].SpacingPrev = Math.Abs(pivotData.StakeInfos[i].StationValue - pivotData.StakeInfos[i - 1].StationValue);
                }
            }

            return pivotData;
        }

        private static (List<string> materials, int decimalPlaces, Dictionary<string, double> additionalValues) GetUserOrderedMaterialsAndDecimalPlaces(List<string> materialTypes, string alignmentName = "", string sampleLineGroupName = "", int sampleLineGroupCount = 0)
        {
            try
            {
                var defaultOrderedMaterials = SortMaterialsByPriority(materialTypes);
                
                A.Ed.WriteMessage($"\n  - Tìm thấy {materialTypes.Count} loại vật liệu. Hiển thị form sắp xếp...");
                
                // Tạo form với thông tin alignment, SampleLineGroup và số lượng SLG
                MaterialOrderForm orderForm;
                if (!string.IsNullOrEmpty(alignmentName) || !string.IsNullOrEmpty(sampleLineGroupName))
                {
                    orderForm = new MaterialOrderForm(defaultOrderedMaterials, alignmentName, sampleLineGroupName, sampleLineGroupCount);
                }
                else
                {
                    orderForm = new MaterialOrderForm(defaultOrderedMaterials);
                }
                
                Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(orderForm);
                
                if (orderForm.DialogResult_OK && orderForm.OrderedMaterialTypes.Count > 0)
                {
                    A.Ed.WriteMessage($"\n  - Người dùng đã sắp xếp thứ tự: {string.Join(", ", orderForm.OrderedMaterialTypes)}");
                    A.Ed.WriteMessage($"\n  - Số chữ số thập phân: {orderForm.DecimalPlaces}");
                    
                    return (orderForm.OrderedMaterialTypes, orderForm.DecimalPlaces, orderForm.MaterialAdditionalValues);
                }
                else
                {
                    A.Ed.WriteMessage("\n  - Sử dụng thứ tự mặc định.");
                    var defaultAdditionalValues = new Dictionary<string, double>();
                    foreach (var material in defaultOrderedMaterials)
                    {
                        defaultAdditionalValues[material] = 0.0;
                    }
                    return (defaultOrderedMaterials, 2, defaultAdditionalValues);
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n⚠️  Lỗi hiển thị form sắp xếp: {ex.Message}");
                var defaultOrderedMaterials = SortMaterialsByPriority(materialTypes);
                var defaultAdditionalValues = new Dictionary<string, double>();
                foreach (var material in defaultOrderedMaterials)
                {
                    defaultAdditionalValues[material] = 0.0;
                }
                return (defaultOrderedMaterials, 2, defaultAdditionalValues);
            }
        }

        private static List<string> SortMaterialsByPriority(List<string> materialTypes)
        {
            var materialPriority = new Dictionary<string, int>
            {
                { "Đào đất", 1 }, { "Dao dat", 1 },
                { "Đắp đất", 2 }, { "Dap dat", 2 },
                { "Đất đào", 3 }, { "Dat dao", 3 },
                { "Đất đắp", 4 }, { "Dat dap", 4 },
                { "BTN hạt mịn", 10 },
                { "Bê tông nhựa", 14 },
                { "CPDD loại 1", 20 },
                { "Cấp phối đá dăm", 23 },
                { "Bóc hữu cơ", 50 }, { "Boc huu co", 50 }
            };

            return materialTypes
                .OrderBy(material => 
                {
                    if (materialPriority.ContainsKey(material))
                        return materialPriority[material];
                    
                    var normalizedMaterial = material.ToLower().Trim();
                    foreach (var kvp in materialPriority)
                    {
                        if (normalizedMaterial.Contains(kvp.Key.ToLower()) || 
                            kvp.Key.ToLower().Contains(normalizedMaterial))
                        {
                            return kvp.Value;
                        }
                    }
                    
                    return 1000;
                })
                .ThenBy(material => material)
                .ToList();
        }

        // Helper classes
        private class SheetData
        {
            public string SheetName { get; set; } = "";
            public string AlignmentName { get; set; } = "";
            public string SampleLineGroupName { get; set; } = "";
            public int SampleLineGroupCount { get; set; } = 0;
            public List<MaterialVolumeInfo> MaterialInfoList { get; set; } = new();
        }

        private class MaterialVolumeInfo
        {
            public string StakeName { get; set; } = "";
            public string Station { get; set; } = "";
            public double StationValue { get; set; }
            public string MaterialName { get; set; } = "";
            public double Area { get; set; }
            public string SourceName { get; set; } = "";
        }

        private class PivotTableData
        {
            public List<string> MaterialTypes { get; set; } = new();
            public List<StakeInfo> StakeInfos { get; set; } = new();
            public int DecimalPlaces { get; set; } = 3;
            public Dictionary<string, double> MaterialAdditionalValues { get; set; } = new();
        }

        private class StakeInfo
        {
            public string Station { get; set; } = "";
            public string StakeName { get; set; } = "";
            public double StationValue { get; set; }
            public double SpacingPrev { get; set; }
            public Dictionary<string, double> MaterialAreas { get; set; } = new();
        }
    }
}
