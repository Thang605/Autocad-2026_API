// Nhóm lệnh: Thay đổi hiển thị cắt ngang (MSS, giới hạn, khung in)
// Tách từ 08.Sectionview.cs
//
using System;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.Civil.DatabaseServices;

using MyFirstProject.Extensions;

[assembly: CommandClass(typeof(Civil3DCsharp.SectionViewsDisplay))]

namespace Civil3DCsharp
{
    public class SectionViewsDisplay
    {

        [CommandMethod("CTSV_ThayDoi_MSS_Min_Max")]
        public static void CTSVThayDoiMSSMinMax()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                ObjectIdCollection objectIdCollection = UserInput.GSelectionSet("Chọn các cắt ngang cần thay đổi MSS:");
                Double X = 0;
                Double elevationMin = 0;
                Double elevationMax = 0;
                Point3d point3D_min = UserInput.GPoint("\n Chọn MSS min:");
                Point3d point3D_max = UserInput.GPoint("\n Chọn MSS max:");
                SectionView? sectionView0 = tr.GetObject(objectIdCollection[0], OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602
                sectionView0.FindOffsetAndElevationAtXY(point3D_min.X, point3D_min.Y, ref X, ref elevationMin);
#pragma warning restore CS8602
                sectionView0.FindOffsetAndElevationAtXY(point3D_max.X, point3D_max.Y, ref X, ref elevationMax);
                foreach (ObjectId objectId in objectIdCollection)
                {
                    if (objectId.ObjectClass.DxfName == "AECC_GRAPH_SECTION_VIEW")
                    {
                        SectionView? sectionView = tr.GetObject(objectId, OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602
                        sectionView.IsElevationRangeAutomatic = false;
#pragma warning restore CS8602
                        sectionView.ElevationMax = elevationMax;
                        sectionView.ElevationMin = elevationMin;
                    }
                }
                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTSV_ThayDoi_GioiHan_traiPhai")]
        public static void CTSVThayDoiGioiHanTraiPhai()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                ObjectIdCollection objectIdCollection = UserInput.GSelectionSet("Chọn các cắt ngang cần thay đổi bề rộng trái phải:");
                Double X = 0;
                Double OffsetLeft = 0;
                Double OffsetRight = 0;
                Point3d point3D_left = UserInput.GPoint("\n Chọn điểm giới hạn bên trái:");
                Point3d point3D_right = UserInput.GPoint("\n Chọn điểm giới hạn bên phải:");
                SectionView? sectionView0 = tr.GetObject(objectIdCollection[0], OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602
                sectionView0.FindOffsetAndElevationAtXY(point3D_left.X, point3D_left.Y, ref OffsetLeft, ref X);
#pragma warning restore CS8602
                sectionView0.FindOffsetAndElevationAtXY(point3D_right.X, point3D_right.Y, ref OffsetRight, ref X);
                foreach (ObjectId objectId in objectIdCollection)
                {
                    if (objectId.ObjectClass.DxfName == "AECC_GRAPH_SECTION_VIEW")
                    {
                        SectionView? sectionView = tr.GetObject(objectId, OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602
                        sectionView.IsOffsetRangeAutomatic = false;
#pragma warning restore CS8602
                        sectionView.OffsetLeft = OffsetLeft;
                        sectionView.OffsetRight = OffsetRight;
                    }
                }
                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }


        [CommandMethod("CTSV_ThayDoi_KhungIn")]
        public static void CTSVThayDoiKhungIn()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                ObjectIdCollection objectIdCollection = UserInput.GSelectionSetWithType("Chọn các cắt ngang cần thay đổi khung in \n (Chọn cắt ngang đầu, sau đó chọn các cắt ngang còn lại):", "AECC_GRAPH_SECTION_VIEW");
                Double X = 0;
                Double OffsetLeft = 0;
                Double OffsetRight = 0;
                Point3d point3D_left = UserInput.GPoint("\n Chọn điểm giới hạn bên trái dưới:");
                Point3d point3D_right = UserInput.GPoint("\n Chọn điểm giới hạn bên phải trên:");
                SectionView? sectionView0 = tr.GetObject(objectIdCollection[0], OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602
                sectionView0.FindOffsetAndElevationAtXY(point3D_left.X, point3D_left.Y, ref OffsetLeft, ref X);
#pragma warning restore CS8602
                sectionView0.FindOffsetAndElevationAtXY(point3D_right.X, point3D_right.Y, ref OffsetRight, ref X);
                foreach (ObjectId objectId in objectIdCollection)
                {
                    if (objectId.ObjectClass.DxfName == "AECC_GRAPH_SECTION_VIEW")
                    {
                        SectionView? sectionView = tr.GetObject(objectId, OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602
                        sectionView.IsOffsetRangeAutomatic = false;
#pragma warning restore CS8602
                        sectionView.OffsetLeft = OffsetLeft;
                        sectionView.OffsetRight = OffsetRight;
                    }
                }
                Double elevationMin = 0;
                Double elevationMax = 0;
                sectionView0.FindOffsetAndElevationAtXY(point3D_left.X, point3D_left.Y, ref X, ref elevationMin);
                sectionView0.FindOffsetAndElevationAtXY(point3D_right.X, point3D_right.Y, ref X, ref elevationMax);
                foreach (ObjectId objectId in objectIdCollection)
                {
                    if (objectId.ObjectClass.DxfName == "AECC_GRAPH_SECTION_VIEW")
                    {
                        SectionView? sectionView = tr.GetObject(objectId, OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602
                        sectionView.IsElevationRangeAutomatic = false;
#pragma warning restore CS8602
                        sectionView.ElevationMax = elevationMax;
                        sectionView.ElevationMin = elevationMin;
                    }
                }
                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }
    }
}
