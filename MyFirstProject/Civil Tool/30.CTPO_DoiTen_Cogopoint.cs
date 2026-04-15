using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.DatabaseServices;
using MyFirstProject.Extensions;
using MyFirstProject.Civil_Tool;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.CTPO_DoiTen_Cogopoint_Commands))]

namespace Civil3DCsharp
{
    class CTPO_DoiTen_Cogopoint_Commands
    {
        [CommandMethod("CTPO_DoiTen_Cogopoint")]
        public static void CTPO_DoiTen_Cogopoint()
        {
            // Show the Name Template form
            using (DoiTenCogopointForm form = new DoiTenCogopointForm())
            {
                // Show dialog
                DialogResult result = Autodesk.AutoCAD.ApplicationServices.Application
                    .ShowModalDialog(form);

                if (result != DialogResult.OK || !form.FormAccepted)
                {
                    A.Ed.WriteMessage("\nĐã hủy lệnh đổi tên CogoPoint.");
                    return;
                }

                // Get template settings
                int counter = form.StartingNumber;
                int increment = form.IncrementValue;

                A.Ed.WriteMessage($"\nTemplate: {form.NameTemplate}");
                A.Ed.WriteMessage("\nChọn các CogoPoint cần đổi tên (quét chọn nhiều điểm)...");

                // Prompt user to select multiple CogoPoints using selection set
                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = "\n Chọn các CogoPoint cần đổi tên: ";
                pso.AllowDuplicates = false;

                // Filter only CogoPoint objects
                TypedValue[] filterList = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start, "AECC_COGO_POINT")
                };
                SelectionFilter filter = new SelectionFilter(filterList);

                PromptSelectionResult selResult = A.Ed.GetSelection(pso, filter);

                if (selResult.Status != PromptStatus.OK || selResult.Value == null)
                {
                    A.Ed.WriteMessage("\nKhông có CogoPoint nào được chọn.");
                    return;
                }

                ObjectId[] selectedIds = selResult.Value.GetObjectIds();

                if (selectedIds.Length == 0)
                {
                    A.Ed.WriteMessage("\nKhông có CogoPoint nào được chọn.");
                    return;
                }

                A.Ed.WriteMessage($"\nĐã chọn {selectedIds.Length} CogoPoint. Đang chuẩn bị xem trước...");

                // ===== PHASE 1: Build preview data (read-only) =====
                var renameList = new List<CogoPointRenameInfo>();
                var sortedIds = new List<(ObjectId Id, uint PointNumber)>();

                using (Transaction trRead = A.Db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // Collect point info for sorting
                        foreach (ObjectId id in selectedIds)
                        {
                            CogoPoint? cp = trRead.GetObject(id, OpenMode.ForWrite) as CogoPoint;
                            if (cp != null)
                            {
                                sortedIds.Add((id, cp.PointNumber));
                            }
                        }

                        // Sort by PointNumber
                        sortedIds = sortedIds.OrderBy(p => p.PointNumber).ToList();

                        // Generate preview names
                        int tempCounter = counter;
                        foreach (var item in sortedIds)
                        {
                            CogoPoint? cp = trRead.GetObject(item.Id, OpenMode.ForWrite) as CogoPoint;
                            if (cp != null)
                            {
                                string newName = form.GenerateName(
                                    tempCounter,
                                    cp.PointNumber,
                                    cp.PointName,
                                    cp.RawDescription ?? "",
                                    cp.Easting,
                                    cp.Northing,
                                    cp.Elevation
                                );

                                renameList.Add(new CogoPointRenameInfo
                                {
                                    PointNumber = cp.PointNumber,
                                    OldName = cp.PointName,
                                    NewName = newName,
                                    Description = cp.RawDescription ?? "",
                                    Easting = cp.Easting,
                                    Northing = cp.Northing,
                                    Elevation = cp.Elevation
                                });

                                tempCounter += increment;
                            }
                        }

                        trRead.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\nLỗi khi đọc dữ liệu: {ex.Message}");
                        trRead.Abort();
                        return;
                    }
                }

                if (renameList.Count == 0)
                {
                    A.Ed.WriteMessage("\nKhông có CogoPoint nào hợp lệ.");
                    return;
                }

                // ===== PHASE 2: Show Preview Form (có thể Export/Import Excel chỉnh sửa) =====
                using (var previewForm = new DoiTenCogopointPreviewForm(renameList))
                {
                    DialogResult previewResult = Autodesk.AutoCAD.ApplicationServices.Application
                        .ShowModalDialog(previewForm);

                    if (previewResult != DialogResult.OK || !previewForm.FormAccepted)
                    {
                        A.Ed.WriteMessage("\nĐã hủy đổi tên CogoPoint.");
                        return;
                    }

                    // Lấy lại danh sách đã cập nhật (có thể đã import từ Excel)
                    renameList = previewForm.GetUpdatedRenameList();
                }

                // ===== PHASE 3: Execute rename =====
                int processedCount = 0;

                using (Transaction tr = A.Db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        for (int i = 0; i < sortedIds.Count; i++)
                        {
                            CogoPoint? cogoPoint = tr.GetObject(sortedIds[i].Id, OpenMode.ForWrite) as CogoPoint;
                            if (cogoPoint != null && i < renameList.Count)
                            {
                                cogoPoint.PointName = renameList[i].NewName;

                                A.Ed.WriteMessage($"\n  [{renameList[i].OldName}] → [{renameList[i].NewName}]");
                                processedCount++;
                            }
                        }

                        tr.Commit();
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        A.Ed.WriteMessage($"\nLỗi: {ex.Message}");
                        tr.Abort();
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\nLỗi không xác định: {ex.Message}");
                        tr.Abort();
                    }
                }

                // Final summary
                if (processedCount > 0)
                {
                    // Update all point groups
                    using (Transaction tr = A.Db.TransactionManager.StartTransaction())
                    {
                        try
                        {
                            UtilitiesC3D.UpdateAllPointGroup();
                            tr.Commit();
                        }
                        catch
                        {
                            tr.Abort();
                        }
                    }

                    A.Ed.Regen();
                    A.Ed.WriteMessage($"\n\nHoàn thành! Đã đổi tên {processedCount} CogoPoint.");
                }
                else
                {
                    A.Ed.WriteMessage("\nKhông có CogoPoint nào được đổi tên.");
                }
            }
        }
    }
}
