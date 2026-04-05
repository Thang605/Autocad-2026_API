// Lệnh: AT_UpdateLayoutSectionview
// Chức năng: Cập nhật layout cho Section View Group (sắp xếp lại vị trí các cắt ngang)
//
using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.Civil.DatabaseServices;

using MyFirstProject.Extensions;
using MyFirstProject.Civil_Tool;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(Civil3DCsharp.UpdateLayoutSectionview))]

namespace Civil3DCsharp
{
    public class UpdateLayoutSectionview
    {
        [CommandMethod("AT_UpdateLayoutSectionview")]
        public static void ATUpdateLayoutSectionview()
        {
            var ed = A.Ed;

            try
            {
                // === HIỂN THỊ FORM ĐỂ CHỌN SECTION VIEW GROUPS ===
                List<SectionViewGroupItem> selectedItems;

                using (var form = new UpdateLayoutSectionviewForm())
                {
                    var dialogResult = Application.ShowModalDialog(form);
                    if (dialogResult != System.Windows.Forms.DialogResult.OK || !form.FormAccepted)
                    {
                        ed.WriteMessage("\n Đã hủy lệnh.");
                        return;
                    }

                    selectedItems = form.SelectedItems;
                    if (selectedItems.Count == 0)
                    {
                        ed.WriteMessage("\n Không có Section View Group nào được chọn.");
                        return;
                    }
                }

                // === THỰC HIỆN UPDATE LAYOUT ===
                int successCount = 0;
                int failCount = 0;

                using (Transaction tr = A.Db.TransactionManager.StartTransaction())
                {
                    foreach (SectionViewGroupItem svgItem in selectedItems)
                    {
                        try
                        {
                            // Mở lại SampleLineGroup → lấy SectionViewGroup theo index
                            SampleLineGroup? slGroup = tr.GetObject(svgItem.SampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;
                            if (slGroup == null)
                            {
                                failCount++;
                                ed.WriteMessage($"\n  ❌ Không mở được SampleLineGroup: {svgItem.SampleLineGroupName}");
                                continue;
                            }

                            SectionViewGroupCollection svGroups = slGroup.SectionViewGroups;
                            if (svgItem.SectionViewGroupIndex >= svGroups.Count)
                            {
                                failCount++;
                                ed.WriteMessage($"\n  ❌ SectionViewGroup index không hợp lệ: {svgItem.SectionViewGroupName}");
                                continue;
                            }

                            SectionViewGroup svg = svGroups[svgItem.SectionViewGroupIndex];
                            svg.UpdateLayout();
                            successCount++;
                            ed.WriteMessage($"\n  ✅ Đã cập nhật: {svg.Name}");
                        }
                        catch (System.Exception ex)
                        {
                            failCount++;
                            ed.WriteMessage($"\n  ❌ Lỗi khi cập nhật '{svgItem.SectionViewGroupName}': {ex.Message}");
                        }
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\n\n✅ Hoàn thành! Đã cập nhật {successCount} Section View Group(s).");
                if (failCount > 0)
                    ed.WriteMessage($"\n⚠ {failCount} group(s) bị lỗi.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n❌ Lỗi: " + ex.Message);
            }
        }
    }
}
