using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;

namespace MyFirstProject
{
    /// <summary>
    /// Dữ liệu 1 lệnh trong Tool Palette
    /// </summary>
    public class PaletteCommandInfo
    {
        public string Category { get; set; } = "";
        public string Label { get; set; } = "";
        public string Command { get; set; } = "";
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Đọc file Excel ToolPalette.xlsx → danh sách lệnh
    /// 
    /// Hỗ trợ 2 cấu trúc:
    /// 
    /// ► Cấu trúc 1: Mỗi sheet = 1 category (tên sheet = tên category)
    ///   Cột: Tên hiển thị | Tên lệnh | Mô tả  (3 cột)
    ///   
    /// ► Cấu trúc 2: Tất cả trong 1 sheet "Commands"
    ///   Cột: Category | Tên hiển thị | Tên lệnh | Mô tả  (4 cột)
    ///   
    /// Tự động nhận diện dựa trên sự tồn tại của sheet "Commands".
    /// </summary>
    public static class ToolPaletteExcelReader
    {
        public static List<PaletteCommandInfo> ReadFromExcel(string excelPath)
        {
            using var workbook = new XLWorkbook(excelPath);

            // Nếu có sheet "Commands" → đọc cấu trúc 1-sheet (cũ)
            if (workbook.Worksheets.TryGetWorksheet("Commands", out var commandsSheet))
                return ReadSingleSheet(commandsSheet);

            // Không có "Commands" → mỗi sheet = 1 category
            return ReadMultiSheet(workbook);
        }

        /// <summary>
        /// Cấu trúc multi-sheet: tên sheet = category
        /// Cột: Tên hiển thị | Tên lệnh | Mô tả
        /// </summary>
        private static List<PaletteCommandInfo> ReadMultiSheet(XLWorkbook workbook)
        {
            var result = new List<PaletteCommandInfo>();

            foreach (var ws in workbook.Worksheets)
            {
                string category = ws.Name.Trim();

                // Bỏ qua sheet ẩn hoặc sheet tên bắt đầu bằng "_" (dự phòng)
                if (string.IsNullOrEmpty(category) || category.StartsWith("_"))
                    continue;

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

                for (int r = 2; r <= lastRow; r++) // bỏ header dòng 1
                {
                    var label = ws.Cell(r, 1).GetString().Trim();
                    var command = ws.Cell(r, 2).GetString().Trim();
                    var desc = ws.Cell(r, 3).GetString().Trim();

                    if (string.IsNullOrEmpty(label) && string.IsNullOrEmpty(command))
                        continue;

                    result.Add(new PaletteCommandInfo
                    {
                        Category = category,
                        Label = string.IsNullOrEmpty(label) ? command : label,
                        Command = command,
                        Description = desc
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Cấu trúc single-sheet "Commands" (tương thích cũ)
        /// Cột: Category | Tên hiển thị | Tên lệnh | Mô tả
        /// </summary>
        private static List<PaletteCommandInfo> ReadSingleSheet(IXLWorksheet ws)
        {
            var result = new List<PaletteCommandInfo>();
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            string currentCategory = "";

            for (int r = 2; r <= lastRow; r++)
            {
                var cat = ws.Cell(r, 1).GetString().Trim();
                var label = ws.Cell(r, 2).GetString().Trim();
                var command = ws.Cell(r, 3).GetString().Trim();
                var desc = ws.Cell(r, 4).GetString().Trim();

                if (!string.IsNullOrEmpty(cat))
                    currentCategory = cat;

                if (string.IsNullOrEmpty(label) && string.IsNullOrEmpty(command))
                    continue;

                result.Add(new PaletteCommandInfo
                {
                    Category = currentCategory,
                    Label = string.IsNullOrEmpty(label) ? command : label,
                    Command = command,
                    Description = desc
                });
            }

            return result;
        }
    }
}
