// Nhóm lệnh: Đánh cấp trắc ngang
// Tách từ 08.Sectionview.cs
//
using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Section = Autodesk.Civil.DatabaseServices.Section;

using MyFirstProject.Extensions;

[assembly: CommandClass(typeof(Civil3DCsharp.SectionViewsDanhCap))]

namespace Civil3DCsharp
{
    public class SectionViewsDanhCap
    {

        [CommandMethod("CTSV_DanhCap")]
        public static void CTSVDanhCap()
        {
            // start transantion CTSV_DanhCap
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();

                //start here
                ObjectId sectionViewId = UserInput.GSectionView("Chọn 1 bảng cắt ngang " + " trong nhóm cần tính đánh cấp: \n");
                SectionView? sectionView = tr.GetObject(sectionViewId, OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLineId = sectionView.SampleLineId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLine? sampleLine = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLineGroupId = sampleLine.GroupId; ;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;

                // vị trí text đánh cấp
                Double deltaX = 3;
                double deltaY = 7; //UI.G_Double("Nhập vị trí đặt text đánh cấp: (7) \n");
                double deltaX2 = 4; // UI.G_Double("Nhập khoảng giữa tên vật liệu và khối lượng: (4)");

                //số cột trong bảng đánh cấp
                List<String> listLyTrinh = [];
                List<String> listTenCoc = [];
                List<String> listDanhCap = [];

                ObjectId alignmentId = sampleLine.GetParentAlignmentId();
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                String alignmentName = alignment.Name + "_danhCap";
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                LayerTableRecord layer = UtilitiesCAD.CCreateLayer(alignmentName);

                //lấy sectionsource TN và DATUM
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                SectionSourceCollection sectionSources = sampleLineGroup.GetSectionSources();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                ObjectId sectionSource_TN_Id = new();
                foreach (SectionSource sectionsource in sectionSources)
                {
                    if ((sectionsource.SourceType == SectionSourceType.TinSurface) & (sectionsource.IsSampled == true))
                    {
                        TinSurface? type = tr.GetObject(sectionsource.SourceId, OpenMode.ForRead) as TinSurface;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        if (type.Name.Contains("TN", StringComparison.CurrentCultureIgnoreCase))
                        {
                            sectionSource_TN_Id = sectionsource.SourceId;
                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                }
                ObjectId sectionSource_datum_Id = new();
                foreach (SectionSource sectionsource in sectionSources)
                {
                    if ((sectionsource.SourceType == SectionSourceType.CorridorSurface) & (sectionsource.IsSampled == true))
                    {
                        TinSurface? type = tr.GetObject(sectionsource.SourceId, OpenMode.ForRead) as TinSurface;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        if (type.Name.Contains("DATUM", StringComparison.CurrentCultureIgnoreCase))
                        {
                            sectionSource_datum_Id = sectionsource.SourceId;
                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                }


                // get sectionViewIdColl
                //SectionViewGroup sectionViewGroup = sectionView.SectionViewGroupObject();

                SectionViewGroupCollection sectionViewGroupCollection = sampleLineGroup.SectionViewGroups;
                SectionViewGroup sectionViewGroup = sectionViewGroupCollection[0];
                if (sectionViewGroupCollection.Count > 1)
                {
                    int num = 0;
                    A.Ed.WriteMessage("Danh sách nhóm cắt ngang:\n");
                    foreach (var item in sectionViewGroupCollection)
                    {
                        A.Ed.WriteMessage(num.ToString() + " " + sectionViewGroupCollection[num].Name.ToString() + "\n");
                        num++;
                    }
                    int numPass = UserInput.GInt("Nhập thứ tự nhóm cắt ngang cần tính đánh cấp:");
                    sectionViewGroup = sectionViewGroupCollection[numPass];
                }


                ObjectIdCollection sectionViewIdColl = sectionViewGroup.GetSectionViewIds();
                double bacCap = 2; // UI.G_Double("\n Nhập bề rộng đánh cấp (2m):");
                double docDanhCap = 0.2; // UI.G_Double("\n Nhập điều kiện đánh cấp (0.2):");

                //vẽ đánh cấp
                // add section Tn và Datum
                foreach (ObjectId sectionviewId in sectionViewIdColl)
                {
                    SectionView? sectionView1 = tr.GetObject(sectionviewId, OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    ObjectId sampleLine1Id = sectionView1.SampleLineId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    SampleLine? sampleLine1 = tr.GetObject(sampleLine1Id, OpenMode.ForWrite) as SampleLine;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    ObjectId sectionTnId = sampleLine1.GetSectionId(sectionSource_TN_Id);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    Section? section = tr.GetObject(sectionTnId, OpenMode.ForWrite) as Section;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    SectionPointCollection sectionPoints = section.SectionPoints;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                    //get sectionview location
                    double X = sectionView1.Location.X;
                    double Y = sectionView1.Location.Y;
                    sectionView1.IsElevationRangeAutomatic = false;
                    double Z = sectionView1.ElevationMin;
                    double Z1 = sectionView1.ElevationMax;
                    sectionView1.IsElevationRangeAutomatic = true;

                    //mat datum để kiểm tra đánh cấp hay ko
                    ObjectId sectionDatumId = sampleLine1.GetSectionId(sectionSource_datum_Id);
                    Section? sectionDatum = tr.GetObject(sectionDatumId, OpenMode.ForWrite) as Section;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    SectionPointCollection sectionDatumPoints = sectionDatum.SectionPoints;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    double X0 = sectionDatum.LeftOffset + X;
                    double Xn = sectionDatum.RightOffset + X;

                    double polyLineArea = new();
                    List<Double> ListArea = [];

                    for (int i = 0; i < sectionPoints.Count - 1; i++)
                    {
                        double x1 = sectionPoints[i].Location.X + X;
                        double x2 = sectionPoints[i + 1].Location.X + X;
                        double y1 = sectionPoints[i].Location.Y + Y - Z;
                        double y2 = sectionPoints[i + 1].Location.Y + Y - Z;
                        double at = (y2 - y1) / (x2 - x1);
                        double b = -x1 * (y2 - y1) / (x2 - x1) + y1;

                        //kiểm tra dk đánh cấp
                        if (Math.Abs(at) >= docDanhCap)
                        {
                            //tìm x1, x2  

                            if (!((x1 < X0 & x2 < X0) | (x1 > Xn & x2 > Xn)))
                            {
                                //kiểm điều kiện đánh cấp theo phương X
                                {
                                    if (x1 < X0 & x2 > X0 & x2 <= Xn)
                                    {
                                        x1 = X0;
                                    }
                                    if (x1 < X0 & x2 >= Xn)
                                    {
                                        x1 = X0;
                                        x2 = Xn;
                                    }
                                    if (x1 >= X0 & x2 <= Xn)
                                    {
                                        //x1 = x1;
                                        //x2 = x2;
                                    }
                                    if (x1 <= Xn & x2 > Xn)
                                    {
                                        x2 = Xn;
                                    }
                                    y1 = at * x1 + b;
                                    y2 = at * x2 + b;
                                }

                                {
                                    //kiểm điều kiện đánh cấp theo phương Y
                                    double yt1 = UtilitiesC3D.FindY(sectionDatumPoints, x1, X, Y, Z);
                                    double yt2 = UtilitiesC3D.FindY(sectionDatumPoints, x2, X, Y, Z);

                                    if (!(yt1 <= y1 & yt2 <= y2))
                                    {

                                        if (yt1 > y1 & yt2 > y2)
                                        {
                                        }

                                        if (yt1 > y1 & yt2 < y2)
                                        {

                                            double x = x1;
                                            double yt11 = UtilitiesC3D.FindY(sectionDatumPoints, x, X, Y, Z);
                                            double y11 = at * x + b;
                                            while (Math.Abs(yt11 - y11) > 0.1)
                                            {
                                                yt11 = UtilitiesC3D.FindY(sectionDatumPoints, x, X, Y, Z);
                                                y11 = at * x + b;
                                                x += 0.1;
                                            }
                                            x2 = x;
                                        }
                                        if (yt1 < y1 & yt2 > y2)
                                        {

                                            double x = x1;
                                            double yt11 = UtilitiesC3D.FindY(sectionDatumPoints, x, X, Y, Z);
                                            double y11 = at * x + b;
                                            while (Math.Abs(yt11 - y11) > 0.1)
                                            {
                                                yt11 = UtilitiesC3D.FindY(sectionDatumPoints, x, X, Y, Z);
                                                y11 = at * x + b;
                                                x += 0.1;
                                            }
                                            x1 = x;
                                        }
                                        y1 = at * x1 + b;
                                        y2 = at * x2 + b;
                                        polyLineArea = UtilitiesCAD.CreatePolylineDanhCap(x1, x2, y1, y2, bacCap, docDanhCap);
                                        ListArea.Add(polyLineArea);
                                    }

                                }

                            }


                        }

                    }

                    //ghi text đánh cấp                        
                    Point3d textPosition = new(X + deltaX, Y + deltaY, 0);
                    UtilitiesCAD.CCreateTextWithOutPut(textPosition, 0.4, "Đánh cấp:", alignmentName, "Standard");
                    Point3d textPosition2 = new(X + deltaX + deltaX2, Y + deltaY, 0);
                    DBText Text = UtilitiesCAD.CCreateText2(textPosition2, 0.4, UtilitiesCAD.CSumList(ListArea).ToString("N2") + " m2", alignmentName, "Standard");
                    String textFile = UtilitiesCAD.ConvertTextToField(Text);

                    //đưa data vào mảng của table
                    listLyTrinh.Add(sampleLine1.Station.ToString("N2"));
                    listTenCoc.Add(sampleLine1.Name.ToString());
                    listDanhCap.Add(textFile);

                }

                // bảng đánh cấp

                UtilitiesCAD.CreateTableKhoiLuong(listLyTrinh.Count, 3, alignment.Name, listLyTrinh, listTenCoc, listDanhCap, alignmentName, "đánh cấp");



                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTSV_DanhCap_XoaBo")]
        public static void CTSVDanhCapXoaBo()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            UserInput UI = new();
            UtilitiesCAD CAD = new();
            UtilitiesC3D C3D = new();
            try
            {
                ObjectId polylineId = UserInput.GPolyline("/n Chọn 1 polyline " + " trong nhóm đánh cấp cần xóa: \n");
                Polyline? polyline = tr.GetObject(polylineId, OpenMode.ForWrite) as Polyline;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                String layerPolyline = polyline.Layer;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                UtilitiesCAD.CDelLayerAndObjectOnIt(layerPolyline);

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }


        [CommandMethod("CTSV_DanhCap_VeThem")]
        public static void CTSVDanhCapVeThem()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here
                ObjectId sectionViewId = UserInput.GSectionView("\n Chọn 1 bảng cắt ngang " + " trong nhóm cần tính đánh cấp bổ sung: \n");
                SectionView? sectionView = tr.GetObject(sectionViewId, OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLineId = sectionView.SampleLineId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLine? sampleLine = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLineGroupId = sampleLine.GroupId; ;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;

                ObjectId alignmentId = sampleLine.GetParentAlignmentId();
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                String alignmentName = alignment.Name + "_danhCap";
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                Point3d point1 = UserInput.GPoint("\n Chọn vị trí điểm" + "\n Chọn vị trí điểm" + "để xác định điểm ĐẦU đánh cấp bổ sung: \n");
                Point3d point2 = UserInput.GPoint("\n Chọn vị trí điểm" + "\n Chọn vị trí điểm" + "để xác định điểm CUỐI đánh cấp bổ sung: \n");
                UtilitiesCAD.CreatePolylineDanhCap(point1.X, point2.X, point1.Y, point2.Y, 2, 0.2);

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTSV_DanhCap_VeThem2")]
        public static void CTSVDanhCapVeThem2()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                Point3d point1 = UserInput.GPoint("\n Chọn vị trí điểm" + "\n Chọn vị trí điểm" + "để xác định điểm ĐẦU đánh cấp bổ sung: \n");
                Point3d point2 = UserInput.GPoint("\n Chọn vị trí điểm" + "\n Chọn vị trí điểm" + "để xác định điểm CUỐI đánh cấp bổ sung: \n");
                UtilitiesCAD.CreatePolylineDanhCap(point1.X, point2.X, point1.Y, point2.Y, 2, 0.2);

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTSV_DanhCap_VeThem1")]
        public static void CTSVDanhCapVeThem1()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                Point3d point1 = UserInput.GPoint("\n Chọn vị trí điểm" + "\n Chọn vị trí điểm" + "để xác định điểm ĐẦU đánh cấp bổ sung: \n");
                Point3d point2 = UserInput.GPoint("\n Chọn vị trí điểm" + "\n Chọn vị trí điểm" + "để xác định điểm CUỐI đánh cấp bổ sung: \n");
                UtilitiesCAD.CreatePolylineDanhCap(point1.X, point2.X, point1.Y, point2.Y, 1, 0.2);

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTSV_DanhCap_CapNhat")]
        public static void CTSVDanhCapCapNhat()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            UserInput UI = new();
            UtilitiesCAD CAD = new();
            UtilitiesC3D C3D = new();
            try
            {
                ObjectIdCollection polylineIds = UserInput.GSelectionSetWithType("trong nhóm polyline đánh cấp cần bổ sung khối lượng: \n", "LWPOLYLINE");
                List<Double> listPolyLineArea = [];
                foreach (ObjectId polylineId in polylineIds)
                {
                    Polyline? polyline = tr.GetObject(polylineId, OpenMode.ForWrite) as Polyline;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    listPolyLineArea.Add(polyline.Area);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
                BlockTable? acBlkTbl;
                acBlkTbl = tr.GetObject(A.Db.BlockTableId, OpenMode.ForRead) as BlockTable;
                // Open the Block table record Model space for write
                BlockTableRecord? acBlkTblRec;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                // Change text contatant
                ObjectId textId = UserInput.GDbText("\n Chọn 1 text  " + " để cập nhật đánh cấp: \n");
                DBText? dBText = tr.GetObject(textId, OpenMode.ForWrite) as DBText;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                dBText.TextString = "Đánh cấp: " + UtilitiesCAD.CSumList(listPolyLineArea).ToString("N2") + " m2";
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                dBText.ColorIndex = 1;


                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

    }
}
