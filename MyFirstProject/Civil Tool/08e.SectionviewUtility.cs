// Nhóm lệnh: Tiện ích cắt ngang (Chuyển đổi, Khóa, Ẩn)
// Tách từ 08.Sectionview.cs
//
using System;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using CivSurface = Autodesk.Civil.DatabaseServices.TinSurface;
using Section = Autodesk.Civil.DatabaseServices.Section;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.ApplicationServices;

using MyFirstProject.Extensions;

[assembly: CommandClass(typeof(Civil3DCsharp.SectionViewsUtility))]

namespace Civil3DCsharp
{
    public class SectionViewsUtility
    {

        [CommandMethod("CTSV_ChuyenDoi_TNTK_TNTN")]
        public static void CTSVChuyenDoiTNTKTNTN()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                ObjectId sectionViewId1 = UserInput.GSectionView("Chọn 1 trắc ngang trong nhóm cần hiệu chỉnh: \n");
                SectionView? sectionview1 = tr.GetObject(sectionViewId1, OpenMode.ForWrite) as SectionView;

#pragma warning disable CS8602
                ObjectId sampleLineId1 = sectionview1.SampleLineId;
#pragma warning restore CS8602
                SampleLine? sampleLine1 = tr.GetObject(sampleLineId1, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8604
                ObjectId alignmentId = sampleLine1.GetParentAlignmentId();
#pragma warning restore CS8604
                ObjectId sampleLineGroupId = sampleLine1.GroupId;
                SampleLineGroup? sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;
#pragma warning disable CS8602
                ObjectIdCollection sampleLineIds = sampleLineGroup.GetSampleLineIds();
#pragma warning restore CS8602

                //remove sectionsource
                ObjectIdCollection sectionSourceIdColl = [];
                SectionSourceCollection sectionSources = sampleLineGroup.GetSectionSources();
                foreach (SectionSource sectionsource in sectionSources)
                {
                    if (sectionsource.SourceType == SectionSourceType.TinSurface)
                    {
                        TinSurface? type = tr.GetObject(sectionsource.SourceId, OpenMode.ForRead) as TinSurface;
#pragma warning disable CS8602
                        if (type.Name.Contains("TN"))
                        {
                            sectionsource.IsSampled = true;
                            sectionSourceIdColl.Add(sectionsource.SourceId);
                            sectionsource.StyleId = A.Cdoc.Styles.SectionStyles["1.TN Ground"];
                        }
                        else sectionsource.IsSampled = false;
#pragma warning restore CS8602
                    }

                    if (sectionsource.SourceType == SectionSourceType.Corridor)
                    {
                        Corridor? type = tr.GetObject(sectionsource.SourceId, OpenMode.ForRead) as Corridor;
#pragma warning disable CS8602
                        if (type.Name.Contains("MoHinh"))
                        {
                            sectionsource.IsSampled = false;
                            sectionsource.StyleId = A.Cdoc.Styles.CodeSetStyles["1. All Codes 1-1000"];
                        }
#pragma warning restore CS8602
                    }

                    if (sectionsource.SourceType == SectionSourceType.CorridorSurface)
                    {
                        TinSurface? type = tr.GetObject(sectionsource.SourceId, OpenMode.ForRead) as TinSurface;
#pragma warning disable CS8602
                        if (type.Name.Contains("top"))
                        {
                            sectionsource.IsSampled = false;
                            sectionSourceIdColl.Add(sectionsource.SourceId);
                            sectionsource.StyleId = A.Cdoc.Styles.SectionStyles["2.Top Ground"];
                        }
                        else if (type.Name.Contains("datum"))
                        {
                            sectionsource.IsSampled = false;
                            sectionSourceIdColl.Add(sectionsource.SourceId);
                            sectionsource.StyleId = A.Cdoc.Styles.SectionStyles["3.Datum Ground"];
                        }
                        else sectionsource.IsSampled = false;
#pragma warning restore CS8602
                    }

                    if (sectionsource.SourceType == SectionSourceType.Material)
                    {
                        sectionsource.IsSampled = false;
                    }
                }

                //remove band
                foreach (ObjectId sampleLineId in sampleLineIds)
                {
                    SampleLine? sampleLine = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8602
                    ObjectIdCollection sectionViewIdColl = sampleLine.GetSectionViewIds();
#pragma warning restore CS8602
                    foreach (ObjectId sectionViewId in sectionViewIdColl)
                    {
                        SectionView? sectionView = tr.GetObject(sectionViewId, OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8604
                        UtilitiesC3D.RemoveSectionBand(sectionView, "Cao do thiet ke 1-1000");
#pragma warning restore CS8604
                        UtilitiesC3D.RemoveSectionBand(sectionView, "Khoang cach mia TK 1-1000");
                    }
                }

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTSV_KhoaCatNgang_AddPoint")]
        public static void CTSVKhoaCatNgangAddPoint()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                ObjectId sectionViewId = UserInput.GSectionView("Chọn 1 cắt trong nhóm những cắt ngang");
                SectionView? sectionView = tr.GetObject(sectionViewId, OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602
                sectionView.Description = "check";
#pragma warning restore CS8602

                ObjectId surfaceId = UserInput.GSurfaceId("Chọn mặt phẳng để lấy thông tin khóa cắt ngang");
                CivSurface? civSurface = tr.GetObject(surfaceId, OpenMode.ForWrite) as CivSurface;

                double khoangCachDiemMia = UserInput.GInt("Nhập khoảng cách điểm mia tối thiểu yêu cầu:");

                ObjectId surfaceId2 = UserInput.GSurfaceId("Chọn mặt phẳng để thêm điểm khóa cắt ngang");
                CivSurface? civSurface2 = tr.GetObject(surfaceId2, OpenMode.ForWrite) as CivSurface;

                ObjectId sampleLineId = sectionView.SampleLineId;
                SampleLine? sampleLine = tr.GetObject(sampleLineId, OpenMode.ForWrite) as SampleLine;
#pragma warning disable CS8604
                ObjectId alignmentId = sampleLine.GetParentAlignmentId();
#pragma warning restore CS8604
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;

                ObjectId sampleLineGroupId = sampleLine.GroupId;
                SampleLineGroup? sampleLineGroup = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;
#pragma warning disable CS8602
                SectionViewGroupCollection sectionViewGroups = sampleLineGroup.SectionViewGroups;
#pragma warning restore CS8602

                SectionViewGroup sectionViewGroup1_ok = sectionViewGroups[0];
                ObjectIdCollection sectionViews = sectionViewGroup1_ok.GetSectionViewIds();

                SectionSourceCollection sectionSources = sampleLineGroup.GetSectionSources();
                ObjectId sectionSource_TN_Id = new();
                foreach (SectionSource sectionsource in sectionSources)
                {
                    if ((sectionsource.SourceType == SectionSourceType.TinSurface) & (sectionsource.IsSampled == true))
                    {
                        TinSurface? type = tr.GetObject(sectionsource.SourceId, OpenMode.ForRead) as TinSurface;
#pragma warning disable CS8602
                        if (type.Name.Contains("TN", StringComparison.CurrentCultureIgnoreCase))
                        {
                            sectionSource_TN_Id = sectionsource.SourceId;
                        }
#pragma warning restore CS8602
                    }
                }

                foreach (ObjectId sectionViewId_1 in sectionViews)
                {
                    SectionView? sectionView1 = tr.GetObject(sectionViewId_1, OpenMode.ForWrite) as SectionView;
#pragma warning disable CS8602
                    ObjectId sampleLine1Id = sectionView1.SampleLineId;
#pragma warning restore CS8602
                    SampleLine? sampleLine1 = tr.GetObject(sampleLine1Id, OpenMode.ForWrite) as SampleLine;

#pragma warning disable CS8602
                    ObjectId sectionTnId = sampleLine1.GetSectionId(sectionSource_TN_Id);
#pragma warning restore CS8602
                    Section? section = tr.GetObject(sectionTnId, OpenMode.ForWrite) as Section;
#pragma warning disable CS8602
                    SectionPointCollection sectionPoints = section.SectionPoints;
#pragma warning restore CS8602

                    Double easthing = new();
                    Double northing = new();
                    for (int i = 0; i < sectionPoints.Count; i++)
                    {
#pragma warning disable CS8602
                        alignment.PointLocation(sampleLine1.Station, sectionPoints[i].Location.X, ref easthing, ref northing);
#pragma warning restore CS8602
                        Point3d point3D = new(easthing, northing, sectionPoints[i].Location.Y);
#pragma warning disable CS8602
                        civSurface2.AddVertex(point3D);
#pragma warning restore CS8602
                    }

                    for (int i = 1; i < sectionPoints.Count; i++)
                    {
                        double x1 = sectionPoints[i - 1].Location.X;
                        double x2 = sectionPoints[i].Location.X;
                        double c = Math.Abs(x2 - x1);

                        int j = 0;
                        while (Math.Abs(x2 - x1) > khoangCachDiemMia)
                        {
                            x1 = x1 + khoangCachDiemMia - j * 0.1;
#pragma warning disable CS8602
                            alignment.PointLocation(sampleLine1.Station, x1, ref easthing, ref northing);
#pragma warning restore CS8602
#pragma warning disable CS8602
                            Point3d point3D_1 = new(easthing, northing, civSurface.FindElevationAtXY(easthing, northing));
#pragma warning restore CS8602
#pragma warning disable CS8602
                            civSurface2.AddVertex(point3D_1);
#pragma warning restore CS8602
                            j++;
                        }
                    }
                }

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        [CommandMethod("CTSV_An_DuongDiaChat")]
        public static void CTSVAnDuongDiaChat()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();

                ObjectIdCollection sectionViewIdColl = UserInput.GSelectionSetWithType("Chọn các section cần ẩn đi: \n", "AECC_SECTION");

                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                PromptIntegerOptions pIntOpts = new("")
                {
                    Message = "\nNhập tên lớp địa chất hoặc chọn ",
                    AllowZero = false,
                    AllowNegative = false
                };

                pIntOpts.Keywords.Add("TN1");
                pIntOpts.Keywords.Add("TN2");
                pIntOpts.Keywords.Add("TN3");
                pIntOpts.Keywords.Add("TN4");
                pIntOpts.Keywords.Add("TN5");
                pIntOpts.Keywords.Add("TN6");
                pIntOpts.Keywords.Default = "TN1";
                pIntOpts.AllowNone = true;

                PromptIntegerResult pIntRes = acDoc.Editor.GetInteger(pIntOpts);

                foreach (ObjectId sectionId in sectionViewIdColl)
                {
                    Section? section = tr.GetObject(sectionId, OpenMode.ForWrite) as Section;
#pragma warning disable CS8602
                    if (section.StyleName != pIntRes.StringResult)
                    {
                        if (!section.StyleName.Contains(" -defpoints"))
                        {
                            section.StyleName += " -defpoints";
                        }
                    }
#pragma warning restore CS8602
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
