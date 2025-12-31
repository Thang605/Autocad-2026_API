// (C) Copyright 2024 by T27
// Hệ thống Help cho các lệnh AutoCAD/Civil3D

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(Civil3DCsharp.HelpSystem.HelpSystem))]

namespace Civil3DCsharp.HelpSystem
{
    /// <summary>
    /// Hệ thống Help để tra cứu thông tin các lệnh trong dự án
    /// </summary>
    public class HelpSystem
    {
        // Dictionary chứa tất cả thông tin lệnh
        private static Dictionary<string, CommandInfo> _commands;

        /// <summary>
        /// Khởi tạo dictionary các lệnh
        /// </summary>
        static HelpSystem()
        {
            InitializeCommands();
        }

        /// <summary>
        /// Khởi tạo tất cả thông tin lệnh
        /// </summary>
        private static void InitializeCommands()
        {
            _commands = new Dictionary<string, CommandInfo>(StringComparer.OrdinalIgnoreCase);

            // ========== ACAD TOOL - CÁC LỆNH CAD CƠ BẢN ==========

            // 01. CAD.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_TongDoDai_Full",
                Category = "CAD - Tổng hợp",
                Description = "Tính tổng độ dài các đối tượng và ghi ra text mới",
                Usage = "AT_TongDoDai_Full",
                Steps = new[] {
                    "1. Gõ lệnh AT_TongDoDai_Full",
                    "2. Chọn các đối tượng Line, Polyline, Arc, Circle...",
                    "3. Chọn vị trí đặt text kết quả",
                    "4. Lệnh sẽ tạo text hiển thị tổng độ dài"
                },
                Notes = new[] { "Hỗ trợ: Line, Polyline, Arc, Circle, Ellipse" }
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_TongDoDai_Replace",
                Category = "CAD - Tổng hợp",
                Description = "Tính tổng độ dài và thay thế vào text có sẵn",
                Usage = "AT_TongDoDai_Replace",
                Steps = new[] {
                    "1. Gõ lệnh AT_TongDoDai_Replace",
                    "2. Chọn các đối tượng cần tính độ dài",
                    "3. Chọn text để thay thế giá trị",
                    "4. Giá trị tổng độ dài sẽ được ghi vào text đã chọn"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_TongDoDai_Replace2",
                Category = "CAD - Tổng hợp",
                Description = "Tính tổng độ dài và thay thế (phiên bản 2)",
                Usage = "AT_TongDoDai_Replace2"
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_TongDoDai_Replace_CongThem",
                Category = "CAD - Tổng hợp",
                Description = "Tính tổng độ dài và cộng thêm vào giá trị text hiện có",
                Usage = "AT_TongDoDai_Replace_CongThem",
                Steps = new[] {
                    "1. Gõ lệnh AT_TongDoDai_Replace_CongThem",
                    "2. Chọn các đối tượng cần tính",
                    "3. Chọn text chứa giá trị cũ",
                    "4. Giá trị mới = Giá trị cũ + Tổng độ dài mới"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "ET_TongDienTich_Full",
                Category = "CAD - Tổng hợp",
                Description = "Tính tổng diện tích các đối tượng và ghi ra text mới",
                Usage = "ET_TongDienTich_Full",
                Steps = new[] {
                    "1. Gõ lệnh ET_TongDienTich_Full",
                    "2. Chọn các đối tượng Polyline kín, Circle, Hatch...",
                    "3. Chọn vị trí đặt text kết quả"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_TongDienTich_Replace",
                Category = "CAD - Tổng hợp",
                Description = "Tính tổng diện tích và thay thế vào text có sẵn",
                Usage = "AT_TongDienTich_Replace"
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_TongDienTich_Replace2",
                Category = "CAD - Tổng hợp",
                Description = "Tính tổng diện tích và thay thế (phiên bản 2)",
                Usage = "AT_TongDienTich_Replace2"
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_TongDienTich_Replace_CongThem",
                Category = "CAD - Tổng hợp",
                Description = "Tính tổng diện tích và cộng thêm vào giá trị hiện có",
                Usage = "AT_TongDienTich_Replace_CongThem"
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_TextLink",
                Category = "CAD - Text",
                Description = "Liên kết nội dung giữa các text",
                Usage = "AT_TextLink"
            });

            AddCommand(new CommandInfo
            {
                Name = "ET_DanhSoThuTu",
                Category = "CAD - Text",
                Description = "Đánh số thứ tự tự động cho các text",
                Usage = "ET_DanhSoThuTu",
                Steps = new[] {
                    "1. Gõ lệnh ET_DanhSoThuTu",
                    "2. Nhập số bắt đầu",
                    "3. Chọn các text cần đánh số theo thứ tự"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_XoayDoiTuong_Theo2Diem",
                Category = "CAD - Transform",
                Description = "Xoay đối tượng theo hướng của 2 điểm",
                Usage = "AT_XoayDoiTuong_Theo2Diem",
                Steps = new[] {
                    "1. Gõ lệnh AT_XoayDoiTuong_Theo2Diem",
                    "2. Chọn đối tượng cần xoay",
                    "3. Chọn điểm 1 (điểm gốc xoay)",
                    "4. Chọn điểm 2 (xác định hướng)"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_TextLayout",
                Category = "CAD - Layout",
                Description = "Chuyển text từ Model sang Layout với tỉ lệ đúng",
                Usage = "AT_TextLayout"
            });

            AddCommand(new CommandInfo
            {
                Name = "ET_TaoMoi_TextLayout",
                Category = "CAD - Layout",
                Description = "Tạo mới text trong Layout",
                Usage = "ET_TaoMoi_TextLayout"
            });

            AddCommand(new CommandInfo
            {
                Name = "ET_DimLayout",
                Category = "CAD - Layout",
                Description = "Chuyển Dimension từ Model sang Layout",
                Usage = "ET_DimLayout"
            });

            AddCommand(new CommandInfo
            {
                Name = "ET_DimLayout2",
                Category = "CAD - Layout",
                Description = "Chuyển Dimension từ Model sang Layout (phiên bản 2)",
                Usage = "ET_DimLayout2"
            });

            AddCommand(new CommandInfo
            {
                Name = "ET_BlockLayout",
                Category = "CAD - Layout",
                Description = "Chuyển Block từ Model sang Layout",
                Usage = "ET_BlockLayout"
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_Label_FromText",
                Category = "CAD - Label",
                Description = "Tạo Label từ nội dung Text có sẵn",
                Usage = "AT_Label_FromText"
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_XoaDoiTuong_CungLayer",
                Category = "CAD - Layer",
                Description = "Xóa tất cả đối tượng cùng layer với đối tượng được chọn",
                Usage = "AT_XoaDoiTuong_CungLayer",
                Steps = new[] {
                    "1. Gõ lệnh AT_XoaDoiTuong_CungLayer",
                    "2. Chọn một đối tượng mẫu",
                    "3. Tất cả đối tượng cùng layer sẽ bị xóa"
                },
                Notes = new[] { "⚠ Cẩn thận: Lệnh này xóa đối tượng không thể Undo" }
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_XoaDoiTuong_3DSolid_Body",
                Category = "CAD - 3D",
                Description = "Xóa các 3D Solid và Body trong bản vẽ",
                Usage = "AT_XoaDoiTuong_3DSolid_Body"
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_UpdateLayout",
                Category = "CAD - Layout",
                Description = "Cập nhật tất cả các Layout trong bản vẽ",
                Usage = "AT_UpdateLayout"
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_Offset_2Ben",
                Category = "CAD - Edit",
                Description = "Offset đối tượng về cả 2 bên cùng lúc",
                Usage = "AT_Offset_2Ben",
                Steps = new[] {
                    "1. Gõ lệnh AT_Offset_2Ben",
                    "2. Nhập khoảng cách offset",
                    "3. Chọn đối tượng cần offset",
                    "4. Đối tượng sẽ được offset về cả 2 bên"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_annotive_scale_currentOnly",
                Category = "CAD - Scale",
                Description = "Chỉ giữ lại annotation scale hiện tại, xóa các scale khác",
                Usage = "AT_annotive_scale_currentOnly"
            });

            // 02. AT_Solid_frompolyline.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_Solid_frompolyline",
                Category = "CAD - 3D",
                Description = "Tạo 3D Solid từ Polyline kín bằng cách extrude",
                Usage = "AT_Solid_frompolyline",
                Steps = new[] {
                    "1. Gõ lệnh AT_Solid_frompolyline",
                    "2. Chọn các Polyline kín",
                    "3. Nhập chiều cao extrude",
                    "4. Solid 3D sẽ được tạo"
                },
                VideoLink = "https://www.youtube.com/watch?v=_VSCzOUSj6E"
            });

            // 03. Command_XUATBANG_ToaDoPolyline.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_XuatBangToaDo_Polyline",
                Category = "CAD - Export",
                Description = "Xuất bảng tọa độ các đỉnh Polyline ra Excel",
                Usage = "AT_XuatBangToaDo_Polyline",
                Steps = new[] {
                    "1. Gõ lệnh AT_XuatBangToaDo_Polyline",
                    "2. Chọn Polyline cần xuất tọa độ",
                    "3. Chọn vị trí lưu file Excel",
                    "4. File Excel sẽ chứa bảng tọa độ X, Y của các đỉnh"
                }
            });

            // 04. AT_XuatBang_Civil3D_ToExcel.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_XuatBang_Civil3D_ToExcel",
                Category = "Civil - Export",
                Description = "Xuất các bảng Civil 3D ra file Excel",
                Usage = "AT_XuatBang_Civil3D_ToExcel"
            });

            // 05. AT_TaoOutline.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_TaoOutline",
                Category = "CAD - Create",
                Description = "Tạo outline (đường bao) cho các đối tượng",
                Usage = "AT_TaoOutline",
                Steps = new[] {
                    "1. Gõ lệnh AT_TaoOutline",
                    "2. Chọn các đối tượng cần tạo outline",
                    "3. Nhập offset cho đường bao",
                    "4. Polyline outline sẽ được tạo"
                }
            });

            // 06. CT_Copy_NoiDung_Text.cs
            AddCommand(new CommandInfo
            {
                Name = "CT_Copy_NoiDung_Text",
                Category = "CAD - Text",
                Description = "Copy nội dung từ text này sang text khác",
                Usage = "CT_Copy_NoiDung_Text",
                Steps = new[] {
                    "1. Gõ lệnh CT_Copy_NoiDung_Text",
                    "2. Chọn text nguồn",
                    "3. Chọn text đích",
                    "4. Nội dung sẽ được copy"
                }
            });

            // 07. CA_CopyVaDichTiengAnh.cs
            AddCommand(new CommandInfo
            {
                Name = "CA_CopyVaDichTiengAnh",
                Category = "CAD - Text",
                Description = "Copy text và dịch sang tiếng Anh",
                Usage = "CA_CopyVaDichTiengAnh"
            });

            // 08. AT_DocNgang.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_DoDoc",
                Category = "CAD - Đo lường",
                Description = "Tính và hiển thị độ dốc giữa 2 điểm so với trục X",
                Usage = "AT_DoDoc",
                Steps = new[] {
                    "1. Gõ lệnh AT_DoDoc",
                    "2. Chọn điểm đầu (điểm thấp hơn)",
                    "3. Chọn điểm cuối (điểm cao hơn)",
                    "4. Kết quả hiển thị độ dốc %, góc, tỉ lệ",
                    "5. Có thể vẽ text thể hiện độ dốc lên bản vẽ"
                },
                Notes = new[] {
                    "Kết quả bao gồm: độ dốc (%), góc (°), tỉ lệ (1:n)",
                    "Có tùy chọn vẽ text độ dốc lên bản vẽ"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_DoDoc_Simple",
                Category = "CAD - Đo lường",
                Description = "Tính độ dốc đơn giản - chỉ hiển thị kết quả",
                Usage = "AT_DoDoc_Simple"
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_DoDoc_Object",
                Category = "CAD - Đo lường",
                Description = "Tính độ dốc từ một đường Line hoặc Polyline có sẵn",
                Usage = "AT_DoDoc_Object",
                Steps = new[] {
                    "1. Gõ lệnh AT_DoDoc_Object",
                    "2. Chọn Line hoặc Polyline",
                    "3. Kết quả độ dốc sẽ được hiển thị"
                }
            });

            // 09. AT_Xref_all_file.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_Xref_all_file",
                Category = "CAD - Xref",
                Description = "Quản lý Xref cho tất cả file trong thư mục",
                Usage = "AT_Xref_all_file"
            });

            // 10. AT_XuatXref.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_XuatXref",
                Category = "CAD - Xref",
                Description = "Xuất thông tin Xref ra file",
                Usage = "AT_XuatXref"
            });

            // 11. AT_XoayDoiTuong_TheoViewport.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_XoayDoiTuong_TheoViewport",
                Category = "CAD - Viewport",
                Description = "Xoay đối tượng theo góc của Viewport",
                Usage = "AT_XoayDoiTuong_TheoViewport"
            });

            // 12. AT_BoTri_ViewPort_TheoHinh.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_BoTri_ViewPort_TheoHinh",
                Category = "CAD - Viewport",
                Description = "Tự động bố trí Viewport theo hình dạng trong Layout",
                Usage = "AT_BoTri_ViewPort_TheoHinh",
                Steps = new[] {
                    "1. Gõ lệnh AT_BoTri_ViewPort_TheoHinh",
                    "2. Chọn vùng chứa các hình cần tạo viewport",
                    "3. Các viewport sẽ được tạo tự động"
                }
            });

            // 13. AT_Xoay_ViewPortHienHanh_Theo2Diem.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_Xoay_ViewPortHienHanh_Theo2Diem",
                Category = "CAD - Viewport",
                Description = "Xoay Viewport hiện hành theo hướng 2 điểm",
                Usage = "AT_Xoay_ViewPortHienHanh_Theo2Diem"
            });

            // 14. AT_TaoBlock_TungDoiTuong.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_TaoBlock_TungDoiTuong",
                Category = "CAD - Block",
                Description = "Tạo Block riêng cho từng đối tượng được chọn",
                Usage = "AT_TaoBlock_TungDoiTuong",
                Steps = new[] {
                    "1. Gõ lệnh AT_TaoBlock_TungDoiTuong",
                    "2. Chọn các đối tượng",
                    "3. Nhập tiền tố tên Block",
                    "4. Mỗi đối tượng sẽ được chuyển thành 1 Block riêng"
                }
            });

            // 15. AT_InModel_HangLoat.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_InModel_HangLoat",
                Category = "CAD - In ấn",
                Description = "In hàng loạt các bản vẽ trong Model Space",
                Usage = "AT_InModel_HangLoat",
                Steps = new[] {
                    "1. Gõ lệnh AT_InModel_HangLoat",
                    "2. Chọn các khung in (Block) trong Model",
                    "3. Chọn máy in và cỡ giấy",
                    "4. Các bản vẽ sẽ được in tự động"
                },
                Notes = new[] {
                    "Hỗ trợ in ra PDF hoặc máy in vật lý",
                    "Tự động đặt tên file theo tên block"
                }
            });

            // 16. AT_TextToSolid.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_TextToSolid",
                Category = "CAD - Text",
                Description = "Chuyển Text thành Solid Hatch hoặc 3D Solid",
                Usage = "AT_TextToSolid",
                Steps = new[] {
                    "1. Gõ lệnh AT_TextToSolid",
                    "2. Chọn Text hoặc MText",
                    "3. Chọn kiểu: Hatch 2D hoặc 3D Solid",
                    "4. Solid sẽ được tạo theo hình dạng text"
                }
            });

            // 17. AT_InBanVe_TheoBlock.cs
            AddCommand(new CommandInfo
            {
                Name = "AT_InBanVe_TheoBlock",
                Category = "CAD - In ấn",
                Description = "In bản vẽ theo Block trong Layout",
                Usage = "AT_InBanVe_TheoBlock",
                Steps = new[] {
                    "1. Gõ lệnh AT_InBanVe_TheoBlock",
                    "2. Chọn các Block làm khung in",
                    "3. Chọn máy in, cỡ giấy",
                    "4. Các bản vẽ sẽ được in tự động"
                }
            });

            // ========== CIVIL TOOL - CÁC LỆNH CIVIL 3D ==========

            // 01. Corridor
            AddCommand(new CommandInfo
            {
                Name = "CTC_DieuChinh_PhanDoan",
                Category = "Civil - Corridor",
                Description = "Điều chỉnh phân đoạn (Region) của Corridor",
                Usage = "CTC_DieuChinh_PhanDoan",
                Steps = new[] {
                    "1. Gõ lệnh CTC_DieuChinh_PhanDoan",
                    "2. Chọn Corridor cần điều chỉnh",
                    "3. Form hiện ra để chỉnh sửa các Region"
                },
                VideoLink = "https://www.youtube.com/watch?v=T_Hm4Jm-uK0"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTC_TaoCorridor_ChoTuyenDuong",
                Category = "Civil - Corridor",
                Description = "Tạo Corridor cho tuyến đường từ Alignment và Profile",
                Usage = "CTC_TaoCorridor_ChoTuyenDuong",
                Steps = new[] {
                    "1. Gõ lệnh CTC_TaoCorridor_ChoTuyenDuong",
                    "2. Chọn Alignment (tim tuyến)",
                    "3. Chọn Profile (trắc dọc thiết kế)",
                    "4. Chọn Assembly (mặt cắt ngang)",
                    "5. Corridor sẽ được tạo tự động"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "CTPI_Corridor_SetTargets",
                Category = "Civil - Corridor",
                Description = "Thiết lập Targets cho Corridor (bề mặt, độ cao...)",
                Usage = "CTPI_Corridor_SetTargets"
            });

            // 02. Parcel
            AddCommand(new CommandInfo
            {
                Name = "CTPa_ParcelInfo",
                Category = "Civil - Parcel",
                Description = "Hiển thị thông tin Parcel (thửa đất)",
                Usage = "CTPa_ParcelInfo"
            });

            // 04. Pipe and Structures
            AddCommand(new CommandInfo
            {
                Name = "CTPi_DieuChinh_BeMat_ThamChieu",
                Category = "Civil - Pipe",
                Description = "Điều chỉnh bề mặt tham chiếu cho Pipe Network",
                Usage = "CTPi_DieuChinh_BeMat_ThamChieu"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTPi_ThayDoi_CaoDo_DayCong",
                Category = "Civil - Pipe",
                Description = "Thay đổi cao độ đáy cống trong Pipe Network",
                Usage = "CTPi_ThayDoi_CaoDo_DayCong",
                Steps = new[] {
                    "1. Gõ lệnh CTPi_ThayDoi_CaoDo_DayCong",
                    "2. Chọn Pipe cần thay đổi",
                    "3. Nhập cao độ mới",
                    "4. Pipe sẽ được cập nhật"
                }
            });

            // 05. Point
            AddCommand(new CommandInfo
            {
                Name = "CTPo_DoiTen_CogoPoint_fromAlignment",
                Category = "Civil - Point",
                Description = "Đổi tên các CoGo Point theo Alignment",
                Usage = "CTPo_DoiTen_CogoPoint_fromAlignment"
            });

            // 06. Profile and ProfileView
            AddCommand(new CommandInfo
            {
                Name = "CTP_ThayDoi_profile_Band",
                Category = "Civil - Profile",
                Description = "Thay đổi Profile Band trong ProfileView",
                Usage = "CTP_ThayDoi_profile_Band"
            });

            // 07. Sampleline
            AddCommand(new CommandInfo
            {
                Name = "CTS_DoiTenCoc",
                Category = "Civil - Sampleline",
                Description = "Đổi tên cọc (Sample Line) theo quy tắc",
                Usage = "CTS_DoiTenCoc",
                Steps = new[] {
                    "1. Gõ lệnh CTS_DoiTenCoc",
                    "2. Chọn Sample Line Group",
                    "3. Nhập quy tắc đặt tên",
                    "4. Tên các cọc sẽ được cập nhật"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "CTS_DoiTenCoc2",
                Category = "Civil - Sampleline",
                Description = "Đổi tên cọc (phiên bản 2)",
                Usage = "CTS_DoiTenCoc2"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTS_DoiTenCoc3",
                Category = "Civil - Sampleline",
                Description = "Đổi tên cọc (phiên bản 3)",
                Usage = "CTS_DoiTenCoc3"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTS_TaoBang_ToaDoCoc",
                Category = "Civil - Sampleline",
                Description = "Tạo bảng tọa độ các cọc",
                Usage = "CTS_TaoBang_ToaDoCoc"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTS_TaoBang_ToaDoCoc2",
                Category = "Civil - Sampleline",
                Description = "Tạo bảng tọa độ cọc (phiên bản 2)",
                Usage = "CTS_TaoBang_ToaDoCoc2"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTS_TaoBang_ToaDoCoc3",
                Category = "Civil - Sampleline",
                Description = "Tạo bảng tọa độ cọc (phiên bản 3)",
                Usage = "CTS_TaoBang_ToaDoCoc3"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTS_ThayDoi_BeRong_Sampleline",
                Category = "Civil - Sampleline",
                Description = "Thay đổi bề rộng Sample Line (trái/phải)",
                Usage = "CTS_ThayDoi_BeRong_Sampleline"
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_UPdate2Table",
                Category = "Civil - Sampleline",
                Description = "Cập nhật thông tin vào bảng",
                Usage = "AT_UPdate2Table"
            });

            // 08. Sectionview
            AddCommand(new CommandInfo
            {
                Name = "CTSV_ChuyenDoi_TNTK_TNTN",
                Category = "Civil - SectionView",
                Description = "Chuyển đổi giữa trắc ngang thiết kế và trắc ngang tự nhiên",
                Usage = "CTSV_ChuyenDoi_TNTK_TNTN"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_DanhCap",
                Category = "Civil - SectionView",
                Description = "Đánh cấp (Grade) trên mặt cắt ngang",
                Usage = "CTSV_DanhCap",
                Steps = new[] {
                    "1. Gõ lệnh CTSV_DanhCap",
                    "2. Chọn Section View",
                    "3. Nhập thông số cấp",
                    "4. Các đường cấp sẽ được vẽ"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_DanhCap_XoaBo",
                Category = "Civil - SectionView",
                Description = "Xóa bỏ các đường đánh cấp",
                Usage = "CTSV_DanhCap_XoaBo"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_DanhCap_VeThem",
                Category = "Civil - SectionView",
                Description = "Vẽ thêm đường đánh cấp",
                Usage = "CTSV_DanhCap_VeThem"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_DanhCap_VeThem1",
                Category = "Civil - SectionView",
                Description = "Vẽ thêm đường đánh cấp (phiên bản 1)",
                Usage = "CTSV_DanhCap_VeThem1"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_DanhCap_VeThem2",
                Category = "Civil - SectionView",
                Description = "Vẽ thêm đường đánh cấp (phiên bản 2)",
                Usage = "CTSV_DanhCap_VeThem2"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_DanhCap_CapNhat",
                Category = "Civil - SectionView",
                Description = "Cập nhật đường đánh cấp",
                Usage = "CTSV_DanhCap_CapNhat"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_ThemVatLieu_TrenCatNgang",
                Category = "Civil - SectionView",
                Description = "Thêm vật liệu (Material) trên mặt cắt ngang",
                Usage = "CTSV_ThemVatLieu_TrenCatNgang"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_ThayDoi_MSS_Min_Max",
                Category = "Civil - SectionView",
                Description = "Thay đổi Min/Max của Multi Section Sheet (MSS)",
                Usage = "CTSV_ThayDoi_MSS_Min_Max"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_ThayDoi_GioiHan_traiPhai",
                Category = "Civil - SectionView",
                Description = "Thay đổi giới hạn trái/phải của Section View",
                Usage = "CTSV_ThayDoi_GioiHan_traiPhai"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_ThayDoi_KhungIn",
                Category = "Civil - SectionView",
                Description = "Thay đổi khung in cho Section View",
                Usage = "CTSV_ThayDoi_KhungIn"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_KhoaCatNgang_AddPoint",
                Category = "Civil - SectionView",
                Description = "Thêm điểm vào khóa cắt ngang",
                Usage = "CTSV_KhoaCatNgang_AddPoint"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_fit_KhungIn",
                Category = "Civil - SectionView",
                Description = "Fit Section View vào khung in",
                Usage = "CTSV_fit_KhungIn"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_fit_KhungIn_5_5_top",
                Category = "Civil - SectionView",
                Description = "Fit Section View với margin 5-5 từ top",
                Usage = "CTSV_fit_KhungIn_5_5_top"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_fit_KhungIn_5_10_top",
                Category = "Civil - SectionView",
                Description = "Fit Section View với margin 5-10 từ top",
                Usage = "CTSV_fit_KhungIn_5_10_top"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_An_DuongDiaChat",
                Category = "Civil - SectionView",
                Description = "Ẩn các đường địa chất trên mặt cắt ngang",
                Usage = "CTSV_An_DuongDiaChat"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_HieuChinh_Section",
                Category = "Civil - SectionView",
                Description = "Hiệu chỉnh Section View",
                Usage = "CTSV_HieuChinh_Section"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_HieuChinh_Section_Dynamic",
                Category = "Civil - SectionView",
                Description = "Hiệu chỉnh Section View động",
                Usage = "CTSV_HieuChinh_Section_Dynamic"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_DieuChinh_DuongTuNhien",
                Category = "Civil - SectionView",
                Description = "Điều chỉnh đường tự nhiên trên mặt cắt ngang",
                Usage = "CTSV_DieuChinh_DuongTuNhien",
                VideoLink = "https://www.youtube.com/watch?v=QbU1SG3-44E"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_KhoiLuongCatNgang",
                Category = "Civil - SectionView",
                Description = "Tính khối lượng từ mặt cắt ngang",
                Usage = "CTSV_KhoiLuongCatNgang",
                Steps = new[] {
                    "1. Gõ lệnh CTSV_KhoiLuongCatNgang",
                    "2. Chọn Section View",
                    "3. Khối lượng đào/đắp sẽ được tính toán và hiển thị"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_XuatKhoiLuongRaExcel",
                Category = "Civil - SectionView",
                Description = "Xuất bảng khối lượng ra file Excel",
                Usage = "CTSV_XuatKhoiLuongRaExcel"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_VeTracNgangThietKe",
                Category = "Civil - SectionView",
                Description = "Vẽ trắc ngang thiết kế trên Section View",
                Usage = "CTSV_VeTracNgangThietKe"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_TaoCorridorSurface",
                Category = "Civil - SectionView",
                Description = "Tạo Corridor Surface từ Corridor",
                Usage = "CTSV_TaoCorridorSurface"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_TaoCorridorSurfaceMultiple",
                Category = "Civil - SectionView",
                Description = "Tạo nhiều Corridor Surface cùng lúc",
                Usage = "CTSV_TaoCorridorSurfaceMultiple"
            });

            AddCommand(new CommandInfo
            {
                Name = "CTSV_TaoCorridorSurfaceSingle",
                Category = "Civil - SectionView",
                Description = "Tạo một Corridor Surface đơn lẻ",
                Usage = "CTSV_TaoCorridorSurfaceSingle"
            });

            // 09. Surfaces
            AddCommand(new CommandInfo
            {
                Name = "CTS_TaoSpotElevation_OnSurface_TaiTim",
                Category = "Civil - Surface",
                Description = "Tạo Spot Elevation trên Surface tại tim tuyến",
                Usage = "CTS_TaoSpotElevation_OnSurface_TaiTim"
            });

            // 10. Property Sets
            AddCommand(new CommandInfo
            {
                Name = "AT_Solid_Set_PropertySet",
                Category = "Civil - Property",
                Description = "Thiết lập Property Set cho 3D Solid",
                Usage = "AT_Solid_Set_PropertySet",
                VideoLink = "https://www.youtube.com/watch?v=FBallJsCKmM"
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_Solid_Show_Info",
                Category = "Civil - Property",
                Description = "Hiển thị thông tin Property của 3D Solid",
                Usage = "AT_Solid_Show_Info"
            });

            // 11. OffsetAlignment
            AddCommand(new CommandInfo
            {
                Name = "AT_OffsetAlignment",
                Category = "Civil - Alignment",
                Description = "Tạo Offset Alignment từ Alignment có sẵn",
                Usage = "AT_OffsetAlignment",
                Steps = new[] {
                    "1. Gõ lệnh AT_OffsetAlignment",
                    "2. Chọn Alignment gốc",
                    "3. Nhập khoảng cách offset",
                    "4. Alignment mới sẽ được tạo"
                },
                VideoLink = "https://www.youtube.com/watch?v=OeO-w83qgmY"
            });

            // 14. CTA_BangThongKeCacTuyenDuong
            AddCommand(new CommandInfo
            {
                Name = "CTA_BangThongKeCacTuyenDuong",
                Category = "Civil - Alignment",
                Description = "Tạo bảng thống kê các tuyến đường trong bản vẽ",
                Usage = "CTA_BangThongKeCacTuyenDuong"
            });

            // 19. AT_PolylineFromSection
            AddCommand(new CommandInfo
            {
                Name = "AT_PolylineFromSection",
                Category = "Civil - SectionView",
                Description = "Tạo Polyline từ Section View",
                Usage = "AT_PolylineFromSection"
            });

            // 20. CT_ThongTinDoiTuong
            AddCommand(new CommandInfo
            {
                Name = "CT_ThongTinDoiTuong",
                Category = "Civil - Info",
                Description = "Hiển thị thông tin chi tiết của đối tượng Civil 3D",
                Usage = "CT_ThongTinDoiTuong",
                Steps = new[] {
                    "1. Gõ lệnh CT_ThongTinDoiTuong",
                    "2. Chọn đối tượng Civil 3D",
                    "3. Thông tin chi tiết sẽ được hiển thị"
                }
            });

            // 21. CTSU_CaoDoMatPhang_TaiCogopoint
            AddCommand(new CommandInfo
            {
                Name = "CTSU_CaoDoMatPhang_TaiCogopoint",
                Category = "Civil - Surface",
                Description = "Lấy cao độ mặt phẳng tại vị trí CoGo Point",
                Usage = "CTSU_CaoDoMatPhang_TaiCogopoint"
            });

            // 27. CT_TaoDuong_ConnectedAlignment_NutGiao
            AddCommand(new CommandInfo
            {
                Name = "CT_TaoDuong_ConnectedAlignment_NutGiao",
                Category = "Civil - Alignment",
                Description = "Tạo đường nối (Connected Alignment) tại nút giao",
                Usage = "CT_TaoDuong_ConnectedAlignment_NutGiao"
            });

            // 28. CTC_TaoCooridor_DuongDoThi_RePhai
            AddCommand(new CommandInfo
            {
                Name = "CTC_TaoCooridor_DuongDoThi_RePhai",
                Category = "Civil - Corridor",
                Description = "Tạo Corridor cho đường đô thị với rẽ phải",
                Usage = "CTC_TaoCooridor_DuongDoThi_RePhai"
            });

            // Menu
            AddCommand(new CommandInfo
            {
                Name = "SHOW_MENU",
                Category = "Menu",
                Description = "Hiển thị menu chính của chương trình",
                Usage = "SHOW_MENU"
            });

            // Help commands
            AddCommand(new CommandInfo
            {
                Name = "AT_Help",
                Category = "Help",
                Description = "Hiển thị hướng dẫn chi tiết cho một lệnh cụ thể",
                Usage = "AT_Help",
                Steps = new[] {
                    "1. Gõ lệnh AT_Help",
                    "2. Nhập tên lệnh cần tra cứu",
                    "3. Thông tin chi tiết sẽ được hiển thị"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_HelpList",
                Category = "Help",
                Description = "Hiển thị danh sách tất cả các lệnh theo nhóm",
                Usage = "AT_HelpList"
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_HelpSearch",
                Category = "Help",
                Description = "Tìm kiếm lệnh theo từ khóa",
                Usage = "AT_HelpSearch",
                Steps = new[] {
                    "1. Gõ lệnh AT_HelpSearch",
                    "2. Nhập từ khóa tìm kiếm",
                    "3. Danh sách lệnh phù hợp sẽ được hiển thị"
                }
            });

            AddCommand(new CommandInfo
            {
                Name = "AT_HelpForm",
                Category = "Help",
                Description = "Mở bảng hướng dẫn với giao diện có tabs phân loại",
                Usage = "AT_HelpForm",
                Steps = new[] {
                    "1. Gõ lệnh AT_HelpForm",
                    "2. Form hiện ra với các tab phân loại lệnh",
                    "3. Click vào lệnh để xem chi tiết",
                    "4. Double-click hoặc nhấn 'Copy tên lệnh' để copy"
                },
                Notes = new[] {
                    "Form có thể tìm kiếm nhanh bằng ô Search",
                    "Tabs phân loại theo nhóm: CAD, Civil, Menu, Help"
                }
            });
        }

        /// <summary>
        /// Thêm lệnh vào dictionary
        /// </summary>
        private static void AddCommand(CommandInfo cmd)
        {
            if (!string.IsNullOrEmpty(cmd.Name))
            {
                _commands[cmd.Name] = cmd;
            }
        }

        /// <summary>
        /// Lệnh hiển thị danh sách tất cả lệnh
        /// </summary>
        [CommandMethod("AT_HelpList")]
        public static void ShowCommandList()
        {
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                // Nhóm các lệnh theo category
                var grouped = _commands.Values
                    .GroupBy(c => c.Category)
                    .OrderBy(g => g.Key);

                ed.WriteMessage("\n");
                ed.WriteMessage("\n╔══════════════════════════════════════════════════════════════════════════════╗");
                ed.WriteMessage("\n║              DANH SÁCH CÁC LỆNH AUTOCAD/CIVIL3D - T27 TOOLS                  ║");
                ed.WriteMessage("\n╠══════════════════════════════════════════════════════════════════════════════╣");
                ed.WriteMessage("\n║  Gõ 'AT_Help' để xem chi tiết một lệnh                                       ║");
                ed.WriteMessage("\n║  Gõ 'AT_HelpSearch' để tìm kiếm lệnh theo từ khóa                            ║");
                ed.WriteMessage("\n╚══════════════════════════════════════════════════════════════════════════════╝");

                foreach (var group in grouped)
                {
                    ed.WriteMessage("\n");
                    ed.WriteMessage("\n┌──────────────────────────────────────────────────────────────────────────────┐");
                    ed.WriteMessage("\n│  ▶ {0,-74}│", group.Key.ToUpper());
                    ed.WriteMessage("\n├──────────────────────────────────────────────────────────────────────────────┤");

                    foreach (var cmd in group.OrderBy(c => c.Name))
                    {
                        string desc = cmd.Description ?? "";
                        if (desc.Length > 45)
                            desc = desc.Substring(0, 42) + "...";

                        ed.WriteMessage("\n│  {0,-28} │ {1,-45}│", cmd.Name, desc);
                    }
                    ed.WriteMessage("\n└──────────────────────────────────────────────────────────────────────────────┘");
                }

                ed.WriteMessage("\n");
                ed.WriteMessage("\n═══════════════════════════════════════════════════════════════════════════════");
                ed.WriteMessage("\n  Tổng cộng: {0} lệnh", _commands.Count);
                ed.WriteMessage("\n═══════════════════════════════════════════════════════════════════════════════");
                ed.WriteMessage("\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n❌ Lỗi: " + ex.Message);
            }
        }

        /// <summary>
        /// Lệnh hiển thị help cho một lệnh cụ thể
        /// </summary>
        [CommandMethod("AT_Help")]
        public static void ShowHelp()
        {
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                // Yêu cầu nhập tên lệnh
                PromptStringOptions pso = new PromptStringOptions("\nNhập tên lệnh cần tra cứu: ");
                pso.AllowSpaces = false;
                PromptResult pr = ed.GetString(pso);

                if (pr.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pr.StringResult))
                {
                    ed.WriteMessage("\n⚠ Đã hủy hoặc không nhập tên lệnh.");
                    return;
                }

                string cmdName = pr.StringResult.Trim();

                // Tìm lệnh
                if (_commands.TryGetValue(cmdName, out CommandInfo cmd))
                {
                    DisplayCommandHelp(ed, cmd);
                }
                else
                {
                    // Tìm gần đúng
                    var similar = _commands.Keys
                        .Where(k => k.IndexOf(cmdName, StringComparison.OrdinalIgnoreCase) >= 0)
                        .Take(5)
                        .ToList();

                    ed.WriteMessage("\n");
                    ed.WriteMessage("\n⚠ Không tìm thấy lệnh '{0}'", cmdName);

                    if (similar.Count > 0)
                    {
                        ed.WriteMessage("\n");
                        ed.WriteMessage("\n📌 Có thể bạn muốn tìm:");
                        foreach (var s in similar)
                        {
                            ed.WriteMessage("\n   • {0}", s);
                        }
                    }
                    ed.WriteMessage("\n\n💡 Gõ 'AT_HelpList' để xem danh sách tất cả lệnh");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n❌ Lỗi: " + ex.Message);
            }
        }

        /// <summary>
        /// Hiển thị thông tin chi tiết của một lệnh
        /// </summary>
        private static void DisplayCommandHelp(Editor ed, CommandInfo cmd)
        {
            ed.WriteMessage("\n");
            ed.WriteMessage("\n╔══════════════════════════════════════════════════════════════════════════════╗");
            ed.WriteMessage("\n║  HƯỚNG DẪN SỬ DỤNG LỆNH: {0,-51}║", cmd.Name);
            ed.WriteMessage("\n╠══════════════════════════════════════════════════════════════════════════════╣");

            // Nhóm lệnh
            ed.WriteMessage("\n║  📁 Nhóm: {0,-66}║", cmd.Category ?? "Chưa phân loại");

            // Mô tả
            ed.WriteMessage("\n╠──────────────────────────────────────────────────────────────────────────────╣");
            ed.WriteMessage("\n║  📝 MÔ TẢ:                                                                   ║");
            WrapText(ed, cmd.Description ?? "Chưa có mô tả", 74, "║     ");

            // Cách sử dụng
            ed.WriteMessage("\n╠──────────────────────────────────────────────────────────────────────────────╣");
            ed.WriteMessage("\n║  ⌨ CÚ PHÁP: {0,-64}║", cmd.Usage ?? cmd.Name);

            // Các bước thực hiện
            if (cmd.Steps != null && cmd.Steps.Length > 0)
            {
                ed.WriteMessage("\n╠──────────────────────────────────────────────────────────────────────────────╣");
                ed.WriteMessage("\n║  📋 CÁC BƯỚC THỰC HIỆN:                                                      ║");
                foreach (var step in cmd.Steps)
                {
                    WrapText(ed, step, 74, "║     ");
                }
            }

            // Ví dụ
            if (cmd.Examples != null && cmd.Examples.Length > 0)
            {
                ed.WriteMessage("\n╠──────────────────────────────────────────────────────────────────────────────╣");
                ed.WriteMessage("\n║  💡 VÍ DỤ:                                                                   ║");
                foreach (var ex in cmd.Examples)
                {
                    WrapText(ed, ex, 74, "║     ");
                }
            }

            // Ghi chú
            if (cmd.Notes != null && cmd.Notes.Length > 0)
            {
                ed.WriteMessage("\n╠──────────────────────────────────────────────────────────────────────────────╣");
                ed.WriteMessage("\n║  ⚠ LƯU Ý:                                                                    ║");
                foreach (var note in cmd.Notes)
                {
                    WrapText(ed, note, 74, "║     ");
                }
            }

            ed.WriteMessage("\n╚══════════════════════════════════════════════════════════════════════════════╝");
            ed.WriteMessage("\n");
        }

        /// <summary>
        /// Wrap text để vừa với độ rộng cột
        /// </summary>
        private static void WrapText(Editor ed, string text, int maxWidth, string prefix)
        {
            if (string.IsNullOrEmpty(text)) return;

            int prefixLen = prefix.Length;
            int contentWidth = maxWidth - prefixLen - 1; // -1 for ending ║

            // Split into words
            var words = text.Split(' ');
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 <= contentWidth)
                {
                    if (currentLine.Length > 0)
                        currentLine.Append(" ");
                    currentLine.Append(word);
                }
                else
                {
                    // Output current line
                    ed.WriteMessage("\n{0}{1,-" + contentWidth + "}║", prefix, currentLine.ToString());
                    currentLine.Clear();
                    currentLine.Append(word);
                }
            }

            // Output remaining
            if (currentLine.Length > 0)
            {
                ed.WriteMessage("\n{0}{1,-" + contentWidth + "}║", prefix, currentLine.ToString());
            }
        }

        /// <summary>
        /// Lệnh tìm kiếm lệnh theo từ khóa
        /// </summary>
        [CommandMethod("AT_HelpSearch")]
        public static void SearchCommand()
        {
            Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                // Yêu cầu nhập từ khóa
                PromptStringOptions pso = new PromptStringOptions("\nNhập từ khóa tìm kiếm: ");
                pso.AllowSpaces = true;
                PromptResult pr = ed.GetString(pso);

                if (pr.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pr.StringResult))
                {
                    ed.WriteMessage("\n⚠ Đã hủy hoặc không nhập từ khóa.");
                    return;
                }

                string keyword = pr.StringResult.Trim().ToLower();

                // Tìm kiếm trong tên và mô tả
                var results = _commands.Values
                    .Where(c =>
                        (c.Name?.ToLower().Contains(keyword) == true) ||
                        (c.Description?.ToLower().Contains(keyword) == true) ||
                        (c.Category?.ToLower().Contains(keyword) == true))
                    .OrderBy(c => c.Category)
                    .ThenBy(c => c.Name)
                    .ToList();

                if (results.Count == 0)
                {
                    ed.WriteMessage("\n");
                    ed.WriteMessage("\n⚠ Không tìm thấy lệnh nào với từ khóa '{0}'", keyword);
                    ed.WriteMessage("\n💡 Thử tìm với từ khóa khác hoặc gõ 'AT_HelpList' để xem tất cả lệnh");
                    return;
                }

                ed.WriteMessage("\n");
                ed.WriteMessage("\n╔══════════════════════════════════════════════════════════════════════════════╗");
                ed.WriteMessage("\n║  KẾT QUẢ TÌM KIẾM: '{0,-55}║", keyword + "'");
                ed.WriteMessage("\n╠══════════════════════════════════════════════════════════════════════════════╣");
                ed.WriteMessage("\n║  Tìm thấy {0} lệnh phù hợp                                                   ║", results.Count.ToString().PadRight(3));
                ed.WriteMessage("\n╠══════════════════════════════════════════════════════════════════════════════╣");

                foreach (var cmd in results)
                {
                    string desc = cmd.Description ?? "";
                    if (desc.Length > 40)
                        desc = desc.Substring(0, 37) + "...";

                    ed.WriteMessage("\n║  {0,-28} │ {1,-45}║", cmd.Name, desc);
                }

                ed.WriteMessage("\n╚══════════════════════════════════════════════════════════════════════════════╝");
                ed.WriteMessage("\n");
                ed.WriteMessage("\n💡 Gõ 'AT_Help' rồi nhập tên lệnh để xem chi tiết");
                ed.WriteMessage("\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n❌ Lỗi: " + ex.Message);
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả lệnh (để sử dụng từ code khác)
        /// </summary>
        public static IEnumerable<CommandInfo> GetAllCommands()
        {
            return _commands.Values;
        }

        /// <summary>
        /// Lấy thông tin một lệnh cụ thể
        /// </summary>
        public static CommandInfo GetCommand(string name)
        {
            _commands.TryGetValue(name, out CommandInfo cmd);
            return cmd;
        }

        /// <summary>
        /// Lấy danh sách các category
        /// </summary>
        public static IEnumerable<string> GetCategories()
        {
            return _commands.Values.Select(c => c.Category).Distinct().OrderBy(c => c);
        }

        /// <summary>
        /// Lệnh mở Form hiển thị danh sách lệnh với tabs phân loại
        /// </summary>
        [CommandMethod("AT_HelpForm")]
        public static void ShowHelpForm()
        {
            try
            {
                // Hiển thị form modeless trong AutoCAD
                HelpForm form = new HelpForm();
                AcadApp.ShowModelessDialog(form);
            }
            catch (System.Exception ex)
            {
                Editor ed = AcadApp.DocumentManager.MdiActiveDocument.Editor;
                ed.WriteMessage("\n❌ Lỗi khi mở form: " + ex.Message);
            }
        }
    }
}
