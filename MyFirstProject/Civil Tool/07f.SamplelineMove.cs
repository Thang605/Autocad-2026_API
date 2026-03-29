// Nhóm lệnh: Dịch cọc tịnh tiến (SampleLine Move)
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

[assembly: CommandClass(typeof(Civil3DCsharp.SamplelineMove))]

namespace Civil3DCsharp
{
    public class SamplelineMove
    {
        [CommandMethod("CTS_DichCoc_TinhTien")]
        public static void CTSDichCocTinhTien()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here

                ObjectIdCollection sampleLineIds = UserInput.GSelectionSet("\n Chọn sampleLine cần dịch cọc: \n");

                // Cho phép nhập khoảng dịch, chọn 2 điểm, hoặc chọn 2 sampleline
                double delta = 0;
                PromptDoubleOptions pdo = new("\n Nhập khoảng dịch cọc hoặc [Chọn 2 điểm/P] [Chọn 2 sampleLine/S]:");
                pdo.Keywords.Add("P");
                pdo.Keywords.Add("S");
                pdo.AllowNegative = true;
                pdo.AllowZero = true;

                PromptDoubleResult pdr = A.Ed.GetDouble(pdo);

                if (pdr.Status == PromptStatus.Keyword && pdr.StringResult == "P")
                {
                    // Cách 2: Lấy alignment từ sampleLine đầu tiên để tính station
                    if (sampleLineIds.Count == 0)
                    {
                        A.Ed.WriteMessage("\n Không có sampleLine nào được chọn.");
                        return;
                    }

                    SampleLine? firstSampleLine = tr.GetObject(sampleLineIds[0], OpenMode.ForRead) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    ObjectId alignmentId = firstSampleLine.GetParentAlignmentId();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;

                    // Chọn 2 điểm và tính station trên tuyến
                    Point3d point1 = UserInput.GPoint("\n Chọn điểm thứ nhất trên tuyến:");
                    Point3d point2 = UserInput.GPoint("\n Chọn điểm thứ hai trên tuyến:");

                    double station1 = 0, offset1 = 0;
                    double station2 = 0, offset2 = 0;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    alignment.StationOffset(point1.X, point1.Y, ref station1, ref offset1);
                    alignment.StationOffset(point2.X, point2.Y, ref station2, ref offset2);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                    delta = station2 - station1;
                    A.Ed.WriteMessage($"\n Station điểm 1: {station1:F3}, Station điểm 2: {station2:F3}");
                    A.Ed.WriteMessage($"\n Khoảng dịch tính được: {delta:F3}");
                }
                else if (pdr.Status == PromptStatus.Keyword && pdr.StringResult == "S")
                {
                    // Cách 3: Chọn 2 sampleline và nhập khoảng cách yêu cầu
                    ObjectId sampleLineId1 = UserInput.GSampleLineId("\n Chọn sampleLine thứ nhất (cọc gốc):");
                    ObjectId sampleLineId2 = UserInput.GSampleLineId("\n Chọn sampleLine thứ hai (cọc cần kiểm tra):");

                    SampleLine? sl1 = tr.GetObject(sampleLineId1, OpenMode.ForRead) as SampleLine;
                    SampleLine? sl2 = tr.GetObject(sampleLineId2, OpenMode.ForRead) as SampleLine;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    double stationSL1 = sl1.Station;
                    double stationSL2 = sl2.Station;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                    double khoangCachHienTai = Math.Abs(stationSL2 - stationSL1);
                    A.Ed.WriteMessage($"\n Khoảng cách hiện tại giữa 2 sampleLine: {khoangCachHienTai:F3}");

                    double khoangCachYeuCau = UserInput.GDouble("\n Nhập khoảng cách yêu cầu giữa 2 sampleLine:");

                    // Tính khoảng dịch = khoảng cách yêu cầu - khoảng cách hiện tại
                    delta = khoangCachYeuCau - khoangCachHienTai;
                    // Xác định hướng dịch: nếu SL2 > SL1 thì dịch về phía cuối tuyến (dương)
                    // delta > 0: dịch ra xa (khoảng cách hiện tại < yêu cầu)
                    // delta < 0: dịch lại gần (khoảng cách hiện tại > yêu cầu)
                    if (stationSL2 < stationSL1)
                    {
                        delta = -delta; // Đảo hướng nếu SL2 nằm trước SL1
                    }
                    A.Ed.WriteMessage($"\n Khoảng dịch tính được: {delta:F3}");
                }
                else if (pdr.Status == PromptStatus.OK)
                {
                    delta = pdr.Value;
                }
                else
                {
                    A.Ed.WriteMessage("\n Đã hủy lệnh.");
                    return;
                }

                foreach (ObjectId objectId in sampleLineIds)
                {
                    SampleLine? sampleLine = tr.GetObject(objectId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    sampleLine.Station += delta;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }


                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_DichCoc_TinhTien40")]
        public static void CTSDichCocTinhTien40()
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
                ObjectId sampleLineId_D = UserInput.GSampleLineId("\n Chọn sampleLine cọc cần dịch: \n");
                SampleLine? sampleLine_D = tr.GetObject(sampleLineId_D, OpenMode.ForWrite) as SampleLine;
                Double khoangDich = 40;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                sampleLine_D.Station = sampleLine_S.Station + khoangDich;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8602 // Dereference of a possibly null reference.



                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_DichCoc_TinhTien_20")]
        public static void CTSDichCocTinhTien20()
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
                ObjectId sampleLineId_D = UserInput.GSampleLineId("\n Chọn sampleLine cọc cần dịch: \n");
                SampleLine? sampleLine_D = tr.GetObject(sampleLineId_D, OpenMode.ForWrite) as SampleLine;
                Double khoangDich = 20;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                sampleLine_D.Station = sampleLine_S.Station + khoangDich;
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
