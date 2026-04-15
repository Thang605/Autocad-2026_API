// Nhóm lệnh: Chèn / Phát sinh cọc (SampleLine Create)
// Tách từ 07.Sampleline.cs
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using ATable = Autodesk.AutoCAD.DatabaseServices.Table;

using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.Civil;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Civil.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using MyFirstProject.Extensions;

[assembly: CommandClass(typeof(Civil3DCsharp.SamplelineCreate))]

namespace Civil3DCsharp
{
    public class SamplelineCreate
    {
        [CommandMethod("CTS_ChenCoc_TrenTracDoc")]
        public static void CTSChenCocTrenTracDoc()
        {
            // 1. Hiển thị form để thiết lập tên cọc
            var form = new MyFirstProject.Civil_Tool.ChenCocTrenTracDocForm();
            var result = Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(form);

            if (result != System.Windows.Forms.DialogResult.OK || !form.FormAccepted)
            {
                A.Ed.WriteMessage("\n Đã hủy lệnh.");
                return;
            }

            // 2. Khởi tạo utilities
            _ = new UserInput();
            _ = new UtilitiesCAD();
            _ = new UtilitiesC3D();

            // 3. Chọn ProfileView
            ObjectId profileViewId = UserInput.GProfileViewId("\n Chọn 1 bảng trắc dọc để chèn cọc: \n");

            // ============================================
            // PHASE 1: Thu thập tất cả các điểm trước
            // ============================================
            List<double> collectedStations = new();
            A.Ed.WriteMessage("\n === Chọn các vị trí cọc trên trắc dọc (ESC để kết thúc chọn) ===");

            // Mở 1 transaction chỉ để đọc ProfileView (tìm station)
            using (Transaction trRead = A.Db.TransactionManager.StartTransaction())
            {
                try
                {
                    ProfileView? profileView = trRead.GetObject(profileViewId, OpenMode.ForWrite) as ProfileView;
                    if (profileView == null) { A.Ed.WriteMessage("\n Không tìm thấy trắc dọc."); return; }

                    ObjectId alignmentId = profileView.AlignmentId;
                    Alignment? alignment = trRead.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
                    if (alignment == null) { A.Ed.WriteMessage("\n Không tìm thấy tim tuyến."); return; }

                    while (true)
                    {
                        try
                        {
                            var ppo = new Autodesk.AutoCAD.EditorInput.PromptPointOptions($"\n Chọn vị trí cọc thứ {collectedStations.Count + 1} (ESC để kết thúc): ");
                            var rppo = A.Ed.GetPoint(ppo);

                            if (rppo.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                            {
                                // User pressed ESC or cancelled
                                break;
                            }

                            Point3d point3D = rppo.Value;
                            double station = 0, elevation = 0;
                            profileView.FindStationAndElevationAtXY(point3D.X, point3D.Y, ref station, ref elevation);

                            // Kiểm tra station hợp lệ
                            if (station < alignment.StartingStation || station > alignment.EndingStation)
                            {
                                A.Ed.WriteMessage($"\n ⚠️ Vị trí nằm ngoài phạm vi tuyến. Bỏ qua.");
                            }
                            else
                            {
                                collectedStations.Add(station);
                                A.Ed.WriteMessage($"\n   ✓ Điểm {collectedStations.Count}: Km{station / 1000:F3}");
                            }
                        }
                        catch
                        {
                            // Any other error
                            break;
                        }
                    }

                    trRead.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception e)
                {
                    A.Ed.WriteMessage("\n " + e.Message);
                    return;
                }
            }

            // Kiểm tra có điểm nào được chọn không
            if (collectedStations.Count == 0)
            {
                A.Ed.WriteMessage("\n Không có điểm nào được chọn. Đã hủy lệnh.");
                return;
            }

            // Sắp xếp theo station tăng dần
            collectedStations.Sort();
            A.Ed.WriteMessage($"\n\n === Đang tạo {collectedStations.Count} cọc... ===");

            // ============================================
            // PHASE 2: Tạo tất cả SampleLine cùng lúc
            // ============================================
            int cocDaTao = 0;
            using (Transaction tr = A.Db.TransactionManager.StartTransaction())
            {
                try
                {
                    ProfileView? profileView = tr.GetObject(profileViewId, OpenMode.ForWrite) as ProfileView;
                    ObjectId alignmentId = profileView!.AlignmentId;
                    Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;

                    // Lấy hoặc tạo SampleLineGroup
                    ObjectId sampleLineGroupId;
                    if (alignment!.GetSampleLineGroupIds().Count == 0)
                    {
                        sampleLineGroupId = SampleLineGroup.Create(alignment.Name, alignmentId);
                    }
                    else
                    {
                        sampleLineGroupId = alignment.GetSampleLineGroupIds()[0];
                    }

                    // Lấy danh sách tên đã tồn tại
                    SampleLineGroup? sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;
                    HashSet<string> existingNames = new(StringComparer.OrdinalIgnoreCase);
                    if (sampleLineGroup != null)
                    {
                        foreach (ObjectId slId in sampleLineGroup.GetSampleLineIds())
                        {
                            SampleLine? sl = tr.GetObject(slId, OpenMode.ForRead) as SampleLine;
                            if (sl != null) existingNames.Add(sl.Name);
                        }
                    }

                    // Tạo từng cọc
                    foreach (double station in collectedStations)
                    {
                        // Tạo tên — tự nhảy số nếu trùng
                        string samplelineName = form.GetCurrentStakeName();
                        while (existingNames.Contains(samplelineName))
                        {
                            form.IncrementCounter();
                            samplelineName = form.GetCurrentStakeName();
                        }

                        // Tạo SampleLine với tên tạm → rename (tránh lỗi trùng tên API)
                        string tempName = $"z_{Guid.NewGuid():N}";
                        Point2dCollection point2Ds = new();
                        double easting = 0, northing = 0;
                        alignment.PointLocation(station, -10, ref easting, ref northing);
                        point2Ds.Add(new Point2d(easting, northing));
                        alignment.PointLocation(station, 10, ref easting, ref northing);
                        point2Ds.Add(new Point2d(easting, northing));

                        ObjectId samplelineId = SampleLine.Create(tempName, sampleLineGroupId, point2Ds);

                        // Rename sang tên thật + set style
                        SampleLine? sampleline = tr.GetObject(samplelineId, OpenMode.ForWrite) as SampleLine;
                        if (sampleline != null)
                        {
                            sampleline.Name = samplelineName;
                            existingNames.Add(samplelineName); // tracking để lần sau không trùng

                            if (!string.IsNullOrEmpty(form.SampleLineStyleName))
                            {
                                try { sampleline.StyleName = form.SampleLineStyleName; }
                                catch { /* Style không tồn tại */ }
                            }
                        }

                        form.IncrementCounter();
                        cocDaTao++;

                        A.Ed.WriteMessage($"\n ✓ {samplelineName} tại Km{station / 1000:F3}");
                    }

                    tr.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception e)
                {
                    A.Ed.WriteMessage("\n Lỗi: " + e.Message);
                }
            }

            A.Ed.WriteMessage($"\n\n ✅ Hoàn thành: đã tạo {cocDaTao}/{collectedStations.Count} cọc.");
        }

        [CommandMethod("CTS_CHENCOC_TRENTRACNGANG")]
        public static void CTSCHENCOCTRENTRACNGANG()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here
                ObjectId sectionViewId = UserInput.GSectionView("Chọn 1 bảng cắt ngang " + ": \n");
                SectionView? sectionView = tr.GetObject(sectionViewId, OpenMode.ForRead) as SectionView;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                string decription = UserInput.GString("Nhập mã mô tả cho điểm sẽ chèn: \n ");
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                string ans = "Enter";
                while (ans == "Enter")
                {
                    Point3d point3D = UserInput.GPoint("\n Chọn vị trí điểm" + "Chọn điểm cần chèn trên trắc ngang: \n ");

                    //find offset, elevation
                    double offset = 1;
                    double elevation = 2;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    sectionView.FindOffsetAndElevationAtXY(point3D.X, point3D.Y, ref offset, ref elevation);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                    //find alignment
                    ObjectId samplelineId = sectionView.SampleLineId;
                    SampleLine? sampleLine = tr.GetObject(samplelineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    double station = sampleLine.Station;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    ObjectId alignmentId = sampleLine.GetParentAlignmentId();
                    Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;

                    // create point
                    double x = 1;
                    double y = 1;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    alignment.PointLocation(station, offset, ref x, ref y);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    Point3d point3D1 = new(x, y, elevation);
#pragma warning disable CS8604 // Possible null reference argument.
                    UtilitiesC3D.CreateCogoPointFromPoint3D(point3D1, decription);
#pragma warning restore CS8604 // Possible null reference argument.
                    ans = UserInput.GStopWithESC();
                }


                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_PhatSinhCoc")]
        public static void CTSPhatSinhCoc()
        {
            // Show form first
            var form = new MyFirstProject.Civil_Tool.PhatSinhCocForm();
            var result = Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(form);

            if (result != System.Windows.Forms.DialogResult.OK || !form.FormAccepted)
            {
                A.Ed.WriteMessage("\n Đã hủy lệnh.");
                return;
            }

            // Get values from form
            bool phatSinhCocH = form.PhatSinhCocH;
            int Km = form.KmBatDau;
            int khoangCach = form.KhoangCachChiTiet;
            string labelStyleName = form.SampleLineLabelStyleName;

            // start transaction
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                Csharp CS = new();

                //start here CTS_PhatSinhCoc_DiemDacBiet
                ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường " + "để phát sinh cọc: \n");
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                Station[] station = alignment.GetStationSet(StationTypes.GeometryPoint);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                String[] sampleLineGeo = new string[station.Count()];
                int y = 0;
                for (int i = 0; i < station.Count(); i++)
                {
                    if (station[i].GeometryStationType == AlignmentGeometryPointStationType.BegOfAlign)
                    {
                        sampleLineGeo[i] = "zDT";
                        y++;
                    }
                    if (station[i].GeometryStationType == AlignmentGeometryPointStationType.TanTan)
                    {
                        sampleLineGeo[i] = "zD" + y.ToString();
                        y++;
                    }
                    if (station[i].GeometryStationType == AlignmentGeometryPointStationType.TanCurve)
                    {
                        sampleLineGeo[i] = "zTD" + y.ToString();
                        sampleLineGeo[i + 1] = "zP" + y.ToString();
                        sampleLineGeo[i + 2] = "zTC" + y.ToString();
                        y++;

                    }
                    if (station[i].GeometryStationType == AlignmentGeometryPointStationType.TanSpiral)
                    {
                        sampleLineGeo[i] = "zND" + y.ToString();
                        sampleLineGeo[i + 1] = "zTD" + y.ToString();
                        sampleLineGeo[i + 2] = "zP" + y.ToString();
                        sampleLineGeo[i + 3] = "zTC" + y.ToString();
                        sampleLineGeo[i + 4] = "zNC" + y.ToString();
                        y++;
                    }
                    if (station[i].GeometryStationType == AlignmentGeometryPointStationType.EndOfAlign)
                    {

                        sampleLineGeo[i] = "zCT";
                    }
                }

                //phát sinh cọc đặc biệt
                ObjectId sampleLineGroupId = new();
                if (alignment.GetSampleLineGroupIds().Count == 0)
                {
                    sampleLineGroupId = SampleLineGroup.Create(alignment.Name, alignmentId);
                }
                else
                {
                    sampleLineGroupId = alignment.GetSampleLineGroupIds()[0];
                }

                for (int i = 0; i < station.Count(); i++)
                {
                    ObjectId sampleLineId = UtilitiesC3D.CreateSampleline(sampleLineGeo[i].ToString(), sampleLineGroupId, alignment, station[i].RawStation);
                }

                //phát sinh cọc H (từ form)
                int LyTrinh = Km;
                int H_number = 1;
                int soLuongCocH = 0;
                for (int i = 0; i < alignment.Length / 100 - 1; i++)
                {
                    soLuongCocH++;
                }
                Double[] stationCocH = new double[soLuongCocH];
                if (phatSinhCocH)
                {
                    for (int i = 0; i < alignment.Length / 100 - 1; i++)
                    {
                        stationCocH[i] = 100 + i * 100;
                        if (stationCocH[i] % 1000 == 0)
                        {
                            ObjectId sampleLineId = UtilitiesC3D.CreateSampleline("Km" + ((i + 1) / 10 + Km).ToString(), sampleLineGroupId, alignment, 100 + i * 100);
                            LyTrinh = ((i + 1) / 10 + Km);
                            H_number = 1;
                        }
                        if (stationCocH[i] % 1000 != 0)
                        {
                            ObjectId sampleLineId = UtilitiesC3D.CreateSampleline("H" + H_number + " (Km" + LyTrinh + ")", sampleLineGroupId, alignment, 100 + i * 100);
                            H_number++;
                        }
                    }
                }
                A.Ed.WriteMessage("\n Số cọc H: " + soLuongCocH);

                //cộng 2 mảng đặc biệt và H
                Double[] rawStation = new double[station.Count() + soLuongCocH];
                for (int i = 0; i < station.Count(); i++)
                {
                    rawStation[i] = station[i].RawStation;
                }
                for (int i = 0; i < soLuongCocH; i++)
                {
                    rawStation[i + station.Count()] = stationCocH[i];
                }
                rawStation = Csharp.CSMangSapXepTangDan(rawStation);

                //cọc chi tiết (từ form)
                List<double> stationList = [];
                List<String> sampleLineNameS = [];
                for (int i = 0; i < rawStation.Count() - 1; i++)
                {
                    double stationDetail = rawStation[i];
                    for (int j = 1; j < Math.Ceiling((rawStation[i + 1] - rawStation[i]) / khoangCach); j++)
                    {
                        stationDetail += khoangCach;
                        stationList.Add(stationDetail);
                    }
                }

                int k = 1;
                foreach (var item in stationList)
                {
                    sampleLineNameS.Add("z" + Convert.ToString(k));
                    k++;
                }

                for (int i = 0; i < stationList.Count; i++)
                {
                    ObjectId sampleLineId = UtilitiesC3D.CreateSampleline(sampleLineNameS[i], sampleLineGroupId, alignment, stationList[i]);
                }


                //rename sampleline

                char[] charsToTrim = ['z', ' ', '\''];
                SampleLineGroup? sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;
                
                // Lấy danh sách tên sampleline đã tồn tại trong group
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                HashSet<string> existingNames = new();
                foreach (ObjectId slId in sampleLineGroup.GetSampleLineIds())
                {
                    SampleLine? sl = tr.GetObject(slId, OpenMode.ForRead) as SampleLine;
                    if (sl != null && !sl.Name.StartsWith("z"))
                    {
                        existingNames.Add(sl.Name);
                    }
                }

                foreach (ObjectId sampleLineId in sampleLineGroup.GetSampleLineIds())
                {
                    SampleLine? sampleline = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    // Chỉ rename nếu tên bắt đầu bằng 'z' (tên tạm)
                    if (sampleline.Name.StartsWith("z"))
                    {
                        string newName = sampleline.Name.Trim(charsToTrim);
                        
                        // Kiểm tra nếu tên đã tồn tại, thêm suffix
                        if (existingNames.Contains(newName))
                        {
                            int suffix = 1;
                            while (existingNames.Contains($"{newName}_{suffix}"))
                            {
                                suffix++;
                            }
                            newName = $"{newName}_{suffix}";
                        }
                        
                        try
                        {
                            sampleline.Name = newName;
                            existingNames.Add(newName);
                        }
                        catch (System.ArgumentException)
                        {
                            // Tên đã tồn tại, bỏ qua
                            A.Ed.WriteMessage($"\n Bỏ qua sampleline có tên trùng: {newName}");
                        }
                    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                // tạo label (từ form)
                ObjectId labelId = GetLabelStyleId(labelStyleName);
                if (labelId != ObjectId.Null)
                {
                    ObjectId labelSampleLineGroup = SampleLineLabelGroup.Create(sampleLineGroupId, labelId);
                }

                A.Ed.WriteMessage($"\n Đã phát sinh {station.Count()} cọc đặc biệt, {soLuongCocH} cọc H/Km, {stationList.Count} cọc chi tiết.");
                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        /// <summary>
        /// Helper method to get SampleLine Label Style ID
        /// </summary>
        private static ObjectId GetLabelStyleId(string styleName)
        {
            try
            {
                return A.Cdoc.Styles.LabelStyles.SampleLineLabelStyles.LabelStyles[styleName];
            }
            catch
            {
                try
                {
                    return A.Cdoc.Styles.LabelStyles.SampleLineLabelStyles.LabelStyles["Tên cọc"];
                }
                catch
                {
                    return ObjectId.Null;
                }
            }
        }

        [CommandMethod("CTS_PhatSinhCoc_theoKhoangDelta")]
        public static void CTSPhatSinhCocTheoKhoangDelta()
        {
            // start transantion
            _ = new            // start transantion
            UserInput();
            _ = new UtilitiesCAD();
            _ = new UtilitiesC3D();
            //start here
            ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường " + "để phát sinh cọc: \n");
            Double khoangCach = UserInput.GDouble("Nhập khoảng cách cọc sẽ phát sinh:\n");
            double station = new();
            double offset = new();
            int increment = 1000;
            String sampleLineName = Convert.ToString(increment);
            int stationIncrement = 1;

            String i = "Enter";
            while (i == "Enter")
            {
                using (Transaction tr = A.Db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        ObjectId sampleLineGroupId = alignment.GetSampleLineGroupIds()[0];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        Point3d point3D = UserInput.GPoint("\n Chọn vị trí điểm" + "để phát sinh cọc: \n");

                        // phát sinh cọc
                        alignment.StationOffset(point3D.X, point3D.Y, ref station, ref offset);

                        station += khoangCach * stationIncrement;
                        ObjectId sampleLineId = UtilitiesC3D.CreateSampleline(sampleLineName, sampleLineGroupId, alignment, station);



                        tr.Commit();
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception e)
                    {
                        A.Ed.WriteMessage(e.Message);
                    }
                }
                i = UserInput.GStopWithESC();
                stationIncrement++;
                increment++;
                sampleLineName = Convert.ToString(increment);
            }
        }

        [CommandMethod("CTS_PhatSinhCoc_TuCogoPoint")]
        public static void CTSPhatSinhCocTuCogoPoint()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here
                //get point group
                ObjectId cogoPointId = UserInput.GCogoPointId("\n Chọn cogo point " + " thuộc nhóm điểm cần phát sinh cọc: \n");
                CogoPoint? cogoPoint = tr.GetObject(cogoPointId, OpenMode.ForWrite) as CogoPoint;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId pointGroupId = cogoPoint.PrimaryPointGroupId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                ObjectIdCollection pointIds = UtilitiesC3D.GPointIdsFromPointGroup(pointGroupId);

                // get alignment
                ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường cần phát sinh cọc:");
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
                double station = new();
                double offset = new();
                foreach (ObjectId pointId in pointIds)
                {
                    CogoPoint? cogoPoint1 = tr.GetObject(pointId, OpenMode.ForRead) as CogoPoint;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    alignment.StationOffset(cogoPoint1.Easting, cogoPoint1.Northing, ref station, ref offset);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                    ObjectId sampleLineId = UtilitiesC3D.CreateSampleline(cogoPoint1.PointName, alignment.GetSampleLineGroupIds()[0], alignment, station);
                }


                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTS_PhatSinhCoc_TheoBang")]
        public static void CTSPhatSinhCocTheoBang()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here
                ObjectId bangId = UserInput.GTable("Chọn bảng tọa độ cọc có lý trình:");
                int soTenCoc = UserInput.GInt("Nhập số thứ tự cột chứa tên cọc trong bảng:");
                int soLyTrinh = UserInput.GInt("Nhập số thứ tự cột chứa lý trình trong bảng:");
                int soHangCoc = UserInput.GInt("Nhập số thứ tự hàng bắt đầu chứa cọc trong bảng:");
                ObjectId alignmentId = UserInput.GAlignmentId("Chọn tim tuyến cần bổ sung cọc:");

                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
                ATable? bang = tr.GetObject(bangId, OpenMode.ForRead) as ATable;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (alignment.GetSampleLineGroupIds().Count == 0)
                {
                    ObjectId slgId = SampleLineGroup.Create(alignment.Name, alignment.ObjectId);
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                List<String> listTenCoc = [];
                List<Double> listLyTrinh = [];
                String tenCoc;
                int soCocThem = 1;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                int soHang = bang.Rows.Count;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                for (int i = soHangCoc - 1; i < soHang; i++)
                {
                    tenCoc = bang.Cells[i, soTenCoc - 1].GetTextString(FormatOption.IgnoreMtextFormat);
                    listTenCoc.Add(tenCoc);
                    //check duplicate element
                    for (int j = 0; j < listTenCoc.Count - 1; j++)
                    {
                        if (tenCoc == listTenCoc[j].ToString())
                        {
                            listTenCoc[j] = tenCoc + soCocThem.ToString();
                            soCocThem++;
                        }
                    }
                    listLyTrinh.Add(Convert.ToDouble(bang.Cells[i, soLyTrinh - 1].GetTextString(FormatOption.IgnoreMtextFormat)));
                }
                listLyTrinh[listTenCoc.Count - 1] = listLyTrinh[listTenCoc.Count - 1] - 0.01;

                for (int i = 0; i < listTenCoc.Count; i++)
                {
                    A.Ok(listTenCoc[i].ToString());
                    A.Ok(listLyTrinh[i].ToString());
                    ObjectId samplelineId = UtilitiesC3D.CreateSampleline(listTenCoc[i], alignment.GetSampleLineGroupIds()[0], alignment, listLyTrinh[i]);
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
