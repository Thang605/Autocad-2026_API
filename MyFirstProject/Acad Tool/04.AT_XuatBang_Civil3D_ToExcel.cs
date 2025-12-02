using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using ATable = Autodesk.AutoCAD.DatabaseServices.Table;
using MyFirstProject.Extensions;

// This line improves loading performance of commands in this class
[assembly: CommandClass(typeof(Civil3DCsharp.CT_XuatBang_Civil3D_ToExcel_Commands))]

namespace Civil3DCsharp
{
 public class CT_XuatBang_Civil3D_ToExcel_Commands
 {
 // Lưu thông tin cho lần sử dụng kế tiếp trong phiên làm việc
 private static string? _lastExportDirectory;
 private static string? _lastExportFileName;

 [CommandMethod("AT_XuatBang_Civil3D_ToExcel")]
 public static void AT_XuatBang_Civil3D_ToExcel()
 {
 // start transaction theo pattern dự án (A.Db, A.Ed)
 using Transaction tr = A.Db.TransactionManager.StartTransaction();
 try
 {
 // Khởi tạo theo pattern (nếu cần dùng các tiện ích khác)
 _ = new UserInput();
 _ = new UtilitiesCAD();
 _ = new UtilitiesC3D();

 //1) Chọn đối tượng bất kỳ, sau đó kiểm tra có phải là bảng hay không
 A.Ed.WriteMessage("\nChọn các bảng (Table) cần xuất ra Excel...");
 ObjectIdCollection selectedIds = UserInput.GSelectionSet("\nChọn đối tượng: ");
 if (selectedIds == null || selectedIds.Count ==0)
 {
 A.Ed.WriteMessage("\nKhông có đối tượng nào được chọn.");
 tr.Abort();
 return;
 }

 //2) Hộp thoại lưu file CSV (ghi nhớ đường dẫn và tên file cuối)
 string suggestedName = _lastExportFileName ?? $"Civil3D_Tables_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
 string initialDir = _lastExportDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

 using SaveFileDialog sfd = new()
 {
 Title = "Chọn nơi lưu file Excel (CSV)",
 Filter = "Excel CSV (*.csv)|*.csv",
 FileName = suggestedName,
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

 //3) Đọc nội dung và ghi ra CSV (chỉ export nếu là bảng ATable)
 StringBuilder sb = new();
 int exported =0;

 int i =0;
 foreach (ObjectId id in selectedIds)
 {
 if (id.IsNull) continue;

 DBObject dbo = tr.GetObject(id, OpenMode.ForRead);
 string dxf = dbo.GetRXClass().DxfName;
 string handle = string.Empty;
 try { handle = (dbo as Entity)?.Handle.ToString() ?? string.Empty; } catch { }

 if (dbo is ATable table)
 {
 // Phân cách giữa các bảng trong file CSV
 if (exported >0)
 {
 sb.AppendLine();
 sb.AppendLine();
 }
 sb.AppendLine($"# Object: {dxf}, Handle: {handle}");

 if (ExportAcadTable(table, sb))
 {
 exported++;
 }
 }
 else
 {
 // Không phải bảng -> ghi thông báo (theo pattern log A.Ed)
 A.Ed.WriteMessage($"\nBỏ qua '{dxf}' (Handle {handle}): không phải bảng.");
 }

 i++;
 }

 if (exported ==0)
 {
 A.Ed.WriteMessage("\nKhông xuất được dữ liệu vì không có bảng hợp lệ.");
 tr.Abort();
 return;
 }

 // Ghi ra file CSV (UTF-8 BOM để Excel hiển thị Unicode tốt)
 Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);
 File.WriteAllText(exportPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

 // Lưu lại thông tin cho lần sau
 _lastExportDirectory = Path.GetDirectoryName(exportPath);
 _lastExportFileName = Path.GetFileName(exportPath);

 A.Ed.WriteMessage($"\nĐã xuất {exported} bảng vào: {exportPath}");

 //4) Mở file bằng ứng dụng mặc định (thường là Excel)
 try
 {
 ProcessStartInfo psi = new()
 {
 FileName = exportPath,
 UseShellExecute = true
 };
 Process.Start(psi);
 }
 catch (System.Exception openEx)
 {
 A.Ed.WriteMessage($"\nKhông thể mở file sau khi xuất: {openEx.Message}");
 }

 tr.Commit();
 }
 catch (Autodesk.AutoCAD.Runtime.Exception e)
 {
 A.Ed.WriteMessage("\nLỗi AutoCAD: " + e.Message);
 A.Ed.WriteMessage("\nError Code: " + e.ErrorStatus);
 tr.Abort();
 }
 catch (System.Exception ex)
 {
 A.Ed.WriteMessage("\nLỗi hệ thống: " + ex.Message);
 tr.Abort();
 }
 }

 private static bool ExportAcadTable(ATable table, StringBuilder sb)
 {
 try
 {
 string styleName = string.Empty;
 try { styleName = table.TableStyleName; } catch { }
 if (!string.IsNullOrEmpty(styleName))
 sb.AppendLine($"# TableStyle: {styleName}");

 int rowCount;
 int colCount;
                rowCount = table.Rows.Count;
                colCount = table.Columns.Count;

 for (int r =0; r < rowCount; r++)
 {
 string[] cells = new string[colCount];
 for (int c =0; c < colCount; c++)
 {
 string text = string.Empty;
 try { text = table.Cells[r, c]?.TextString ?? string.Empty; } catch { text = string.Empty; }
 cells[c] = CsvEscape(text);
 }
 sb.AppendLine(string.Join(",", cells));
 }
 return true;
 }
 catch
 {
 return false;
 }
 }

 private static string CsvEscape(string input)
 {
 if (string.IsNullOrEmpty(input)) return string.Empty;
 // Thay xuống dòng bằng khoảng trắng để tránh phá vỡ CSV
 string s = input.Replace("\r", " ").Replace("\n", " ");
 // Escape dấu nháy kép bằng cách double quotes theo chuẩn CSV
 s = s.Replace("\"", "\"\"");
 // Bao bọc bởi dấu nháy kép nếu có dấu phẩy hoặc nháy hoặc khoảng trắng đầu/cuối
 if (s.Contains(',') || s.Contains('"') || s.StartsWith(' ') || s.EndsWith(' '))
 s = "\"" + s + "\"";
 return s;
 }
 }
}
