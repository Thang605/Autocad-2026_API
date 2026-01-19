using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using ATable = Autodesk.AutoCAD.DatabaseServices.Table;
using MyFirstProject.Extensions;

[assembly: CommandClass(typeof(Civil3DCsharp.AT_XuatBang_Civil_SangExcel))]

namespace Civil3DCsharp
{
    public class AT_XuatBang_Civil_SangExcel
    {
        private static string? _lastExportDirectory;

        /// <summary>
        /// Lệnh xuất 1 bảng Civil 3D Table ra file CSV (mở được bằng Excel)
        /// Hỗ trợ cả bảng AutoCAD Table và bảng Civil 3D (AECC_PARCEL_TABLE, v.v.)
        /// </summary>
        [CommandMethod("AT_XuatBang_SangExcel")]
        public static void XuatBangSangExcel()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                // 1) Chọn 1 đối tượng bảng
                PromptEntityOptions peo = new("\nChọn bảng cần xuất ra Excel: ");
                peo.AllowNone = false;

                PromptEntityResult per = A.Ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                {
                    A.Ed.WriteMessage("\nĐã hủy lệnh.");
                    tr.Abort();
                    return;
                }

                // 2) Lấy đối tượng
                Entity entity = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    A.Ed.WriteMessage("\nKhông thể đọc đối tượng.");
                    tr.Abort();
                    return;
                }

                string dxfName = entity.GetRXClass().DxfName;
                A.Ed.WriteMessage($"\nLoại đối tượng: {dxfName}");

                // Tên file mặc định
                string defaultFileName = $"{dxfName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string initialDir = _lastExportDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                // 3) Hộp thoại lưu file
                using SaveFileDialog sfd = new()
                {
                    Title = "Lưu bảng ra file CSV (mở bằng Excel)",
                    Filter = "CSV File (*.csv)|*.csv",
                    FileName = defaultFileName,
                    InitialDirectory = initialDir,
                    AddExtension = true,
                    DefaultExt = "csv",
                    OverwritePrompt = true
                };

                if (sfd.ShowDialog() != DialogResult.OK)
                {
                    A.Ed.WriteMessage("\nĐã hủy lưu file.");
                    tr.Abort();
                    return;
                }

                string exportPath = sfd.FileName;
                _lastExportDirectory = Path.GetDirectoryName(exportPath);

                // 4) Xuất bảng ra CSV
                bool success;

                if (entity is ATable acadTable)
                {
                    A.Ed.WriteMessage("\nXuất bảng AutoCAD Table...");
                    success = ExportAcadTableToCsv(acadTable, exportPath);
                }
                else if (dxfName.StartsWith("AECC_", StringComparison.OrdinalIgnoreCase) &&
                         dxfName.Contains("TABLE", StringComparison.OrdinalIgnoreCase))
                {
                    A.Ed.WriteMessage("\nXuất bảng Civil 3D...");
                    success = ExportCivil3DTableToCsv(entity, tr, exportPath);
                }
                else
                {
                    A.Ed.WriteMessage($"\nĐối tượng '{dxfName}' không phải là bảng hợp lệ.");
                    tr.Abort();
                    return;
                }

                if (!success)
                {
                    A.Ed.WriteMessage("\nKhông thể xuất bảng.");
                    tr.Abort();
                    return;
                }

                A.Ed.WriteMessage($"\nĐã xuất bảng ra file: {exportPath}");

                // 5) Tự động mở file
                try
                {
                    ProcessStartInfo psi = new()
                    {
                        FileName = exportPath,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    A.Ed.WriteMessage("\nĐã mở file.");
                }
                catch (System.Exception openEx)
                {
                    A.Ed.WriteMessage($"\nKhông thể mở file: {openEx.Message}");
                }

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\nLỗi AutoCAD: {e.Message}");
                tr.Abort();
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi: {ex.Message}");
                tr.Abort();
            }
        }

        /// <summary>
        /// Xuất bảng AutoCAD Table ra CSV
        /// </summary>
        private static bool ExportAcadTableToCsv(ATable table, string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                int rowCount = table.Rows.Count;
                int colCount = table.Columns.Count;

                for (int r = 0; r < rowCount; r++)
                {
                    var cells = new string[colCount];
                    for (int c = 0; c < colCount; c++)
                    {
                        string cellText = "";
                        try { cellText = table.Cells[r, c]?.TextString ?? ""; } catch { }
                        cells[c] = CsvEscape(cellText);
                    }
                    sb.AppendLine(string.Join(",", cells));
                }

                SaveCsvFile(filePath, sb.ToString());
                return true;
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi xuất CSV: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xuất bảng Civil 3D ra CSV bằng cách explode đệ quy
        /// </summary>
        private static bool ExportCivil3DTableToCsv(Entity entity, Transaction tr, string filePath)
        {
            try
            {
                // Explode đệ quy để tìm tất cả text
                var result = ExplodeRecursively(entity, 0);

                // Nếu tìm thấy AutoCAD Table, xuất trực tiếp
                if (result.FoundTable != null)
                {
                    A.Ed.WriteMessage("\nTìm thấy AutoCAD Table bên trong...");
                    return ExportAcadTableToCsv(result.FoundTable, filePath);
                }

                var cellDataList = result.Cells;

                if (cellDataList.Count == 0)
                {
                    A.Ed.WriteMessage("\nKhông tìm thấy dữ liệu text trong bảng.");
                    A.Ed.WriteMessage($"\nLoại đối tượng tìm thấy: {result.ObjectTypesFound}");
                    return false;
                }

                // Nhóm cells theo hàng với tolerance
                double tolerance = 1.0;
                var rowGroups = GroupByTolerance(cellDataList.Select(c => c.Y).Distinct().ToList(), tolerance);
                rowGroups = rowGroups.OrderByDescending(g => g.Average()).ToList();

                // Tạo row mapping trước
                var rowMapping = new Dictionary<double, int>();
                for (int i = 0; i < rowGroups.Count; i++)
                    foreach (var y in rowGroups[i])
                        rowMapping[y] = i;

                // Đếm số cell trong mỗi dòng để xác định dòng tiêu đề
                var cellsPerRow = new Dictionary<int, int>();
                foreach (var cell in cellDataList)
                {
                    if (rowMapping.TryGetValue(cell.Y, out int r))
                    {
                        if (!cellsPerRow.ContainsKey(r))
                            cellsPerRow[r] = 0;
                        cellsPerRow[r]++;
                    }
                }

                // Lấy danh sách X chỉ từ các dòng có nhiều hơn 1 cell (không phải tiêu đề)
                var xValuesForColumns = cellDataList
                    .Where(c => rowMapping.TryGetValue(c.Y, out int r) && cellsPerRow.GetValueOrDefault(r, 0) > 1)
                    .Select(c => c.X)
                    .Distinct()
                    .ToList();

                var colGroups = GroupByTolerance(xValuesForColumns, tolerance);
                colGroups = colGroups.OrderBy(g => g.Average()).ToList();

                var colMapping = new Dictionary<double, int>();
                for (int i = 0; i < colGroups.Count; i++)
                    foreach (var x in colGroups[i])
                        colMapping[x] = i;

                // Tạo bảng 2D
                int rowCount = rowGroups.Count;
                int colCount = colGroups.Count;
                string[,] tableData = new string[rowCount, colCount];

                // Lưu riêng nội dung các dòng tiêu đề (chỉ có 1 cell)
                var titleRows = new Dictionary<int, string>();

                foreach (var cell in cellDataList)
                {
                    if (rowMapping.TryGetValue(cell.Y, out int r))
                    {
                        // Kiểm tra xem đây có phải dòng tiêu đề không
                        if (cellsPerRow.GetValueOrDefault(r, 0) == 1)
                        {
                            // Dòng tiêu đề - lưu riêng để đặt vào cột đầu
                            titleRows[r] = cell.Text;
                        }
                        else if (colMapping.TryGetValue(cell.X, out int c))
                        {
                            // Dòng dữ liệu bình thường
                            if (!string.IsNullOrEmpty(tableData[r, c]))
                                tableData[r, c] += " " + cell.Text;
                            else
                                tableData[r, c] = cell.Text;
                        }
                    }
                }

                // Đặt nội dung tiêu đề vào cột đầu tiên
                foreach (var kvp in titleRows)
                {
                    tableData[kvp.Key, 0] = kvp.Value;
                }

                A.Ed.WriteMessage($"\nĐã tạo bảng {rowCount} hàng x {colCount} cột");

                // Xuất ra CSV
                var sb = new StringBuilder();
                for (int r = 0; r < rowCount; r++)
                {
                    var cells = new string[colCount];
                    for (int c = 0; c < colCount; c++)
                    {
                        cells[c] = CsvEscape(tableData[r, c] ?? "");
                    }
                    sb.AppendLine(string.Join(",", cells));
                }

                SaveCsvFile(filePath, sb.ToString());
                return true;
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi xuất CSV: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kết quả từ việc explode đệ quy
        /// </summary>
        private class ExplodeResult
        {
            public List<CellData> Cells { get; set; } = new();
            public ATable? FoundTable { get; set; }
            public string ObjectTypesFound { get; set; } = "";
        }

        /// <summary>
        /// Explode đệ quy để tìm tất cả text và tables
        /// </summary>
        private static ExplodeResult ExplodeRecursively(Entity entity, int depth, int maxDepth = 5)
        {
            var result = new ExplodeResult();
            var objectTypes = new HashSet<string>();

            if (depth > maxDepth) return result;

            try
            {
                DBObjectCollection explodedObjects = new();
                entity.Explode(explodedObjects);

                foreach (DBObject obj in explodedObjects)
                {
                    string typeName = obj.GetType().Name;
                    string dxfName = "";
                    try { dxfName = obj.GetRXClass().DxfName; } catch { }
                    objectTypes.Add($"{typeName}({dxfName})");

                    if (obj is ATable table)
                    {
                        result.FoundTable = table;
                        result.ObjectTypesFound = string.Join(", ", objectTypes);
                        return result;
                    }
                    else if (obj is MText mtext)
                    {
                        string text = CleanMTextContent(mtext.Contents ?? "");
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            result.Cells.Add(new CellData
                            {
                                X = Math.Round(mtext.Location.X, 2),
                                Y = Math.Round(mtext.Location.Y, 2),
                                Text = text.Trim()
                            });
                        }
                    }
                    else if (obj is DBText dbtext)
                    {
                        string text = dbtext.TextString ?? "";
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            result.Cells.Add(new CellData
                            {
                                X = Math.Round(dbtext.Position.X, 2),
                                Y = Math.Round(dbtext.Position.Y, 2),
                                Text = text.Trim()
                            });
                        }
                    }
                    else if (obj is Entity subEntity)
                    {
                        var subResult = ExplodeRecursively(subEntity, depth + 1, maxDepth);
                        if (subResult.FoundTable != null)
                        {
                            result.FoundTable = subResult.FoundTable;
                            result.ObjectTypesFound = string.Join(", ", objectTypes);
                            return result;
                        }
                        result.Cells.AddRange(subResult.Cells);
                    }
                }

                explodedObjects.Dispose();
            }
            catch { }

            result.ObjectTypesFound = string.Join(", ", objectTypes);
            return result;
        }

        /// <summary>
        /// Loại bỏ các mã format của MText và decode Unicode
        /// </summary>
        private static string CleanMTextContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return "";

            string result = content;

            // Decode Unicode escape sequences
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\\U\+([0-9A-Fa-f]{4})", match =>
            {
                int codePoint = Convert.ToInt32(match.Groups[1].Value, 16);
                return char.ConvertFromUtf32(codePoint);
            });

            result = System.Text.RegularExpressions.Regex.Replace(result, @"\+([0-9A-Fa-f]{4})(?![0-9A-Fa-f])", match =>
            {
                try
                {
                    int codePoint = Convert.ToInt32(match.Groups[1].Value, 16);
                    if (codePoint >= 0x0080)
                        return char.ConvertFromUtf32(codePoint);
                }
                catch { }
                return match.Value;
            });

            // Loại bỏ các mã format MText
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\{\\[^;]+;([^}]*)\}", "$1");
            result = result.Replace("\\P", " ");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\\[HWQTACLOKShwqtacloks][0-9.]*;?", "");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\\[FfNn][^;]*;", "");
            result = result.Replace("{", "").Replace("}", "");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");

            return result.Trim();
        }

        /// <summary>
        /// Nhóm các giá trị với tolerance
        /// </summary>
        private static List<List<double>> GroupByTolerance(List<double> values, double tolerance)
        {
            if (values.Count == 0) return new List<List<double>>();

            var sorted = values.OrderBy(v => v).ToList();
            var groups = new List<List<double>>();
            var currentGroup = new List<double> { sorted[0] };

            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i] - currentGroup.Average() <= tolerance)
                    currentGroup.Add(sorted[i]);
                else
                {
                    groups.Add(currentGroup);
                    currentGroup = new List<double> { sorted[i] };
                }
            }
            groups.Add(currentGroup);

            return groups;
        }

        /// <summary>
        /// Escape chuỗi cho CSV
        /// </summary>
        private static string CsvEscape(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            string s = input.Replace("\r", " ").Replace("\n", " ");
            s = s.Replace("\"", "\"\"");
            if (s.Contains(',') || s.Contains('"') || s.StartsWith(' ') || s.EndsWith(' '))
                s = "\"" + s + "\"";
            return s;
        }

        /// <summary>
        /// Lưu file CSV với UTF-8 BOM
        /// </summary>
        private static void SaveCsvFile(string filePath, string content)
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        /// <summary>
        /// Lớp chứa dữ liệu cell
        /// </summary>
        private class CellData
        {
            public double X { get; set; }
            public double Y { get; set; }
            public string Text { get; set; } = "";
        }
    }
}
