using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.Civil.Runtime;
using Autodesk.Civil.Settings;
using MyFirstProject.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Acad = Autodesk.AutoCAD.ApplicationServices;
using Civil = Autodesk.Civil.ApplicationServices;
using CivSurface = Autodesk.Civil.DatabaseServices.TinSurface;
// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.Profiles))]

namespace Civil3DCsharp
{
    public class Profiles
    {
        [CommandMethod("CTP_VeTracDoc_TuNhien")]
        public static void ProfileAndProfileViewCreate()
        {
            // get the surface
            _ = new            // get the surface
            UserInput();
            ObjectId surfaceId = UserInput.GSurfaceId("\n Chọn mặt phẳng " + "để vẽ trắc dọc tự nhiên:\n");

            // draw 100 profiles
            String i = "Enter";
            while (i == "Enter")
            {


                // start transantion
                using (Transaction tr = A.Db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // get alignment for the profiles
                        ObjectId alignmentId = UserInput.GAlignmentId("\n Chọn tim đường " + "để vẽ trắc dọc: \n");

                        // get point of the first profiles
                        Point3d basePoint = UserInput.GPoint("\n Chọn vị trí điểm" + " đặt trắc dọc:\n");


                        // get layer ID, surface ID, profiles style, profiles label, profile view style for the profiles 
                        Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        ObjectId layerID = alignment.LayerId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        ObjectId profileStyleId = A.Cdoc.Styles.ProfileStyles["0.TN"];
                        ObjectId profileLabelSetId = A.Cdoc.Styles.LabelSetStyles.ProfileLabelSetStyles["ĐTN (đường TN)"];
                        ObjectId profileViewStyleId = A.Cdoc.Styles.ProfileViewStyles["TDTN GT 1-1000"];
                        ObjectId profileBandStyleId = A.Cdoc.Styles.ProfileViewBandSetStyles["TRẮC DỌC DO THI"];

                        // create the profiles from surface and profile view
                        CivSurface? surface = tr.GetObject(surfaceId, OpenMode.ForRead) as CivSurface;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        string profileName = surface.Name + "-" + alignment.Name;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        ObjectId profilesId = Profile.CreateFromSurface(profileName, alignmentId, surfaceId, layerID, profileStyleId, profileLabelSetId);
                        ObjectId profileViewId = ProfileView.Create(alignmentId, basePoint, alignment.Name, profileBandStyleId, profileViewStyleId);
                        tr.Commit();
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception e)
                    {
                        A.Ed.WriteMessage(e.Message);

                    }
                }
                i = UserInput.GStopWithESC();
            }
        }

        [CommandMethod("CTP_VeTracDoc_TuNhien_TatCaTuyen")]
        public static void CTPVeTracDocTuNhienTatCaTuyen()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here
                // get alignment
                ObjectId surfaceId = UserInput.GSurfaceId("\n Chọn mặt phẳng " + "để vẽ trắc dọc tự nhiên:\n");
                ObjectIdCollection alignmentIds = A.Cdoc.GetAlignmentIds();
                Point3d basePoint = UserInput.GPoint("\n Chọn vị trí điểm" + " đặt trắc dọc:\n");
                int khoangCach = UserInput.GInt("Nhập khoảng cách giữa các trắc dọc:(300) \n");
                // Draw all profiles
                int x = 0;
                foreach (ObjectId alignmentId in alignmentIds)
                {
                    Alignment? alignment1 = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (alignment1.AlignmentType == AlignmentType.Centerline)
                    {
                        x++;
                        Point3d basePointNext = new(basePoint.X, basePoint.Y - x * khoangCach, basePoint.Z);

                        // get layer ID, 1st surface ID, 1st profiles style, 1st profiles label, 1st profile view style, 
                        Autodesk.Civil.DatabaseServices.Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Alignment;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        ObjectId layerID = alignment.LayerId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        ObjectId profileStyleId = A.Cdoc.Styles.ProfileStyles["0.TN"];
                        ObjectId profileLabelSetId = A.Cdoc.Styles.LabelSetStyles.ProfileLabelSetStyles["_No Labels"];
                        ObjectId profileViewStyleId = A.Cdoc.Styles.ProfileViewStyles["TDTN GT 1-1000"];
                        ObjectId profileBandStyleId = A.Cdoc.Styles.ProfileViewBandSetStyles["TRẮC DỌC DO THI"];

                        // create the profiles from surface and profile view
                        CivSurface? surface = tr.GetObject(surfaceId, OpenMode.ForRead) as CivSurface;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        string profileName = surface.Name + "-" + alignment.Name;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        ObjectId profilesId = Profile.CreateFromSurface(profileName, alignmentId, surfaceId, layerID, profileStyleId, profileLabelSetId);
                        ObjectId profileViewId = ProfileView.Create(alignmentId, basePointNext, alignment.Name, profileBandStyleId, profileViewStyleId);
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

        [CommandMethod("CTP_Fix_DuongTuNhien_TheoCoc")]
        public static void CTPFixDuongTuNhienTheoCoc()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here
                ObjectId profileId = UserInput.GProfileId("\n Chọn đường tự nhiên " + "để sửa theo cọc: ");
                Profile? profile = tr.GetObject(profileId, OpenMode.ForWrite) as Profile;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                profile.StyleName = "Existing Ground Profile (KHONG IN)";
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                ObjectId alignmentId = profile.AlignmentId;
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;

                // get stations of samplelines
#pragma warning disable CS8604 // Possible null reference argument.
                UtilitiesC3D.GCoordinatePointFromAlignment(alignment, 0, out string[] samplelineName, out string[] eastings, out string[] northings, out string[] stations);
#pragma warning restore CS8604 // Possible null reference argument.

                // Get profile start and end stations for validation
                double profileStartStation = profile.StartingStation;
                double profileEndStation = profile.EndingStation;
                A.Ed.WriteMessage($"\n Profile range: {profileStartStation:F3} to {profileEndStation:F3}");

                // Filter and sort unique stations within profile range
                var validStations = new List<double>();
                var uniqueStations = new HashSet<double>();

                foreach (string stationStr in stations)
                {
                    if (double.TryParse(stationStr, out double stationValue))
                    {
                        // Only include stations within profile range and avoid duplicates
                        if (stationValue >= profileStartStation && stationValue <= profileEndStation)
                        {
                            // Round to avoid floating point precision issues
                            double roundedStation = Math.Round(stationValue, 2);
                            if (uniqueStations.Add(roundedStation))
                            {
                                validStations.Add(roundedStation);
                            }
                        }
                    }
                }

                // Sort stations
                validStations.Sort();
                A.Ed.WriteMessage($"\n Số station hợp lệ: {validStations.Count}");

                // Display valid stations
                A.Ed.WriteMessage("\n Lý trình các cọc hợp lệ: ");
                foreach (double station in validStations)
                {
                    A.Ed.WriteMessage(station.ToString("F2") + ", ");
                }

                //get elevation for valid stations
                var stationElevations = new List<(double station, double elevation)>();
                
                for (int i = 0; i < validStations.Count; i++)
                {
                    try
                    {
                        double elevation = profile.ElevationAt(validStations[i]);
                        stationElevations.Add((validStations[i], elevation));
                        A.Ed.WriteMessage($"\n Lý trình: {validStations[i]:F2} Cao độ: {elevation:F3}");
                    }
                    catch (System.ArgumentException)
                    {
                        A.Ed.WriteMessage($"\n Lỗi lấy cao độ tại station {validStations[i]:F2}: Value does not fall within the expected range.");
                        // Skip this station instead of using interpolated value
                        continue;
                    }
                }

                if (stationElevations.Count == 0)
                {
                    A.Ed.WriteMessage("\n Không có station hợp lệ nào để tạo profile.");
                    return;
                }

                //create a fix profile
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId layerID = alignment.LayerId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                ObjectId profileStyleId = A.Cdoc.Styles.ProfileStyles["0.TN"];
                ObjectId profileLabelSetId = A.Cdoc.Styles.LabelSetStyles.ProfileLabelSetStyles["_No Labels"];
                ObjectId fixProfileId = Profile.CreateByLayout("Fix-" + profile.Name, alignmentId, layerID, profileStyleId, profileLabelSetId);
                Profile? fixProfile = tr.GetObject(fixProfileId, OpenMode.ForWrite) as Profile;

                // Add PVIs with additional validation
                int successCount = 0;
                foreach (var (station, elevation) in stationElevations)
                {
                    try
                    {
                        // Additional check: ensure station is within alignment range
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        if (station >= alignment.StartingStation && station <= alignment.EndingStation)
                        {
                            fixProfile.PVIs.AddPVI(station, elevation);
                            successCount++;
                        }
                        else
                        {
                            A.Ed.WriteMessage($"\n Station {station:F2} nằm ngoài phạm vi alignment.");
                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                    catch (System.ArgumentException ex)
                    {
                        A.Ed.WriteMessage($"\n Lỗi thêm PVI tại station {station:F2}: {ex.Message}");
                    }
                }

                A.Ed.WriteMessage($"\n Đã tạo thành công {successCount} PVI trong tổng số {stationElevations.Count} station.");

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTP_GanNhanNutGiao_LenTracDoc")]
        public static void CTPGanNhanNutGiaoLenTracDoc()
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
                ObjectId cogoPointId = UserInput.GCogoPointId("\n Chọn cogo point " + " thuộc nhóm điểm cần gắn nhãn nút giao lên trắc dọc: \n");
                
                // Validate cogo point selection
                if (cogoPointId == ObjectId.Null)
                {
                    A.Ed.WriteMessage("\n Lỗi: Không chọn được cogo point hợp lệ.");
                    return;
                }
                
                Double saiSo = UserInput.GDouble("\n Nhập sai số điểm so với tuyến (0.02):");
                CogoPoint? cogoPoint = tr.GetObject(cogoPointId, OpenMode.ForWrite) as CogoPoint;
                
                if (cogoPoint == null)
                {
                    A.Ed.WriteMessage("\n Lỗi: Không thể truy cập cogo point.");
                    return;
                }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId pointGroupId = cogoPoint.PrimaryPointGroupId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                
                // Validate point group
                if (pointGroupId == ObjectId.Null)
                {
                    A.Ed.WriteMessage("\n Lỗi: Cogo point không thuộc về nhóm điểm nào.");
                    return;
                }

                // Get points once from point group
                ObjectIdCollection pointIds = UtilitiesC3D.GPointIdsFromPointGroup(pointGroupId);
                if (pointIds.Count == 0)
                {
                    A.Ed.WriteMessage("\n Cảnh báo: Không tìm thấy điểm nào trong nhóm điểm.");
                    return;
                }

                // Commit transaction once before the loop
                tr.Commit();

                // Process multiple ProfileViews in a loop - UPDATED LOGIC
                string continueProcess = "Enter";
                int processedCount = 0;
                
                while (continueProcess == "Enter")
                {
                    try
                    {
                        using (Transaction innerTr = A.Db.TransactionManager.StartTransaction())
                        {
                            // *** QUY TRÌNH CHÍNH XÁC: Chọn ProfileView trước ***
                            ObjectId profileViewId = UserInput.GProfileViewId("Chọn trắc dọc cần gắn nhãn: \n");
                            
                            // Validate profile view selection
                            if (profileViewId == ObjectId.Null)
                            {
                                A.Ed.WriteMessage("\n Người dùng hủy chọn ProfileView.");
                                innerTr.Commit();
                                break;
                            }
                            
                            // Get ProfileView and its corresponding Alignment
                            ProfileView? profileView = innerTr.GetObject(profileViewId, OpenMode.ForRead) as ProfileView;
                            if (profileView == null)
                            {
                                A.Ed.WriteMessage("\n Lỗi: Profile view không hợp lệ.");
                                innerTr.Commit();
                                continue;
                            }

                            // Get alignment from ProfileView
                            ObjectId alignmentId = profileView.AlignmentId;
                            Alignment? alignment = innerTr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                            
                            if (alignment == null)
                            {
                                A.Ed.WriteMessage("\n Lỗi: Không thể lấy alignment từ ProfileView.");
                                innerTr.Commit();
                                continue;
                            }

                            A.Ed.WriteMessage($"\n Đang xử lý ProfileView: {profileView.Name}");
                            A.Ed.WriteMessage($"\n Alignment tương ứng: {alignment.Name}");
                            
                            // *** KIỂM TRA CHÍNH XÁC CÁC ĐIỂM TRÊN ALIGNMENT ***
                            ObjectIdCollection validPointIds = new ObjectIdCollection();
                            int totalPoints = 0;
                            int validPoints = 0;
                            
                            A.Ed.WriteMessage($"\n=== KIỂM TRA {pointIds.Count} ĐIỂM TRONG NHÓM ===");
                            
                            foreach (ObjectId pointId in pointIds)
                            {
                                totalPoints++;
                                CogoPoint? point = innerTr.GetObject(pointId, OpenMode.ForRead) as CogoPoint;
                                if (point == null) continue;

                                double station = 0;
                                double offset = 0;
                                
                                try
                                {
                                    // Tính station và offset của điểm so với alignment
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                    alignment.StationOffset(point.Easting, point.Northing, ref station, ref offset);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                                    
                                    double absOffset = Math.Abs(offset);
                                    
                                    A.Ed.WriteMessage($"\n Điểm {point.PointName}: Station={station:F3}, Offset={offset:F3}, |Offset|={absOffset:F3}");
                                    
                                    // Kiểm tra khoảng cách nhỏ hơn sai số cho phép
                                    if (absOffset <= saiSo)
                                    {
                                        validPointIds.Add(pointId);
                                        validPoints++;
                                        A.Ed.WriteMessage($" -> HỢP LỆ (≤ {saiSo})");
                                    }
                                    else
                                    {
                                        A.Ed.WriteMessage($" -> LOẠI BỎ (> {saiSo})");
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    A.Ed.WriteMessage($"\n Lỗi tính station/offset cho điểm {point.PointName}: {ex.Message}");
                                }
                            }
                            
                            A.Ed.WriteMessage($"\n=== KẾT QUẢ LỌC ===");
                            A.Ed.WriteMessage($"\n Tổng điểm kiểm tra: {totalPoints}");
                            A.Ed.WriteMessage($"\n Điểm hợp lệ (≤ {saiSo}m): {validPoints}");
                            A.Ed.WriteMessage($"\n Điểm bị loại bỏ: {totalPoints - validPoints}");
                            
                            if (validPointIds.Count == 0)
                            {
                                A.Ed.WriteMessage($"\n CẢNH BÁO: Không có điểm nào trong phạm vi sai số {saiSo}m so với alignment '{alignment.Name}'");
                                innerTr.Commit();
                                continue;
                            }
                            
                            // Set selection and execute command với các điểm đã được lọc chính xác
                            try
                            {
                                ObjectId[] pointArray = UserInput.ConvertObjectIdCollectionToArray(validPointIds);
                                A.Ed.SetImpliedSelection(pointArray);
                                
                                // Commit inner transaction before using Editor.Command
                                innerTr.Commit();
                                
                                // Execute the AutoCAD command
                                A.Ed.WriteMessage($"\n Đang thực hiện lệnh gắn {validPointIds.Count} điểm lên ProfileView...");
                                A.Ed.Command("_AECCPROJECTOBJECTSTOPROF", profileViewId);
                                
                                A.Ed.WriteMessage($"\n ✓ ĐÃ GẮN THÀNH CÔNG {validPointIds.Count} điểm lên ProfileView '{profileView.Name}'");
                                processedCount++;
                            }
                            catch (Autodesk.AutoCAD.Runtime.Exception ex)
                            {
                                A.Ed.WriteMessage($"\n Lỗi khi thực hiện lệnh project objects to profile: {ex.Message}");
                                innerTr.Commit();
                            }
                        }
                        
                        // Ask user if they want to continue with another ProfileView
                        continueProcess = UserInput.GStopWithESC();
                        
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\n Lỗi trong vòng lặp xử lý: {ex.Message}");
                        continueProcess = "Cancel"; // Exit loop on error
                    }
                }
                
                A.Ed.WriteMessage($"\n=== HOÀN THÀNH ===");
                A.Ed.WriteMessage($"\n Đã xử lý thành công: {processedCount} ProfileView(s)");
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\n Lỗi: {e.Message}");
            }
        }

        /* lệnh tạo cogo point từ điểm PVI trên trắc dọc
         */
        [CommandMethod("CTP_TaoCogoPointTuPVI")]
        public static void CTPTaoCogoPointTuPVI()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here
                //get profile
                ObjectId profileId = UserInput.GProfileId("\n Chọn trắc dọc " + "để tạo điểm cogo point từ PVI: \n");
                Profile? profile = tr.GetObject(profileId, OpenMode.ForWrite) as Profile;

                //get alignment
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                ObjectId alignmentId = profile.AlignmentId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;

                ProfilePVICollection pviIds = profile.PVIs;
                foreach (ProfilePVI pviId in pviIds)
                {
                    //lấy lý trình của điểm PVI
                    double station = pviId.RawStation;
                    double elevation = pviId.Elevation;

                    // set eathings, northings
                    double easting = 0;
                    double northing = 0;

                    //lấy tọa độ X, Y của điểm đựa vào lý trình trên tuyến
                    // ddihn
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    alignment.PointLocation(station, 0, ref easting, ref northing);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                    //tạo điểm cogo point
                    UtilitiesC3D.CreateCogoPointFromPoint3D(new Point3d(easting, northing, elevation), profile.Name);

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

