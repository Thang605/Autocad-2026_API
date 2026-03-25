using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using MyFirstProject.Extensions;
using System.Linq;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(Civil3DCsharp.CTPProfileToPolyline))]

namespace Civil3DCsharp
{
    /// <summary>
    /// Lệnh chuyển đổi Profile thành Polyline trên Profile View
    /// </summary>
    public class CTPProfileToPolyline
    {
        [CommandMethod("CTP_Profile_To_Polyline")]
        public static void ProfileToPolyline()
        {
            A.Ed.WriteMessage("\n=== CHUYỂN ĐỔI PROFILE THÀNH POLYLINE ===");

            // 1. Chọn Profile trực tiếp trên model
            ObjectId selectedProfileId = UserInput.GProfileId("\n Chọn Profile cần chuyển thành Polyline:");

            if (selectedProfileId == ObjectId.Null)
            {
                A.Ed.WriteMessage("\n Đã hủy: Không chọn Profile.");
                return;
            }

            // 2. Chọn ProfileView
            ObjectId profileViewId = UserInput.GProfileViewId("\n Chọn Profile View chứa Profile:");

            if (profileViewId == ObjectId.Null)
            {
                A.Ed.WriteMessage("\n Đã hủy: Không chọn Profile View.");
                return;
            }

            // 3. Xử lý chuyển đổi
            using (Transaction tr = A.Db.TransactionManager.StartTransaction())
            {
                try
                {
                    Profile? selectedProfile = tr.GetObject(selectedProfileId, OpenMode.ForRead) as Profile;
                    if (selectedProfile == null)
                    {
                        A.Ed.WriteMessage("\n Lỗi: Không thể lấy Profile.");
                        return;
                    }

                    ProfileView? profileView = tr.GetObject(profileViewId, OpenMode.ForRead) as ProfileView;
                    if (profileView == null)
                    {
                        A.Ed.WriteMessage("\n Lỗi: Không thể lấy Profile View.");
                        return;
                    }

                    A.Ed.WriteMessage($"\n Profile: {selectedProfile.Name}");
                    A.Ed.WriteMessage($"\n Profile View: {profileView.Name}");

                    // 4. Lấy các điểm từ Profile
                    List<Point2d> xyPoints = new();

                    PromptKeywordOptions pko = new PromptKeywordOptions("\n Chọn phương pháp lấy mẫu [Sampleline/Interval/Pvi] <Sampleline>:");
                    pko.Keywords.Add("Sampleline");
                    pko.Keywords.Add("Interval");
                    pko.Keywords.Add("Pvi");
                    pko.Keywords.Default = "Sampleline";
                    PromptResult pkr = A.Ed.GetKeywords(pko);

                    if (pkr.Status != PromptStatus.OK)
                    {
                        A.Ed.WriteMessage("\n Đã hủy.");
                        return;
                    }

                    string sampleMethod = pkr.StringResult;

                    double startStation = selectedProfile.StartingStation;
                    double endStation = selectedProfile.EndingStation;

                    if (sampleMethod == "Sampleline")
                    {
                        ObjectId alignId = selectedProfile.AlignmentId;
                        Alignment? alignment = tr.GetObject(alignId, OpenMode.ForRead) as Alignment;
                        
                        // Lấy danh sách cọc
                        UtilitiesC3D.GCoordinatePointFromAlignment(alignment, 0, out string[] samplelineName, out string[] eastings, out string[] northings, out string[] stationsStr);

                        if (stationsStr != null && stationsStr.Length > 0)
                        {
                            List<double> validStations = new List<double>();
                            foreach (string s in stationsStr)
                            {
                                if (double.TryParse(s, out double stat))
                                {
                                    if (stat >= startStation && stat <= endStation)
                                    {
                                        validStations.Add(Math.Round(stat, 3));
                                    }
                                }
                            }

                            // Sắp xếp và loại bỏ trùng lặp
                            validStations.Sort();
                            validStations = validStations.Distinct().ToList();

                            A.Ed.WriteMessage($"\n Lấy mẫu theo {validStations.Count} cọc (Sample Lines)");

                            foreach (double station in validStations)
                            {
                                try
                                {
                                    double elevation = selectedProfile.ElevationAt(station);
                                    double x = 0, y = 0;
                                    profileView.FindXYAtStationAndElevation(station, elevation, ref x, ref y);
                                    xyPoints.Add(new Point2d(x, y));
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            A.Ed.WriteMessage("\n Lỗi: Không tìm thấy tập hợp Cọc (Sample Line Group) trên tuyến này.");
                            return;
                        }
                    }
                    else if (sampleMethod == "Interval")
                    {
                        PromptDoubleOptions dblOpt = new PromptDoubleOptions("\n Khoảng cách lấy mẫu (m) [nhỏ = chi tiết hơn]:");
                        dblOpt.DefaultValue = 5.0;
                        dblOpt.AllowNegative = false;
                        dblOpt.AllowZero = false;
                        PromptDoubleResult dblRes = A.Ed.GetDouble(dblOpt);

                        double interval = dblRes.Status == PromptStatus.OK ? dblRes.Value : 5.0;

                        A.Ed.WriteMessage($"\n Lấy mẫu từ station {startStation:F2} đến {endStation:F2}, khoảng cách {interval:F2}m");

                        for (double station = startStation; station <= endStation; station += interval)
                        {
                            try
                            {
                                double elevation = selectedProfile.ElevationAt(station);
                                double x = 0, y = 0;
                                profileView.FindXYAtStationAndElevation(station, elevation, ref x, ref y);
                                xyPoints.Add(new Point2d(x, y));
                            }
                            catch { }
                        }

                        // Đảm bảo thêm điểm cuối cùng
                        try
                        {
                            double lastElev = selectedProfile.ElevationAt(endStation);
                            double lx = 0, ly = 0;
                            profileView.FindXYAtStationAndElevation(endStation, lastElev, ref lx, ref ly);
                            Point2d lastPt = new Point2d(lx, ly);

                            if (xyPoints.Count == 0 || xyPoints[xyPoints.Count - 1].GetDistanceTo(lastPt) > 0.001)
                            {
                                xyPoints.Add(lastPt);
                            }
                        }
                        catch { }
                    }
                    else if (sampleMethod == "Pvi")
                    {
                        // Profile layout - lấy điểm PVI
                        foreach (ProfilePVI pvi in selectedProfile.PVIs)
                        {
                            try
                            {
                                double x = 0, y = 0;
                                profileView.FindXYAtStationAndElevation(pvi.RawStation, pvi.Elevation, ref x, ref y);
                                xyPoints.Add(new Point2d(x, y));
                            }
                            catch (System.Exception ex)
                            {
                                A.Ed.WriteMessage($"\n Cảnh báo: Không chuyển được PVI tại station {pvi.RawStation:F2}: {ex.Message}");
                            }
                        }
                    }

                    if (xyPoints.Count < 2)
                    {
                        A.Ed.WriteMessage("\n Lỗi: Không đủ điểm để tạo Polyline (cần ít nhất 2 điểm).");
                        return;
                    }

                    A.Ed.WriteMessage($"\n Số điểm: {xyPoints.Count}");

                    // 5. Tạo Polyline từ các điểm XY
                    BlockTable? bt = tr.GetObject(A.Db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord? btr = tr.GetObject(bt![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    Polyline polyline = new Polyline();
                    for (int i = 0; i < xyPoints.Count; i++)
                    {
                        polyline.AddVertexAt(i, xyPoints[i], 0, 0, 0);
                    }

                    // Đặt màu đỏ để dễ nhận biết
                    polyline.ColorIndex = 1;

                    btr!.AppendEntity(polyline);
                    tr.AddNewlyCreatedDBObject(polyline, true);

                    A.Ed.WriteMessage($"\n\n=== KẾT QUẢ ===");
                    A.Ed.WriteMessage($"\n Profile: {selectedProfile.Name}");
                    A.Ed.WriteMessage($"\n Polyline đã tạo với {xyPoints.Count} đỉnh");
                    A.Ed.WriteMessage($"\n Màu: Đỏ (Color Index 1)");

                    tr.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception e)
                {
                    A.Ed.WriteMessage($"\n Lỗi: {e.Message}");
                }
            }
        }
    }
}
