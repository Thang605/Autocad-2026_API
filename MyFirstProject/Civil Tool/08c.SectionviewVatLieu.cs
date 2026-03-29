// Nhóm lệnh: Thêm vật liệu trên cắt ngang
// Tách từ 08.Sectionview.cs
//
using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.Civil.DatabaseServices;

using MyFirstProject.Extensions;

[assembly: CommandClass(typeof(Civil3DCsharp.SectionViewsVatLieu))]

namespace Civil3DCsharp
{
    public class SectionViewsVatLieu
    {

        [CommandMethod("CTSV_ThemVatLieu_TrenCatNgang")]
        public static void CTSVThemVatLieuTrenCatNgang()
        {
            // start transantion CTSV_DanhCap
            using Transaction tr = A.Db.TransactionManager.StartTransaction();

            UserInput UI = new();
            UtilitiesCAD CAD = new();
            UtilitiesC3D C3D = new();

            SectionViewGroupCreationPlacementOptions sectionViewGroupCreationPlacementOptions = new();
            sectionViewGroupCreationPlacementOptions.UseProductionPlacement("Z:/Z.FORM MAU LAM VIEC/1. BIM/2.MAU C3D/2.THU VIEN C3D/2.LAYOUT C3D/LAYOUT CIVIL 3D.dwt", "A3-TN-1-200");

            //start here
            ObjectId sectionViewId = UserInput.GSectionView("\n Chọn 1 bảng cắt ngang để thêm text khối lượng: ");
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            String tenVatLieu = UserInput.GString("\n Nhập tên vật liệu cần gắn lên trắc ngang:");
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            tenVatLieu = tenVatLieu.Replace(":", "");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            String donViVatLieu = UserInput.GString("\n Nhập tên đơn vị của vật liệu (m or m2):");
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            SectionView? sectionView = tr.GetObject(sectionViewId, OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            ObjectId sampleLineId = sectionView.SampleLineId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            SampleLine? sampleLine = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            ObjectId sampleLineGroupId = sampleLine.GroupId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            SampleLineGroup? sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;

            //find offset, elevation
            Point3d point3D = UserInput.GPoint("\n Chọn vị trí điểm cần đặt TEXT vật liệu trên trắc ngang: \n ");
            double offset = 1;
            double elevation = 2;
            sectionView.FindOffsetAndElevationAtXY(point3D.X, point3D.Y, ref offset, ref elevation);
            Double deltaX = offset;
            double deltaY = elevation;
            double deltaX2 = tenVatLieu.Length * 0.35;

            //số cột trong bảng đánh cấp
            List<String> listLyTrinh = [];
            List<String> listTenCoc = [];
            List<String> listKhoiLuong = [];
            List<Point3d> listTextPosition = [];
            List<Point3d> listTextPosition2 = [];

            ObjectId alignmentId = sampleLine.GetParentAlignmentId();
            Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            String alignmentName = alignment.Name + "_" + tenVatLieu;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            LayerTableRecord layer = UtilitiesCAD.CCreateLayer(alignmentName);

            // get sectionViewIdColl
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            SectionViewGroupCollection sectionViewGroupCollection = sampleLineGroup.SectionViewGroups;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            SectionViewGroup sectionViewGroup = sectionViewGroupCollection[0];
            if (sectionViewGroupCollection.Count > 1)
            {
                int num = 0;
                A.Ed.WriteMessage("\n Danh sách nhóm cắt ngang:");
                foreach (var item in sectionViewGroupCollection)
                {
                    A.Ed.WriteMessage(num.ToString() + " " + sectionViewGroupCollection[num].Name.ToString());
                    num++;
                }
                int numPass = UserInput.GInt("\n Đường có nhiều hơn 1 nhóm cắt ngang! \n Nhập thứ tự nhóm cắt ngang cần tính đánh cấp:");
                sectionViewGroup = sectionViewGroupCollection[numPass];
            }

            ObjectIdCollection sectionViewIdColl = sectionViewGroup.GetSectionViewIds();

            // add section Tn và Datum
            foreach (ObjectId sectionviewId in sectionViewIdColl)
            {
                SectionView? sectionView1 = tr.GetObject(sectionviewId, OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLine1Id = sectionView1.SampleLineId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLine? sampleLine1 = tr.GetObject(sampleLine1Id, OpenMode.ForWrite) as SampleLine;

                //get sectionview location
                double X = sectionView1.Location.X;
                double Y = sectionView1.Location.Y;

                //ghi text đánh cấp                        
                Point3d textPosition = new(X + deltaX, Y + deltaY, 0);
                Point3d textPosition2 = new(X + deltaX + deltaX2, Y + deltaY, 0);

                //đưa data vào mảng của table
                listTextPosition.Add(textPosition);
                listTextPosition2.Add(textPosition2);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                listLyTrinh.Add(sampleLine1.Station.ToString("N2"));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                listTenCoc.Add(sampleLine1.Name.ToString());
            }


            tenVatLieu += ":";
            for (int i = 0; i < listLyTrinh.Count; i++)
            {
                UtilitiesCAD.CCreateTextWithOutPut(listTextPosition[i], 0.4, tenVatLieu, alignmentName, "Standard");
                DBText Text = UtilitiesCAD.CCreateText2(listTextPosition2[i], 0.4, "0.00 " + donViVatLieu, alignmentName, "Standard");
                String textFile = UtilitiesCAD.ConvertTextToField(Text);
                listKhoiLuong.Add(textFile);

            }
            UtilitiesCAD.CreateTableKhoiLuong(listLyTrinh.Count, 3, alignment.Name, listLyTrinh, listTenCoc, listKhoiLuong, alignmentName, tenVatLieu);

            tr.Commit();
        }

    }
}
