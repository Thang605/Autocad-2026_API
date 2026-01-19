// Template: Civil 3D Command với Windows Form
// Sử dụng template này khi cần form nhập liệu từ người dùng

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using Autodesk.Civil.DatabaseServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(Civil3DCsharp.Template_FormCommand))]

namespace Civil3DCsharp
{
    public class Template_FormCommand
    {
        /// <summary>
        /// Lệnh mẫu có sử dụng Windows Form
        /// </summary>
        [CommandMethod("CT_TEN_LENH_FORM")]
        public static void CTTenLenhForm()
        {
            // 1. Hiển thị form TRƯỚC khi bắt đầu transaction
            var form = new MyFirstProject.Civil_Tool.TenForm();
            var result = Application.ShowModalDialog(form);

            // 2. Kiểm tra user đã OK hay Cancel
            if (result != DialogResult.OK || !form.FormAccepted)
            {
                A.Ed.WriteMessage("\n Đã hủy lệnh.");
                return;
            }

            // 3. Lấy các giá trị từ form
            string tenTuyen = form.TenTuyen;
            int khoangCach = form.KhoangCach;
            bool optionA = form.OptionA;

            // 4. Bắt đầu transaction với các giá trị từ form
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesC3D C3D = new();

                // 5. Tiếp tục lấy input từ CAD
                ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường:");
                // QUAN TRỌNG: Luôn dùng ForWrite để tránh crash AutoCAD
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;

                if (alignment == null)
                {
                    A.Ed.WriteMessage("\n Không thể đọc đối tượng.");
                    return;
                }

                // 6. Xử lý với các giá trị từ form
                A.Ed.WriteMessage($"\n Tên tuyến: {tenTuyen}");
                A.Ed.WriteMessage($"\n Khoảng cách: {khoangCach}m");
                A.Ed.WriteMessage($"\n Option A: {(optionA ? "Có" : "Không")}");

                // TODO: Thêm logic xử lý ở đây

                tr.Commit();
                A.Ed.WriteMessage("\n Hoàn thành.");
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\n Lỗi: {e.Message}");
            }
        }
    }
}
