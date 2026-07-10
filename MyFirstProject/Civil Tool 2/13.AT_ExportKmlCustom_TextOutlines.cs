using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Document = Autodesk.AutoCAD.ApplicationServices.Document;
using AcadEntity = Autodesk.AutoCAD.DatabaseServices.Entity;
using DBObject = Autodesk.AutoCAD.DatabaseServices.DBObject;

namespace Civil3DCsharp
{
    public partial class AT_ExportKmlCustom_Commands
    {
        private static List<Curve> GetTextOutlines(Document doc, ObjectId textId)
        {
            Database db = doc.Database;
            Editor ed = doc.Editor;
            List<Curve> outlines = new List<Curve>();
            
            string tempFile = Path.Combine(Path.GetTempPath(), $"temp_kml_txtexp_{Guid.NewGuid().ToString().Substring(0,8)}.wmf");
            
            // Save current view
            using (ViewTableRecord view = ed.GetCurrentView())
            {
                try
                {
                    ObjectId cloneId = ObjectId.Null;
                    double originalRotation = 0;
                    Point3d originalCenter = Point3d.Origin;
                    double originalHeight = 0;
                    
                    // Turn off background plot and other interfering settings
                    object oldBgPlot = Application.GetSystemVariable("BACKGROUNDPLOT");
                    object oldCmdecho = Application.GetSystemVariable("CMDECHO");
                    object oldFileDia = Application.GetSystemVariable("FILEDIA");

                    Application.SetSystemVariable("BACKGROUNDPLOT", 0);
                    Application.SetSystemVariable("CMDECHO", 0);
                    Application.SetSystemVariable("FILEDIA", 0);

                    try
                    {
                        // Step 1: Clone and Append to Database
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            AcadEntity ent = tr.GetObject(textId, OpenMode.ForRead) as AcadEntity;
                            if (ent != null && (ent is DBText || ent is MText))
                            {
                                // Skip empty or whitespace text
                                if (ent is DBText txt && string.IsNullOrWhiteSpace(txt.TextString))
                                {
                                    tr.Commit();
                                    return outlines;
                                }
                                if (ent is MText mtxt && string.IsNullOrWhiteSpace(mtxt.Text) && string.IsNullOrWhiteSpace(mtxt.Contents))
                                {
                                    tr.Commit();
                                    return outlines;
                                }

                                if (ent is DBText txt2)
                                {
                                    originalRotation = txt2.Rotation;
                                    originalHeight = txt2.Height;
                                }
                                else if (ent is MText mtxt2)
                                {
                                    originalRotation = mtxt2.Rotation;
                                    originalHeight = mtxt2.TextHeight;
                                }

                                AcadEntity clone = (AcadEntity)ent.Clone();
                                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                                btr.AppendEntity(clone);
                                tr.AddNewlyCreatedDBObject(clone, true);
                                cloneId = clone.ObjectId;

                                if (clone is DBText cTxt)
                                {
                                    cTxt.Rotation = 0;
                                }
                                else if (clone is MText cMtxt)
                                {
                                    cMtxt.Rotation = 0;
                                    try { cMtxt.BackgroundFill = false; } catch {}
                                }
                                clone.RecordGraphicsModified(true);
                            }
                            tr.Commit();
                        }

                        if (cloneId == ObjectId.Null) return outlines;

                        // Force graphics calculation to populate geometric extents
                        doc.TransactionManager.QueueForGraphicsFlush();
                        doc.TransactionManager.FlushGraphics();

                        // Step 2: Query clone extents and normalize clone's position
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            AcadEntity clone = (AcadEntity)tr.GetObject(cloneId, OpenMode.ForWrite);
                            if (clone != null)
                            {
                                Extents3d cloneExt;
                                Point3d cloneCenter;
                                try
                                {
                                    cloneExt = clone.GeometricExtents;
                                    cloneCenter = cloneExt.MinPoint + ((cloneExt.MaxPoint - cloneExt.MinPoint) / 2.0);
                                }
                                catch
                                {
                                    if (clone is DBText fallbackTxt)
                                    {
                                        cloneCenter = fallbackTxt.Position;
                                    }
                                    else if (clone is MText fallbackMtxt)
                                    {
                                        cloneCenter = fallbackMtxt.Location;
                                    }
                                    else
                                    {
                                        cloneCenter = Point3d.Origin;
                                    }

                                    double estWidth = originalHeight * 10.0;
                                    if (clone is DBText fallbackTxt2)
                                    {
                                        estWidth = fallbackTxt2.Height * Math.Max(1, fallbackTxt2.TextString.Length) * 0.7;
                                    }
                                    else if (clone is MText fallbackMtxt2)
                                    {
                                        string txt = string.IsNullOrEmpty(fallbackMtxt2.Text) ? fallbackMtxt2.Contents : fallbackMtxt2.Text;
                                        estWidth = fallbackMtxt2.TextHeight * Math.Max(1, txt.Length) * 0.7;
                                    }
                                    cloneExt = new Extents3d(
                                        new Point3d(cloneCenter.X - estWidth / 2, cloneCenter.Y - originalHeight / 2, cloneCenter.Z),
                                        new Point3d(cloneCenter.X + estWidth / 2, cloneCenter.Y + originalHeight / 2, cloneCenter.Z)
                                    );
                                }

                                originalHeight = cloneExt.MaxPoint.Y - cloneExt.MinPoint.Y;
                                originalCenter = cloneCenter;

                                Matrix3d moveMat = Matrix3d.Displacement(Point3d.Origin - cloneCenter);
                                clone.TransformBy(moveMat);
                            }
                            tr.Commit();
                        }

                        // Step 3: Zoom to clone and export WMF (now located at the origin)
                        Extents3d cloneExtents;
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            AcadEntity clone = (AcadEntity)tr.GetObject(cloneId, OpenMode.ForRead);
                            double estWidth = originalHeight * 10.0;
                            if (clone is DBText fallbackTxt)
                            {
                                estWidth = fallbackTxt.Height * Math.Max(1, fallbackTxt.TextString.Length) * 0.7;
                            }
                            else if (clone is MText fallbackMtxt)
                            {
                                string txt = string.IsNullOrEmpty(fallbackMtxt.Text) ? fallbackMtxt.Contents : fallbackMtxt.Text;
                                estWidth = fallbackMtxt.TextHeight * Math.Max(1, txt.Length) * 0.7;
                            }
                            cloneExtents = new Extents3d(
                                new Point3d(-estWidth / 2, -originalHeight / 2, 0),
                                new Point3d(estWidth / 2, originalHeight / 2, 0)
                            );
                            tr.Commit();
                        }

                        ZoomToExtentsHelper(ed, cloneExtents, 1.05);
                        ed.SetImpliedSelection(new[] { cloneId });

                        ed.Command("_.WMFOUT", tempFile, "");
                        ed.Command("_.WMFIN", tempFile, Point3d.Origin, "2.0", "2.0", "0.0");

                        // Step 4: Explode and transform outlines
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                            ObjectId lastId = btr.Cast<ObjectId>().LastOrDefault();
                            
                            if (lastId != cloneId && lastId != ObjectId.Null)
                            {
                                AcadEntity wmfEnt = tr.GetObject(lastId, OpenMode.ForWrite) as AcadEntity;
                                if (wmfEnt is BlockReference wmfBlock)
                                {
                                    DBObjectCollection explodedObjects = new DBObjectCollection();
                                    wmfBlock.Explode(explodedObjects);

                                    Extents3d wmfExt = new Extents3d();
                                    bool hasExt = false;
                                    foreach (DBObject obj in explodedObjects)
                                    {
                                        if (obj is AcadEntity e && e is Curve)
                                        {
                                            try { wmfExt.AddExtents(e.GeometricExtents); hasExt = true; } catch {}
                                        }
                                    }

                                    if (hasExt)
                                    {
                                        double wmfHeight = wmfExt.MaxPoint.Y - wmfExt.MinPoint.Y;
                                        double scale = 1.0;
                                        if (wmfHeight > 0.00001 && originalHeight > 0.00001)
                                        {
                                            scale = originalHeight / wmfHeight;
                                        }

                                        Point3d wmfCenter = wmfExt.MinPoint + ((wmfExt.MaxPoint - wmfExt.MinPoint) / 2.0);
                                        Matrix3d alignMat = Matrix3d.Displacement(Point3d.Origin - wmfCenter);
                                        Matrix3d scaleMat = Matrix3d.Scaling(scale, Point3d.Origin);
                                        Matrix3d rotMat = Matrix3d.Rotation(originalRotation, Vector3d.ZAxis, Point3d.Origin);
                                        Matrix3d posMat = Matrix3d.Displacement(originalCenter - Point3d.Origin);
                                        Matrix3d finalMat = posMat * rotMat * scaleMat * alignMat;

                                        foreach (DBObject obj in explodedObjects)
                                        {
                                            if (obj is Curve curve)
                                            {
                                                curve.TransformBy(finalMat);
                                                outlines.Add(curve);
                                            }
                                            else
                                            {
                                                obj.Dispose();
                                            }
                                        }
                                    }
                                    wmfBlock.Erase();
                                }
                            }

                            // Clean up clone
                            try
                            {
                                AcadEntity clone = (AcadEntity)tr.GetObject(cloneId, OpenMode.ForWrite);
                                clone.Erase();
                            }
                            catch {}

                            tr.Commit();
                        }
                    }
                    finally
                    {
                        Application.SetSystemVariable("BACKGROUNDPLOT", oldBgPlot);
                        Application.SetSystemVariable("CMDECHO", oldCmdecho);
                        Application.SetSystemVariable("FILEDIA", oldFileDia);
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n[Cảnh báo] Lỗi sinh nét vẽ chữ: {ex.Message}");
                }
                finally
                {
                    if (File.Exists(tempFile))
                    {
                        try { File.Delete(tempFile); } catch {}
                    }
                    ed.SetCurrentView(view);
                }
            }
            return outlines;
        }

        private static void ZoomToExtentsHelper(Editor ed, Extents3d ext, double factor)
        {
            using (ViewTableRecord view = ed.GetCurrentView())
            {
                Point3d min = ext.MinPoint;
                Point3d max = ext.MaxPoint;
                
                Point2d min2d = new Point2d(min.X, min.Y);
                Point2d max2d = new Point2d(max.X, max.Y);
                
                view.CenterPoint = min2d + ((max2d - min2d) / 2.0);
                view.Height = (max2d.Y - min2d.Y) * factor;
                view.Width = (max2d.X - min2d.X) * factor;
                
                ed.SetCurrentView(view);
            }
        }
    }
}
