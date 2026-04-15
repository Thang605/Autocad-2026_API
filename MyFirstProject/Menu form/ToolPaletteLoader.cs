using System;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows;
using System.Windows.Forms.Integration;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(MyFirstProject.ToolPaletteLoader))]

namespace MyFirstProject
{
    /// <summary>
    /// Quản lý PaletteSet chứa Tool Palette — hệ thống lệnh tách rời Ribbon
    /// 
    /// Các lệnh:
    ///   SHOW_PALETTE   — Mở/hiện Tool Palette
    ///   HIDE_PALETTE   — Ẩn Tool Palette
    ///   RELOAD_PALETTE — Reload danh sách lệnh từ Excel
    ///   PALETTE_CONFIG — Chọn file Excel khác
    /// </summary>
    public class ToolPaletteLoader
    {
        // Singleton PaletteSet
        private static PaletteSet _paletteSet;
        private static ToolPaletteControl _control;
        private static readonly Guid PaletteGuid = new("B7E3F1A2-C4D6-4E8F-9A0B-1C2D3E4F5678");

        // Đường dẫn Excel tùy chọn (user chọn bằng PALETTE_CONFIG)
        private static string _customExcelPath = null;

        // ═══════════════════════════════════════════════════════════════
        //  LỆNH CHÍNH
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Mở Tool Palette (tạo lần đầu nếu chưa có)
        /// </summary>
        [CommandMethod("SHOW_PALETTE")]
        public static void ShowPalette()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                if (_paletteSet == null)
                {
                    CreatePaletteSet();
                    LoadCommandsFromExcel(ed);
                }

                _paletteSet.Visible = true;
                ed.WriteMessage("\n✅ Tool Palette đã mở.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ Lỗi mở Palette: {ex.Message}");
            }
        }

        /// <summary>
        /// Ẩn Tool Palette
        /// </summary>
        [CommandMethod("HIDE_PALETTE")]
        public static void HidePalette()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;

            if (_paletteSet != null)
            {
                _paletteSet.Visible = false;
                ed.WriteMessage("\n✅ Tool Palette đã ẩn.");
            }
            else
            {
                ed.WriteMessage("\n⚠️ Palette chưa được tạo.");
            }
        }

        /// <summary>
        /// Reload danh sách lệnh từ file Excel (không cần đóng palette)
        /// </summary>
        [CommandMethod("RELOAD_PALETTE")]
        public static void ReloadPalette()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;

            if (_control == null)
            {
                ed.WriteMessage("\n⚠️ Palette chưa được tạo. Chạy SHOW_PALETTE trước.");
                return;
            }

            LoadCommandsFromExcel(ed);
            ed.WriteMessage("\n🔄 Đã reload Tool Palette.");
        }

        /// <summary>
        /// Chọn file Excel khác để nạp danh sách lệnh
        /// </summary>
        [CommandMethod("PALETTE_CONFIG")]
        public static void PaletteConfig()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;

            ed.WriteMessage("\n═══════════════════════════════════════");
            ed.WriteMessage($"\n📂 File Excel hiện tại: {FindToolPaletteExcel() ?? "(chưa chọn)"}");
            ed.WriteMessage("\n═══════════════════════════════════════");

            var options = new PromptKeywordOptions("\nChọn hành động:");
            options.Keywords.Add("Browse", "Browse", "Browse - Chọn file Excel");
            options.Keywords.Add("Reset", "Reset", "Reset - Về mặc định");
            options.Keywords.Add("Reload", "Reload", "Reload - Nạp lại lệnh");
            options.AllowNone = true;

            var result = ed.GetKeywords(options);
            if (result.Status != PromptStatus.OK) return;

            switch (result.StringResult)
            {
                case "Browse":
                    BrowseForExcel(ed);
                    break;

                case "Reset":
                    _customExcelPath = null;
                    ed.WriteMessage("\n✅ Đã reset về mặc định.");
                    if (_control != null) LoadCommandsFromExcel(ed);
                    break;

                case "Reload":
                    if (_control != null) LoadCommandsFromExcel(ed);
                    else ed.WriteMessage("\n⚠️ Chạy SHOW_PALETTE trước.");
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  TẠO PALETTE SET
        // ═══════════════════════════════════════════════════════════════

        private static void CreatePaletteSet()
        {
            _paletteSet = new PaletteSet("Tool Palette", PaletteGuid);

            _paletteSet.Style =
                PaletteSetStyles.ShowAutoHideButton |
                PaletteSetStyles.ShowCloseButton |
                PaletteSetStyles.Snappable |
                PaletteSetStyles.UsePaletteNameAsTitleForSingle;

            _paletteSet.MinimumSize = new System.Drawing.Size(250, 300);
            _paletteSet.Size = new System.Drawing.Size(340, 600);
            _paletteSet.DockEnabled = DockSides.Left | DockSides.Right;

            // Tạo WPF control
            _control = new ToolPaletteControl(ExecuteCommand);

            // Host WPF inside PaletteSet bằng ElementHost
            var host = new ElementHost
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                Child = _control
            };

            _paletteSet.Add("Commands", host);
        }

        // ═══════════════════════════════════════════════════════════════
        //  ĐỌC EXCEL
        // ═══════════════════════════════════════════════════════════════

        private static void LoadCommandsFromExcel(Editor ed)
        {
            string excelPath = FindToolPaletteExcel();

            if (string.IsNullOrEmpty(excelPath))
            {
                ed.WriteMessage("\n⚠️ Không tìm thấy file ToolPalette.xlsx");
                ed.WriteMessage("\n   Dùng lệnh PALETTE_CONFIG → Browse để chọn file.");
                return;
            }

            try
            {
                var commands = ToolPaletteExcelReader.ReadFromExcel(excelPath);
                _control.Dispatcher.Invoke(() => _control.LoadCommands(commands));
                ed.WriteMessage($"\n📋 Đã nạp {commands.Count} lệnh từ {Path.GetFileName(excelPath)}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ Lỗi đọc Excel: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  TÌM FILE EXCEL
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Tìm file ToolPalette.xlsx theo thứ tự ưu tiên:
        /// 1. Đường dẫn user chọn (PALETTE_CONFIG)
        /// 2. Cùng thư mục với MenuConfig.xlsx (ổ mạng Z:\)
        /// 3. Thư mục "Menu form" local
        /// 4. Cùng thư mục DLL
        /// </summary>
        private static string FindToolPaletteExcel()
        {
            // 1. Custom path
            if (!string.IsNullOrEmpty(_customExcelPath) && File.Exists(_customExcelPath))
                return _customExcelPath;

            // 2. Thư mục Excel chia sẻ (ổ mạng / MenuConfig folder)
            string excelFolder = MenuLoader.FindExcelFolder();
            if (!string.IsNullOrEmpty(excelFolder))
            {
                string path = Path.Combine(excelFolder, "ToolPalette.xlsx");
                if (File.Exists(path)) return path;
            }

            // 3. Thư mục DLL / Menu form
            string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string[] searchPaths = new[]
            {
                Path.Combine(dllDir, "ToolPalette.xlsx"),
                Path.Combine(dllDir, "Menu form", "ToolPalette.xlsx"),
                Path.Combine(dllDir, "..", "Menu form", "ToolPalette.xlsx"),
            };

            foreach (var p in searchPaths)
            {
                if (File.Exists(p)) return Path.GetFullPath(p);
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════════════
        //  BROWSE / EXECUTE
        // ═══════════════════════════════════════════════════════════════

        private static void BrowseForExcel(Editor ed)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Chọn file Excel danh sách lệnh",
                Filter = "Excel files (*.xlsx)|*.xlsx",
                InitialDirectory = MenuLoader.FindExcelFolder() ?? @"Z:\"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _customExcelPath = dialog.FileName;
                ed.WriteMessage($"\n✅ Đã chọn: {_customExcelPath}");

                if (_control != null)
                    LoadCommandsFromExcel(ed);
            }
        }

        /// <summary>
        /// Gửi lệnh AutoCAD khi user click button trong palette
        /// </summary>
        private static void ExecuteCommand(string command)
        {
            if (string.IsNullOrEmpty(command)) return;

            try
            {
                // COM SendCommand — đáng tin cậy nhất
                dynamic acadApp = AcadApp.AcadApplication;
                acadApp.ActiveDocument.SendCommand(command + "\n");
            }
            catch
            {
                try
                {
                    // Fallback: Managed API
                    var doc = AcadApp.DocumentManager.MdiActiveDocument;
                    doc?.SendStringToExecute(command + "\n", true, false, true);
                }
                catch { }
            }
        }
    }
}
