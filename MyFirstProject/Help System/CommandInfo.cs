// (C) Copyright 2024 by T27
// Model class chứa thông tin của mỗi lệnh

namespace Civil3DCsharp.HelpSystem
{
    /// <summary>
    /// Class chứa thông tin chi tiết của một lệnh AutoCAD/Civil3D
    /// </summary>
    public class CommandInfo
    {
        /// <summary>
        /// Tên lệnh (ví dụ: AT_DoDoc)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Nhóm lệnh (CAD, Corridor, SectionView, etc.)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Mô tả ngắn gọn về chức năng của lệnh
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Cú pháp sử dụng lệnh
        /// </summary>
        public string Usage { get; set; }

        /// <summary>
        /// Các bước thực hiện chi tiết
        /// </summary>
        public string[] Steps { get; set; }

        /// <summary>
        /// Ví dụ sử dụng
        /// </summary>
        public string[] Examples { get; set; }

        /// <summary>
        /// Ghi chú, lưu ý khi sử dụng
        /// </summary>
        public string[] Notes { get; set; }

        /// <summary>
        /// Link video hướng dẫn trên YouTube
        /// </summary>
        public string VideoLink { get; set; }

        /// <summary>
        /// Constructor mặc định
        /// </summary>
        public CommandInfo()
        {
            Steps = new string[0];
            Examples = new string[0];
            Notes = new string[0];
        }

        /// <summary>
        /// Constructor với các thông tin cơ bản
        /// </summary>
        public CommandInfo(string name, string category, string description)
        {
            Name = name;
            Category = category;
            Description = description;
            Steps = new string[0];
            Examples = new string[0];
            Notes = new string[0];
        }

        /// <summary>
        /// Constructor đầy đủ
        /// </summary>
        public CommandInfo(string name, string category, string description, string usage, string[] steps, string[] examples, string[] notes)
        {
            Name = name;
            Category = category;
            Description = description;
            Usage = usage;
            Steps = steps ?? new string[0];
            Examples = examples ?? new string[0];
            Notes = notes ?? new string[0];
        }
    }
}
