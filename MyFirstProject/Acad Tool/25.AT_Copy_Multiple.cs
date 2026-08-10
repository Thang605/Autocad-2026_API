// (C) Copyright 2026 by T27
// Lệnh C - Copy đối tượng nhiều lần (Multiple Copy)
// - Hỗ trợ chọn đối tượng trước (PickSet) hoặc chọn sau
// - Cho phép chọn điểm gốc (Base Point) và nhấp chọn nhiều điểm đến liên tiếp
// - Hỗ trợ tùy chọn Undo (quay lại) và Exit (thoát)
//

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;

[assembly: CommandClass(typeof(Civil3DCsharp.AT_Copy_Multiple_Commands))]

namespace Civil3DCsharp
{
    public class AT_Copy_Multiple_Commands
    {
        /// <summary>
        /// Lệnh C: Copy đối tượng nhiều lần (Multiple Copy)
        /// </summary>
        [CommandMethod("C", CommandFlags.UsePickSet | CommandFlags.Redraw | CommandFlags.Modal)]
        [CommandMethod("CMULTIPLE", CommandFlags.UsePickSet | CommandFlags.Redraw | CommandFlags.Modal)]
        [CommandMethod("AT_COPY_MULTIPLE", CommandFlags.UsePickSet | CommandFlags.Redraw | CommandFlags.Modal)]
        public static void CopyMultipleCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                // 1. Lấy danh sách đối tượng chọn (PickSet trước hoặc chọn mới)
                PromptSelectionResult selRes = ed.SelectImplied();
                if (selRes.Status != PromptStatus.OK || selRes.Value == null || selRes.Value.Count == 0)
                {
                    PromptSelectionOptions pso = new PromptSelectionOptions
                    {
                        MessageForAdding = "\nChọn các đối tượng cần copy: "
                    };
                    selRes = ed.GetSelection(pso);
                }

                if (selRes.Status != PromptStatus.OK || selRes.Value == null || selRes.Value.Count == 0)
                {
                    ed.WriteMessage("\nChưa chọn đối tượng nào.");
                    return;
                }

                // Bỏ chọn Implied selection để hiển thị màn hình rõ ràng
                ed.SetImpliedSelection(new ObjectId[0]);

                ObjectId[] selectedIds = selRes.Value.GetObjectIds();

                // 2. Chọn điểm gốc (Base Point)
                PromptPointOptions ppoBase = new PromptPointOptions("\nChọn điểm gốc (Base point): ")
                {
                    AllowNone = false
                };
                PromptPointResult pprBase = ed.GetPoint(ppoBase);

                if (pprBase.Status != PromptStatus.OK)
                {
                    return;
                }

                Point3d basePoint = pprBase.Value;

                // Lịch sử các lần copy để hỗ trợ Undo
                Stack<List<ObjectId>> historyStack = new Stack<List<ObjectId>>();
                int copyStepCount = 0;

                // 3. Vòng lặp copy liên tiếp (Multiple Copy Loop)
                while (true)
                {
                    PromptPointOptions ppoNext = new PromptPointOptions("\nChọn điểm đến tiếp theo [Undo/Exit] <Exit>: ")
                    {
                        UseBasePoint = true,
                        BasePoint = basePoint,
                        UseDashedLine = true,
                        AllowNone = true
                    };

                    ppoNext.Keywords.Add("Undo", "U", "Undo (U)");
                    ppoNext.Keywords.Add("Exit", "E", "Exit (E)");
                    ppoNext.Keywords.Default = "Exit";

                    PromptPointResult pprNext = ed.GetPoint(ppoNext);

                    if (pprNext.Status == PromptStatus.Keyword)
                    {
                        string kw = pprNext.StringResult.ToUpper();
                        if (kw == "UNDO" || kw == "U")
                        {
                            if (historyStack.Count > 0)
                            {
                                List<ObjectId> lastCreated = historyStack.Pop();
                                using (Transaction tr = db.TransactionManager.StartTransaction())
                                {
                                    foreach (ObjectId id in lastCreated)
                                    {
                                        if (id.IsValid && !id.IsErased)
                                        {
                                            Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                                            ent?.Erase(true);
                                        }
                                    }
                                    tr.Commit();
                                }
                                copyStepCount--;
                                ed.WriteMessage("\nĐã Hoàn tác (Undo) lần copy gần nhất.");
                                ed.Regen();
                            }
                            else
                            {
                                ed.WriteMessage("\nKhông còn thao tác copy nào để Undo.");
                            }
                            continue;
                        }
                        else // Exit
                        {
                            break;
                        }
                    }
                    else if (pprNext.Status == PromptStatus.OK)
                    {
                        Point3d destPoint = pprNext.Value;
                        Vector3d displacement = destPoint - basePoint;

                        List<ObjectId> createdIdsThisStep = new List<ObjectId>();

                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                            foreach (ObjectId id in selectedIds)
                            {
                                if (id.IsValid && !id.IsErased)
                                {
                                    DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                                    Entity ent = obj as Entity;
                                    if (ent != null)
                                    {
                                        Entity clone = ent.Clone() as Entity;
                                        if (clone != null)
                                        {
                                            clone.TransformBy(Matrix3d.Displacement(displacement));
                                            ObjectId newId = currentSpace.AppendEntity(clone);
                                            tr.AddNewlyCreatedDBObject(clone, true);
                                            createdIdsThisStep.Add(newId);
                                        }
                                    }
                                }
                            }

                            tr.Commit();
                        }

                        if (createdIdsThisStep.Count > 0)
                        {
                            historyStack.Push(createdIdsThisStep);
                            copyStepCount++;
                            ed.WriteMessage($"\n[Lần {copyStepCount}] Đã copy {createdIdsThisStep.Count} đối tượng.");
                        }
                    }
                    else
                    {
                        // Nhấn ESC, Enter, Cancel hoặc rỗng
                        break;
                    }
                }

                if (copyStepCount > 0)
                {
                    ed.WriteMessage($"\nLệnh C (Copy Multiple) hoàn tất: Tổng cộng {copyStepCount} lần copy.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nLỗi khi thực thi lệnh C: {ex.Message}");
            }
        }
    }
}
