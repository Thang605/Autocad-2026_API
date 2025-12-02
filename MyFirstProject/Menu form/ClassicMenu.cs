using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(MyFirstProject.ClassicMenu))]

namespace MyFirstProject
{
    public class ClassicMenu
    {
        [CommandMethod("CreateAllMenus")]
        public static void CreateAllMenus()
        {
            CreatePhuocToolMenu();
            CreateCivilToolMenu();
            CreateAcadToolMenu();
        }

        [CommandMethod("CreatePhuocToolMenu")]
        public static void CreatePhuocToolMenu()
        {
            CreateMenuGeneric("Phước_Tool", BuildPhuocToolStructure);
        }

        [CommandMethod("CreateCivilToolMenu")]
        public static void CreateCivilToolMenu()
        {
            CreateMenuGeneric("Civil tool", BuildCivilToolStructure);
        }

        [CommandMethod("CreateAcadToolMenu")]
        public static void CreateAcadToolMenu()
        {
            CreateMenuGeneric("Acad tool", BuildAcadToolStructure);
        }

        private static void CreateMenuGeneric(string menuName, Action<dynamic> buildAction)
        {
            try
            {
                dynamic acadApp = AcadApp.AcadApplication;
                dynamic menuBar = acadApp.MenuBar;
                dynamic popupMenus = acadApp.MenuGroups.Item(0).Menus;

                // 1. Remove existing menu if it exists
                try
                {
                    for (int i = 0; i < menuBar.Count; i++)
                    {
                        if (menuBar.Item(i).Name == menuName)
                        {
                            menuBar.Item(i).RemoveFromMenuBar();
                            break;
                        }
                    }
                }
                catch { }

                dynamic targetMenu = null;
                bool exists = false;
                foreach (dynamic menu in popupMenus)
                {
                    if (menu.Name == menuName)
                    {
                        targetMenu = menu;
                        exists = true;
                        while (targetMenu.Count > 0)
                        {
                            targetMenu.Item(0).Delete();
                        }
                        break;
                    }
                }

                if (!exists)
                {
                    targetMenu = popupMenus.Add(menuName);
                }

                // 2. Build Structure
                buildAction(targetMenu);

                // 3. Add to MenuBar
                targetMenu.InsertInMenuBar(menuBar.Count + 1);

                AcadApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nMenu '{menuName}' created successfully.");
            }
            catch (System.Exception ex)
            {
                AcadApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nError creating menu '{menuName}': {ex.Message}");
            }
        }

        private static void BuildPhuocToolStructure(dynamic menu)
        {
            // 01. TIỆN ÍCH HẠ TẦNG - GIAO THÔNG
            dynamic subMenu01 = menu.AddSubMenu(menu.Count + 1, "01. TIỆN ÍCH HẠ TẦNG - GIAO THÔNG");
            
            // --- Bình đồ tuyến ---
            AddHeader(subMenu01, "------ Bình đồ tuyến ------");
            AddMenuItem(subMenu01, "IPD - Phun điểm từ file CSV vào Cad", "IPD ");
            AddMenuItem(subMenu01, "PSC - Phát sinh cọc trên tuyến", "PSC ");
            AddMenuItem(subMenu01, "TTTN - Nhặt số liệu trắc ngang từ bình đồ", "TTTN ");
            AddMenuItem(subMenu01, "GLT - Ghi lý trình trên tuyến", "GLT ");
            AddMenuItem(subMenu01, "DLAYT - Đổi layer cao độ tại cọc trên tim tuyến", "DLAYT ");
            AddMenuItem(subMenu01, "NTD - Tạo file ntd trong Nova", "NTD ");
            AddMenuItem(subMenu01, "RTV - Xoay Text, Viewport trong Model - Layout", "RTV ");
            AddMenuItem(subMenu01, "RKI - Tạo Viewport theo đường cong Polyline", "RKI ");
            AddMenuItem(subMenu01, "TKI - Tạo khung in bình đồ", "TKI ");
            AddMenuItem(subMenu01, "Video - Hướng dẫn tạo khung bình đồ - TKI", "Video_TKI ");
            AddMenuItem(subMenu01, "GKBD - Khép khung bình đồ (do AJS phát triển - 150k/máy)", "GKBD ");

            // --- Trắc dọc tuyến ---
            AddHeader(subMenu01, "------ Trắc dọc tuyến ------");
            AddMenuItem(subMenu01, "VETD - Vẽ Trắc Dọc tuyến", "VETD ");

            // --- Trắc ngang tuyến ---
            AddHeader(subMenu01, "------ Trắc ngang tuyến ------");
            AddMenuItem(subMenu01, "VETN - Vẽ Trắc Ngang tuyến", "VETN ");
            AddMenuItem(subMenu01, "TCS - Thiết lập thông số", "TCS ");
            AddMenuItem(subMenu01, "TCD - Tính cao độ trắc ngang", "TCD ");
            AddMenuItem(subMenu01, "CNCD - Cập nhật cao độ trắc ngang", "CNCD ");

            // --- Bảng thông số nút giao ---
            AddHeader(subMenu01, "------ Bảng thông số nút giao ------");
            AddMenuItem(subMenu01, "Knut - Khai báo cao chữ bảng thông số", "Knut ");
            AddMenuItem(subMenu01, "Gnut - Vẽ bảng thông số T-P-K nút giao", "Gnut ");

            // --- Đánh Ký hiệu bản vẽ - DMBV ---
            AddHeader(subMenu01, "------ Đánh Ký hiệu bản vẽ - DMBV ------");
            AddMenuItem(subMenu01, "KhoaBV - Khoá bản vẽ", "KhoaBV ");
            AddMenuItem(subMenu01, "DBVN - Đóng nhanh các bản vẽ đang mở", "DBVN ");
            AddMenuItem(subMenu01, "DMBV - Danh mục bản vẽ LHB", "DMBV ");
            AddMenuItem(subMenu01, "Video - DMBV - Đánh danh mục bản vẽ LHB", "Video_DMBV ");
            AddMenuItem(subMenu01, "ATOC - Danh mục bản vẽ (do AJS phát triển - 150k/máy)", "ATOC ");

            // --- Google Map - Cad ---
            AddHeader(subMenu01, "------ Google Map - Cad ------");
            AddMenuItem(subMenu01, "IRT - Chuyển ảnh Google Map vào Cad", "IRT ");
            AddMenuItem(subMenu01, "IRT1 - Hiện đối tượng lên trên ảnh", "IRT1 ");
            AddMenuItem(subMenu01, "VIT - Đổi Sang tiếng việt IRT", "VIT ");

            // --- In ấn ---
            AddHeader(subMenu01, "------ In ấn ------");
            AddMenuItem(subMenu01, "QQP - In bản vẽ nhanh", "QQP ");
            AddMenuItem(subMenu01, "MPL - In hàng loạt", "MPL ");
            AddMenuItem(subMenu01, "IBV - In nhanh bản vẽ LHB", "IBV ");
            AddMenuItem(subMenu01, "Video - IBV - In hàng loạt bản vẽ LHB", "Video_IBV ");
            AddMenuItem(subMenu01, "D2P - In nhanh (do AJS phát triển - 150k/máy)", "D2P ");
            AddMenuItem(subMenu01, "XuatVP - Xuất ViewPort", "XuatVP ");
            AddMenuItem(subMenu01, "Xuattn - Xuất TN vào ViewPort", "Xuattn ");
            AddMenuItem(subMenu01, "DDH - Hỗ trợ Bình đồ - Trắc ngang - In ấn", "DDH ");

            // Placeholders
            menu.AddSubMenu(menu.Count + 1, "02. TIỆN ÍCH KHẢO SÁT ĐỊA HÌNH");
            menu.AddSubMenu(menu.Count + 1, "03. TIỆN ÍCH TRẮC ĐỊA BÁC (N.T. DUÂN)");
            menu.AddSubMenu(menu.Count + 1, "04. TIỆN ÍCH SAN NỀN BÁC (HOAN HML2.5.1)");
            menu.AddSubMenu(menu.Count + 1, "05. TIỆN ÍCH N.Đ.LÝ HÙNG (LHB_TOOL)");
            menu.AddSubMenu(menu.Count + 1, "06. TIỆN ÍCH ĐIỆN BÁC (NHẤT NGUYÊN)");
            menu.AddSubMenu(menu.Count + 1, "07. TIỆN ÍCH BÁC 3DUY (GROUP 1)");
            menu.AddSubMenu(menu.Count + 1, "08. TIỆN ÍCH BÁC 3DUY (GROUP 2)");
            menu.AddSubMenu(menu.Count + 1, "09. TIỆN ÍCH DIM - TỌA ĐỘ - FONT");
            menu.AddSubMenu(menu.Count + 1, "10. TIỆN ÍCH TEXT - LAYER - HATCH");
            menu.AddSubMenu(menu.Count + 1, "11. TIỆN ÍCH BLOCK - D/TÍCH - C/ DÀI");
            menu.AddSubMenu(menu.Count + 1, "12. TIỆN ÍCH VẼ NHANH");
        }

        private static void BuildCivilToolStructure(dynamic menu)
        {
            // Alignment
            dynamic subMenuAlignment = menu.AddSubMenu(menu.Count + 1, "Alignment");
            AddMenuItem(subMenuAlignment, "Offset from Alignment", "AT_OffsetAlignment ");
            AddMenuItem(subMenuAlignment, "Bảng Thống Kê Tuyến Đường", "CTA_BangThongKeCacTuyenDuong ");

            // Corridor
            dynamic subMenuCorridor = menu.AddSubMenu(menu.Count + 1, "Corridor");
            AddHeader(subMenuCorridor, "--- Thiết lập/Thêm ---");
            AddMenuItem(subMenuCorridor, "Add All Section", "CTC_AddAllSection ");
            AddMenuItem(subMenuCorridor, "Tạo Corridor Rẽ Phải", "CTC_TaoCooridor_DuongDoThi_RePhai ");
            AddHeader(subMenuCorridor, "--- Điều chỉnh ---");
            AddMenuItem(subMenuCorridor, "Điều Chỉnh Phân Đoạn", "CTC_DieuChinh_PhanDoan ");

            // Pipe Network
            dynamic subMenuPipe = menu.AddSubMenu(menu.Count + 1, "Pipe Network");
            AddHeader(subMenuPipe, "--- Thay đổi thông số ---");
            AddMenuItem(subMenuPipe, "Thay Đổi Đường Kính Cống", "CTPI_ThayDoi_DuongKinhCong ");
            AddMenuItem(subMenuPipe, "Thay Đổi Mặt Phẳng Ref Cống", "CTPI_ThayDoi_MatPhangRef_Cong ");
            AddMenuItem(subMenuPipe, "Thay Đổi Độ Dốc Cống", "CTPI_ThayDoi_DoanDocCong ");
            AddHeader(subMenuPipe, "--- Bảng/Xoay ---");
            AddMenuItem(subMenuPipe, "Bảng Cao Độ Hố Thu", "CTPI_BangCaoDo_TuNhienHoThu ");
            AddMenuItem(subMenuPipe, "Xoay Hố Thu Theo 2 Điểm", "CTPI_XoayHoThu_Theo2diem ");
            AddHeader(subMenuPipe, "--- Bề mặt tham chiếu ---");
            AddMenuItem(subMenuPipe, "Điều Chỉnh Bề Mặt Tham Chiếu", "CTPi_DieuChinh_BeMat_ThamChieu ");

            // Point
            dynamic subMenuPoint = menu.AddSubMenu(menu.Count + 1, "Point");
            AddHeader(subMenuPoint, "--- Tạo CogoPoint ---");
            AddMenuItem(subMenuPoint, "Tạo CogoPoint Từ Surface", "CTPO_TaoCogoPoint_CaoDo_FromSurface ");
            AddMenuItem(subMenuPoint, "Tạo CogoPoint Từ Elevation Spot", "CTPO_TaoCogoPoint_CaoDo_Elevationspot ");
            AddMenuItem(subMenuPoint, "Tạo CogoPoint Từ Text", "CTPO_CreateCogopointFromText ");
            AddHeader(subMenuPoint, "--- Quản lý/Ẩn ---");
            AddMenuItem(subMenuPoint, "Update All Point Group", "CTPO_UpdateAllPointGroup ");
            AddMenuItem(subMenuPoint, "Ẩn CogoPoint", "CTPO_An_CogoPoint ");
            AddHeader(subMenuPoint, "--- Đổi tên ---");
            AddMenuItem(subMenuPoint, "Đổi Tên CogoPoint Theo Alignment", "CTPo_DoiTen_CogoPoint_fromAlignment ");

            // Profile
            dynamic subMenuProfile = menu.AddSubMenu(menu.Count + 1, "Profile");
            AddHeader(subMenuProfile, "--- Vẽ trắc dọc ---");
            AddMenuItem(subMenuProfile, "Vẽ Trắc Dọc Tự Nhiên", "CTP_VeTracDoc_TuNhien ");
            AddMenuItem(subMenuProfile, "Vẽ Trắc Dọc Tất Cả Tuyến", "CTP_VeTracDoc_TuNhien_TatCaTuyen ");
            AddHeader(subMenuProfile, "--- Sửa/Gắn nhãn ---");
            AddMenuItem(subMenuProfile, "Sửa Đường Tự Nhiên Theo Cọc", "CTP_Fix_DuongTuNhien_TheoCoc ");
            AddMenuItem(subMenuProfile, "Gắn Nhãn Nút Giao Lên Trắc Dộc", "CTP_GanNhanNutGiao_LenTracDoc ");
            AddHeader(subMenuProfile, "--- Tạo điểm ---");
            AddMenuItem(subMenuProfile, "Tạo CogoPoint Từ PVI", "CTP_TaoCogoPointTuPVI ");
            AddHeader(subMenuProfile, "--- Band Profile ---");
            AddMenuItem(subMenuProfile, "Thay Đổi Profile trong Band", "CTP_ThayDoi_profile_Band ");

            // Surface
            dynamic subMenuSurface = menu.AddSubMenu(menu.Count + 1, "Surface");
            AddMenuItem(subMenuSurface, "Cao Độ Mặt Phẳng Tại CogoPoint", "CTSU_CaoDoMatPhang_TaiCogopoint ");

            // Sampleline
            dynamic subMenuSample = menu.AddSubMenu(menu.Count + 1, "Sampleline");
            AddHeader(subMenuSample, "--- Đổi tên cọc ---");
            AddMenuItem(subMenuSample, "Đổi Tên Cọc", "CTS_DoiTenCoc ");
            AddMenuItem(subMenuSample, "Đổi Tên Cọc Km", "CTS_DoiTenCoc3 ");
            AddMenuItem(subMenuSample, "Đổi Tên Cọc Theo Đoạn", "CTS_DoiTenCoc2 ");
            AddMenuItem(subMenuSample, "Đổi Tên Cọc Từ CogoPoint", "CTS_DoiTenCoc_fromCogoPoint ");
            AddMenuItem(subMenuSample, "Đổi Tên Cọc Theo Thứ Tự", "CTS_DoiTenCoc_TheoThuTu ");
            AddMenuItem(subMenuSample, "Đổi Tên Cọc H", "CTS_DoiTenCoc_H ");
            AddHeader(subMenuSample, "--- Bảng tọa độ/Update ---");
            AddMenuItem(subMenuSample, "Tạo Bảng Tọa Độ Cọc", "CTS_TaoBang_ToaDoCoc ");
            AddMenuItem(subMenuSample, "Tạo Bảng Tọa Độ Cọc (Lý Trình)", "CTS_TaoBang_ToaDoCoc2 ");
            AddMenuItem(subMenuSample, "Tạo Bảng Tọa Độ Cọc (Cao Độ)", "CTS_TaoBang_ToaDoCoc3 ");
            AddMenuItem(subMenuSample, "Cập Nhật 2 Table", "AT_UPdate2Table ");
            AddHeader(subMenuSample, "--- Chèn/Phát sinh cọc ---");
            AddMenuItem(subMenuSample, "Chèn Cọc Trên Trắc Dọc", "CTS_ChenCoc_TrenTracDoc ");
            AddMenuItem(subMenuSample, "Chèn Cọc Trên Trắc Ngang", "CTS_CHENCOC_TRENTRACNGANG ");
            AddMenuItem(subMenuSample, "Phát Sinh Cọc", "CTS_PhatSinhCoc ");
            AddMenuItem(subMenuSample, "Phát Sinh Cọc Chi Tiết", "CTS_PhatSinhCoc_ChiTiet ");
            AddMenuItem(subMenuSample, "Phát Sinh Cọc Theo Delta", "CTS_PhatSinhCoc_theoKhoangDelta ");
            AddMenuItem(subMenuSample, "Phát Sinh Cọc Từ CogoPoint", "CTS_PhatSinhCoc_TuCogoPoint ");
            AddMenuItem(subMenuSample, "Phát Sinh Cọc Theo Bảng", "CTS_PhatSinhCoc_TheoBang ");
            AddHeader(subMenuSample, "--- Dịch/Copy/Đồng bộ ---");
            AddMenuItem(subMenuSample, "Dịch Cọc Tịnh Tiến", "CTS_DichCoc_TinhTien ");
            AddMenuItem(subMenuSample, "Copy Nhóm Cọc", "CTS_Copy_NhomCoc ");
            AddMenuItem(subMenuSample, "Đồng Bộ 2 Nhóm Cọc", "CTS_DongBo_2_NhomCoc ");
            AddMenuItem(subMenuSample, "Đồng Bộ 2 Nhóm Cọc Theo Đoạn", "CTS_DongBo_2_NhomCoc_TheoDoan ");
            AddMenuItem(subMenuSample, "Dịch Cọc 40m", "CTS_DichCoc_TinhTien40 ");
            AddMenuItem(subMenuSample, "Dịch Cọc 20m", "CTS_DichCoc_TinhTien_20 ");
            AddHeader(subMenuSample, "--- Bề rộng Sample Line ---");
            AddMenuItem(subMenuSample, "Copy Bề Rộng Sample Line", "CTS_Copy_BeRong_sampleLine ");
            AddMenuItem(subMenuSample, "Thay Đổi Bề Rộng Sample Line", "CTS_Thaydoi_BeRong_sampleLine ");
            AddMenuItem(subMenuSample, "Offset Bề Rộng Sample Line", "CTS_Offset_BeRong_sampleLine ");

            // Section View
            dynamic subMenuSection = menu.AddSubMenu(menu.Count + 1, "Section View");
            AddHeader(subMenuSection, "--- Vẽ trắc ngang ---");
            AddMenuItem(subMenuSection, "Vẽ Trắc Ngang Thiết Kế", "CTSV_VeTracNgangThietKe ");
            AddMenuItem(subMenuSection, "Vẽ Tất Cả Trắc Ngang", "CVSV_VeTatCa_TracNgangThietKe ");
            AddHeader(subMenuSection, "--- Đánh cấp ---");
            AddMenuItem(subMenuSection, "Tính Đánh Cấp", "CTSV_DanhCap ");
            AddMenuItem(subMenuSection, "Xóa Bỏ Đánh Cấp", "CTSV_DanhCap_XoaBo ");
            AddMenuItem(subMenuSection, "Vẽ Thêm Đánh Cấp", "CTSV_DanhCap_VeThem ");
            AddMenuItem(subMenuSection, "Vẽ Thêm Đánh Cấp 2m", "CTSV_DanhCap_VeThem2 ");
            AddMenuItem(subMenuSection, "Vẽ Thêm Đánh Cấp 1m", "CTSV_DanhCap_VeThem1 ");
            AddMenuItem(subMenuSection, "Cập Nhật Đánh Cấp", "CTSV_DanhCap_CapNhat ");
            AddHeader(subMenuSection, "--- Thiết lập/giới hạn ---");
            AddMenuItem(subMenuSection, "Thay Đổi MSS Min Max", "CTSV_ThayDoi_MSS_Min_Max ");
            AddMenuItem(subMenuSection, "Thay Đổi Giới Hạn Trái Phải", "CTSV_ThayDoi_GioiHan_traiPhai ");
            AddHeader(subMenuSection, "--- Khung in ---");
            AddMenuItem(subMenuSection, "Thay Đổi Khung In", "CTSV_ThayDoi_KhungIn ");
            AddMenuItem(subMenuSection, "Fit Khung In", "CTSV_fit_KhungIn ");
            AddMenuItem(subMenuSection, "Fit Khung In 5x5", "CTSV_fit_KhungIn_5_5_top ");
            AddMenuItem(subMenuSection, "Fit Khung In 5x10", "CTSV_fit_KhungIn_5_10_top ");
            AddHeader(subMenuSection, "--- Khóa/ẩn ---");
            AddMenuItem(subMenuSection, "Khóa Cắt Ngang Add Point", "CTSV_KhoaCatNgang_AddPoint ");
            AddMenuItem(subMenuSection, "Ẩn Đường Địa Chất", "CTSV_An_DuongDiaChat ");
            AddHeader(subMenuSection, "--- Hiệu chỉnh ---");
            AddMenuItem(subMenuSection, "Hiệu Chỉnh Section Static", "CTSV_HieuChinh_Section ");
            AddMenuItem(subMenuSection, "Hiệu Chỉnh Section Dynamic", "CTSV_HieuChinh_Section_Dynamic ");
            AddMenuItem(subMenuSection, "Điều Chỉnh Đường Tự Nhiên", "CTSV_DieuChinh_DuongTuNhien ");
            AddHeader(subMenuSection, "--- Khác ---");
            AddMenuItem(subMenuSection, "Chuyển Đổi TN-TK sang TN-TN", "CTSV_ChuyenDoi_TNTK_TNTN ");
            AddMenuItem(subMenuSection, "Thêm Vật Liệu Trên Cắt Ngang", "CTSV_ThemVatLieu_TrenCatNgang ");
            AddMenuItem(subMenuSection, "Xuất Thông Tin Material Section", "CTSV_MaterialSection ");
            AddMenuItem(subMenuSection, "Tạo Polyline Từ Section", "AT_PolylineFromSection ");
            AddMenuItem(subMenuSection, "Xuất Khối Lượng ra Excel", "CTSV_XuatKhoiLuongRaExcel ");
            AddMenuItem(subMenuSection, "Khối Lượng Cắt Ngang", "CTSV_KhoiLuongCatNgang ");

            // Property Sets
            dynamic subMenuProp = menu.AddSubMenu(menu.Count + 1, "Property Sets");
            AddHeader(subMenuProp, "--- 3D Solid ---");
            AddMenuItem(subMenuProp, "Set PropertySet 3D Solid", "AT_Solid_Set_PropertySet ");
            AddMenuItem(subMenuProp, "Show 3D Solid Info", "AT_Solid_Show_Info ");
        }

        private static void BuildAcadToolStructure(dynamic menu)
        {
             // Tổng độ dài
            dynamic subMenuLen = menu.AddSubMenu(menu.Count + 1, "Tổng độ dài");
            AddMenuItem(subMenuLen, "Tổng Độ Dài (Full)", "AT_TongDoDai_Full ");
            AddMenuItem(subMenuLen, "Tổng Độ Dài (Replace)", "AT_TongDoDai_Replace ");
            AddMenuItem(subMenuLen, "Tổng Độ Dài (Replace2)", "AT_TongDoDai_Replace2 ");
            AddMenuItem(subMenuLen, "Tổng Độ Dài (Cộng Thêm)", "AT_TongDoDai_Replace_CongThem ");

            // Tổng diện tích
            dynamic subMenuArea = menu.AddSubMenu(menu.Count + 1, "Tổng diện tích");
            AddMenuItem(subMenuArea, "Tổng Diện Tích (Full)", "AT_TongDienTich_Full ");
            AddMenuItem(subMenuArea, "Tổng Diện Tích (Replace)", "AT_TongDienTich_Replace ");
            AddMenuItem(subMenuArea, "Tổng Diện Tích (Replace2)", "AT_TongDienTich_Replace2 ");
            AddMenuItem(subMenuArea, "Tổng Diện Tích (Cộng Thêm)", "AT_TongDienTich_Replace_CongThem ");

            // Biên tập Text
            dynamic subMenuText = menu.AddSubMenu(menu.Count + 1, "Biên tập Text");
            AddMenuItem(subMenuText, "Text Link", "AT_TextLink ");
            AddMenuItem(subMenuText, "Text Layout", "AT_TextLayout ");
            AddMenuItem(subMenuText, "Tạo Mới Text Layout", "AT_TaoMoi_TextLayout ");
            AddMenuItem(subMenuText, "Label From Text", "AT_Label_FromText ");
            AddMenuItem(subMenuText, "Đánh Số Thứ Tự", "AT_DanhSoThuTu ");

            // Xoay đối tượng
            dynamic subMenuRotate = menu.AddSubMenu(menu.Count + 1, "Xoay đối tượng");
            AddMenuItem(subMenuRotate, "Xoay Đối Tượng Theo Viewport", "AT_XoayDoiTuong_TheoViewport ");
            AddMenuItem(subMenuRotate, "Xoay Đối Tượng Theo 2 Điểm", "AT_XoayDoiTuong_Theo2Diem ");

            // Layout
            dynamic subMenuLayout = menu.AddSubMenu(menu.Count + 1, "Layout");
            AddMenuItem(subMenuLayout, "Dim Layout 2", "AT_DimLayout2 ");
            AddMenuItem(subMenuLayout, "Dim Layout", "AT_DimLayout ");
            AddMenuItem(subMenuLayout, "Block Layout", "AT_BlockLayout ");
            AddMenuItem(subMenuLayout, "Update Layout", "AT_UpdateLayout ");

            // Khác
            dynamic subMenuOther = menu.AddSubMenu(menu.Count + 1, "Khác");
            AddMenuItem(subMenuOther, "Tạo Outline", "AT_TaoOutline ");
            AddMenuItem(subMenuOther, "Xóa Đối Tượng Cùng Layer", "AT_XoaDoiTuong_CungLayer ");
            AddMenuItem(subMenuOther, "Xóa 3DSolid/Body", "AT_XoaDoiTuong_3DSolid_Body ");
            AddMenuItem(subMenuOther, "Offset 2 Bên", "AT_Offset_2Ben ");
            AddMenuItem(subMenuOther, "Annotative Scale Current Only", "AT_annotive_scale_currentOnly ");
        }

        private static void AddMenuItem(dynamic menu, string label, string macro)
        {
            // AddMenuItem(Index, Label, Macro)
            // Prepend ^C^C to ensure clean command execution
            string actualMacro = "^C^C" + macro;
            menu.AddMenuItem(menu.Count + 1, label, actualMacro);
        }

        private static void AddHeader(dynamic menu, string label)
        {
            // Add a disabled item to act as a header
            // Use a valid macro even if disabled
            var item = menu.AddMenuItem(menu.Count + 1, label, "^C^C");
            item.Enable = false; 
        }
    }
}
