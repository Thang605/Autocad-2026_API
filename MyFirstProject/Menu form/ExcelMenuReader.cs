using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace MyFirstProject
{
    /// <summary>
    /// Đọc file Excel (.xlsx) và chuyển thành RibbonTabDef
    /// Cấu trúc Excel (sheet "Ribbon"):
    ///   Tab | Panel | Loại | Tên hiển thị | Tên lệnh | Kích thước | Mô tả
    ///
    /// Loại hỗ trợ:
    ///   button    — Nút đơn (mặc định nếu để trống)
    ///   split     — Nút chính của SplitButton, các dòng "sub" tiếp theo là dropdown
    ///   row       — Bắt đầu nhóm nút nhỏ, các dòng "sub" tiếp theo là nút con
    ///   sub       — Item con thuộc split/row ở trên
    ///   separator — Đường phân cách
    /// </summary>
    public static class ExcelMenuReader
    {
        /// <summary>
        /// Đọc tất cả Ribbon definitions từ 1 file Excel
        /// </summary>
        public static List<RibbonTabDef> ReadRibbonsFromExcel(string excelPath)
        {
            var result = new List<RibbonTabDef>();

            using var workbook = new XLWorkbook(excelPath);

            // Tìm sheet "Ribbon"
            if (!workbook.Worksheets.TryGetWorksheet("Ribbon", out var ws))
                return result;

            // Đọc tất cả rows (bỏ header)
            var rows = new List<ExcelRow>();
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

            for (int r = 2; r <= lastRow; r++) // bỏ dòng 1 (header)
            {
                var tab = ws.Cell(r, 1).GetString().Trim();
                var panel = ws.Cell(r, 2).GetString().Trim();
                var type = ws.Cell(r, 3).GetString().Trim().ToLower();
                var label = ws.Cell(r, 4).GetString().Trim();
                var command = ws.Cell(r, 5).GetString().Trim();
                var size = ws.Cell(r, 6).GetString().Trim().ToLower();
                var desc = ws.Cell(r, 7).GetString().Trim();

                // Bỏ dòng trống hoàn toàn
                if (string.IsNullOrEmpty(tab) && string.IsNullOrEmpty(label) && string.IsNullOrEmpty(command))
                    continue;

                rows.Add(new ExcelRow
                {
                    Tab = tab,
                    Panel = panel,
                    Type = string.IsNullOrEmpty(type) ? "button" : type,
                    Label = label,
                    Command = command,
                    Size = string.IsNullOrEmpty(size) ? "small" : size,
                    Description = desc
                });
            }

            // Chuyển flat rows → cấu trúc phân cấp
            result = BuildRibbonStructure(rows);

            return result;
        }

        /// <summary>
        /// Chuyển danh sách phẳng từ Excel thành cấu trúc Ribbon phân cấp
        /// </summary>
        private static List<RibbonTabDef> BuildRibbonStructure(List<ExcelRow> rows)
        {
            var tabs = new Dictionary<string, RibbonTabDef>();

            // Biến theo dõi split/row hiện tại để gắn sub items
            RibbonItemDef currentParent = null;
            string currentTab = "";
            string currentPanel = "";

            foreach (var row in rows)
            {
                // Kế thừa Tab/Panel từ dòng trước nếu để trống
                string tab = !string.IsNullOrEmpty(row.Tab) ? row.Tab : currentTab;
                string panel = !string.IsNullOrEmpty(row.Panel) ? row.Panel : currentPanel;
                currentTab = tab;
                currentPanel = panel;

                if (string.IsNullOrEmpty(tab)) continue;

                // Đảm bảo tab tồn tại
                if (!tabs.ContainsKey(tab))
                    tabs[tab] = new RibbonTabDef { Tab = tab };

                // Đảm bảo panel tồn tại trong tab
                var tabDef = tabs[tab];
                var panelDef = tabDef.Panels.FirstOrDefault(p => p.Name == panel);
                if (panelDef == null)
                {
                    panelDef = new RibbonPanelDef { Name = panel };
                    tabDef.Panels.Add(panelDef);
                }

                // Xử lý theo loại
                switch (row.Type)
                {
                    case "sub":
                        // Gắn vào parent (split hoặc row) gần nhất
                        if (currentParent != null)
                        {
                            currentParent.Items.Add(new RibbonItemDef
                            {
                                Type = "button",
                                Label = row.Label,
                                Command = row.Command,
                                Size = row.Size,
                                Description = row.Description
                            });
                        }
                        break;

                    case "split":
                    case "row":
                        var parentItem = new RibbonItemDef
                        {
                            Type = row.Type,
                            Label = row.Label,
                            Command = row.Command,
                            Size = row.Size,
                            Description = row.Description,
                            Items = new List<RibbonItemDef>()
                        };
                        panelDef.Items.Add(parentItem);
                        currentParent = parentItem;
                        break;

                    case "separator":
                        panelDef.Items.Add(new RibbonItemDef { Type = "separator" });
                        currentParent = null;
                        break;

                    case "button":
                    default:
                        panelDef.Items.Add(new RibbonItemDef
                        {
                            Type = "button",
                            Label = row.Label,
                            Command = row.Command,
                            Size = row.Size,
                            Description = row.Description
                        });
                        currentParent = null;
                        break;
                }
            }

            return tabs.Values.ToList();
        }

        /// <summary>
        /// Row tạm khi đọc từ Excel
        /// </summary>
        private class ExcelRow
        {
            public string Tab { get; set; }
            public string Panel { get; set; }
            public string Type { get; set; }
            public string Label { get; set; }
            public string Command { get; set; }
            public string Size { get; set; }
            public string Description { get; set; }
        }
    }
}
