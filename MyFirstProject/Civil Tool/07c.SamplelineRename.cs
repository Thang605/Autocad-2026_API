// Nhóm lệnh: Đổi tên cọc (SampleLine Rename)
// Tách từ 07.Sampleline.cs
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.Civil.DatabaseServices;
using MyFirstProject.Extensions;

[assembly: CommandClass(typeof(Civil3DCsharp.SamplelineRename))]

namespace Civil3DCsharp
{
    public class SamplelineRename
    {
        [CommandMethod("CTS_DoiTenCoc")]
        public static void CTSDoiTenCoc()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput uI = new();

                //start here
                // choose an alignment
                ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường " + "để đổi tên cọc: \n");

                //get alignment for read
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                //get first sampleline group
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId samplelineGroup = alignment.GetSampleLineGroupIds()[0];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? sampleLineGroup = tr.GetObject(samplelineGroup, OpenMode.ForRead) as SampleLineGroup;

                //reset name sampleline
                int i = 0;
                int j = 1000;
                int value;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                foreach (ObjectId sampleLineId in sampleLineGroup.GetSampleLineIds())
                {
                    SampleLine? sampleline = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (int.TryParse(sampleline.Name, out value))
                    {
                        sampleline.Name = Convert.ToString(j);
                        j++;
                    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    i++;
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                //rename sampleline

                j = 1;
                foreach (ObjectId sampleLineId in sampleLineGroup.GetSampleLineIds())
                {
                    SampleLine? sampleline = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (int.TryParse(sampleline.Name, out value))
                    {
                        sampleline.Name = Convert.ToString(j);
                        j++;
                    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    i++;
                }
                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_DoiTenCoc3")]
        public static void CTSDoiTenCoc3()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput uI = new();

                //start here
                // choose an alignment
                ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường " + "để đổi tên cọc: \n");

                //get alignment for read
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                //get first sampleline group
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId samplelineGroup = alignment.GetSampleLineGroupIds()[0];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? sampleLineGroup = tr.GetObject(samplelineGroup, OpenMode.ForRead) as SampleLineGroup;

                //reset name sampleline
                int i = 0;
                int j = 1000;
                int value;
                String lyTrinh;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                foreach (ObjectId sampleLineId in sampleLineGroup.GetSampleLineIds())
                {
                    SampleLine? sampleline = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (int.TryParse(sampleline.Name, out value))
                    {
                        if ((sampleline.Station % 1000) < 100)
                        {
                            lyTrinh = "0" + (sampleline.Station % 1000).ToString();
                        }
                        else lyTrinh = (sampleline.Station % 1000).ToString();
                        sampleline.Name = "Km " + Math.Floor(sampleline.Station / 1000) + "+" + lyTrinh;
                        j++;
                    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    i++;
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                //rename sampleline

                j = 1;
                foreach (ObjectId sampleLineId in sampleLineGroup.GetSampleLineIds())
                {
                    SampleLine? sampleline = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (int.TryParse(sampleline.Name, out value))
                    {
                        sampleline.Name = Convert.ToString(j);
                        j++;
                    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    i++;
                }
                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }


        [CommandMethod("CTS_DoiTenCoc2")]
        public static void CTSDoiTenCoc2()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput uI = new();

                //start here
                // choose an alignment
                ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường " + "để đổi tên cọc: \n");

                //get alignment for read
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                //get first sampleline group
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId samplelineGroup = alignment.GetSampleLineGroupIds()[0];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? sampleLineGroup = tr.GetObject(samplelineGroup, OpenMode.ForRead) as SampleLineGroup;

                //rename sampleline
                ObjectId sampleLineDauId = UserInput.GSampleLineId("Chọn sampleLine điểm đầu đoạn tuyến cần đổi tên:");
                SampleLine? sampleLineDau = tr.GetObject(sampleLineDauId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                int sampleLineNunberDau = sampleLineDau.Number;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                ObjectId sampleLineCuoiId = UserInput.GSampleLineId("Chọn sampleLine điểm đầu đoạn tuyến cần đổi tên:");
                SampleLine? sampleLineCuoi = tr.GetObject(sampleLineCuoiId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                int sampleLineNunberCuoi = sampleLineCuoi.Number;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                String tienTo = UserInput.GString("Nhập tiền tố cho đoạn cọc muốn đổi tên:");
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                int soTT = UserInput.GInt("Nhập số thứ tự của cọc đầu của tên đoạn muốn đổi tên cọc:");
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectIdCollection samplePlineIds = sampleLineGroup.GetSampleLineIds();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                int j = soTT;

                for (int k = sampleLineNunberDau; k < sampleLineNunberCuoi; k++)
                {
                    SampleLine? sampleline = tr.GetObject(samplePlineIds[k], OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (int.TryParse(sampleline.Name, out int value))
                    {
                        sampleline.Name = tienTo + Convert.ToString(j);
                        j++;
                    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }



                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_DoiTenCoc_fromCogoPoint")]
        public static void CTSDoiTenCocFromCogoPoint()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here
                ObjectId cogoPointId = UserInput.GCogoPointId("\n Chọn cogo point " + "để lấy tên cho sampleline:\n");
                CogoPoint? cogoPoint = tr.GetObject(cogoPointId, OpenMode.ForWrite) as CogoPoint;
                ObjectIdCollection sampleLineIds = UserInput.GSelectionSet("Chọn sampleLine cần đổi tên: \n");
                foreach (ObjectId objectId in sampleLineIds)
                {
                    SampleLine? sampleLine = tr.GetObject(objectId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    sampleLine.Name = cogoPoint.PointName;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_DoiTenCoc_TheoThuTu")]
        public static void CTSDoiTenCocTheoThuTu()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here

                ObjectIdCollection sampleLineIds = UserInput.GSelectionSet("\n Chọn sampleLine cần đổi tên: \n");
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                String tienToCoc = UserInput.GString("\n Nhập tiền tố cho tên cọc:");
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                int thuTuCoc = UserInput.GInt("\n Nhập số thứ tự bắt đầu (1):"); ;
                foreach (ObjectId objectId in sampleLineIds)
                {
                    SampleLine? sampleLine = tr.GetObject(objectId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    sampleLine.Name = tienToCoc + thuTuCoc;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    thuTuCoc++;
                }
                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_DoiTenCoc_H")]
        public static void CTSDoiTenCocH()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here

                ObjectId sampleLineId_S = UserInput.GSampleLineId("\n Chọn sampleLine cọc mốc: \n");
                SampleLine? sampleLine_S = tr.GetObject(sampleLineId_S, OpenMode.ForWrite) as SampleLine;
                ObjectId sampleLineId_D = UserInput.GSampleLineId("\n Chọn sampleLine cọc cần đổi tên: \n");
                SampleLine? sampleLine_D = tr.GetObject(sampleLineId_D, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                sampleLine_D.Name = sampleLine_S.Name + "A";
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8602 // Dereference of a possibly null reference.



                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }
    }
}
