// Template: Civil 3D Command với vòng lặp
// Sử dụng template này khi cần user thực hiện nhiều lần liên tiếp

using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using Autodesk.Civil.DatabaseServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(Civil3DCsharp.Template_LoopCommand))]

namespace Civil3DCsharp
{
    public class Template_LoopCommand
    {
        /// <summary>
        /// Lệnh mẫu cho phép user thực hiện nhiều lần
        /// Mỗi lần là một transaction riêng biệt
        /// </summary>
        [CommandMethod("CT_TEN_LENH_LOOP")]
        public static void CTTenLenhLoop()
        {
            UserInput UI = new();
            UtilitiesC3D C3D = new();

            // 1. Chọn đối tượng base TRƯỚC vòng lặp (ngoài transaction)
            ObjectId profileViewId = UserInput.GProfileViewId("\n Chọn trắc dọc:");

            // 2. Vòng lặp cho phép thực hiện nhiều lần
            string answer = "y";
            int count = 0;

            while (answer == "y" || answer == "Y")
            {
                // Mỗi lần lặp là một transaction riêng
                using (Transaction tr = A.Db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // 3. Mở lại đối tượng trong transaction mới
                        // QUAN TRỌNG: Luôn dùng ForWrite để tránh crash AutoCAD
                        ProfileView? profileView = tr.GetObject(profileViewId, OpenMode.ForWrite) as ProfileView;
                        
                        if (profileView == null)
                        {
                            A.Ed.WriteMessage("\n Không thể đọc ProfileView.");
                            break;
                        }

                        // 4. Lấy input chi tiết trong mỗi vòng lặp
                        Point3d point = UserInput.GPoint("\n Chọn vị trí:");
                        
                        double station = 0, elevation = 0;
                        profileView.FindStationAndElevationAtXY(point.X, point.Y, ref station, ref elevation);

                        // 5. Xử lý logic
                        // TODO: Thêm logic xử lý ở đây
                        A.Ed.WriteMessage($"\n Station: {station:F2}, Elevation: {elevation:F2}");

                        count++;
                        tr.Commit();
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception e)
                    {
                        A.Ed.WriteMessage($"\n Lỗi: {e.Message}");
                    }
                }

                // 6. Hỏi tiếp tục (NGOÀI transaction)
                answer = UserInput.GString("\n Tiếp tục? (y/n)") ?? "n";
            }

            A.Ed.WriteMessage($"\n Đã thực hiện {count} lần.");
        }

        /// <summary>
        /// Lệnh mẫu với vòng lặp dừng bằng ESC
        /// </summary>
        [CommandMethod("CT_TEN_LENH_ESC")]
        public static void CTTenLenhEsc()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                // 1. Chọn đối tượng base
                ObjectId sectionViewId = UserInput.GSectionView("\n Chọn trắc ngang:");
                // QUAN TRỌNG: Luôn dùng ForWrite để tránh crash AutoCAD
                SectionView? sectionView = tr.GetObject(sectionViewId, OpenMode.ForWrite) as SectionView;

                if (sectionView == null)
                {
                    A.Ed.WriteMessage("\n Không thể đọc SectionView.");
                    return;
                }

                // 2. Vòng lặp dừng bằng ESC
                string continueLoop = "Enter";
                int count = 0;

                while (continueLoop == "Enter")
                {
                    // 3. Lấy input
                    Point3d point = UserInput.GPoint("\n Chọn điểm (ESC để dừng):");

                    double offset = 0, elevation = 0;
                    sectionView.FindOffsetAndElevationAtXY(point.X, point.Y, ref offset, ref elevation);

                    // 4. Xử lý
                    A.Ed.WriteMessage($"\n Offset: {offset:F2}, Elevation: {elevation:F2}");
                    count++;

                    // 5. Kiểm tra ESC
                    continueLoop = UserInput.GStopWithESC();
                }

                tr.Commit();
                A.Ed.WriteMessage($"\n Đã xử lý {count} điểm.");
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\n Lỗi: {e.Message}");
            }
        }
    }
}
