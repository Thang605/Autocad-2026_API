// Nhóm lệnh: Bảng tọa độ cọc + Cập nhật bảng
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
using ATable = Autodesk.AutoCAD.DatabaseServices.Table;

using Autodesk.Civil.DatabaseServices;
using CivSurface = Autodesk.Civil.DatabaseServices.TinSurface;
using MyFirstProject.Extensions;

[assembly: CommandClass(typeof(Civil3DCsharp.SamplelineTable))]

namespace Civil3DCsharp
{
    public class SamplelineTable
    {
        [CommandMethod("CTS_TaoBang_ToaDoCoc")]
        public static void CTSTaoBangToaDoCoc()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput uI = new();
                UtilitiesCAD uti = new();

                //start here

                // get an alignment
                ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường " + "for export coordinate table");
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                //get the first samplelineGroup
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLineGroup = alignment.GetSampleLineGroupIds()[0];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? samplelineGroup = tr.GetObject(sampleLineGroup, OpenMode.ForRead) as SampleLineGroup;

                //check number of sampleline in samplelinegroup
                int numberSampleline = 1;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                foreach (ObjectId sampleLineId in samplelineGroup.GetSampleLineIds())
                {
                    numberSampleline++;
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                // get coordinate and name of point
                string[] samplelineName = new string[numberSampleline];
                string[] eastings = new string[numberSampleline];
                string[] northings = new string[numberSampleline];
                int orderSampleline = 1;
                foreach (ObjectId sampleLineId in samplelineGroup.GetSampleLineIds())
                {
                    SampleLine? sampleline = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    Double station = sampleline.Station;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    double easting = 0;
                    double northing = 0;
                    alignment.PointLocation(station, 0, ref easting, ref northing);
                    samplelineName[orderSampleline] = sampleline.Name.ToUpper();
                    eastings[orderSampleline] = Convert.ToString(Math.Round(easting, 3));
                    northings[orderSampleline] = Convert.ToString(Math.Round(northing, 3));
                    orderSampleline++;
                }

                //create a coordinate table
                UtilitiesCAD.CreateTableCoordinate(numberSampleline, 3, alignment.Name, samplelineName, eastings, northings);

                // draw polyline for check coordinnate
                UtilitiesCAD.CreateOpenPolyline(numberSampleline, eastings, northings);


                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_TaoBang_ToaDoCoc2")]
        public static void CTSTaoBangToaDoCoc2()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput uI = new();
                UtilitiesCAD uti = new();

                //start here

                // get an alignment
                ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường " + "for export coordinate table");
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                //get the first samplelineGroup
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLineGroup = alignment.GetSampleLineGroupIds()[0];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? samplelineGroup = tr.GetObject(sampleLineGroup, OpenMode.ForRead) as SampleLineGroup;

                //check number of sampleline in samplelinegroup
                int numberSampleline = 1;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                foreach (ObjectId sampleLineId in samplelineGroup.GetSampleLineIds())
                {
                    numberSampleline++;
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                // get coordinate and name of point
                string[] samplelineName = new string[numberSampleline];
                string[] eastings = new string[numberSampleline];
                string[] northings = new string[numberSampleline];
                string[] stations = new string[numberSampleline];
                int orderSampleline = 1;
                foreach (ObjectId sampleLineId in samplelineGroup.GetSampleLineIds())
                {
                    SampleLine? sampleline = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    Double station = sampleline.Station;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    double easting = 0;
                    double northing = 0;
                    alignment.PointLocation(station, 0, ref easting, ref northing);
                    samplelineName[orderSampleline] = sampleline.Name.ToUpper();
                    eastings[orderSampleline] = Convert.ToString(Math.Round(easting, 3));
                    northings[orderSampleline] = Convert.ToString(Math.Round(northing, 3));
                    stations[orderSampleline] = Convert.ToString(Math.Round(station, 3));
                    orderSampleline++;
                }

                //create a coordinate table
                UtilitiesCAD.CreateTableCoordinate2(numberSampleline, 4, alignment.Name, samplelineName, eastings, northings, stations);

                // draw polyline for check coordinnate
                UtilitiesCAD.CreateOpenPolyline(numberSampleline, eastings, northings);


                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_TaoBang_ToaDoCoc3")]
        public static void CTSTaoBangToaDoCoc3()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput uI = new();
                UtilitiesCAD uti = new();

                //start here

                // get an alignment
                ObjectId surfaceId = UserInput.GObjId("Chọn mặt phẳng cần lấy cao độ:");
                CivSurface? civSurface = tr.GetObject(surfaceId, OpenMode.ForRead) as CivSurface;
                ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường " + "for export coordinate table");
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                //get the first samplelineGroup
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId sampleLineGroup = alignment.GetSampleLineGroupIds()[0];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                SampleLineGroup? samplelineGroup = tr.GetObject(sampleLineGroup, OpenMode.ForRead) as SampleLineGroup;

                //check number of sampleline in samplelinegroup
                int numberSampleline = 1;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                foreach (ObjectId sampleLineId in samplelineGroup.GetSampleLineIds())
                {
                    numberSampleline++;
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                // get coordinate and name of point
                string[] samplelineName = new string[numberSampleline];
                string[] eastings = new string[numberSampleline];
                string[] northings = new string[numberSampleline];
                string[] elevation = new string[numberSampleline];
                int orderSampleline = 1;
                foreach (ObjectId sampleLineId in samplelineGroup.GetSampleLineIds())
                {
                    SampleLine? sampleline = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    Double station = sampleline.Station;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    double easting = 0;
                    double northing = 0;
                    double elevate = 0;
                    alignment.PointLocation(station, 0, ref easting, ref northing);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    elevate = civSurface.FindElevationAtXY(easting, northing);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    samplelineName[orderSampleline] = sampleline.Name.ToUpper();
                    eastings[orderSampleline] = Convert.ToString(Math.Round(easting, 3));
                    northings[orderSampleline] = Convert.ToString(Math.Round(northing, 3));
                    elevation[orderSampleline] = Convert.ToString(Math.Round(elevate, 3));
                    orderSampleline++;
                }

                //create a coordinate table
                UtilitiesCAD.CreateTableCoordinate2(numberSampleline, 4, alignment.Name, samplelineName, eastings, northings, elevation);

                // draw polyline for check coordinnate
                UtilitiesCAD.CreateOpenPolyline(numberSampleline, eastings, northings);


                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("AT_UPdate2Table")]
        public static void ATUPdate2Table()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here
                ObjectId tableId1 = UserInput.GTable("Chọn 1 bảng " + "for source table:");
                ATable? table1 = tr.GetObject(tableId1, OpenMode.ForWrite) as ATable;
                ObjectId tableId2 = UserInput.GTable("Chọn 1 bảng " + "for destinate table:");
                ATable? table2 = tr.GetObject(tableId2, OpenMode.ForWrite) as ATable;

                //Update table
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (table2.Rows.Count == table1.Rows.Count)
                {
                    for (int i = 2; i < table2.Rows.Count; i++)
                    {
                        table2.Cells[i, 0].TextHeight = 2;
                        table2.Cells[i, 0].TextString = table1.Cells[i, 0].TextString;
                        table2.Cells[i, 0].Alignment = CellAlignment.MiddleCenter;

                        table2.Cells[i, 1].TextHeight = 2;
                        table2.Cells[i, 1].TextString = table1.Cells[i, 1].TextString;
                        table2.Cells[i, 1].Alignment = CellAlignment.MiddleCenter;

                        table2.Cells[i, 2].TextHeight = 2;
                        table2.Cells[i, 2].TextString = table1.Cells[i, 2].TextString;
                        table2.Cells[i, 2].Alignment = CellAlignment.MiddleCenter;
                    }
                    ;
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                ;

                if (table2.Rows.Count < table1.Rows.Count)
                {
                    table2.InsertRows(3, 5, table1.Rows.Count - table2.Rows.Count);
                    for (int i = 2; i < table2.Rows.Count; i++)
                    {
                        table2.Cells[i, 0].TextHeight = 2;
                        table2.Cells[i, 0].TextString = table1.Cells[i, 0].TextString;
                        table2.Cells[i, 0].Alignment = CellAlignment.MiddleCenter;

                        table2.Cells[i, 1].TextHeight = 2;
                        table2.Cells[i, 1].TextString = table1.Cells[i, 1].TextString;
                        table2.Cells[i, 1].Alignment = CellAlignment.MiddleCenter;

                        table2.Cells[i, 2].TextHeight = 2;
                        table2.Cells[i, 2].TextString = table1.Cells[i, 2].TextString;
                        table2.Cells[i, 2].Alignment = CellAlignment.MiddleCenter;

                    }
                    ;
                }
                ;
                if (table2.Rows.Count > table1.Rows.Count)
                {
                    table2.DeleteRows(3, table2.Rows.Count - table1.Rows.Count);
                    for (int i = 2; i < table2.Rows.Count; i++)
                    {
                        table2.Cells[i, 0].TextHeight = 2;
                        table2.Cells[i, 0].TextString = table1.Cells[i, 0].TextString;
                        table2.Cells[i, 0].Alignment = CellAlignment.MiddleCenter;

                        table2.Cells[i, 1].TextHeight = 2;
                        table2.Cells[i, 1].TextString = table1.Cells[i, 1].TextString;
                        table2.Cells[i, 1].Alignment = CellAlignment.MiddleCenter;

                        table2.Cells[i, 2].TextHeight = 2;
                        table2.Cells[i, 2].TextString = table1.Cells[i, 2].TextString;
                        table2.Cells[i, 2].Alignment = CellAlignment.MiddleCenter;

                    }
                    ;
                }
                ;


                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }
    }
}
