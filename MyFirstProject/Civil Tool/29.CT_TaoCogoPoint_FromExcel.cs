// (C) Copyright 2024 by T27 Co.
// Lệnh tạo COGO Point từ file Excel
// File Excel cần có các cột: X, Y, Z (Elevation), Description (tùy chọn)

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using MyFirstProject.Extensions;
using ClosedXML.Excel;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.CT_TaoCogoPoint_FromExcel_Commands))]

namespace Civil3DCsharp
{
    public class CT_TaoCogoPoint_FromExcel_Commands
    {
        private static string? _lastImportDirectory;

        /// <summary>
        /// Lệnh chính: Tạo COGO Point từ file Excel
        /// File Excel cần có các cột: X, Y, Z, Description (tùy chọn)
        /// </summary>
        [CommandMethod("CTPO_TaoCogoPoint_FromExcel")]
        public static void CreateCogoPointFromExcel()
        {
            try
            {
                A.Ed.WriteMessage("\n📊 Lệnh tạo COGO Point từ file Excel...");

                // Step 1: Chọn file Excel
                A.Ed.WriteMessage("\n\n🎯 BƯỚC 1: Chọn file Excel chứa tọa độ điểm");
                string initialDir = _lastImportDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                using OpenFileDialog ofd = new()
                {
                    Title = "Chọn file Excel chứa tọa độ điểm",
                    Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                    InitialDirectory = initialDir,
                    Multiselect = false
                };

                if (ofd.ShowDialog() != DialogResult.OK)
                {
                    A.Ed.WriteMessage("\n❌ Đã hủy lệnh.");
                    return;
                }

                string excelFilePath = ofd.FileName;
                _lastImportDirectory = Path.GetDirectoryName(excelFilePath);
                A.Ed.WriteMessage($"\n✓ Đã chọn file: {Path.GetFileName(excelFilePath)}");

                // Step 2: Đọc dữ liệu từ file Excel
                A.Ed.WriteMessage("\n\n🎯 BƯỚC 2: Đọc dữ liệu từ file Excel");
                List<PointData> pointDataList = ReadPointDataFromExcel(excelFilePath);

                if (pointDataList.Count == 0)
                {
                    A.Ed.WriteMessage("\n❌ Không tìm thấy dữ liệu điểm hợp lệ trong file Excel.");
                    A.Ed.WriteMessage("\n📋 File Excel cần có các cột: X, Y, Z (hoặc Easting, Northing, Elevation)");
                    return;
                }

                A.Ed.WriteMessage($"\n✓ Đọc được {pointDataList.Count} điểm từ file Excel");

                // Step 3: Tạo COGO Points
                A.Ed.WriteMessage("\n\n🎯 BƯỚC 3: Tạo COGO Points");
                int createdCount = 0;
                int errorCount = 0;

                using (Transaction tr = A.Db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // Lấy CogoPointCollection từ Civil Document
                        CogoPointCollection cogoPointColl = A.Cdoc.CogoPoints;

                        foreach (var pointData in pointDataList)
                        {
                            try
                            {
                                Point3d point3D = new(pointData.X, pointData.Y, pointData.Z);
                                
                                // Sử dụng Description từ file Excel (mặc định là "EG")
                                string description = pointData.Description;
                                
                                // Tạo COGO Point
                                ObjectId pointId = cogoPointColl.Add(point3D, description, true);
                                
                                // Đặt tên cho point nếu có
                                if (!string.IsNullOrEmpty(pointData.Name))
                                {
                                    CogoPoint? cogoPoint = tr.GetObject(pointId, OpenMode.ForWrite) as CogoPoint;
                                    if (cogoPoint != null)
                                    {
                                        // Tên điểm được lưu trong PointName hoặc sử dụng như description key
                                        cogoPoint.PointName = pointData.Name;
                                    }
                                }
                                
                                createdCount++;

                                // Hiển thị tiến trình mỗi 10 điểm
                                if (createdCount % 10 == 0)
                                {
                                    A.Ed.WriteMessage($"\n  - Đã tạo {createdCount}/{pointDataList.Count} điểm...");
                                }
                            }
                            catch (System.Exception ex)
                            {
                                errorCount++;
                                A.Ed.WriteMessage($"\n  ⚠️ Lỗi tạo điểm tại ({pointData.X:F3}, {pointData.Y:F3}): {ex.Message}");
                            }
                        }

                        tr.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\n❌ Lỗi trong quá trình tạo điểm: {ex.Message}");
                        tr.Abort();
                        return;
                    }
                }


                // Kết quả
                A.Ed.WriteMessage($"\n\n✅ ===== HOÀN THÀNH =====");
                A.Ed.WriteMessage($"\n📍 Đã tạo thành công: {createdCount} điểm COGO Point");
                if (errorCount > 0)
                {
                    A.Ed.WriteMessage($"\n⚠️ Số điểm lỗi: {errorCount}");
                }

            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\n❌ Lỗi AutoCAD: {e.Message}");
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n❌ Lỗi hệ thống: {ex.Message}");
            }
        }

        /// <summary>
        /// Đọc dữ liệu điểm từ file Excel
        /// Hỗ trợ các định dạng cột: X/Easting, Y/Northing, Z/Elevation, Description/Mô tả
        /// </summary>
        private static List<PointData> ReadPointDataFromExcel(string filePath)
        {
            List<PointData> pointDataList = new();

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1); // Lấy sheet đầu tiên

                // Tìm header row và xác định vị trí các cột
                int headerRow = 1;
                int colName = -1, colX = -1, colY = -1, colZ = -1, colDesc = -1;

                // Duyệt các ô trong hàng đầu tiên để tìm header
                var headerRowCells = worksheet.Row(headerRow).CellsUsed();
                foreach (var cell in headerRowCells)
                {
                    string headerValue = cell.GetString().ToLower().Trim();
                    int colIndex = cell.Address.ColumnNumber;

                    // Xác định cột Tên (Name)
                    if (headerValue == "tên" || headerValue == "ten" || headerValue == "name" || headerValue == "point name" || headerValue == "tên điểm")
                    {
                        colName = colIndex;
                    }
                    // Xác định cột X
                    else if (headerValue == "x" || headerValue == "easting" || headerValue == "tọa độ x" || headerValue == "toadox")
                    {
                        colX = colIndex;
                    }
                    // Xác định cột Y
                    else if (headerValue == "y" || headerValue == "northing" || headerValue == "tọa độ y" || headerValue == "toadoy")
                    {
                        colY = colIndex;
                    }
                    // Xác định cột Z
                    else if (headerValue == "z" || headerValue == "elevation" || headerValue == "cao độ" || headerValue == "caodo" || headerValue == "h")
                    {
                        colZ = colIndex;
                    }
                    // Xác định cột Description
                    else if (headerValue == "description" || headerValue == "desc" || headerValue == "mô tả" || headerValue == "mota" || headerValue == "ghi chú")
                    {
                        colDesc = colIndex;
                    }
                }

                // Nếu không tìm thấy header, giả định cột 1=Tên, 2=X, 3=Y, 4=Z, 5=Description
                if (colX == -1 || colY == -1)
                {
                    A.Ed.WriteMessage("\n⚠️ Không tìm thấy header. Sử dụng thứ tự mặc định: Cột 1=Tên, Cột 2=X, Cột 3=Y, Cột 4=Z, Cột 5=Description");
                    colName = 1;
                    colX = 2;
                    colY = 3;
                    colZ = 4;
                    colDesc = 5;
                    headerRow = 0; // Không có header row
                }
                else
                {
                    A.Ed.WriteMessage($"\n✓ Tìm thấy header: Tên=Cột {(colName > 0 ? colName.ToString() : "Không có")}, X=Cột {colX}, Y=Cột {colY}, Z=Cột {(colZ > 0 ? colZ.ToString() : "Không có")}, Desc=Cột {(colDesc > 0 ? colDesc.ToString() : "Không có")}");
                }

                // Đọc dữ liệu từ hàng sau header
                int startRow = headerRow + 1;
                var lastRowUsed = worksheet.LastRowUsed();
                int endRow = lastRowUsed?.RowNumber() ?? startRow;

                for (int row = startRow; row <= endRow; row++)
                {
                    try
                    {
                        var rowData = worksheet.Row(row);
                        
                        // Kiểm tra nếu hàng trống
                        if (!rowData.CellsUsed().Any())
                            continue;

                        // Đọc Tên điểm
                        string pointName = "";
                        if (colName > 0)
                        {
                            var cellName = worksheet.Cell(row, colName);
                            pointName = cellName.GetString()?.Trim() ?? "";
                        }

                        // Đọc giá trị X
                        var cellX = worksheet.Cell(row, colX);
                        if (!TryGetDoubleValue(cellX, out double x))
                            continue;

                        // Đọc giá trị Y
                        var cellY = worksheet.Cell(row, colY);
                        if (!TryGetDoubleValue(cellY, out double y))
                            continue;

                        // Đọc giá trị Z (mặc định = 0 nếu không có)
                        double z = 0;
                        if (colZ > 0)
                        {
                            var cellZ = worksheet.Cell(row, colZ);
                            TryGetDoubleValue(cellZ, out z);
                        }

                        // Đọc Description (mặc định = "EG" nếu không có)
                        string description = "EG";
                        if (colDesc > 0)
                        {
                            var cellDesc = worksheet.Cell(row, colDesc);
                            string descValue = cellDesc.GetString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(descValue))
                            {
                                description = descValue;
                            }
                        }

                        pointDataList.Add(new PointData
                        {
                            Name = pointName,
                            X = x,
                            Y = y,
                            Z = z,
                            Description = description
                        });
                    }
                    catch
                    {
                        // Bỏ qua các hàng lỗi
                        continue;
                    }
                }


                A.Ed.WriteMessage($"\n✓ Đọc thành công {pointDataList.Count} điểm từ sheet '{worksheet.Name}'");
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n❌ Lỗi đọc file Excel: {ex.Message}");
            }

            return pointDataList;
        }

        /// <summary>
        /// Thử chuyển đổi giá trị ô thành số thực
        /// </summary>
        private static bool TryGetDoubleValue(IXLCell cell, out double value)
        {
            value = 0;

            if (cell == null || cell.IsEmpty())
                return false;

            // Thử lấy giá trị số trực tiếp
            if (cell.DataType == XLDataType.Number)
            {
                value = cell.GetDouble();
                return true;
            }

            // Thử parse từ chuỗi
            string stringValue = cell.GetString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(stringValue))
                return false;

            // Thay thế dấu phẩy bằng dấu chấm (cho định dạng số Việt Nam)
            stringValue = stringValue.Replace(",", ".");

            return double.TryParse(stringValue, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// Cấu trúc dữ liệu điểm
        /// </summary>
        private class PointData
        {
            public string Name { get; set; } = "";
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public string Description { get; set; } = "EG";
        }
    }
}
