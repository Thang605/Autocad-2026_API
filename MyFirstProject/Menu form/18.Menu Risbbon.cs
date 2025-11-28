using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Windows;

[assembly: CommandClass(typeof(MyFirstProject.Autocad))]

namespace MyFirstProject
{
    public class Autocad
    {
        [CommandMethod("ShowForm")]
        public static void ShowForm()
        {
            TestForm frmTest = new();
            frmTest.Show();
        }

        [CommandMethod("AdskGreeting")]
        public static void AdskGreeting()
        {
            Document? acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument ?? throw new InvalidOperationException("No active document found.");
            Database? acCurDb = acDoc.Database ?? throw new InvalidOperationException("No database found for the active document.");

            using Transaction acTrans = acCurDb.TransactionManager.StartTransaction();
            BlockTable acBlkTbl = (BlockTable)acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) ?? throw new InvalidOperationException("BlockTable could not be retrieved.");
            BlockTableRecord acBlkTblRec = (BlockTableRecord)acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) ?? throw new InvalidOperationException("BlockTableRecord could not be retrieved.");

            using (MText objText = new())
            {
                objText.Location = new Autodesk.AutoCAD.Geometry.Point3d(2, 2, 0);
                objText.Contents = "Greetings, Welcome to AutoCAD .NET";
                objText.TextStyleId = acCurDb.Textstyle;
                acBlkTblRec.AppendEntity(objText);
                acTrans.AddNewlyCreatedDBObject(objText, true);
            }
            acTrans.Commit();
        }

        [CommandMethod("show_menu")]
        public static void ShowMenu()
        {
            try
            {
                var ribbon = ComponentManager.Ribbon;
                if (ribbon == null)
                {
                    var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                    doc?.SendStringToExecute("RIBBON ", true, false, false);
                    ribbon = ComponentManager.Ribbon;
                    if (ribbon == null)
                    {
                        doc?.Editor.WriteMessage("\nKhông thể khởi tạo Ribbon. Hãy bật RIBBON rồi chạy lại lệnh.");
                        return;
                    }
                }

                // Remove previous tab if exists
                var existing = ribbon.Tabs.FirstOrDefault(t => t.Id == "MyFirstProject.C3DTab");
                if (existing != null)
                {
                    ribbon.Tabs.Remove(existing);
                }
                var existingAcad = ribbon.Tabs.FirstOrDefault(t => t.Id == "MyFirstProject.AcadTab");
                if (existingAcad != null)
                {
                    ribbon.Tabs.Remove(existingAcad);
                }

                // Create new Civil tool tab
                RibbonTab tab = new()
                {
                    Title = "Civil tool",
                    Id = "MyFirstProject.C3DTab"
                };
                ribbon.Tabs.Add(tab);

                // Create new Acad tool tab
                RibbonTab acadTab = new()
                {
                    Title = "Acad tool",
                    Id = "MyFirstProject.AcadTab"
                };
                ribbon.Tabs.Add(acadTab);

                // Helper: add a dropdown panel for Civil tool from grouped commands
                void AddCivilDropdownPanel(RibbonTab targetTab, string panelTitle, (string GroupTitle, (string Command, string Label)[] Commands)[] groups)
                {
                    if (groups.Length == 0) return;

                    RibbonPanelSource src = new() { Title = panelTitle };
                    RibbonPanel panel = new() { Source = src };
                    RibbonSplitButton splitButton = new()
                    {
                        Text = panelTitle,
                        ShowText = true,
                        ShowImage = false,
                        Size = RibbonItemSize.Large,
                        Width = 120
                    };

                    for (int g = 0; g < groups.Length; g++)
                    {
                        var (_, cmds) = groups[g];
                        int groupNumber = g + 1; // group index per panel starts at 1
                        for (int i = 0; i < cmds.Length; i++)
                        {
                            var (command, label) = cmds[i];
                            string prefixedLabel = $"{groupNumber}.{label}"; // structure: groupIndex.label

                            RibbonButton btn = new()
                            {
                                Text = prefixedLabel,
                                ShowText = true,
                                ShowImage = false,
                                Orientation = System.Windows.Controls.Orientation.Vertical,
                                Size = RibbonItemSize.Large,
                                CommandHandler = new SimpleRibbonCommandHandler(),
                                Tag = command
                            };
                            splitButton.Items.Add(btn);
                        }
                    }

                    src.Items.Add(splitButton);
                    targetTab.Panels.Add(panel);
                }

                // Helper: add a dropdown panel for Acad tool from grouped commands
                void AddDropdownPanel(RibbonTab targetTab, string title, (string GroupTitle, (string Command, string Label)[] Commands)[] groups)
                {
                    RibbonPanelSource src = new() { Title = title };
                    RibbonPanel panel = new() { Source = src };
                    RibbonSplitButton splitButton = new()
                    {
                        Text = title,
                        ShowText = true,
                        ShowImage = false,
                        Size = RibbonItemSize.Large,
                        Width = 120
                    };

                    for (int g = 0; g < groups.Length; g++)
                    {
                        var (_, cmds) = groups[g];
                        int groupNumber = g + 1; // group index per panel starts at 1
                        for (int i = 0; i < cmds.Length; i++)
                        {
                            var (command, label) = cmds[i];
                            string prefixedLabel = $"{groupNumber}.{label}";

                            RibbonButton btn = new()
                            {
                                Text = prefixedLabel,
                                ShowText = true,
                                ShowImage = false,
                                Orientation = System.Windows.Controls.Orientation.Vertical,
                                Size = RibbonItemSize.Large,
                                CommandHandler = new SimpleRibbonCommandHandler(),
                                Tag = command
                            };
                            splitButton.Items.Add(btn);
                        }
                    }

                    src.Items.Add(splitButton);
                    targetTab.Panels.Add(panel);
                }

                // Grouped commands per panel
                // Alignment - Di chuyển lên đầu và xóa lệnh Test Command
                (string GroupTitle, (string Command, string Label)[] Commands)[] alignmentGroups =
                [
                    ("Offset", [
                        ("AT_OffsetAlignment", "Offset from Alignment")
                    ]),
                    ("Thống kê", [
                        ("CTA_BangThongKeCacTuyenDuong", "Bảng Thống Kê Tuyến Đường")
                    ])
                ];

                // Corridor
                (string GroupTitle, (string Command, string Label)[] Commands)[] corridorGroups =
                [
                    ("Thiết lập/Thêm", [
                        ("CTC_AddAllSection", "Add All Section"),
                        ("CTC_TaoCooridor_DuongDoThi_RePhai", "Tạo Corridor Rẽ Phải")
                    ]),
                    ("Điều chỉnh", [
                        ("CTC_DieuChinh_PhanDoan", "Điều Chỉnh Phân Đoạn")
                    ])
                ];

                // Pipe Network
                (string GroupTitle, (string Command, string Label)[] Commands)[] pipeGroups =
                [
                    ("Thay đổi thông số", [
                        ("CTPI_ThayDoi_DuongKinhCong", "Thay Đổi Đường Kính Cống"),
                        ("CTPI_ThayDoi_MatPhangRef_Cong", "Thay Đổi Mặt Phẳng Ref Cống"),
                        ("CTPI_ThayDoi_DoanDocCong", "Thay Đổi Độ Dốc Cống")
                    ]),
                    ("Bảng/Xoay", [
                        ("CTPI_BangCaoDo_TuNhienHoThu", "Bảng Cao Độ Hố Thu"),
                        ("CTPI_XoayHoThu_Theo2diem", "Xoay Hố Thu Theo 2 Điểm")
                    ]),
                    ("Bề mặt tham chiếu", [
                        ("CTPi_DieuChinh_BeMat_ThamChieu", "Điều Chỉnh Bề Mặt Tham Chiếu")
                    ])
                ];

                // Property Sets
                (string GroupTitle, (string Command, string Label)[] Commands)[] propertySetGroups =
                [
                    ("3D Solid", [
                        ("AT_Solid_Set_PropertySet", "Set PropertySet 3D Solid"),
                        ("AT_Solid_Show_Info", "Show 3D Solid Info")
                    ])
                ];

                // Point
                (string GroupTitle, (string Command, string Label)[] Commands)[] pointGroups =
                [
                    ("Tạo CogoPoint", [
                        ("CTPO_TaoCogoPoint_CaoDo_FromSurface", "Tạo CogoPoint Từ Surface"),
                        ("CTPO_TaoCogoPoint_CaoDo_Elevationspot", "Tạo CogoPoint Từ Elevation Spot"),
                        ("CTPO_CreateCogopointFromText", "Tạo CogoPoint Từ Text")
                    ]),
                    ("Quản lý/Ẩn", [
                        ("CTPO_UpdateAllPointGroup", "Update All Point Group"),
                        ("CTPO_An_CogoPoint", "Ẩn CogoPoint")
                    ]),
                    ("Đổi tên", [
                        ("CTPo_DoiTen_CogoPoint_fromAlignment", "Đổi Tên CogoPoint Theo Alignment")
                    ])
                ];

                // Surface
                (string GroupTitle, (string Command, string Label)[] Commands)[] surfaceGroups =
                [
                    ("Cao độ mặt phẳng", [
                        ("CTSU_CaoDoMatPhang_TaiCogopoint", "Cao Độ Mặt Phẳng Tại CogoPoint")
                    ])
                ];

                // Profile - Updated to include the new command
                (string GroupTitle, (string Command, string Label)[] Commands)[] profileGroups =
                [
                    ("Vẽ trắc dọc", [
                        ("CTP_VeTracDoc_TuNhien", "Vẽ Trắc Dọc Tự Nhiên"),
                        ("CTP_VeTracDoc_TuNhien_TatCaTuyen", "Vẽ Trắc Dọc Tất Cả Tuyến")
                    ]),
                    ("Sửa/Gắn nhãn", [
                        ("CTP_Fix_DuongTuNhien_TheoCoc", "Sửa Đường Tự Nhiên Theo Cọc"),
                        ("CTP_GanNhanNutGiao_LenTracDoc", "Gắn Nhãn Nút Giao Lên Trắc Dộc")
                    ]),
                    ("Tạo điểm", [
                        ("CTP_TaoCogoPointTuPVI", "Tạo CogoPoint Từ PVI")
                    ]),
                    ("Band Profile", [
                        ("CTP_ThayDoi_profile_Band", "Thay Đổi Profile trong Band")
                    ])
                ];

                // Sampleline
                (string GroupTitle, (string Command, string Label)[] Commands)[] samplelineGroups =
                [
                    ("Đổi tên cọc", [
                        ("CTS_DoiTenCoc", "Đổi Tên Cọc"),
                        ("CTS_DoiTenCoc3", "Đổi Tên Cọc Km"),
                        ("CTS_DoiTenCoc2", "Đổi Tên Cọc Theo Đoạn"),
                        ("CTS_DoiTenCoc_fromCogoPoint", "Đổi Tên Cọc Từ CogoPoint"),
                        ("CTS_DoiTenCoc_TheoThuTu", "Đổi Tên Cọc Theo Thứ Tự"),
                        ("CTS_DoiTenCoc_H", "Đổi Tên Cọc H")
                    ]),
                    ("Bảng tọa độ/Update", [
                        ("CTS_TaoBang_ToaDoCoc", "Tạo Bảng Tọa Độ Cọc"),
                        ("CTS_TaoBang_ToaDoCoc2", "Tạo Bảng Tọa Độ Cọc (Lý Trình)"),
                        ("CTS_TaoBang_ToaDoCoc3", "Tạo Bảng Tọa Độ Cọc (Cao Độ)"),
                        ("AT_UPdate2Table", "Cập Nhật 2 Table")
                    ]),
                    ("Chèn/Phát sinh cọc", [
                        ("CTS_ChenCoc_TrenTracDoc", "Chèn Cọc Trên Trắc Dọc"),
                        ("CTS_CHENCOC_TRENTRACNGANG", "Chèn Cọc Trên Trắc Ngang"),
                        ("CTS_PhatSinhCoc", "Phát Sinh Cọc"),
                        ("CTS_PhatSinhCoc_ChiTiet", "Phát Sinh Cọc Chi Tiết"),
                        ("CTS_PhatSinhCoc_theoKhoangDelta", "Phát Sinh Cọc Theo Delta"),
                        ("CTS_PhatSinhCoc_TuCogoPoint", "Phát Sinh Cọc Từ CogoPoint"),
                        ("CTS_PhatSinhCoc_TheoBang", "Phát Sinh Cọc Theo Bảng")
                    ]),
                    ("Dịch/Copy/Đồng bộ", [
                        ("CTS_DichCoc_TinhTien", "Dịch Cọc Tịnh Tiến"),
                        ("CTS_Copy_NhomCoc", "Copy Nhóm Cọc"),
                        ("CTS_DongBo_2_NhomCoc", "Đồng Bộ 2 Nhóm Cọc"),
                        ("CTS_DongBo_2_NhomCoc_TheoDoan", "Đồng Bộ 2 Nhóm Cọc Theo Đoạn"),
                        ("CTS_DichCoc_TinhTien40", "Dịch Cọc 40m"),
                        ("CTS_DichCoc_TinhTien_20", "Dịch Cọc 20m")
                    ]),
                    ("Bề rộng Sample Line", [
                        ("CTS_Copy_BeRong_sampleLine", "Copy Bề Rộng Sample Line"),
                        ("CTS_Thaydoi_BeRong_sampleLine", "Thay Đổi Bề Rộng Sample Line"),
                        ("CTS_Offset_BeRong_sampleLine", "Offset Bề Rộng Sample Line")
                    ])
                ];

                // Section View
                (string GroupTitle, (string Command, string Label)[] Commands)[] sectionviewGroups =
                [
                    ("Vẽ trắc ngang", [
                        ("CTSV_VeTracNgangThietKe", "Vẽ Trắc Ngang Thiết Kế"),
                        ("CVSV_VeTatCa_TracNgangThietKe", "Vẽ Tất Cả Trắc Ngang")
                    ]),
                    ("Đánh cấp", [
                        ("CTSV_DanhCap", "Tính Đánh Cấp"),
                        ("CTSV_DanhCap_XoaBo", "Xóa Bỏ Đánh Cấp"),
                        ("CTSV_DanhCap_VeThem", "Vẽ Thêm Đánh Cấp"),
                        ("CTSV_DanhCap_VeThem2", "Vẽ Thêm Đánh Cấp 2m"),
                        ("CTSV_DanhCap_VeThem1", "Vẽ Thêm Đánh Cấp 1m"),
                        ("CTSV_DanhCap_CapNhat", "Cập Nhật Đánh Cấp")
                    ]),
                    ("Thiết lập/giới hạn", [
                        ("CTSV_ThayDoi_MSS_Min_Max", "Thay Đổi MSS Min Max"),
                        ("CTSV_ThayDoi_GioiHan_traiPhai", "Thay Đổi Giới Hạn Trái Phải")
                    ]),
                    ("Khung in", [
                        ("CTSV_ThayDoi_KhungIn", "Thay Đổi Khung In"),
                        ("CTSV_fit_KhungIn", "Fit Khung In"),
                        ("CTSV_fit_KhungIn_5_5_top", "Fit Khung In 5x5"),
                        ("CTSV_fit_KhungIn_5_10_top", "Fit Khung In 5x10")
                    ]),
                    ("Khóa/ẩn", [
                        ("CTSV_KhoaCatNgang_AddPoint", "Khóa Cắt Ngang Add Point"),
                        ("CTSV_An_DuongDiaChat", "Ẩn Đường Địa Chất")
                    ]),
                    ("Hiệu chỉnh", [
                        ("CTSV_HieuChinh_Section", "Hiệu Chỉnh Section Static"),
                        ("CTSV_HieuChinh_Section_Dynamic", "Hiệu Chỉnh Section Dynamic"),
                        ("CTSV_DieuChinh_DuongTuNhien", "Điều Chỉnh Đường Tự Nhiên")
                    ]),
                    ("Khác", [
                        ("CTSV_ChuyenDoi_TNTK_TNTN", "Chuyển Đổi TN-TK sang TN-TN"),
                        ("CTSV_ThemVatLieu_TrenCatNgang", "Thêm Vật Liệu Trên Cắt Ngang"),
                        ("CTSV_MaterialSection", "Xuất Thông Tin Material Section"),
                        ("AT_PolylineFromSection", "Tạo Polyline Từ Section"),
                        ("CTSV_XuatKhoiLuongRaExcel", "Xuất Khối Lượng ra Excel"),
                        ("CTSV_KhoiLuongCatNgang", "Khối Lượng Cắt Ngang")
                    ])
                ];

                // Acad tool
                (string GroupTitle, (string Command, string Label)[] Commands)[] acadGroups =
                [
                    ("Tổng độ dài", [
                        ("AT_TongDoDai_Full", "Tổng Độ Dài (Full)"),
                        ("AT_TongDoDai_Replace", "Tổng Độ Dài (Replace)"),
                        ("AT_TongDoDai_Replace2", "Tổng Độ Dài (Replace2)"),
                        ("AT_TongDoDai_Replace_CongThem", "Tổng Độ Dài (Cộng Thêm)")
                    ]),
                    ("Tổng diện tích", [
                        ("AT_TongDienTich_Full", "Tổng Diện Tích (Full)"),
                        ("AT_TongDienTich_Replace", "Tổng Diện Tích (Replace)"),
                        ("AT_TongDienTich_Replace2", "Tổng Diện Tích (Replace2)"),
                        ("AT_TongDienTich_Replace_CongThem", "Tổng Diện Tích (Cộng Thêm)")
                    ]),
                    ("Biên tập Text", [
                        ("AT_TextLink", "Text Link"),
                        ("AT_TextLayout", "Text Layout"),
                        ("AT_TaoMoi_TextLayout", "Tạo Mới Text Layout"),
                        ("AT_Label_FromText", "Label From Text"),
                        ("AT_DanhSoThuTu", "Đánh Số Thứ Tự")
                    ]),
                    ("Xoay đối tượng", [
                        ("AT_XoayDoiTuong_TheoViewport", "Xoay Đối Tượng Theo Viewport"),
                        ("AT_XoayDoiTuong_Theo2Diem", "Xoay Đối Tượng Theo 2 Điểm")
                    ]),
                    ("Layout", [
                        ("AT_DimLayout2", "Dim Layout 2"),
                        ("AT_DimLayout", "Dim Layout"),
                        ("AT_BlockLayout", "Block Layout"),
                        ("AT_UpdateLayout", "Update Layout")
                    ]),
                    ("Khác", [
                        ("AT_TaoOutline", "Tạo Outline"),
                        ("AT_XoaDoiTuong_CungLayer", "Xóa Đối Tượng Cùng Layer"),
                        ("AT_XoaDoiTuong_3DSolid_Body", "Xóa 3DSolid/Body"),
                        ("AT_Offset_2Ben", "Offset 2 Bên"),
                        ("AT_annotive_scale_currentOnly", "Annotative Scale Current Only")
                    ])
                ];

                // Add panels with Alignment moved to first position
                AddCivilDropdownPanel(tab, "Alignment", alignmentGroups);
                AddCivilDropdownPanel(tab, "Corridor", corridorGroups);
                AddCivilDropdownPanel(tab, "Pipe Network", pipeGroups);
                AddCivilDropdownPanel(tab, "Point", pointGroups);
                AddCivilDropdownPanel(tab, "Profile", profileGroups);
                AddCivilDropdownPanel(tab, "Surface", surfaceGroups);
                AddCivilDropdownPanel(tab, "Sampleline", samplelineGroups);
                AddCivilDropdownPanel(tab, "Section View", sectionviewGroups);
                AddCivilDropdownPanel(tab, "Property Sets", propertySetGroups);

                AddDropdownPanel(acadTab, "CAD Commands", acadGroups);

                tab.IsActive = true;
                var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?.Editor;
                ed?.WriteMessage("\nĐã tạo tab 'Civil tool' và 'Acad tool' với panel Alignment ở vị trí đầu tiên.");
            }
            catch (System.Exception ex)
            {
                var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?.Editor;
                ed?.WriteMessage($"\nLỗi tạo menu: {ex.Message}");
            }
        }

        private class SimpleRibbonCommandHandler : System.Windows.Input.ICommand
        {
            public bool CanExecute(object? parameter) => true;

            public event EventHandler? CanExecuteChanged { add { } remove { } }

            public void Execute(object? parameter)
            {
                try
                {
                    string? commandToRun = null;
                    
                    if (parameter is RibbonButton btn && btn.Tag is string tag)
                    {
                        commandToRun = tag;
                    }

                    var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                    var ed = doc?.Editor;

                    if (!string.IsNullOrWhiteSpace(commandToRun) && doc != null)
                    {
                        // Execute the command
                        doc.SendStringToExecute(commandToRun + " ", true, false, true);
                    }
                    else
                    {
                        ed?.WriteMessage($"\nLệnh không thể thực thi.");
                    }
                }
                catch (System.Exception ex)
                {
                    var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?.Editor;
                    ed?.WriteMessage($"\nLỗi thực thi lệnh: {ex.Message}");
                }
            }
        }
    }
}
