// Nhóm lệnh: Thêm vật liệu trên cắt ngang
// Tách từ 08.Sectionview.cs
//
using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;

using Autodesk.Civil.DatabaseServices;

using MyFirstProject.Extensions;
using MyFirstProject.Civil_Tool;

[assembly: CommandClass(typeof(Civil3DCsharp.SectionViewsVatLieu))]

namespace Civil3DCsharp
{
    public class SectionViewsVatLieu
    {
        [CommandMethod("CTSV_ThemVatLieu_TrenCatNgang")]
        public static void CTSVThemVatLieuTrenCatNgang()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // === BIẾN LƯU TRỮ GIỮA CÁC TRANSACTION ===
                ObjectId corridorId = ObjectId.Null;
                ObjectId sampleLineGroupId = ObjectId.Null;
                ObjectId alignmentId = ObjectId.Null;
                ObjectId sectionViewId = ObjectId.Null;
                List<string> linkCodeNames = new List<string>();

                // === TRANSACTION 1: ĐỌC DỮ LIỆU CIVIL 3D ===
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    sectionViewId = UserInput.GSectionView("\n Chọn mặt cắt ngang mẫu trong nhóm: ");
                    if (sectionViewId == ObjectId.Null)
                    {
                        ed.WriteMessage("\nKhông chọn được SectionView.");
                        return;
                    }

                    SectionView? sectionView = tr.GetObject(sectionViewId, OpenMode.ForWrite) as SectionView;
                    if (sectionView == null) return;

                    SampleLine? sampleLine = tr.GetObject(sectionView.SampleLineId, OpenMode.ForWrite) as SampleLine;
                    if (sampleLine == null) return;

                    sampleLineGroupId = sampleLine.GroupId;
                    alignmentId = sampleLine.GetParentAlignmentId();

                    SampleLineGroup? sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;
                    if (sampleLineGroup == null) return;

                    // Lấy ra Corridor Id bằng SectionSources
                    SectionSourceCollection sectionSources = sampleLineGroup.GetSectionSources();
                    foreach (SectionSource source in sectionSources)
                    {
                        if (source.SourceType == SectionSourceType.Corridor && source.IsSampled)
                        {
                            corridorId = source.SourceId;
                            break;
                        }
                    }

                    if (corridorId == ObjectId.Null)
                    {
                        ed.WriteMessage("\n❌ Không tìm thấy Corridor sampled trong nhóm Section này.");
                        return;
                    }

                    Corridor? corridor = tr.GetObject(corridorId, OpenMode.ForWrite) as Corridor;
                    if (corridor == null) return;

                    // LẤY MÃ LINK CODES CÓ TRONG CORRIDOR
                    HashSet<string> linkCodesSet = new HashSet<string>();
                    foreach (Baseline baseline in corridor.Baselines)
                    {
                        if (baseline.AlignmentId != alignmentId) continue;
                        foreach (BaselineRegion region in baseline.BaselineRegions)
                        {
                            if (region.AppliedAssemblies.Count == 0) continue;
                            AppliedAssembly assembly = region.AppliedAssemblies[0];
                            foreach (AppliedSubassembly sub in assembly.GetAppliedSubassemblies())
                            {
                                foreach (CalculatedLink link in sub.Links)
                                {
                                    foreach (string code in link.CorridorCodes)
                                    {
                                        linkCodesSet.Add(code);
                                    }
                                }
                            }
                        }
                    }

                    linkCodeNames = new List<string>(linkCodesSet);
                    linkCodeNames.Sort();

                    tr.Commit();
                }

                if (linkCodeNames.Count == 0)
                {
                    ed.WriteMessage("\n❌ Corridor không có Link Code nào.");
                    return;
                }

                // === HIỂN THỊ FORM (NGOÀI TRANSACTION) ===
                string selectedCode = "";
                string prefixText = "";
                double adjustValue = 0;

                using (SectionVatLieuForm form = new SectionVatLieuForm(linkCodeNames))
                {
                    var dialogResult = Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(form);
                    if (dialogResult != System.Windows.Forms.DialogResult.OK || !form.FormAccepted)
                    {
                        ed.WriteMessage("\nLệnh đã bị hủy.");
                        return;
                    }
                    selectedCode = form.SelectedLinkCode;
                    prefixText = form.PrefixText;
                    adjustValue = form.AdjustValue;
                }
                // Thêm dấu ':' vào cuối tên vật liệu nếu chưa có
                if (!prefixText.EndsWith(":"))
                    prefixText = prefixText + ":";

                // === TRANSACTION 2: XÁC ĐỊNH VỊ TRÍ TEXT MẪU ===
                double labelDeltaElev = 0, labelRefOffset = 0;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    SectionView? sectionView = tr.GetObject(sectionViewId, OpenMode.ForWrite) as SectionView;
                    if (sectionView == null) return;

                    // Set false trước khi dùng FindOffsetAndElevationAtXY
                    bool wasAuto = sectionView.IsElevationRangeAutomatic;
                    sectionView.IsElevationRangeAutomatic = false;

                    Point3d ptLabel = UserInput.GPoint("\n Chọn vị trí đặt nhãn vật liệu trên trắc ngang mẫu: \n ");
                    double labelElev = 0;
                    sectionView.FindOffsetAndElevationAtXY(ptLabel.X, ptLabel.Y, ref labelRefOffset, ref labelElev);
                    labelDeltaElev = sectionView.ElevationMax - labelElev;

                    sectionView.IsElevationRangeAutomatic = wasAuto;
                    tr.Commit();
                }

                // === TRANSACTION 3: THU THẬP DỮ LIỆU CORRIDOR (LINK POINTS THEO STATION) ===
                // Dùng CalculatedPoint.XYZ + alignment.StationOffset để lấy offset/elevation
                // chính xác theo hệ tọa độ section view
                Dictionary<double, List<List<Point2d>>> stationLinkPoints = new Dictionary<double, List<List<Point2d>>>();
                // Lưu phạm vi station của corridor regions
                List<(double start, double end)> corridorRegionRanges = new List<(double, double)>();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Corridor? corridor = tr.GetObject(corridorId, OpenMode.ForWrite) as Corridor;
                    if (corridor == null) return;

                    Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                    if (alignment == null) return;

                    foreach (Baseline baseline in corridor.Baselines)
                    {
                        if (baseline.AlignmentId != alignmentId) continue;
                        foreach (BaselineRegion region in baseline.BaselineRegions)
                        {
                            IReadOnlyList<double> sortedStations = region.SortedStations();
                            if (sortedStations.Count == 0) continue;

                            // Lưu phạm vi region
                            corridorRegionRanges.Add((sortedStations[0], sortedStations[sortedStations.Count - 1]));

                            AppliedAssemblyCollection appliedAssemblies = region.AppliedAssemblies;

                            for (int i = 0; i < appliedAssemblies.Count && i < sortedStations.Count; i++)
                            {
                                double station = Math.Round(sortedStations[i], 4);
                                AppliedAssembly assembly = appliedAssemblies[i];

                                List<List<Point2d>> allLinkPtsForStation = new List<List<Point2d>>();

                                foreach (AppliedSubassembly sub in assembly.GetAppliedSubassemblies())
                                {
                                    foreach (CalculatedLink link in sub.Links)
                                    {
                                        if (link.CorridorCodes.Contains(selectedCode))
                                        {
                                            List<Point2d> linkPts = new List<Point2d>();
                                            foreach (CalculatedPoint pt in link.CalculatedPoints)
                                            {
                                                Point3d xyz = pt.XYZ;
                                                double ptStation = 0, ptOffset = 0;
                                                alignment.StationOffset(xyz.X, xyz.Y, ref ptStation, ref ptOffset);
                                                double ptElevation = xyz.Z;
                                                linkPts.Add(new Point2d(ptOffset, ptElevation));
                                            }

                                            if (linkPts.Count > 1)
                                            {
                                                allLinkPtsForStation.Add(linkPts);
                                            }
                                        }
                                    }
                                }

                                if (allLinkPtsForStation.Count > 0)
                                {
                                    if (!stationLinkPoints.ContainsKey(station))
                                        stationLinkPoints[station] = allLinkPtsForStation;
                                }
                            }
                        }
                    }

                    tr.Commit();
                }

                if (stationLinkPoints.Count == 0)
                {
                    ed.WriteMessage($"\n❌ Corridor không có dữ liệu link code '{selectedCode}' nào.");
                    return;
                }

                // Hiển thị phạm vi corridor
                foreach (var range in corridorRegionRanges)
                {
                    ed.WriteMessage($"\n📏 Corridor region: Km{range.start / 1000:F3} → Km{range.end / 1000:F3}");
                }

                // Đảm bảo Layer tồn tại (CCreateLayer tự tạo transaction riêng)
                UtilitiesCAD.CCreateLayer("Defpoints");
                string layerVatLieu = prefixText.TrimEnd(':');
                UtilitiesCAD.CCreateLayer(layerVatLieu);

                // === TRANSACTION 4: VẼ POLYLINE + FIELD TEXT ===
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable btStr = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btrStr = (BlockTableRecord)tr.GetObject(btStr[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    TextStyleTable styleTableStr = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                    ObjectId textStyleIdStr = styleTableStr.Has("Standard") ? styleTableStr["Standard"] : db.Textstyle;

                    // Lấy danh sách SectionView cùng nhóm
                    SampleLineGroup? sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;
                    if (sampleLineGroup == null) return;

                    SectionViewGroupCollection sectionViewGroupCollection = sampleLineGroup.SectionViewGroups;
                    SectionViewGroup? targetSvg = null;
                    foreach (SectionViewGroup svg in sectionViewGroupCollection)
                    {
                        if (svg.GetSectionViewIds().Contains(sectionViewId))
                        {
                            targetSvg = svg;
                            break;
                        }
                    }
                    if (targetSvg == null) return;
                    ObjectIdCollection sectionViewIdColl = targetSvg.GetSectionViewIds();

                    // === SET IsElevationRangeAutomatic = false CHO TẤT CẢ SECTION VIEW ===
                    Dictionary<ObjectId, bool> originalAutoElevStates = new Dictionary<ObjectId, bool>();
                    foreach (ObjectId svId in sectionViewIdColl)
                    {
                        SectionView? sv = tr.GetObject(svId, OpenMode.ForWrite) as SectionView;
                        if (sv == null) continue;
                        originalAutoElevStates[svId] = sv.IsElevationRangeAutomatic;
                        sv.IsElevationRangeAutomatic = false;
                    }

                    int count = 0;
                    ObjectId sourceLabelId = ObjectId.Null; // ID của text tên vật liệu đầu tiên (text gốc)
                    foreach (ObjectId svId in sectionViewIdColl)
                    {
                        SectionView? svTemp = tr.GetObject(svId, OpenMode.ForWrite) as SectionView;
                        if (svTemp == null) continue;

                        SampleLine? slTemp = tr.GetObject(svTemp.SampleLineId, OpenMode.ForWrite) as SampleLine;
                        if (slTemp == null) continue;

                        double station = Math.Round(slTemp.Station, 4);

                        // Kiểm tra station có nằm trong phạm vi corridor region không
                        bool inRegion = false;
                        foreach (var range in corridorRegionRanges)
                        {
                            if (station >= range.start - 0.5 && station <= range.end + 0.5)
                            {
                                inRegion = true;
                                break;
                            }
                        }

                        if (!inRegion)
                        {
                            // Station nằm ngoài corridor → bỏ qua im lặng
                            continue;
                        }

                        // Tìm station gần nhất trong corridor data (không giới hạn tolerance)
                        List<List<Point2d>>? multiLinkPts = null;
                        double minDiff = double.MaxValue;
                        foreach (var kvp in stationLinkPoints)
                        {
                            double diff = Math.Abs(kvp.Key - station);
                            if (diff < minDiff)
                            {
                                minDiff = diff;
                                multiLinkPts = kvp.Value;
                            }
                        }

                        if (multiLinkPts == null || multiLinkPts.Count == 0)
                        {
                            ed.WriteMessage($"\n⚠ Station {station:F2} trong phạm vi corridor nhưng không có link code '{selectedCode}'.");
                            continue;
                        }

                        List<string> listLengthPolyline = new List<string>();
                        string str1 = "%<\\AcObjProp Object(%<\\_ObjId ";
                        string str2 = ">%).Length \\f \"%lu2%pr2\">%";

                        foreach (List<Point2d> linkPts in multiLinkPts)
                        {
                            Polyline polyLineInfo = new Polyline();
                            polyLineInfo.SetDatabaseDefaults();
                            polyLineInfo.Layer = layerVatLieu;
                            polyLineInfo.ColorIndex = 2;

                            for (int i = 0; i < linkPts.Count; i++)
                            {
                                double wcsX = 0, wcsY = 0;
                                // Dùng API chính thức: offset/elevation → WCS
                                svTemp.FindXYAtOffsetAndElevation(linkPts[i].X, linkPts[i].Y, ref wcsX, ref wcsY);
                                polyLineInfo.AddVertexAt(i, new Point2d(wcsX, wcsY), 0, 0, 0);
                            }

                            btrStr.AppendEntity(polyLineInfo);
                            tr.AddNewlyCreatedDBObject(polyLineInfo, true);

                            string strId = polyLineInfo.Id.OldIdPtr.ToString();
                            string formatItem = str1 + strId + str2;
                            listLengthPolyline.Add(formatItem);
                        }

                        // Tính tổng các Polyline bằng AcExpr
                        string formatTong = "0";
                        string str3 = "%<\\AcExpr (";
                        string str4 = ") \\f \"%lu2%pr2\">%";

                        foreach (String item in listLengthPolyline)
                        {
                            formatTong = formatTong + "+" + item;
                        }

                        string valueFieldStr;
                        if (adjustValue != 0)
                        {
                            // Có giá trị cộng/trừ → thêm vào công thức
                            string adjustStr = adjustValue.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
                            string formatTongAdj = $"({formatTong})+({adjustStr})";
                            valueFieldStr = $"{str3}{formatTongAdj}{str4} m";
                        }
                        else
                        {
                            valueFieldStr = $"{str3}{formatTong}{str4} m";
                        }

                        // --- TEXT 1: TÊN VẬT LIỆU (label) - dùng Field link đến text gốc ---
                        double labelElevTarget = svTemp.ElevationMax - labelDeltaElev;
                        double labelX = 0, labelY = 0;
                        svTemp.FindXYAtOffsetAndElevation(labelRefOffset, labelElevTarget, ref labelX, ref labelY);

                        DBText labelText = new DBText();
                        labelText.SetDatabaseDefaults();
                        labelText.Position = new Point3d(labelX, labelY, 0);
                        labelText.Height = 0.4;
                        labelText.Layer = layerVatLieu;
                        labelText.ColorIndex = 2;
                        labelText.TextStyleId = textStyleIdStr;

                        if (sourceLabelId == ObjectId.Null)
                        {
                            // Text đầu tiên: plain text (text gốc để sửa)
                            labelText.TextString = prefixText;
                        }
                        else
                        {
                            // Các text sau: Field link đến text gốc
                            string srcIdStr = sourceLabelId.OldIdPtr.ToString();
                            labelText.TextString = $"%<\\AcObjProp Object(%<\\_ObjId {srcIdStr}>%).TextString>%";
                        }

                        btrStr.AppendEntity(labelText);
                        tr.AddNewlyCreatedDBObject(labelText, true);

                        // Lưu ID text gốc (text đầu tiên)
                        if (sourceLabelId == ObjectId.Null)
                        {
                            sourceLabelId = labelText.Id;
                        }

                        // --- TEXT 2: GIÁ TRỊ (field expression) ---
                        double textHeight = 0.4;
                        double widthFactor = 0.7;
                        double labelWidth = prefixText.Length * textHeight * widthFactor;
                        double spacing = textHeight * 3;
                        double valueX = labelX + labelWidth + spacing;

                        DBText valueText = new DBText();
                        valueText.SetDatabaseDefaults();
                        valueText.Position = new Point3d(valueX, labelY, 0);
                        valueText.Height = textHeight;
                        valueText.Layer = layerVatLieu;
                        valueText.ColorIndex = 2; // Màu vàng
                        valueText.TextStyleId = textStyleIdStr;
                        valueText.TextString = valueFieldStr;

                        btrStr.AppendEntity(valueText);
                        tr.AddNewlyCreatedDBObject(valueText, true);

                        count++;
                    }

                    // === KHÔI PHỤC IsElevationRangeAutomatic CHO TẤT CẢ SECTION VIEW ===
                    foreach (var kvp in originalAutoElevStates)
                    {
                        SectionView? sv = tr.GetObject(kvp.Key, OpenMode.ForWrite) as SectionView;
                        if (sv != null)
                            sv.IsElevationRangeAutomatic = kvp.Value;
                    }

                    ed.WriteMessage($"\n✅ Đã đồ {count} mặt cắt (mỗi cắt ngang gồm nhiều Polyline gộp tính tổng Field).");

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n❌ Lỗi: " + ex.Message);
            }
        }
    }
}
