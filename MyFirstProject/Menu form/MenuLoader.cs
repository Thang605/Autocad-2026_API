using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(MyFirstProject.MenuLoader))]

namespace MyFirstProject
{
    #region JSON Models — Menu
    public class MenuConfig
    {
        [JsonPropertyName("menus")]
        public List<MenuDefinition> Menus { get; set; } = new();

        [JsonPropertyName("ribbons")]
        public List<RibbonTabDef> Ribbons { get; set; } = new();
    }

    public class MenuDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("groups")]
        public List<MenuGroup> Groups { get; set; } = new();
    }

    public class MenuGroup
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("items")]
        public List<MenuItem> Items { get; set; } = new();
    }

    public class MenuItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "command";

        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("command")]
        public string Command { get; set; } = "";

        [JsonPropertyName("help")]
        public string Help { get; set; } = "";
    }
    #endregion

    #region JSON Models — Ribbon
    /// <summary>
    /// Định nghĩa 1 Ribbon Tab
    /// </summary>
    public class RibbonTabDef
    {
        [JsonPropertyName("tab")]
        public string Tab { get; set; } = "";

        [JsonPropertyName("panels")]
        public List<RibbonPanelDef> Panels { get; set; } = new();
    }

    /// <summary>
    /// Định nghĩa 1 Ribbon Panel (nhóm nút bên trong tab)
    /// </summary>
    public class RibbonPanelDef
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("items")]
        public List<RibbonItemDef> Items { get; set; } = new();
    }

    /// <summary>
    /// Định nghĩa 1 Ribbon Item (button, split, row, separator)
    /// </summary>
    public class RibbonItemDef
    {
        /// <summary>
        /// Loại: "button" (mặc định), "split", "row", "separator"
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "button";

        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("command")]
        public string Command { get; set; } = "";

        /// <summary>
        /// Kích thước: "large" hoặc "small" (mặc định)
        /// </summary>
        [JsonPropertyName("size")]
        public string Size { get; set; } = "small";

        /// <summary>
        /// Mô tả tooltip khi hover
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        /// <summary>
        /// Items con (dùng cho type = "split" hoặc "row")
        /// </summary>
        [JsonPropertyName("items")]
        public List<RibbonItemDef> Items { get; set; } = new();
    }
    #endregion

    public class MenuLoader
    {
        /// <summary>
        /// Đường dẫn mặc định trên ổ mạng chia sẻ
        /// </summary>
        private static readonly string DEFAULT_EXCEL_FOLDER =
            @"Z:\Z.FORM MAU LAM VIEC\1. BIM\2.MAU C3D\1.LISP\0.CIVIL TOOL\Excel file";

        /// <summary>
        /// Đường dẫn thư mục Excel hiện tại (có thể thay đổi bằng lệnh MENU_CONFIG)
        /// Lưu trong suốt phiên làm việc AutoCAD
        /// </summary>
        private static string _currentExcelFolder = null;

        /// <summary>
        /// Lấy đường dẫn thư mục Excel đang dùng
        /// </summary>
        private static string ActiveExcelFolder =>
            _currentExcelFolder ?? DEFAULT_EXCEL_FOLDER;

        // ═══════════════════════════════════════════════════════════════
        //  LỆNH CHÍNH
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo menu từ tất cả file .json trong thư mục cấu hình
        /// </summary>
        [CommandMethod("SHOW_MENU")]
        public static void ShowMenu()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                string excelFolder = FindExcelFolder();
                if (string.IsNullOrEmpty(excelFolder))
                {
                    ed.WriteMessage($"\n❌ Không tìm thấy thư mục Excel.");
                    ed.WriteMessage($"\n   Đường dẫn cấu hình: {ActiveExcelFolder}");
                    ed.WriteMessage($"\n   Dùng lệnh MENU_CONFIG để chọn thư mục khác.");
                    return;
                }

                // Tìm tất cả file config (.xlsx)
                var excelFiles = Directory.GetFiles(excelFolder, "*.xlsx")
                    .OrderBy(f => Path.GetFileName(f)).ToList();

                if (excelFiles.Count == 0)
                {
                    ed.WriteMessage($"\n⚠️ Không có file .xlsx nào trong: {excelFolder}");
                    return;
                }

                // Gộp tất cả ribbon definitions
                var allRibbonDefs = new Dictionary<string, RibbonTabDef>();
                int fileCount = 0;

                // 1. Đọc từ Excel (.xlsx)
                foreach (var xlsxFile in excelFiles)
                {
                    try
                    {
                        var ribbons = ExcelMenuReader.ReadRibbonsFromExcel(xlsxFile);
                        foreach (var ribbonDef in ribbons)
                        {
                            if (allRibbonDefs.ContainsKey(ribbonDef.Tab))
                                allRibbonDefs[ribbonDef.Tab].Panels.AddRange(ribbonDef.Panels);
                            else
                                allRibbonDefs[ribbonDef.Tab] = ribbonDef;
                        }
                        fileCount++;
                        ed.WriteMessage($"\n   📊 {Path.GetFileName(xlsxFile)} ({ribbons.Count} tab)");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n   ⚠️ Bỏ qua {Path.GetFileName(xlsxFile)}: {ex.Message}");
                    }
                }



                // Tạo Ribbon
                int totalRibbonCmds = 0;
                if (allRibbonDefs.Count > 0)
                {
                    RibbonLoader.RemoveAllCustomTabs();

                    foreach (var ribbonDef in allRibbonDefs.Values)
                    {
                        int count = RibbonLoader.CreateRibbonTab(ribbonDef);
                        totalRibbonCmds += count;
                    }
                    ed.WriteMessage($"\n✅ Ribbon: {allRibbonDefs.Count} tab, {totalRibbonCmds} lệnh");
                }
                else
                {
                    ed.WriteMessage("\n⚠️ Không có ribbon nào được định nghĩa.");
                    return;
                }

                ed.WriteMessage($"\n📂 {excelFolder} ({fileCount} file)");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Reload menu (alias)
        /// </summary>
        [CommandMethod("RELOAD_MENU")]
        public static void ReloadMenu()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\n🔄 Đang reload menu...");
            ShowMenu();
        }

        /// <summary>
        /// Cấu hình đường dẫn thư mục Excel
        /// Tuỳ chọn: [Chọn thư mục / Nhập đường dẫn / Reset mặc định]
        /// </summary>
        [CommandMethod("MENU_CONFIG")]
        public static void MenuConfig_Command()
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;

            ed.WriteMessage($"\n═══════════════════════════════════════");
            ed.WriteMessage($"\n📂 Thư mục Excel hiện tại: {ActiveExcelFolder}");
            ed.WriteMessage($"\n   Mặc định: {DEFAULT_EXCEL_FOLDER}");
            ed.WriteMessage($"\n═══════════════════════════════════════");

            // Hỏi người dùng chọn hành động
            var options = new PromptKeywordOptions("\nChọn hành động:");
            options.Keywords.Add("Browse", "Browse", "Browse - Chọn thư mục");
            options.Keywords.Add("Path", "Path", "Path - Nhập đường dẫn");
            options.Keywords.Add("Reset", "Reset", "Reset - Về mặc định");
            options.Keywords.Add("Info", "Info", "Info - Xem danh sách file");
            options.AllowNone = true;

            var result = ed.GetKeywords(options);
            if (result.Status != PromptStatus.OK) return;

            switch (result.StringResult)
            {
                case "Browse":
                    BrowseForFolder(ed);
                    break;

                case "Path":
                    InputPath(ed);
                    break;

                case "Reset":
                    _currentExcelFolder = null;
                    ed.WriteMessage($"\n✅ Đã reset về mặc định: {DEFAULT_EXCEL_FOLDER}");
                    break;

                case "Info":
                    ShowExcelFileList(ed);
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  CẤU HÌNH ĐƯỜNG DẪN
        // ═══════════════════════════════════════════════════════════════

        private static void BrowseForFolder(Editor ed)
        {
            // Dùng OpenFileDialog để chọn 1 file Excel → lấy thư mục cha
            var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Chọn file Excel trong thư mục cần dùng",
                Filter = "Excel files (*.xlsx)|*.xlsx",
                InitialDirectory = Directory.Exists(ActiveExcelFolder) ? ActiveExcelFolder : @"Z:\"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folder = Path.GetDirectoryName(dialog.FileName);
                _currentExcelFolder = folder;
                ed.WriteMessage($"\n✅ Đã chọn thư mục: {folder}");
                ed.WriteMessage($"\n   Chạy RELOAD_MENU để cập nhật menu.");
            }
        }

        private static void InputPath(Editor ed)
        {
            var pathResult = ed.GetString("\nNhập đường dẫn thư mục chứa file Excel: ");
            if (pathResult.Status != PromptStatus.OK) return;

            string path = pathResult.StringResult.Trim().Trim('"');

            if (Directory.Exists(path))
            {
                _currentExcelFolder = path;
                ed.WriteMessage($"\n✅ Đã đổi thư mục: {path}");
                ed.WriteMessage($"\n   Chạy RELOAD_MENU để cập nhật menu.");
            }
            else
            {
                ed.WriteMessage($"\n❌ Thư mục không tồn tại: {path}");
            }
        }

        private static void ShowExcelFileList(Editor ed)
        {
            string folder = FindExcelFolder();
            if (string.IsNullOrEmpty(folder))
            {
                ed.WriteMessage($"\n❌ Thư mục không tồn tại: {ActiveExcelFolder}");
                return;
            }

            var files = Directory.GetFiles(folder, "*.xlsx").OrderBy(f => f).ToList();
            ed.WriteMessage($"\n📂 {folder}");
            ed.WriteMessage($"\n   Tìm thấy {files.Count} file Excel:");

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                ed.WriteMessage($"\n   📄 {info.Name} ({info.Length / 1024.0:F1} KB)");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  CORE LOGIC
        // ═══════════════════════════════════════════════════════════════

        #region Core Logic
        private static int CreateMenuFromDefinition(MenuDefinition menuDef)
        {
            dynamic acadApp = AcadApp.AcadApplication;
            dynamic menuBar = acadApp.MenuBar;
            dynamic popupMenus = acadApp.MenuGroups.Item(0).Menus;
            string menuName = menuDef.Name;

            // 1. Xóa menu cũ trên MenuBar nếu có
            try
            {
                for (int i = 0; i < menuBar.Count; i++)
                {
                    if (menuBar.Item(i).Name == menuName)
                    {
                        menuBar.Item(i).RemoveFromMenuBar();
                        break;
                    }
                }
            }
            catch { }

            // 2. Tìm hoặc tạo PopupMenu
            dynamic targetMenu = null;
            bool exists = false;
            foreach (dynamic menu in popupMenus)
            {
                if (menu.Name == menuName)
                {
                    targetMenu = menu;
                    exists = true;
                    while (targetMenu.Count > 0)
                    {
                        targetMenu.Item(0).Delete();
                    }
                    break;
                }
            }

            if (!exists)
            {
                targetMenu = popupMenus.Add(menuName);
            }

            // 3. Build cấu trúc từ JSON
            int commandCount = 0;
            foreach (var group in menuDef.Groups)
            {
                dynamic subMenu = targetMenu.AddSubMenu(targetMenu.Count + 1, group.Name);
                commandCount += BuildMenuItems(subMenu, group.Items);
            }

            // 4. Gắn vào MenuBar
            targetMenu.InsertInMenuBar(menuBar.Count + 1);

            return commandCount;
        }

        private static int BuildMenuItems(dynamic menu, List<MenuItem> items)
        {
            int count = 0;
            foreach (var item in items)
            {
                switch (item.Type?.ToLower())
                {
                    case "separator":
                        menu.AddSeparator(menu.Count + 1);
                        break;

                    case "header":
                        var headerItem = menu.AddMenuItem(menu.Count + 1, item.Label, "^C^C");
                        headerItem.Enable = false;
                        break;

                    case "command":
                    default:
                        string macro = item.Command;
                        if (!string.IsNullOrEmpty(macro) && !macro.EndsWith(" "))
                            macro += " ";

                        var menuItem = menu.AddMenuItem(menu.Count + 1, item.Label, macro);

                        if (!string.IsNullOrEmpty(item.Help))
                        {
                            try { menuItem.HelpString = item.Help; } catch { }
                        }

                        count++;
                        break;
                }
            }
            return count;
        }
        #endregion

        // ═══════════════════════════════════════════════════════════════
        //  TÌM THƯ MỤC EXCEL
        // ═══════════════════════════════════════════════════════════════

        #region File Resolution
        /// <summary>
        /// Tìm thư mục chứa Excel theo thứ tự:
        /// 1. Thư mục đã chọn bằng MENU_CONFIG (nếu có)
        /// 2. Ổ mạng Z:\ mặc định
        /// 3. Fallback: tìm local từ thư mục DLL
        /// </summary>
        public static string FindExcelFolder()
        {
            // Ưu tiên 1: Thư mục đã cấu hình
            if (!string.IsNullOrEmpty(_currentExcelFolder) && Directory.Exists(_currentExcelFolder))
                return _currentExcelFolder;

            // Ưu tiên 2: Ổ mạng mặc định
            if (Directory.Exists(DEFAULT_EXCEL_FOLDER))
                return DEFAULT_EXCEL_FOLDER;

            // Ưu tiên 3: Tìm local (đi lên từ thư mục DLL)
            string currentDir = GetDllDirectory();
            for (int i = 0; i < 5; i++)
            {
                if (string.IsNullOrEmpty(currentDir)) break;

                if (Directory.Exists(currentDir) &&
                    Directory.GetFiles(currentDir, "*.xlsx").Length > 0)
                    return currentDir;

                string menuFormDir = Path.Combine(currentDir, "Menu form");
                if (Directory.Exists(menuFormDir) &&
                    Directory.GetFiles(menuFormDir, "*.xlsx").Length > 0)
                    return menuFormDir;

                currentDir = Directory.GetParent(currentDir)?.FullName;
            }

            return null;
        }

        private static string GetDllDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        }
        #endregion
    }
}
