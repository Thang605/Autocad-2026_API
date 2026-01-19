// Template: Basic Civil 3D Command
// Sử dụng template này làm khởi đầu cho các lệnh Civil 3D mới

using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using Autodesk.Civil.DatabaseServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

// Đăng ký class chứa commands
[assembly: CommandClass(typeof(Civil3DCsharp.Template_BasicCommand))]

namespace Civil3DCsharp
{
    public class Template_BasicCommand
    {
        /// <summary>
        /// Mô tả ngắn về chức năng của lệnh
        /// </summary>
        [CommandMethod("CT_TEN_LENH")]
        public static void CTTenLenh()
        {
            // Bắt đầu transaction
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                // 1. Khởi tạo các helper (nếu cần)
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();

                // 2. Lấy input từ người dùng
                ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường:");
                
                // 3. Mở đối tượng để đọc/ghi
                // QUAN TRỌNG: Luôn dùng ForWrite để tránh crash AutoCAD
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
                
                if (alignment == null)
                {
                    A.Ed.WriteMessage("\n Không thể đọc đối tượng Alignment.");
                    return;
                }

                // 4. Xử lý logic chính
                // TODO: Thêm logic xử lý ở đây
                A.Ed.WriteMessage($"\n Đã chọn tim đường: {alignment.Name}");
                A.Ed.WriteMessage($"\n Chiều dài: {alignment.Length:F2}m");

                // 5. Commit transaction để lưu thay đổi
                tr.Commit();
                
                A.Ed.WriteMessage("\n Hoàn thành lệnh.");
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\n Lỗi: {e.Message}");
            }
        }
    }
}
