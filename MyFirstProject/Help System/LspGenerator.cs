// (C) Copyright 2024 by T27
// Tạo file AutoLISP (.lsp) từ danh sách lệnh tắt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Civil3DCsharp.HelpSystem
{
    /// <summary>
    /// Class tạo file AutoLISP chứa các lệnh tắt
    /// </summary>
    public static class LspGenerator
    {
        /// <summary>
        /// Tạo file .lsp từ danh sách lệnh tắt
        /// </summary>
        public static void GenerateLspFile(List<CommandShortcutInfo> shortcuts, string outputPath)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine(";;; ═══════════════════════════════════════════════════════════════");
            sb.AppendLine(";;; T27 TOOLS - COMMAND SHORTCUTS");
            sb.AppendLine(";;; ═══════════════════════════════════════════════════════════════");
            sb.AppendLine($";;; Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($";;; Total shortcuts: {shortcuts.Count}");
            sb.AppendLine(";;;");
            sb.AppendLine(";;; Cách sử dụng:");
            sb.AppendLine(";;;   1. Load file này vào AutoCAD: (load \"path/to/shortcuts.lsp\")");
            sb.AppendLine(";;;   2. Hoặc thêm vào file acad.lsp để tự động load");
            sb.AppendLine(";;; ═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            // Group by category
            var groupedShortcuts = shortcuts
                .Where(s => !string.IsNullOrWhiteSpace(s.Shortcut))
                .GroupBy(s => GetMainCategory(s.Category))
                .OrderBy(g => g.Key);

            foreach (var group in groupedShortcuts)
            {
                sb.AppendLine($";;; --- {group.Key.ToUpper()} ---");
                sb.AppendLine();

                foreach (var shortcut in group.OrderBy(s => s.Shortcut))
                {
                    // Comment mô tả
                    sb.AppendLine($";;; {shortcut.Shortcut} -> {shortcut.OriginalCommand}");
                    if (!string.IsNullOrEmpty(shortcut.Description))
                    {
                        sb.AppendLine($";;;   {shortcut.Description}");
                    }

                    // Định nghĩa hàm LISP
                    sb.AppendLine($"(defun c:{shortcut.Shortcut} () (command \"{shortcut.OriginalCommand}\") (princ))");
                    sb.AppendLine();
                }
            }

            // Footer
            sb.AppendLine(";;; ═══════════════════════════════════════════════════════════════");
            sb.AppendLine(";;; End of T27 Command Shortcuts");
            sb.AppendLine(";;; ═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("(princ \"\\nT27 Shortcuts loaded successfully!\")");
            sb.AppendLine("(princ)");

            // Write file
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Tạo nội dung preview cho hiển thị
        /// </summary>
        public static string GeneratePreview(List<CommandShortcutInfo> shortcuts, int maxLines = 20)
        {
            var sb = new StringBuilder();
            var validShortcuts = shortcuts.Where(s => !string.IsNullOrWhiteSpace(s.Shortcut)).ToList();

            sb.AppendLine($";;; T27 COMMAND SHORTCUTS - Preview");
            sb.AppendLine($";;; Total: {validShortcuts.Count} shortcuts");
            sb.AppendLine();

            int count = 0;
            foreach (var shortcut in validShortcuts.OrderBy(s => s.Shortcut))
            {
                sb.AppendLine($"(defun c:{shortcut.Shortcut} () (command \"{shortcut.OriginalCommand}\") (princ))");
                count++;

                if (count >= maxLines)
                {
                    sb.AppendLine($";;; ... and {validShortcuts.Count - count} more shortcuts");
                    break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Lấy category chính từ category đầy đủ
        /// </summary>
        private static string GetMainCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) return "Khác";

            if (category.Contains(" - "))
                return category.Split(new[] { " - " }, StringSplitOptions.None)[0].Trim();

            return category;
        }
    }
}
