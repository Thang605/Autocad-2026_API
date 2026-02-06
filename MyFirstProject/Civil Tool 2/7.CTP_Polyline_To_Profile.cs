using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using MyFirstProject.Extensions;
using MyFirstProject.Civil_Tool_2;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(Civil3DCsharp.CTPPolylineToProfile))]

namespace Civil3DCsharp
{
    /// <summary>
    /// Lệnh chuyển đổi Polyline (vẽ trên trắc dọc) thành Profile
    /// </summary>
    public class CTPPolylineToProfile
    {
        [CommandMethod("CTP_Polyline_To_Profile")]
        public static void PolylineToProfile()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                // 1. Chọn ProfileView
                A.Ed.WriteMessage("\n=== CHUYỂN ĐỔI POLYLINE THÀNH PROFILE ===");
                ObjectId profileViewId = UserInput.GProfileViewId("\n Chọn Profile View (trắc dọc) chứa polyline:");
                
                if (profileViewId == ObjectId.Null)
                {
                    A.Ed.WriteMessage("\n Đã hủy: Không chọn Profile View.");
                    return;
                }

                ProfileView? profileView = tr.GetObject(profileViewId, OpenMode.ForWrite) as ProfileView;
                if (profileView == null)
                {
                    A.Ed.WriteMessage("\n Lỗi: Không thể lấy Profile View.");
                    return;
                }

                // 2. Lấy alignment từ ProfileView
                ObjectId alignmentId = profileView.AlignmentId;
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
                if (alignment == null)
                {
                    A.Ed.WriteMessage("\n Lỗi: Không thể lấy Alignment từ Profile View.");
                    return;
                }

                A.Ed.WriteMessage($"\n Profile View: {profileView.Name}");
                A.Ed.WriteMessage($"\n Alignment: {alignment.Name}");

                // 3. Chọn Polyline trên ProfileView
                ObjectId polylineId = SelectPolyline("\n Chọn Polyline để chuyển thành Profile:");
                if (polylineId == ObjectId.Null)
                {
                    A.Ed.WriteMessage("\n Đã hủy: Không chọn Polyline.");
                    return;
                }

                // 4. Lấy các điểm từ Polyline và chuyển đổi về Station/Elevation
                List<(double station, double elevation)> stationElevations = GetStationElevationsFromPolyline(
                    tr, profileView, polylineId);

                if (stationElevations.Count < 2)
                {
                    A.Ed.WriteMessage("\n Lỗi: Polyline phải có ít nhất 2 điểm.");
                    return;
                }

                A.Ed.WriteMessage($"\n Số điểm lấy từ Polyline: {stationElevations.Count}");

                // 5. Nhập tên cho Profile mới
                string profileName = UserInput.GString("\n Nhập tên Profile mới (Enter để dùng tên mặc định):");
                if (string.IsNullOrWhiteSpace(profileName))
                {
                    profileName = $"Profile từ Polyline - {DateTime.Now:HHmmss}";
                }

                // Kiểm tra tên profile đã tồn tại chưa
                ObjectIdCollection existingProfileIds = alignment.GetProfileIds();
                int suffix = 0;
                string originalName = profileName;
                while (IsProfileNameExists(tr, existingProfileIds, profileName))
                {
                    suffix++;
                    profileName = $"{originalName}_{suffix}";
                }

                // 6. Lấy style cho Profile mới
                ObjectId layerID = alignment.LayerId;
                ObjectId profileStyleId = GetDefaultProfileStyle();
                ObjectId profileLabelSetId = GetDefaultProfileLabelSet();

                // 7. Tạo Profile mới bằng CreateByLayout
                ObjectId newProfileId = Profile.CreateByLayout(profileName, alignmentId, layerID, profileStyleId, profileLabelSetId);
                Profile? newProfile = tr.GetObject(newProfileId, OpenMode.ForWrite) as Profile;

                if (newProfile == null)
                {
                    A.Ed.WriteMessage("\n Lỗi: Không thể tạo Profile mới.");
                    return;
                }

                // 8. Thêm các điểm PVI vào Profile mới
                int addedCount = 0;
                foreach (var (station, elevation) in stationElevations)
                {
                    try
                    {
                        // Kiểm tra station nằm trong phạm vi alignment
                        if (station >= alignment.StartingStation && station <= alignment.EndingStation)
                        {
                            newProfile.PVIs.AddPVI(station, elevation);
                            addedCount++;
                        }
                        else
                        {
                            A.Ed.WriteMessage($"\n Bỏ qua điểm tại station {station:F2} (ngoài phạm vi alignment)");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\n Lỗi thêm PVI tại station {station:F2}: {ex.Message}");
                    }
                }

                A.Ed.WriteMessage($"\n\n=== KẾT QUẢ ===");
                A.Ed.WriteMessage($"\n Profile mới đã tạo: {newProfile.Name}");
                A.Ed.WriteMessage($"\n Số điểm PVI đã thêm: {addedCount}/{stationElevations.Count}");
                A.Ed.WriteMessage($"\n Station bắt đầu: {stationElevations[0].station:F2}");
                A.Ed.WriteMessage($"\n Station kết thúc: {stationElevations[^1].station:F2}");

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\n Lỗi: {e.Message}");
            }
        }

        /// <summary>
        /// Lệnh điều chỉnh Profile hiện có theo Polyline
        /// </summary>
        [CommandMethod("CTP_Adjust_Profile_By_Polyline")]
        public static void AdjustProfileByPolyline()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                // 1. Chọn ProfileView
                A.Ed.WriteMessage("\n=== ĐIỀU CHỈNH PROFILE THEO POLYLINE ===");
                ObjectId profileViewId = UserInput.GProfileViewId("\n Chọn Profile View (trắc dọc):");

                if (profileViewId == ObjectId.Null)
                {
                    A.Ed.WriteMessage("\n Đã hủy: Không chọn Profile View.");
                    return;
                }

                ProfileView? profileView = tr.GetObject(profileViewId, OpenMode.ForWrite) as ProfileView;
                if (profileView == null)
                {
                    A.Ed.WriteMessage("\n Lỗi: Không thể lấy Profile View.");
                    return;
                }

                // 2. Lấy alignment và profiles từ ProfileView
                ObjectId alignmentId = profileView.AlignmentId;
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                if (alignment == null)
                {
                    A.Ed.WriteMessage("\n Lỗi: Không thể lấy Alignment từ Profile View.");
                    return;
                }

                // 3. Lấy danh sách profiles
                ObjectIdCollection profileIds = alignment.GetProfileIds();
                if (profileIds.Count == 0)
                {
                    A.Ed.WriteMessage("\n Lỗi: Alignment không có Profile nào.");
                    return;
                }

                List<(ObjectId id, string name, ProfileType type)> profileList = new();
                foreach (ObjectId profileId in profileIds)
                {
                    Profile? profile = tr.GetObject(profileId, OpenMode.ForRead) as Profile;
                    if (profile != null)
                    {
                        profileList.Add((profileId, profile.Name, profile.ProfileType));
                    }
                }

                // 4. Hiển thị form để chọn profile và tùy chọn
                ObjectId targetProfileId;
                int adjustOption;
                
                using (var form = new AdjustProfileForm(profileView.Name, alignment.Name, profileList))
                {
                    var result = Application.ShowModalDialog(form);
                    if (result != DialogResult.OK || !form.FormAccepted)
                    {
                        A.Ed.WriteMessage("\n Đã hủy.");
                        return;
                    }
                    
                    targetProfileId = form.SelectedProfileId;
                    adjustOption = form.AdjustOption;
                }

                Profile? targetProfile = tr.GetObject(targetProfileId, OpenMode.ForWrite) as Profile;
                if (targetProfile == null)
                {
                    A.Ed.WriteMessage("\n Lỗi: Không thể mở Profile để chỉnh sửa.");
                    return;
                }

                A.Ed.WriteMessage($"\n Đã chọn Profile: {targetProfile.Name}");

                // 5. Chọn Polyline trên ProfileView
                ObjectId polylineId = SelectPolyline("\n Chọn Polyline làm mẫu điều chỉnh:");
                if (polylineId == ObjectId.Null)
                {
                    A.Ed.WriteMessage("\n Đã hủy: Không chọn Polyline.");
                    return;
                }

                // 6. Lấy các điểm từ Polyline và chuyển đổi về Station/Elevation
                List<(double station, double elevation)> stationElevations = GetStationElevationsFromPolyline(
                    tr, profileView, polylineId);

                if (stationElevations.Count < 2)
                {
                    A.Ed.WriteMessage("\n Lỗi: Polyline phải có ít nhất 2 điểm.");
                    return;
                }

                A.Ed.WriteMessage($"\n Số điểm từ Polyline: {stationElevations.Count}");

                // 7. Thực hiện điều chỉnh
                int removedCount = 0;
                int addedCount = 0;

                double polylineStartStation = stationElevations[0].station;
                double polylineEndStation = stationElevations[^1].station;

                switch (adjustOption)
                {
                    case 1: // Thay thế toàn bộ
                        // Xóa tất cả PVI cũ
                        while (targetProfile.PVIs.Count > 0)
                        {
                            targetProfile.PVIs.RemoveAt(0);
                            removedCount++;
                        }
                        break;

                    case 2: // Thay thế trong phạm vi
                        // Xóa các PVI trong phạm vi polyline
                        List<int> indicesToRemove = new();
                        for (int i = 0; i < targetProfile.PVIs.Count; i++)
                        {
                            ProfilePVI pvi = targetProfile.PVIs[i];
                            if (pvi.Station >= polylineStartStation && pvi.Station <= polylineEndStation)
                            {
                                indicesToRemove.Add(i);
                            }
                        }
                        // Xóa từ cuối về đầu để không ảnh hưởng index
                        for (int i = indicesToRemove.Count - 1; i >= 0; i--)
                        {
                            targetProfile.PVIs.RemoveAt(indicesToRemove[i]);
                            removedCount++;
                        }
                        break;

                    case 3: // Thêm mới (không xóa gì)
                        break;
                }

                // 8. Thêm các điểm PVI mới
                foreach (var (station, elevation) in stationElevations)
                {
                    try
                    {
                        if (station >= alignment.StartingStation && station <= alignment.EndingStation)
                        {
                            targetProfile.PVIs.AddPVI(station, elevation);
                            addedCount++;
                        }
                        else
                        {
                            A.Ed.WriteMessage($"\n Bỏ qua điểm tại station {station:F2} (ngoài phạm vi alignment)");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\n Lỗi thêm PVI tại station {station:F2}: {ex.Message}");
                    }
                }

                A.Ed.WriteMessage($"\n\n=== KẾT QUẢ ===");
                A.Ed.WriteMessage($"\n Profile đã điều chỉnh: {targetProfile.Name}");
                A.Ed.WriteMessage($"\n Số PVI đã xóa: {removedCount}");
                A.Ed.WriteMessage($"\n Số PVI đã thêm: {addedCount}/{stationElevations.Count}");
                A.Ed.WriteMessage($"\n Tổng số PVI hiện tại: {targetProfile.PVIs.Count}");

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\n Lỗi: {e.Message}");
            }
        }

        /// <summary>
        /// Chọn Polyline hoặc Polyline2D
        /// </summary>
        private static ObjectId SelectPolyline(string prompt)
        {
            PromptEntityOptions options = new PromptEntityOptions(prompt);
            options.SetRejectMessage("\n Đối tượng không phải là Polyline.");
            options.AddAllowedClass(typeof(Polyline), true);
            options.AddAllowedClass(typeof(Polyline2d), true);
            options.AddAllowedClass(typeof(Polyline3d), true);

            PromptEntityResult result = A.Ed.GetEntity(options);
            if (result.Status == PromptStatus.OK)
            {
                return result.ObjectId;
            }
            return ObjectId.Null;
        }

        /// <summary>
        /// Lấy danh sách Station/Elevation từ các vertices của Polyline
        /// </summary>
        private static List<(double station, double elevation)> GetStationElevationsFromPolyline(
            Transaction tr, ProfileView profileView, ObjectId polylineId)
        {
            var result = new List<(double station, double elevation)>();
            var entity = tr.GetObject(polylineId, OpenMode.ForRead);

            List<Point3d> vertices = new List<Point3d>();

            if (entity is Polyline polyline)
            {
                for (int i = 0; i < polyline.NumberOfVertices; i++)
                {
                    vertices.Add(polyline.GetPoint3dAt(i));
                }
            }
            else if (entity is Polyline2d polyline2d)
            {
                foreach (ObjectId vertexId in polyline2d)
                {
                    Vertex2d? vertex = tr.GetObject(vertexId, OpenMode.ForRead) as Vertex2d;
                    if (vertex != null)
                    {
                        vertices.Add(vertex.Position);
                    }
                }
            }
            else if (entity is Polyline3d polyline3d)
            {
                foreach (ObjectId vertexId in polyline3d)
                {
                    PolylineVertex3d? vertex = tr.GetObject(vertexId, OpenMode.ForRead) as PolylineVertex3d;
                    if (vertex != null)
                    {
                        vertices.Add(vertex.Position);
                    }
                }
            }

            // Chuyển đổi tọa độ X,Y thành Station,Elevation
            foreach (Point3d point in vertices)
            {
                try
                {
                    double station = 0;
                    double elevation = 0;
                    profileView.FindStationAndElevationAtXY(point.X, point.Y, ref station, ref elevation);
                    result.Add((station, elevation));
                }
                catch (System.Exception ex)
                {
                    A.Ed.WriteMessage($"\n Cảnh báo: Không thể chuyển đổi điểm ({point.X:F2}, {point.Y:F2}): {ex.Message}");
                }
            }

            // Sắp xếp theo station tăng dần
            result.Sort((a, b) => a.station.CompareTo(b.station));

            return result;
        }

        /// <summary>
        /// Kiểm tra tên profile đã tồn tại chưa
        /// </summary>
        private static bool IsProfileNameExists(Transaction tr, ObjectIdCollection profileIds, string name)
        {
            foreach (ObjectId profileId in profileIds)
            {
                Profile? profile = tr.GetObject(profileId, OpenMode.ForRead) as Profile;
                if (profile != null && profile.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Lấy Profile Style mặc định
        /// </summary>
        private static ObjectId GetDefaultProfileStyle()
        {
            try
            {
                // Thử các style phổ biến
                string[] styleNames = { "Design Profile", "0.TK", "Layout Profile", "Standard" };
                foreach (string styleName in styleNames)
                {
                    try
                    {
                        return A.Cdoc.Styles.ProfileStyles[styleName];
                    }
                    catch { }
                }

                // Nếu không có, lấy style đầu tiên
                if (A.Cdoc.Styles.ProfileStyles.Count > 0)
                {
                    foreach (ObjectId styleId in A.Cdoc.Styles.ProfileStyles)
                    {
                        return styleId;
                    }
                }
            }
            catch { }

            return ObjectId.Null;
        }

        /// <summary>
        /// Lấy Profile Label Set mặc định
        /// </summary>
        private static ObjectId GetDefaultProfileLabelSet()
        {
            try
            {
                // Thử các label set phổ biến
                string[] labelSetNames = { "_No Labels", "Standard", "Complete" };
                foreach (string labelSetName in labelSetNames)
                {
                    try
                    {
                        return A.Cdoc.Styles.LabelSetStyles.ProfileLabelSetStyles[labelSetName];
                    }
                    catch { }
                }

                // Nếu không có, lấy label set đầu tiên
                if (A.Cdoc.Styles.LabelSetStyles.ProfileLabelSetStyles.Count > 0)
                {
                    foreach (ObjectId labelSetId in A.Cdoc.Styles.LabelSetStyles.ProfileLabelSetStyles)
                    {
                        return labelSetId;
                    }
                }
            }
            catch { }

            return ObjectId.Null;
        }
    }
}
