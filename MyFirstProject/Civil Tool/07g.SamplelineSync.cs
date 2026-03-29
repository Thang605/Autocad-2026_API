// Nhóm lệnh: Copy / Đồng bộ nhóm cọc (SampleLine Sync)
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

[assembly: CommandClass(typeof(Civil3DCsharp.SamplelineSync))]

namespace Civil3DCsharp
{
    public class SamplelineSync
    {
        [CommandMethod("CTS_Copy_NhomCoc")]
        public static void CTSCopyNhomCoc()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here

                ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường có nhóm cọc cần sao chép:");
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                String tenSamplelineGroup = UserInput.GString("\n Nhập tên nhóm cọc cần tạo:");
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                //tạo nhóm cọc mới
                ObjectId sampleLineGroupId = SampleLineGroup.Create(tenSamplelineGroup, alignmentId);

                // lấy thông số từ cọc cũ
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLineGroupId_0 = alignment.GetSampleLineGroupIds()[0];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? sampleLineGroup_0 = tr.GetObject(sampleLineGroupId_0, OpenMode.ForWrite) as SampleLineGroup;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectIdCollection sampleLineIds = sampleLineGroup_0.GetSampleLineIds();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                List<double> stationList = [];
                List<String> sampleLineNameS = [];
                foreach (ObjectId sampleLineId in sampleLineIds)
                {
                    SampleLine? sampleLine = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    stationList.Add(sampleLine.Station);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    sampleLineNameS.Add(sampleLine.Name);
                }

                //sampleLineGroup_0.Erase();
                //tạo cọc vào nhóm cọc mới
                for (int i = 0; i < stationList.Count(); i++)
                {
                    A.Ed.WriteMessage(sampleLineNameS[i].ToString() + "-" + stationList[i].ToString());
                    ObjectId sampleLineId = UtilitiesC3D.CreateSampleline(sampleLineNameS[i] + "C", sampleLineGroupId, alignment, stationList[i]);
                    SampleLine? sampleLine = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    sampleLine.Name = sampleLineNameS[i];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }




                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_DongBo_2_NhomCoc")]
        public static void CTSDongBo2NhomCoc()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here

                // lấy thông số từ cọc cũ
                ObjectId sampleLineId_0 = UserInput.GSampleLineId("\n Chọn nhóm cọc NGUỒN để copy: ");
                SampleLine? sampleLine_0 = tr.GetObject(sampleLineId_0, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLineGroupId = sampleLine_0.GroupId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectIdCollection sampleLineIds = sampleLineGroup.GetSampleLineIds();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                int sampleLIneNumber = sampleLineIds.Count;
                List<double> stationList = [];
                List<String> sampleLineNameS = [];
                foreach (ObjectId sampleLineId in sampleLineIds)
                {
                    SampleLine? sampleLine = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    stationList.Add(sampleLine.Station);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    //a.ok(sampleLine.Station.ToString());
                    sampleLineNameS.Add(sampleLine.Name);
                }

                // lấy thông số từ cọc mới
                ObjectId sampleLineId_1 = UserInput.GSampleLineId("\n Chọn nhóm cọc ĐÍCH để copy: ");
                //double lyTrinhBatDau = UI.G_Double(" NHập lý bắt đầu của tuyến: ");
                SampleLine? sampleLine_1 = tr.GetObject(sampleLineId_1, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLineGroupId_1 = sampleLine_1.GroupId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? sampleLineGroup_1 = tr.GetObject(sampleLineGroupId_1, OpenMode.ForWrite) as SampleLineGroup;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectIdCollection sampleLineIds_1 = sampleLineGroup_1.GetSampleLineIds();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                int sampleLIneNumber_1 = sampleLineIds_1.Count;

                //đồng bộ thông tin
                if (sampleLIneNumber_1 <= sampleLIneNumber)
                {
                    for (int i = 0; i < sampleLIneNumber_1; i++)
                    {
                        SampleLine? sampleLine = tr.GetObject(sampleLineIds[i], OpenMode.ForWrite) as SampleLine;
                        SampleLine? sampleLine_c = tr.GetObject(sampleLineIds_1[i], OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        sampleLine_c.Station = sampleLine.Station;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        sampleLine_c.Name = sampleLine.Name + "c";
                    }
                }
                else A.Ok("Số cọc đích nhiều hơn cọc nguồn. Cần xóa bớt cọc đích");


                //rename sampleline
                char[] charsToTrim = ['z', ' ', '\'', 'c'];
                foreach (ObjectId sampleLineId in sampleLineGroup_1.GetSampleLineIds())
                {
                    SampleLine? sampleline = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    sampleline.Name = sampleline.Name.Trim(charsToTrim);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                }


                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_DongBo_2_NhomCoc_TheoDoan")]
        public static void CTSDongBo2NhomCocTheoDoan()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here

                // lấy thông số từ cọc cũ
                ObjectId sampleLineId_0 = UserInput.GSampleLineId("\n Chọn cọc ĐẦU TIÊN của đoạn thuộc NGUỒN để copy: ");
                SampleLine? sampleLine_0 = tr.GetObject(sampleLineId_0, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLineGroupId = sampleLine_0.GroupId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectIdCollection sampleLineIds = sampleLineGroup.GetSampleLineIds();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                int sampleLIneNumber = sampleLineIds.Count;
                int sampleLIneNumberBegin = sampleLine_0.Number;

                List<double> stationList = [];
                List<String> sampleLineNameS = [];

                for (int i = sampleLIneNumberBegin; i < sampleLineIds.Count; i++)
                {
                    ObjectId sampleLineId = sampleLineIds[i];
                    SampleLine? sampleLine = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    stationList.Add(sampleLine.Station);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    //a.ok(sampleLine.Station.ToString());
                    sampleLineNameS.Add(sampleLine.Name);
                }
                /*
                foreach (ObjectId sampleLineId in sampleLineIds)
                {
                    SampleLine sampleLine = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
                    stationList.Add(sampleLine.Station);
                    //a.ok(sampleLine.Station.ToString());
                    sampleLineNameS.Add(sampleLine.Name);
                }
                */

                // lấy thông số từ cọc mới
                ObjectId sampleLineId_1 = UserInput.GSampleLineId("\n Chọn nhóm cọc ĐÍCH để copy: ");
                //double lyTrinhBatDau = UI.G_Double(" NHập lý bắt đầu của tuyến: ");
                SampleLine? sampleLine_1 = tr.GetObject(sampleLineId_1, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLineGroupId_1 = sampleLine_1.GroupId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? sampleLineGroup_1 = tr.GetObject(sampleLineGroupId_1, OpenMode.ForWrite) as SampleLineGroup;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectIdCollection sampleLineIds_1 = sampleLineGroup_1.GetSampleLineIds();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                int sampleLIneNumber_1 = sampleLineIds_1.Count;

                //đồng bộ thông tin
                if (sampleLIneNumber_1 <= sampleLIneNumber)
                {
                    for (int i = 0; i < sampleLIneNumber_1; i++)
                    {
                        SampleLine? sampleLine = tr.GetObject(sampleLineIds[i + sampleLIneNumberBegin - 1], OpenMode.ForWrite) as SampleLine;
                        SampleLine? sampleLine_c = tr.GetObject(sampleLineIds_1[i], OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        sampleLine_c.Station = sampleLine.Station;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        sampleLine_c.Name = sampleLine.Name + "c";
                    }
                }
                else A.Ok("Số cọc đích nhiều hơn cọc nguồn. Cần xóa bớt cọc đích");


                //rename sampleline
                char[] charsToTrim = ['z', ' ', '\'', 'c'];
                foreach (ObjectId sampleLineId in sampleLineGroup_1.GetSampleLineIds())
                {
                    SampleLine? sampleline = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    sampleline.Name = sampleline.Name.Trim(charsToTrim);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

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
