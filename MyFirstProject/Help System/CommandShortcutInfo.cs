// (C) Copyright 2024 by T27
// Class chứa thông tin lệnh tắt

namespace Civil3DCsharp.HelpSystem
{
    /// <summary>
    /// Class chứa thông tin mapping giữa lệnh gốc và lệnh tắt
    /// </summary>
    public class CommandShortcutInfo
    {
        /// <summary>
        /// Tên lệnh gốc (VD: AT_TongDoDai_Full)
        /// </summary>
        public string OriginalCommand { get; set; }

        /// <summary>
        /// Lệnh tắt do người dùng đặt (VD: TDD)
        /// </summary>
        public string Shortcut { get; set; }

        /// <summary>
        /// Nhóm lệnh (CAD, Civil, etc.)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Mô tả ngắn gọn về lệnh
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Constructor mặc định
        /// </summary>
        public CommandShortcutInfo()
        {
        }

        /// <summary>
        /// Constructor với các thông tin cơ bản
        /// </summary>
        public CommandShortcutInfo(string originalCommand, string shortcut, string category, string description)
        {
            OriginalCommand = originalCommand;
            Shortcut = shortcut;
            Category = category;
            Description = description;
        }

        /// <summary>
        /// Tạo từ CommandInfo
        /// </summary>
        public static CommandShortcutInfo FromCommandInfo(CommandInfo cmd)
        {
            return new CommandShortcutInfo
            {
                OriginalCommand = cmd.Name,
                Shortcut = "",
                Category = cmd.Category,
                Description = cmd.Description
            };
        }
    }
}
