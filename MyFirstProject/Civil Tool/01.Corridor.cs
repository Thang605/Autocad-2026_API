using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Acad = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using Civil = Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.Civil.Runtime;
using Autodesk.Civil.Settings;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Civil.ApplicationServices;
using CivSurface = Autodesk.Civil.DatabaseServices.Surface;
using MyFirstProject.Extensions;
// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.Corridors))]

namespace Civil3DCsharp
{
    class Corridors
    {
        [CommandMethod("CTC_AddAllSection")]
        public static void CVC_AddAllSection()
        {
            // 1. Hiển thị form cho user chọn corridor(s)
            var form = new MyFirstProject.Civil_Tool.AddAllSectionForm();
            var dlgResult = Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(form);

            if (dlgResult != System.Windows.Forms.DialogResult.OK || !form.FormAccepted)
            {
                A.Ed.WriteMessage("\n Đã hủy lệnh.");
                return;
            }

            var selectedCorridors = form.SelectedCorridors;
            bool rebuildAfterAdd = form.RebuildAfterAdd;

            if (selectedCorridors.Count == 0)
            {
                A.Ed.WriteMessage("\nKhông có corridor nào được chọn.");
                return;
            }

            // 2. Xử lý từng corridor
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                // Group theo CorridorId để xử lý mỗi corridor 1 lần
                var corridorGroups = selectedCorridors
                    .GroupBy(c => c.CorridorId)
                    .ToList();

                int totalProcessed = 0;

                foreach (var group in corridorGroups)
                {
                    var corridor = tr.GetObject(group.Key, OpenMode.ForWrite) as Corridor;
                    if (corridor == null) continue;

                    A.Ed.WriteMessage($"\n\n═══ Xử lý Corridor: {corridor.Name} ═══");

                    foreach (var info in group)
                    {
                        if (info.SampleLineCount == 0)
                        {
                            A.Ed.WriteMessage($"\n  ⚠ Bỏ qua SL Group '{info.SampleLineGroupName}' (rỗng)");
                            continue;
                        }

                        // Lấy danh sách station từ sample line group
                        var sampleLineStations = GetSampleLineStations(info.SampleLineGroupId, tr);
                        if (sampleLineStations.Length == 0) continue;

                        // Tìm baseline tương ứng
                        if (info.BaselineIndex >= 0 && info.BaselineIndex < corridor.Baselines.Count)
                        {
                            var baseline = corridor.Baselines[info.BaselineIndex];
                            A.Ed.WriteMessage($"\n  → Baseline: {baseline.Name ?? "(unnamed)"}, SL Group: {info.SampleLineGroupName}, Stations: {sampleLineStations.Length}");
                            AddStationsToBaselineRegions(baseline, sampleLineStations);
                            totalProcessed++;
                        }
                    }

                    if (rebuildAfterAdd)
                    {
                        A.Ed.WriteMessage($"\n  🔄 Rebuilding corridor: {corridor.Name}...");
                        corridor.Rebuild();
                    }
                }

                tr.Commit();
                A.Ed.WriteMessage($"\n\n✅ Hoàn thành! Đã xử lý {corridorGroups.Count} corridor(s), {totalProcessed} baseline(s).");
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nError occurred: {ex.Message}");
                tr.Abort();
            }
        }




        private static double[] GetSampleLineStations(ObjectId sampleLineGroupId, Transaction tr)
        {
            var sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForRead) as SampleLineGroup;
            if (sampleLineGroup == null) return new double[0];

            var sampleLineIds = sampleLineGroup.GetSampleLineIds();
            var stations = new List<double>();

            foreach (ObjectId sampleLineId in sampleLineIds)
            {
                var sampleLine = tr.GetObject(sampleLineId, OpenMode.ForRead) as SampleLine;
                if (sampleLine != null)
                {
                    stations.Add(sampleLine.Station);
                }
            }

            return stations.OrderBy(s => s).ToArray();
        }

        private static void AddStationsToBaselineRegions(Baseline baseline, double[] newStations)
        {
            foreach (BaselineRegion baselineRegion in baseline.BaselineRegions)
            {

                // Get existing stations in the region
                var existingStations = new HashSet<double>(baselineRegion.SortedStations());

                // Add new stations that fall within the region and don't already exist
                foreach (double station in newStations)
                {
                    if (IsStationInRegion(station, baselineRegion) && !existingStations.Contains(station))
                    {
                        try
                        {
                            baselineRegion.AddStation(station, "AddSection");
                            A.Ed.WriteMessage($"\nAdded station {station:F3} to region {baselineRegion.StartStation:F3}-{baselineRegion.EndStation:F3}");
                        }
                        catch (System.Exception ex)
                        {
                            A.Ed.WriteMessage($"\nFailed to add station {station:F3}: {ex.Message}");
                        }
                    }
                }
            }
        }

        private static bool IsStationInRegion(double station, BaselineRegion region)
        {
            return station >= region.StartStation && station <= region.EndStation;
        }
    }
}

