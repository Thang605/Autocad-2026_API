// (C) Copyright 2025 by T27
//
using System;
using System.IO;
using System.Collections.Generic;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(Civil3DCsharp.XrefToBlockConverter))]

namespace Civil3DCsharp
{
    /// <summary>
    /// Lệnh chuyển đổi Xref thành Block
    /// </summary>
    public class XrefToBlockConverter
    {
        /// <summary>
        /// Lệnh AT_XrefToBlock - Chuyển đổi Xref đã loaded thành Block (bỏ qua xref chưa load)
        /// Sử dụng Database.BindXrefs() để bind trực tiếp
        /// </summary>
        [CommandMethod("AT_XrefToBlock")]
        public static void AT_XrefToBlock()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== CHUYỂN ĐỔI XREF THÀNH BLOCK ===");
                ed.WriteMessage("\n(Chỉ xử lý Xref đã loaded, bỏ qua các Xref chưa load)\n");

                // Thu thập ObjectId của các xref đã loaded
                ObjectIdCollection loadedXrefIds = new ObjectIdCollection();
                List<string> loadedXrefNames = new List<string>();
                List<string> skippedXrefs = new List<string>();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    foreach (ObjectId btrId in bt)
                    {
                        BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                        if (btr != null && btr.IsFromExternalReference)
                        {
                            XrefStatus status = btr.XrefStatus;

                            if (status == XrefStatus.Resolved)
                            {
                                // Xref đã loaded - thêm vào danh sách bind
                                loadedXrefIds.Add(btrId);
                                loadedXrefNames.Add(btr.Name);
                                ed.WriteMessage($"\n  ✓ Loaded: {btr.Name}");
                            }
                            else
                            {
                                // Xref chưa load - bỏ qua
                                skippedXrefs.Add($"{btr.Name} ({status})");
                                ed.WriteMessage($"\n  ○ Bỏ qua ({status}): {btr.Name}");
                            }
                        }
                    }

                    tr.Commit();
                }

                if (loadedXrefIds.Count == 0)
                {
                    ed.WriteMessage("\n\nKhông có Xref đã loaded nào để xử lý.");
                    return;
                }

                ed.WriteMessage($"\n\nTìm thấy: {loadedXrefIds.Count} Xref đã loaded");
                if (skippedXrefs.Count > 0)
                    ed.WriteMessage($", {skippedXrefs.Count} Xref bỏ qua");

                // Bind tất cả xref đã loaded cùng lúc
                ed.WriteMessage("\n\nĐang bind các Xref...");

                try
                {
                    // BindXrefs: true = Bind (chuyển thành block với prefix $0$)
                    //            false = Insert (chuyển thành block không có prefix)
                    db.BindXrefs(loadedXrefIds, false); // false = Insert style (không có $0$ prefix)
                    
                    ed.WriteMessage($"\n\n=== HOÀN TẤT ===");
                    ed.WriteMessage($"\nĐã bind thành công {loadedXrefIds.Count} Xref:");
                    foreach (string name in loadedXrefNames)
                    {
                        ed.WriteMessage($"\n  ✓ {name}");
                    }
                }
                catch (System.Exception bindEx)
                {
                    ed.WriteMessage($"\n\n! Lỗi khi bind: {bindEx.Message}");
                    
                    // Thử bind từng xref một nếu bind tất cả bị lỗi
                    ed.WriteMessage("\n\nĐang thử bind từng Xref một...");
                    int successCount = 0;
                    
                    for (int i = 0; i < loadedXrefIds.Count; i++)
                    {
                        try
                        {
                            ObjectIdCollection singleId = new ObjectIdCollection();
                            singleId.Add(loadedXrefIds[i]);
                            db.BindXrefs(singleId, false);
                            ed.WriteMessage($"\n  ✓ {loadedXrefNames[i]}");
                            successCount++;
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\n  ! {loadedXrefNames[i]}: {ex.Message}");
                        }
                    }
                    
                    ed.WriteMessage($"\n\nKết quả: {successCount}/{loadedXrefIds.Count} Xref đã bind thành công.");
                }

                ed.WriteMessage("\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nLỗi: {ex.Message}");
            }
        }
    }
}
