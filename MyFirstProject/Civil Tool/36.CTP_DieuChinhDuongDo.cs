using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using MyFirstProject.Civil_Tool;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;

[assembly: CommandClass(typeof(Civil3DCsharp.CTP_DieuChinhDuongDo_Commands))]

namespace Civil3DCsharp
{
    public class CTP_DieuChinhDuongDo_Commands
    {
        // Ghi nhớ đối tượng và PVI đã chọn giữa các lần thực hiện lệnh
        private static ObjectId _lastProfileViewId = ObjectId.Null;
        private static ObjectId _lastProfileId = ObjectId.Null;
        private static int _lastPvi1Index = -1;
        private static int _lastPvi2Index = -1;

        [CommandMethod("CTP_DieuChinhDuongDo")]
        public void CTP_DieuChinhDuongDo()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                DieuChinhDuongDoForm form = new DieuChinhDuongDoForm();

                // Nạp lại đối tượng đã chọn ở lần thực hiện trước (nếu còn hợp lệ)
                RestoreLastSelectedObjects(db, form);

                // Wire up Pick ProfileView / Profile
                form.btnPickProfileView.Click += (s, e) =>
                {
                    try
                    {
                        using (var interaction = ed.StartUserInteraction(form))
                        {
                            PromptEntityOptions peo = new PromptEntityOptions("\nChọn Profile hoặc Trắc dọc (ProfileView) trên bản vẽ: ");
                            peo.SetRejectMessage("\nĐối tượng chọn phải là Profile hoặc ProfileView!");
                            peo.AddAllowedClass(typeof(ProfileView), true);
                            peo.AddAllowedClass(typeof(Profile), true);

                            PromptEntityResult per = ed.GetEntity(peo);
                            interaction.End();

                            if (per.Status == PromptStatus.OK)
                            {
                                using (Transaction tr = db.TransactionManager.StartTransaction())
                                {
                                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForWrite) as Entity;
                                    if (ent is ProfileView pv)
                                    {
                                        form.ProfileViewId = pv.ObjectId;
                                        form.txtProfileViewName.Text = pv.Name;

                                        // Auto find layout profile on alignment
                                        ObjectId alignId = pv.AlignmentId;
                                        if (alignId.IsValid && !alignId.IsNull)
                                        {
                                            Alignment align = tr.GetObject(alignId, OpenMode.ForWrite) as Alignment;
                                            if (align != null)
                                            {
                                                ObjectId profileId = ObjectId.Null;
                                                foreach (ObjectId profId in align.GetProfileIds())
                                                {
                                                    Profile p = tr.GetObject(profId, OpenMode.ForWrite) as Profile;
                                                    if (p != null && p.ProfileType == ProfileType.FG)
                                                    {
                                                        profileId = profId;
                                                        form.txtProfileName.Text = p.Name;
                                                        break;
                                                    }
                                                }

                                                if (profileId.IsNull && align.GetProfileIds().Count > 0)
                                                {
                                                    profileId = align.GetProfileIds()[0];
                                                    Profile p = tr.GetObject(profileId, OpenMode.ForWrite) as Profile;
                                                    if (p != null) form.txtProfileName.Text = p.Name;
                                                }

                                                form.ProfileId = profileId;
                                            }
                                        }
                                    }
                                    else if (ent is Profile prof)
                                    {
                                        form.ProfileId = prof.ObjectId;
                                        form.txtProfileName.Text = prof.Name;
                                    }

                                    // Reset PVI selections
                                    form.Pvi1Index = -1;
                                    form.Pvi2Index = -1;
                                    form.UpdatePvi1Display();
                                    form.UpdatePvi2Display();
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\nLỗi khi chọn đối tượng: {ex.Message}");
                    }
                };

                // Nút pick 1 điểm trên đoạn Profile để lấy 2 điểm PVI (PVI đầu & PVI sau)
                form.btnPickSegment.Click += (s, e) =>
                {
                    if (form.ProfileId.IsNull || !form.ProfileId.IsValid)
                    {
                        MessageBox.Show("Vui lòng chọn Trắc dọc / Profile trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        using (var interaction = ed.StartUserInteraction(form))
                        {
                            PromptPointOptions ppo = new PromptPointOptions("\nPick 1 điểm trên đoạn Profile để lấy 2 đỉnh PVI: ");
                            PromptPointResult ppr = ed.GetPoint(ppo);
                            interaction.End();

                            if (ppr.Status == PromptStatus.OK)
                            {
                                bool success = FindSegmentPvis(db, form.ProfileId, form.ProfileViewId, ppr.Value, 
                                    out int idx1, out double sta1, out double elev1,
                                    out int idx2, out double sta2, out double elev2);

                                if (success)
                                {
                                    form.Pvi1Index = idx1;
                                    form.Pvi1Station = sta1;
                                    form.Pvi1Elevation = elev1;
                                    form.UpdatePvi1Display();

                                    form.Pvi2Index = idx2;
                                    form.Pvi2Station = sta2;
                                    form.Pvi2Elevation = elev2;
                                    form.UpdatePvi2Display();

                                    // Auto compute slope and distance
                                    double dist = Math.Abs(sta2 - sta1);
                                    if (dist > 0.0001)
                                    {
                                        double slope = ((elev2 - elev1) / (sta2 - sta1)) * 100.0;
                                        form.numSlope.Value = (decimal)Math.Round(slope, 4);
                                        form.numDistance.Value = (decimal)Math.Round(dist, 3);
                                    }

                                    ed.WriteMessage($"\n✅ Đã chọn đoạn Profile: PVI #{idx1} (Sta {sta1:F2}m) -> PVI #{idx2} (Sta {sta2:F2}m)");
                                }
                                else
                                {
                                    ed.WriteMessage("\n⚠️ Không tìm thấy đoạn PVI hợp lệ tại điểm pick.");
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\nLỗi khi pick đoạn Profile: {ex.Message}");
                    }
                };

                // Wire up Pick PVI 1
                form.btnPickPvi1.Click += (s, e) =>
                {
                    if (form.ProfileId.IsNull || !form.ProfileId.IsValid)
                    {
                        MessageBox.Show("Vui lòng chọn Trắc dọc / Profile trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        using (var interaction = ed.StartUserInteraction(form))
                        {
                            PromptPointOptions ppo = new PromptPointOptions("\nPick vị trí PVI 1 (cố định) trên trắc dọc: ");
                            PromptPointResult ppr = ed.GetPoint(ppo);
                            interaction.End();

                            if (ppr.Status == PromptStatus.OK)
                            {
                                int nearestPvi = FindNearestPviIndex(db, form.ProfileId, form.ProfileViewId, ppr.Value, out double sta, out double elev);
                                if (nearestPvi >= 0)
                                {
                                    form.Pvi1Index = nearestPvi;
                                    form.Pvi1Station = sta;
                                    form.Pvi1Elevation = elev;
                                    form.UpdatePvi1Display();
                                    ed.WriteMessage($"\n✅ Đã chọn PVI 1: Index {nearestPvi}, Lý trình: {sta:F2}m, Cao độ: {elev:F3}m");

                                    // Nếu PVI 2 đã chọn -> auto cập nhật L và i
                                    if (form.Pvi2Index >= 0 && Math.Abs(form.Pvi2Station - sta) > 0.0001)
                                    {
                                        double dist = Math.Abs(form.Pvi2Station - sta);
                                        double slope = ((form.Pvi2Elevation - elev) / (form.Pvi2Station - sta)) * 100.0;
                                        form.numDistance.Value = (decimal)Math.Round(dist, 3);
                                        form.numSlope.Value = (decimal)Math.Round(slope, 4);
                                    }
                                }
                                else
                                {
                                    ed.WriteMessage("\n⚠️ Không tìm thấy PVI tương ứng.");
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\nLỗi khi pick PVI 1: {ex.Message}");
                    }
                };

                // Wire up Pick PVI 2
                form.btnPickPvi2.Click += (s, e) =>
                {
                    if (form.ProfileId.IsNull || !form.ProfileId.IsValid)
                    {
                        MessageBox.Show("Vui lòng chọn Trắc dọc / Profile trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        using (var interaction = ed.StartUserInteraction(form))
                        {
                            PromptPointOptions ppo = new PromptPointOptions("\nPick vị trí PVI 2 (thay đổi) trên trắc dọc: ");
                            PromptPointResult ppr = ed.GetPoint(ppo);
                            interaction.End();

                            if (ppr.Status == PromptStatus.OK)
                            {
                                int nearestPvi = FindNearestPviIndex(db, form.ProfileId, form.ProfileViewId, ppr.Value, out double sta, out double elev);
                                if (nearestPvi >= 0)
                                {
                                    form.Pvi2Index = nearestPvi;
                                    form.Pvi2Station = sta;
                                    form.Pvi2Elevation = elev;
                                    form.UpdatePvi2Display();
                                    ed.WriteMessage($"\n✅ Đã chọn PVI 2: Index {nearestPvi}, Lý trình: {sta:F2}m, Cao độ: {elev:F3}m");

                                    // Auto cập nhật L và i từ PVI 1 đến PVI 2
                                    if (form.Pvi1Index >= 0 && Math.Abs(sta - form.Pvi1Station) > 0.0001)
                                    {
                                        double dist = Math.Abs(sta - form.Pvi1Station);
                                        double slope = ((elev - form.Pvi1Elevation) / (sta - form.Pvi1Station)) * 100.0;
                                        form.numDistance.Value = (decimal)Math.Round(dist, 3);
                                        form.numSlope.Value = (decimal)Math.Round(slope, 4);
                                    }
                                }
                                else
                                {
                                    ed.WriteMessage("\n⚠️ Không tìm thấy PVI tương ứng.");
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\nLỗi khi pick PVI 2: {ex.Message}");
                    }
                };

                // Nút Pick điểm thứ 2 để tính cả Khoảng cách L và Dốc i từ PVI 1 cố định
                form.btnPickPoint2.Click += (s, e) =>
                {
                    if (form.ProfileId.IsNull || !form.ProfileId.IsValid)
                    {
                        MessageBox.Show("Vui lòng chọn Trắc dọc / Profile trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (form.Pvi1Index < 0)
                    {
                        MessageBox.Show("Vui lòng chọn PVI 1 (cố định) trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        using (var interaction = ed.StartUserInteraction(form))
                        {
                            PromptPointOptions ppo = new PromptPointOptions("\nPick điểm thứ 2 trên trắc dọc để tính khoảng cách L và dốc i từ PVI 1: ");
                            PromptPointResult ppr = ed.GetPoint(ppo);
                            interaction.End();

                            if (ppr.Status == PromptStatus.OK)
                            {
                                Point3d pt2 = ppr.Value;
                                double s2 = pt2.X;
                                double e2 = pt2.Y;

                                if (!form.ProfileViewId.IsNull && form.ProfileViewId.IsValid)
                                {
                                    using (Transaction tr = db.TransactionManager.StartTransaction())
                                    {
                                        ProfileView pv = tr.GetObject(form.ProfileViewId, OpenMode.ForWrite) as ProfileView;
                                        if (pv != null)
                                        {
                                            pv.FindStationAndElevationAtXY(pt2.X, pt2.Y, ref s2, ref e2);
                                        }
                                    }
                                }

                                double s1 = form.Pvi1Station;
                                double e1 = form.Pvi1Elevation;

                                double dist = Math.Abs(s2 - s1);
                                double slope = (Math.Abs(s2 - s1) > 0.0001) ? ((e2 - e1) / (s2 - s1)) * 100.0 : 0.0;

                                form.numDistance.Value = (decimal)Math.Round(dist, 3);
                                form.numSlope.Value = (decimal)Math.Round(slope, 4);

                                ed.WriteMessage($"\n✅ Đã tính từ PVI 1 ({s1:F2}m, {e1:F3}m) đến điểm thứ 2 ({s2:F2}m, {e2:F3}m): Khoảng cách L = {dist:F3}m, Dốc i = {slope:F4}%");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\nLỗi khi pick điểm thứ 2: {ex.Message}");
                    }
                };

                // Hiển thị Dialog
                DialogResult dr = Application.ShowModalDialog(form);

                if (dr == DialogResult.OK && form.FormAccepted)
                {
                    // Lưu lại vết đối tượng và PVI cho lần gọi sau
                    _lastProfileViewId = form.ProfileViewId;
                    _lastProfileId = form.ProfileId;
                    _lastPvi1Index = form.Pvi1Index;
                    _lastPvi2Index = form.Pvi2Index;

                    ExecuteDieuChinhDuongDo(ed, db, form);
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ Lỗi thực thi lệnh CTP_DieuChinhDuongDo: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void RestoreLastSelectedObjects(Database db, DieuChinhDuongDoForm form)
        {
            if (_lastProfileId.IsNull || !_lastProfileId.IsValid || _lastProfileId.IsErased) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Profile profile = tr.GetObject(_lastProfileId, OpenMode.ForWrite) as Profile;
                    if (profile != null)
                    {
                        form.ProfileId = _lastProfileId;
                        form.txtProfileName.Text = profile.Name;

                        if (!_lastProfileViewId.IsNull && _lastProfileViewId.IsValid && !_lastProfileViewId.IsErased)
                        {
                            ProfileView pv = tr.GetObject(_lastProfileViewId, OpenMode.ForWrite) as ProfileView;
                            if (pv != null)
                            {
                                form.ProfileViewId = _lastProfileViewId;
                                form.txtProfileViewName.Text = pv.Name;
                            }
                        }

                        if (_lastPvi1Index >= 0 && _lastPvi1Index < profile.PVIs.Count)
                        {
                            ProfilePVI p1 = profile.PVIs[_lastPvi1Index];
                            form.Pvi1Index = _lastPvi1Index;
                            form.Pvi1Station = p1.RawStation;
                            form.Pvi1Elevation = p1.Elevation;
                            form.UpdatePvi1Display();
                        }

                        if (_lastPvi2Index >= 0 && _lastPvi2Index < profile.PVIs.Count)
                        {
                            ProfilePVI p2 = profile.PVIs[_lastPvi2Index];
                            form.Pvi2Index = _lastPvi2Index;
                            form.Pvi2Station = p2.RawStation;
                            form.Pvi2Elevation = p2.Elevation;
                            form.UpdatePvi2Display();
                        }
                    }
                }
            }
            catch { }
        }

        private bool FindSegmentPvis(Database db, ObjectId profileId, ObjectId profileViewId, Point3d pt,
            out int idx1, out double sta1, out double elev1,
            out int idx2, out double sta2, out double elev2)
        {
            idx1 = -1; sta1 = 0; elev1 = 0;
            idx2 = -1; sta2 = 0; elev2 = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Profile profile = tr.GetObject(profileId, OpenMode.ForWrite) as Profile;
                if (profile == null || profile.PVIs.Count < 2) return false;

                double searchStation = pt.X;
                double searchElev = pt.Y;

                if (!profileViewId.IsNull && profileViewId.IsValid)
                {
                    ProfileView pv = tr.GetObject(profileViewId, OpenMode.ForWrite) as ProfileView;
                    if (pv != null)
                    {
                        pv.FindStationAndElevationAtXY(pt.X, pt.Y, ref searchStation, ref searchElev);
                    }
                }

                int count = profile.PVIs.Count;

                // Tìm đoạn PVI [i, i+1] chứa searchStation
                if (searchStation <= profile.PVIs[0].RawStation)
                {
                    idx1 = 0;
                    idx2 = 1;
                }
                else if (searchStation >= profile.PVIs[count - 1].RawStation)
                {
                    idx1 = count - 2;
                    idx2 = count - 1;
                }
                else
                {
                    for (int i = 0; i < count - 1; i++)
                    {
                        double sCurrent = profile.PVIs[i].RawStation;
                        double sNext = profile.PVIs[i + 1].RawStation;

                        if (searchStation >= sCurrent && searchStation <= sNext)
                        {
                            idx1 = i;
                            idx2 = i + 1;
                            break;
                        }
                    }
                }

                if (idx1 >= 0 && idx2 >= 0)
                {
                    ProfilePVI p1 = profile.PVIs[idx1];
                    ProfilePVI p2 = profile.PVIs[idx2];
                    sta1 = p1.RawStation; elev1 = p1.Elevation;
                    sta2 = p2.RawStation; elev2 = p2.Elevation;
                    return true;
                }

                return false;
            }
        }

        private int FindNearestPviIndex(Database db, ObjectId profileId, ObjectId profileViewId, Point3d pt, out double station, out double elevation)
        {
            station = 0.0;
            elevation = 0.0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Profile profile = tr.GetObject(profileId, OpenMode.ForWrite) as Profile;
                if (profile == null || profile.PVIs.Count == 0) return -1;

                double searchStation = pt.X;
                double searchElev = pt.Y;

                if (!profileViewId.IsNull && profileViewId.IsValid)
                {
                    ProfileView pv = tr.GetObject(profileViewId, OpenMode.ForWrite) as ProfileView;
                    if (pv != null)
                    {
                        pv.FindStationAndElevationAtXY(pt.X, pt.Y, ref searchStation, ref searchElev);
                    }
                }

                int bestIdx = -1;
                double minDeltaSta = double.MaxValue;

                for (int i = 0; i < profile.PVIs.Count; i++)
                {
                    ProfilePVI pvi = profile.PVIs[i];
                    double delta = Math.Abs(pvi.RawStation - searchStation);
                    if (delta < minDeltaSta)
                    {
                        minDeltaSta = delta;
                        bestIdx = i;
                        station = pvi.RawStation;
                        elevation = pvi.Elevation;
                    }
                }

                return bestIdx;
            }
        }

        private void ExecuteDieuChinhDuongDo(Editor ed, Database db, DieuChinhDuongDoForm form)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Profile profile = tr.GetObject(form.ProfileId, OpenMode.ForWrite) as Profile;
                if (profile == null)
                {
                    ed.WriteMessage("\n❌ Lỗi: Không thể tìm thấy đối tượng Profile.");
                    return;
                }

                int idx1 = form.Pvi1Index;
                int idx2 = form.Pvi2Index;

                if (idx1 < 0 || idx1 >= profile.PVIs.Count || idx2 < 0 || idx2 >= profile.PVIs.Count)
                {
                    ed.WriteMessage("\n❌ Lỗi: Chỉ số PVI không hợp lệ trong Profile.");
                    return;
                }

                ProfilePVI pvi1 = profile.PVIs[idx1];
                ProfilePVI pvi2 = profile.PVIs[idx2];

                double sta1 = pvi1.RawStation;
                double elev1 = pvi1.Elevation;

                double oldSta2 = pvi2.RawStation;
                double oldElev2 = pvi2.Elevation;

                int direction = (idx2 >= idx1) ? 1 : -1;
                double newDist = form.NewDistance;
                double slope = form.SlopePercent;

                double newSta2 = sta1 + direction * newDist;
                double newElev2 = elev1 + direction * (slope / 100.0) * newDist;
                double deltaSta = newSta2 - oldSta2;

                // Cập nhật vị trí PVI 2
                try
                {
                    pvi2.RawStation = newSta2;
                    pvi2.Elevation = newElev2;
                }
                catch
                {
                    profile.PVIs.RemoveAt(idx2);
                    profile.PVIs.AddPVI(newSta2, newElev2);
                }

                // Tịnh tiến các PVI phía sau nếu được bật
                int shiftedCount = 0;
                if (form.ShiftSubsequent && Math.Abs(deltaSta) > 0.0001)
                {
                    if (direction > 0)
                    {
                        for (int k = idx2 + 1; k < profile.PVIs.Count; k++)
                        {
                            ProfilePVI subPvi = profile.PVIs[k];
                            try
                            {
                                subPvi.RawStation = subPvi.RawStation + deltaSta;
                                shiftedCount++;
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        for (int k = idx2 - 1; k >= 0; k--)
                        {
                            ProfilePVI subPvi = profile.PVIs[k];
                            try
                            {
                                subPvi.RawStation = subPvi.RawStation + deltaSta;
                                shiftedCount++;
                            }
                            catch { }
                        }
                    }
                }

                tr.Commit();

                ed.WriteMessage("\n==========================================");
                ed.WriteMessage($"\n🎉 ĐÃ ĐIỀU CHỈNH ĐƯỜNG ĐỎ: {profile.Name}");
                ed.WriteMessage($"\n - PVI 1 cố định (#{idx1}): Sta = {sta1:F2}m | Elev = {elev1:F3}m");
                ed.WriteMessage($"\n - PVI 2 trước điều chỉnh (#{idx2}): Sta = {oldSta2:F2}m | Elev = {oldElev2:F3}m");
                ed.WriteMessage($"\n - PVI 2 sau điều chỉnh (#{idx2}): Sta = {newSta2:F2}m | Elev = {newElev2:F3}m");
                ed.WriteMessage($"\n - Dốc thiết lập: {slope:F4}%");
                ed.WriteMessage($"\n - Khoảng cách mới PVI 1-2: {newDist:F3}m");
                if (form.ShiftSubsequent)
                {
                    ed.WriteMessage($"\n - Đã tịnh tiến {shiftedCount} PVI phía sau theo ΔS = {deltaSta:F3}m");
                }
                ed.WriteMessage("\n==========================================");
            }
        }
    }
}
