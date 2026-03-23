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
            // 1. Chọn ProfileView (chỉ 1 lần)
            A.Ed.WriteMessage("\n=== CHUYỂN ĐỔI POLYLINE THÀNH PROFILE ===");

            ObjectId profileViewId;
            ObjectId alignmentId;
            string profileViewName;
            string alignmentName;

            using (Transaction trInit = A.Db.TransactionManager.StartTransaction())
            {
                profileViewId = UserInput.GProfileViewId("\n Chọn Profile View (trắc dọc) chứa polyline:");

                if (profileViewId == ObjectId.Null)
                {
                    A.Ed.WriteMessage("\n Đã hủy: Không chọn Profile View.");
                    return;
                }

                ProfileView? profileView = trInit.GetObject(profileViewId, OpenMode.ForRead) as ProfileView;
                if (profileView == null)
                {
                    A.Ed.WriteMessage("\n Lỗi: Không thể lấy Profile View.");
                    return;
                }

                alignmentId = profileView.AlignmentId;
                Alignment? alignment = trInit.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                if (alignment == null)
                {
                    A.Ed.WriteMessage("\n Lỗi: Không thể lấy Alignment từ Profile View.");
                    return;
                }

                profileViewName = profileView.Name;
                alignmentName = alignment.Name;
                trInit.Commit();
            }

            A.Ed.WriteMessage($"\n Profile View: {profileViewName}");
            A.Ed.WriteMessage($"\n Alignment: {alignmentName}");

            // 2. Vòng lặp chọn Polyline nhiều lần
            int totalCreated = 0;

            while (true)
            {
                ObjectId polylineId = SelectPolyline($"\n Chọn Polyline để chuyển thành Profile (Enter/Esc để kết thúc) [{totalCreated} đã tạo]:");
                if (polylineId == ObjectId.Null)
                {
                    break; // Thoát vòng lặp khi người dùng hủy chọn
                }

                using Transaction tr = A.Db.TransactionManager.StartTransaction();
                try
                {
                    ProfileView? profileView = tr.GetObject(profileViewId, OpenMode.ForRead) as ProfileView;
                    Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;

                    if (profileView == null || alignment == null)
                    {
                        A.Ed.WriteMessage("\n Lỗi: Không thể lấy Profile View hoặc Alignment.");
                        break;
                    }

                    // 3. Lấy các điểm từ Polyline và chuyển đổi về Station/Elevation
                    List<(double station, double elevation)> stationElevations = GetStationElevationsFromPolyline(
                        tr, profileView, polylineId);

                    if (stationElevations.Count < 2)
                    {
                        A.Ed.WriteMessage("\n Lỗi: Polyline phải có ít nhất 2 điểm. Chọn polyline khác.");
                        tr.Commit();
                        continue;
                    }

                    A.Ed.WriteMessage($"\n Số điểm lấy từ Polyline: {stationElevations.Count}");

                    // 4. Nhập tên cho Profile mới
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

                    // 5. Lấy style cho Profile mới
                    ObjectId layerID = alignment.LayerId;
                    ObjectId profileStyleId = GetDefaultProfileStyle();
                    ObjectId profileLabelSetId = GetDefaultProfileLabelSet();

                    // 6. Tạo Profile mới bằng CreateByLayout
                    ObjectId newProfileId = Profile.CreateByLayout(profileName, alignmentId, layerID, profileStyleId, profileLabelSetId);
                    Profile? newProfile = tr.GetObject(newProfileId, OpenMode.ForWrite) as Profile;

                    if (newProfile == null)
                    {
                        A.Ed.WriteMessage("\n Lỗi: Không thể tạo Profile mới.");
                        tr.Commit();
                        continue;
                    }

                    // 7. Thêm các điểm PVI vào Profile mới
                    int addedCount = 0;
                    foreach (var (station, elevation) in stationElevations)
                    {
                        try
                        {
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

                    totalCreated++;
                    A.Ed.WriteMessage($"\n --- Profile #{totalCreated} ---");
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

            A.Ed.WriteMessage($"\n\n=== KẾT QUẢ: Đã tạo {totalCreated} Profile từ Polyline ===");
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

                // Danh sách PVI cần xóa (lưu lại station và elevation để tìm lại PVI vì index sẽ thay đổi)
                List<(double station, double elevation)> pvisToRemove = new();

                if (adjustOption == 1) // Thay thế toàn bộ
                {
                    foreach (ProfilePVI pvi in targetProfile.PVIs)
                    {
                        pvisToRemove.Add((pvi.RawStation, pvi.Elevation));
                    }
                }
                else if (adjustOption == 2) // Thay thế trong phạm vi
                {
                    foreach (ProfilePVI pvi in targetProfile.PVIs)
                    {
                        if (pvi.RawStation >= polylineStartStation && pvi.RawStation <= polylineEndStation)
                        {
                            pvisToRemove.Add((pvi.RawStation, pvi.Elevation));
                        }
                    }
                }

                // 8. Thêm các điểm PVI mới TRƯỚC (để đảm bảo profile luôn hợp lệ)
                // Lưu ý: Nếu PVI mới trùng station với PVI cũ, AddPVI sẽ tự động cập nhật elevation
                foreach (var (station, elevation) in stationElevations)
                {
                    try
                    {
                        // Kiểm tra nếu nằm trong giới hạn Alignment (cho phép mở rộng 1 chút vi sai số)
                        if (station >= alignment.StartingStation - 0.001 && station <= alignment.EndingStation + 0.001)
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

                // 9. Xóa các PVI cũ SAU KHI đã thêm mới
                // Cần lấy lại danh sách PVI hiện tại vì collection đã thay đổi sau khi AddPVI
                // Tuy nhiên, ta chỉ xóa những PVI nằm trong danh sách pvisToRemove đã xác định từ trước
                // VÀ không nằm trong danh sách mới thêm vào (stationElevations) - để tránh xóa nhầm cái vừa update

                // Tạo HashSet các station mới để tra cứu nhanh
                HashSet<double> newStations = new HashSet<double>(
                    stationElevations.Select(x => Math.Round(x.station, 4)));

                // Duyệt ngược để xóa an toàn
                for (int i = targetProfile.PVIs.Count - 1; i >= 0; i--)
                {
                    ProfilePVI pvi = targetProfile.PVIs[i];
                    double currentStation = Math.Round(pvi.RawStation, 4);
                    double currentElev = Math.Round(pvi.Elevation, 4);

                    // Kiểm tra xem PVI này có trong danh sách cần xóa không
                    // So sánh gần đúng
                    bool needsRemoval = false;
                    
                    // Nếu là option 1 (Toàn bộ) -> Xóa nếu không phải là điểm mới
                    if (adjustOption == 1)
                    {
                         // Nếu Station này KHÔNG nằm trong danh sách điểm mới -> XÓA
                         // Nếu Station này CÓ nằm trong danh sách điểm mới -> GIỮ (vì nó là điểm vừa được update/add)
                         if (!newStations.Contains(currentStation))
                         {
                             needsRemoval = true;
                         }
                    }
                    else if (adjustOption == 2) // Trong phạm vi
                    {
                        // Logic tương tự: Chỉ xóa nếu nằm trong phạm vi VÀ không phải là điểm mới
                        if (pvi.RawStation >= polylineStartStation && pvi.RawStation <= polylineEndStation)
                        {
                             if (!newStations.Contains(currentStation))
                             {
                                 needsRemoval = true;
                             }
                        }
                    }

                    if (needsRemoval)
                    {
                        try
                        {
                            targetProfile.PVIs.RemoveAt(i);
                            removedCount++;
                        }
                        catch
                        {
                            // Bỏ qua lỗi nếu không xóa được (ví dụ PVI đầu/cuối còn lại duy nhất)
                        }
                    }
                }

                A.Ed.WriteMessage($"\n\n=== KẾT QUẢ ===");
                A.Ed.WriteMessage($"\n Profile đã điều chỉnh: {targetProfile.Name}");
                A.Ed.WriteMessage($"\n Số PVI đã thêm/cập nhật: {addedCount}/{stationElevations.Count}");
                A.Ed.WriteMessage($"\n Số PVI cũ đã xóa: {removedCount}");
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
