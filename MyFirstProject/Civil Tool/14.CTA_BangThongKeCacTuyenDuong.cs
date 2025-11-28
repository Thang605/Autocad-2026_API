// (C) Copyright 2015 by  
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Acad = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using ATable = Autodesk.AutoCAD.DatabaseServices.Table;

using Civil = Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.Civil.Runtime;
using Autodesk.Civil.Settings;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Civil.ApplicationServices;
using CivSurface = Autodesk.Civil.DatabaseServices.TinSurface;
using Section = Autodesk.Civil.DatabaseServices.Section;
using Autodesk.Civil;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using MyFirstProject.Extensions;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(MyFirstProject.CTA_BangThongKeCacTuyenDuong_Commands))]

namespace MyFirstProject
{
    public class CTA_BangThongKeCacTuyenDuong_Commands
    {
        [CommandMethod("CTA_BangThongKeCacTuyenDuong")]

        public static void CTABangThongKeCacTuyenDuong()
        {
            // start transaction
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();

                A.Ed.WriteMessage("\nTạo bảng thống kê các tuyến đường...");

                // Step 1: Select multiple alignments
                A.Ed.WriteMessage("\nChọn các tuyến đường cần xuất thông tin:");
                
                PromptSelectionOptions pso = new()
                {
                    MessageForAdding = "\nChọn các alignment (tuyến đường): ",
                    AllowDuplicates = false
                };

                // Create selection filter for alignments only
                TypedValue[] filterList =
                [
                    new((int)DxfCode.Start, "AECC_ALIGNMENT")
                ];
                SelectionFilter filter = new(filterList);

                PromptSelectionResult psr = A.Ed.GetSelection(pso, filter);
                if (psr.Status != PromptStatus.OK)
                {
                    A.Ed.WriteMessage("\nĐã hủy lệnh hoặc không chọn được alignment.");
                    return;
                }

                ObjectId[] selectedIds = psr.Value.GetObjectIds();
                if (selectedIds.Length == 0)
                {
                    A.Ed.WriteMessage("\nKhông có alignment nào được chọn.");
                    return;
                }

                A.Ed.WriteMessage($"\nĐã chọn {selectedIds.Length} tuyến đường.");

                // Step 2: Collect alignment information
                List<AlignmentInfo> alignmentData = [];

                foreach (ObjectId alignmentId in selectedIds)
                {
                    try
                    {
                        Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                        if (alignment != null)
                        {
                            AlignmentInfo info = new()
                            {
                                TenDuong = alignment.Name ?? "Không có tên",
                                MoTa = alignment.Description ?? "Không có mô tả",
                                ChieuDaiTuyen = Math.Round(alignment.Length, 3)
                            };

                            alignmentData.Add(info);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        A.Ed.WriteMessage($"\nLỗi đọc alignment: {ex.Message}");
                        continue;
                    }
                }

                if (alignmentData.Count == 0)
                {
                    A.Ed.WriteMessage("\nKhông có dữ liệu alignment hợp lệ để tạo bảng.");
                    return;
                }

                // Step 2.5: Sort by road name (alphabetical order) and assign sequential numbers
                alignmentData = [.. alignmentData.OrderBy(x => x.TenDuong)];
                
                // Assign sequential numbers after sorting
                for (int i = 0; i < alignmentData.Count; i++)
                {
                    alignmentData[i].SoThuTu = i + 1;
                    A.Ed.WriteMessage($"\nTuyến {alignmentData[i].SoThuTu}: {alignmentData[i].TenDuong} - Dài: {alignmentData[i].ChieuDaiTuyen:F3}m");
                }

                // Step 3: Create table
                A.Ed.WriteMessage("\nChọn vị trí đặt bảng thống kê:");
                PromptPointResult ppr = A.Ed.GetPoint("\nChọn điểm đặt bảng: ");
                if (ppr.Status != PromptStatus.OK)
                {
                    A.Ed.WriteMessage("\nĐã hủy lệnh.");
                    return;
                }

                Point3d insertionPoint = ppr.Value;

                // Create table with data
#pragma warning disable CS0612 // Type or member is obsolete
                CreateAlignmentTable(alignmentData, insertionPoint, tr);
#pragma warning restore CS0612 // Type or member is obsolete

                A.Ed.WriteMessage($"\nĐã tạo thành công bảng thống kê {alignmentData.Count} tuyến đường.");

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage($"\nLỗi AutoCAD: {e.Message}");
                A.Ed.WriteMessage($"\nError Code: {e.ErrorStatus}");
                tr.Abort();
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi hệ thống: {ex.Message}");
                tr.Abort();
            }
        }

        [Obsolete]
        private static void CreateAlignmentTable(List<AlignmentInfo> alignmentData, Point3d insertionPoint, Transaction tr)
        {
            try
            {
                // Get current database
                Database db = A.Db;
                
                // Get ModelSpace for writing
                BlockTable? bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (bt == null) return;
                
                BlockTableRecord? btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                if (btr == null) return;

                // Create table
                int numRows = alignmentData.Count + 2; // Data rows + header + title
                int numCols = 4; // STT, Tên đường, Mô tả, Chiều dài

                ATable table = new();
                table.SetSize(numRows, numCols);
                table.Position = insertionPoint;

                // Set table style properties
                table.TableStyle = db.Tablestyle; // Use current table style

                // Set column widths
                table.SetColumnWidth(0, 15.0); // STT
                table.SetColumnWidth(1, 60.0); // Tên đường  
                table.SetColumnWidth(2, 80.0); // Mô tả
                table.SetColumnWidth(3, 25.0); // Chiều dài

                // Set row heights
                for (int i = 0; i < numRows; i++)
                {
                    table.SetRowHeight(i, 8.0);
                }

                // Title row (merge all columns)
                table.MergeCells(CellRange.Create(table, 0, 0, 0, numCols - 1));
                table.Cells[0, 0].TextString = "BẢNG THỐNG KÊ CÁC TUYẾN ĐƯỜNG";
                table.Cells[0, 0].Alignment = CellAlignment.MiddleCenter;
                table.Cells[0, 0].TextHeight = 6.0;

                // Header row
                string[] headers = ["STT", "TÊN ĐƯỜNG", "MÔ TẢ", "CHIỀU DÀI (m)"];
                for (int col = 0; col < numCols; col++)
                {
                    table.Cells[1, col].TextString = headers[col];
                    table.Cells[1, col].Alignment = CellAlignment.MiddleCenter;
                    table.Cells[1, col].TextHeight = 4.0;
                }

                // Data rows
                for (int i = 0; i < alignmentData.Count; i++)
                {
                    int row = i + 2; // Skip title and header rows
                    AlignmentInfo info = alignmentData[i];

                    table.Cells[row, 0].TextString = info.SoThuTu.ToString();
                    table.Cells[row, 0].Alignment = CellAlignment.MiddleCenter;
                    table.Cells[row, 0].TextHeight = 3.5;

                    table.Cells[row, 1].TextString = info.TenDuong;
                    table.Cells[row, 1].Alignment = CellAlignment.MiddleLeft;
                    table.Cells[row, 1].TextHeight = 3.5;

                    table.Cells[row, 2].TextString = info.MoTa;
                    table.Cells[row, 2].Alignment = CellAlignment.MiddleLeft;
                    table.Cells[row, 2].TextHeight = 3.5;

                    table.Cells[row, 3].TextString = info.ChieuDaiTuyen.ToString("F3");
                    table.Cells[row, 3].Alignment = CellAlignment.MiddleCenter;
                    table.Cells[row, 3].TextHeight = 3.5;
                }

                // Add table to database
                btr.AppendEntity(table);
                tr.AddNewlyCreatedDBObject(table, true);

                A.Ed.WriteMessage($"\nĐã tạo bảng tại tọa độ: X={insertionPoint.X:F3}, Y={insertionPoint.Y:F3}");
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi tạo bảng: {ex.Message}");
            }
        }

        // Helper class to store alignment information
        private class AlignmentInfo
        {
            public int SoThuTu { get; set; }
            public string TenDuong { get; set; } = "";
            public string MoTa { get; set; } = "";
            public double ChieuDaiTuyen { get; set; }
        }
    }
}
