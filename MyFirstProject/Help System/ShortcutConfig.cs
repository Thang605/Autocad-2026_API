// (C) Copyright 2024 by T27
// Quản lý cấu hình lệnh tắt - Load/Save JSON

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Civil3DCsharp.HelpSystem
{
    /// <summary>
    /// Class quản lý cấu hình lệnh tắt, hỗ trợ load/save từ file JSON
    /// </summary>
    public class ShortcutConfig
    {
        /// <summary>
        /// Phiên bản của cấu hình
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Thời gian sửa đổi cuối cùng
        /// </summary>
        public string LastModified { get; set; }

        /// <summary>
        /// Danh sách các lệnh tắt
        /// </summary>
        public List<CommandShortcutInfo> Shortcuts { get; set; }

        /// <summary>
        /// Constructor mặc định
        /// </summary>
        public ShortcutConfig()
        {
            Version = "1.0";
            LastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Shortcuts = new List<CommandShortcutInfo>();
        }

        /// <summary>
        /// Load cấu hình từ file JSON
        /// </summary>
        public static ShortcutConfig LoadFromFile(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                return ParseJson(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parse JSON string thành ShortcutConfig
        /// </summary>
        private static ShortcutConfig ParseJson(string json)
        {
            var config = new ShortcutConfig();
            config.Shortcuts = new List<CommandShortcutInfo>();

            // Parse Version
            var versionMatch = Regex.Match(json, "\"Version\"\\s*:\\s*\"([^\"]+)\"");
            if (versionMatch.Success)
                config.Version = versionMatch.Groups[1].Value;

            // Parse LastModified
            var lastModMatch = Regex.Match(json, "\"LastModified\"\\s*:\\s*\"([^\"]+)\"");
            if (lastModMatch.Success)
                config.LastModified = lastModMatch.Groups[1].Value;

            // Parse Shortcuts array
            var shortcutsMatch = Regex.Match(json, "\"Shortcuts\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
            if (shortcutsMatch.Success)
            {
                string shortcutsJson = shortcutsMatch.Groups[1].Value;

                // Match each object in the array
                var objectMatches = Regex.Matches(shortcutsJson, "\\{([^{}]*)\\}");
                foreach (Match objMatch in objectMatches)
                {
                    var shortcut = new CommandShortcutInfo();
                    string objJson = objMatch.Groups[1].Value;

                    var origCmdMatch = Regex.Match(objJson, "\"OriginalCommand\"\\s*:\\s*\"([^\"]+)\"");
                    if (origCmdMatch.Success)
                        shortcut.OriginalCommand = origCmdMatch.Groups[1].Value;

                    var shortcutMatch = Regex.Match(objJson, "\"Shortcut\"\\s*:\\s*\"([^\"]*)\"");
                    if (shortcutMatch.Success)
                        shortcut.Shortcut = shortcutMatch.Groups[1].Value;

                    var categoryMatch = Regex.Match(objJson, "\"Category\"\\s*:\\s*\"([^\"]*)\"");
                    if (categoryMatch.Success)
                        shortcut.Category = categoryMatch.Groups[1].Value;

                    var descMatch = Regex.Match(objJson, "\"Description\"\\s*:\\s*\"([^\"]*)\"");
                    if (descMatch.Success)
                        shortcut.Description = descMatch.Groups[1].Value;

                    if (!string.IsNullOrEmpty(shortcut.OriginalCommand))
                        config.Shortcuts.Add(shortcut);
                }
            }

            return config;
        }

        /// <summary>
        /// Lưu cấu hình ra file JSON
        /// </summary>
        public void SaveToFile(string path)
        {
            LastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string json = ToJson();
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        /// <summary>
        /// Chuyển đổi sang JSON string
        /// </summary>
        private string ToJson()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"Version\": \"{EscapeJson(Version)}\",");
            sb.AppendLine($"  \"LastModified\": \"{EscapeJson(LastModified)}\",");
            sb.AppendLine("  \"Shortcuts\": [");

            for (int i = 0; i < Shortcuts.Count; i++)
            {
                var s = Shortcuts[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"OriginalCommand\": \"{EscapeJson(s.OriginalCommand)}\",");
                sb.AppendLine($"      \"Shortcut\": \"{EscapeJson(s.Shortcut ?? "")}\",");
                sb.AppendLine($"      \"Category\": \"{EscapeJson(s.Category ?? "")}\",");
                sb.AppendLine($"      \"Description\": \"{EscapeJson(s.Description ?? "")}\"");
                sb.Append("    }");
                if (i < Shortcuts.Count - 1)
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Escape special characters for JSON
        /// </summary>
        private static string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        /// <summary>
        /// Merge với danh sách lệnh mới - giữ lại shortcuts đã đặt, thêm lệnh mới
        /// </summary>
        public void MergeWithNewCommands(IEnumerable<CommandInfo> allCommands)
        {
            // Tạo dictionary từ shortcuts hiện có
            var existingShortcuts = Shortcuts.ToDictionary(s => s.OriginalCommand, StringComparer.OrdinalIgnoreCase);

            var newShortcuts = new List<CommandShortcutInfo>();

            foreach (var cmd in allCommands)
            {
                if (existingShortcuts.TryGetValue(cmd.Name, out var existing))
                {
                    // Giữ lại shortcut đã đặt, cập nhật thông tin mới
                    existing.Category = cmd.Category;
                    existing.Description = cmd.Description;
                    newShortcuts.Add(existing);
                }
                else
                {
                    // Lệnh mới, thêm vào với shortcut trống
                    newShortcuts.Add(CommandShortcutInfo.FromCommandInfo(cmd));
                }
            }

            Shortcuts = newShortcuts;
        }

        /// <summary>
        /// Lấy danh sách các lệnh tắt có giá trị (đã được gán)
        /// </summary>
        public List<CommandShortcutInfo> GetAssignedShortcuts()
        {
            return Shortcuts.Where(s => !string.IsNullOrWhiteSpace(s.Shortcut)).ToList();
        }

        /// <summary>
        /// Tìm các lệnh tắt bị trùng
        /// </summary>
        public Dictionary<string, List<string>> FindDuplicateShortcuts()
        {
            return Shortcuts
                .Where(s => !string.IsNullOrWhiteSpace(s.Shortcut))
                .GroupBy(s => s.Shortcut.ToUpper())
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.Select(s => s.OriginalCommand).ToList());
        }
    }
}
