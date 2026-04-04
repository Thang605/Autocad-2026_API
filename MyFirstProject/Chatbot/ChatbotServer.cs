// Voice Chatbot WebSocket Server for AutoCAD
// Chạy bên trong AutoCAD, nhận lệnh từ web chatbot qua WebSocket

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using MyFirstProject.Extensions;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Civil3DCsharp.Chatbot
{
    /// <summary>
    /// WebSocket server chạy trong AutoCAD để nhận lệnh từ chatbot
    /// </summary>
    public class ChatbotServer
    {
        private static ChatbotServer? _instance;
        private HttpListener? _httpListener;
        private CancellationTokenSource? _cts;
        private readonly ConcurrentBag<WebSocket> _clients = new();
        private bool _isRunning = false;

        public int Port { get; private set; } = 8765;
        public bool IsRunning => _isRunning;

        public static ChatbotServer Instance
        {
            get
            {
                _instance ??= new ChatbotServer();
                return _instance;
            }
        }

        /// <summary>
        /// Danh sách tất cả lệnh được hỗ trợ (tên lệnh -> mô tả)
        /// </summary>
        private static readonly Dictionary<string, string> SupportedCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            // === LỆNH AUTOCAD CƠ BẢN ===
            { "LINE", "Vẽ đường thẳng" },
            { "PLINE", "Vẽ polyline" },
            { "CIRCLE", "Vẽ hình tròn" },
            { "ARC", "Vẽ cung tròn" },
            { "RECTANGLE", "Vẽ hình chữ nhật" },
            { "POLYGON", "Vẽ đa giác" },
            { "ELLIPSE", "Vẽ hình elip" },
            { "SPLINE", "Vẽ đường cong spline" },
            { "HATCH", "Tạo hatch" },
            { "MLINE", "Vẽ multiline" },
            { "POINT", "Vẽ điểm" },
            { "RAY", "Vẽ tia" },
            { "XLINE", "Vẽ đường vô hạn" },
            // Modify
            { "MOVE", "Di chuyển đối tượng" },
            { "COPY", "Sao chép đối tượng" },
            { "ROTATE", "Xoay đối tượng" },
            { "SCALE", "Thu phóng đối tượng" },
            { "MIRROR", "Lấy đối xứng" },
            { "OFFSET", "Offset đối tượng" },
            { "TRIM", "Cắt đối tượng" },
            { "EXTEND", "Kéo dài đối tượng" },
            { "FILLET", "Bo tròn góc" },
            { "CHAMFER", "Vát góc" },
            { "ARRAY", "Tạo mảng đối tượng" },
            { "STRETCH", "Kéo giãn đối tượng" },
            { "BREAK", "Bẻ gãy đối tượng" },
            { "JOIN", "Nối đối tượng" },
            { "EXPLODE", "Phá khối" },
            { "ERASE", "Xóa đối tượng" },
            // View
            { "ZOOM", "Zoom" },
            { "PAN", "Pan bản vẽ" },
            { "REGEN", "Tái tạo bản vẽ" },
            { "REDRAW", "Vẽ lại bản vẽ" },
            // File
            { "QSAVE", "Lưu nhanh" },
            { "SAVEAS", "Lưu thành file mới" },
            { "NEW", "Tạo bản vẽ mới" },
            { "OPEN", "Mở file bản vẽ" },
            { "CLOSE", "Đóng bản vẽ" },
            { "PLOT", "In bản vẽ" },
            // Edit
            { "UNDO", "Hoàn tác" },
            { "REDO", "Làm lại" },
            { "OOPS", "Khôi phục đối tượng vừa xóa" },
            // Text & Dimension
            { "TEXT", "Tạo text" },
            { "MTEXT", "Tạo multiline text" },
            { "DIM", "Đo kích thước" },
            { "DIMLINEAR", "Đo kích thước thẳng" },
            { "DIMALIGNED", "Đo kích thước canh" },
            { "DIMRADIUS", "Đo bán kính" },
            { "DIMDIAMETER", "Đo đường kính" },
            { "DIMANGULAR", "Đo góc" },
            // Layer
            { "LAYER", "Quản lý layer" },
            { "LAYOFF", "Tắt layer" },
            { "LAYON", "Bật tất cả layer" },
            { "LAYISO", "Cô lập layer" },
            // Block
            { "BLOCK", "Tạo block" },
            { "INSERT", "Chèn block" },
            { "WBLOCK", "Write block ra file" },
            // Properties
            { "PROPERTIES", "Mở bảng properties" },
            { "MATCHPROP", "Copy thuộc tính" },
            { "CHPROP", "Thay đổi thuộc tính" },
            // 3D
            { "EXTRUDE", "Đùn 3D" },
            { "REVOLVE", "Xoay tròn 3D" },
            { "UNION", "Hợp nhất solid" },
            { "SUBTRACT", "Trừ solid" },
            { "INTERSECT", "Giao solid" },
            // Other
            { "DIST", "Đo khoảng cách" },
            { "AREA", "Đo diện tích" },
            { "LIST", "Liệt kê thông tin đối tượng" },
            { "ID", "Hiển thị tọa độ điểm" },
            { "PURGE", "Dọn dẹp bản vẽ" },
            { "AUDIT", "Kiểm tra bản vẽ" },
            { "PEDIT", "Chỉnh sửa polyline" },

            // === ACAD TOOL (49 lệnh) ===
            { "AT_annotive_scale_currentOnly", "Chỉ giữ annotative scale hiện tại" },
            { "AT_BlockLayout", "Block layout" },
            { "AT_DanhSoThuTu", "Đánh số thứ tự" },
            { "AT_DimLayout", "Dim layout" },
            { "AT_DimLayout2", "Dim layout phiên bản 2" },
            { "AT_Label_FromText", "Tạo label từ text" },
            { "AT_Offset_2Ben", "Offset 2 bên" },
            { "AT_TaoMoi_TextLayout", "Tạo mới text layout" },
            { "AT_TextLayout", "Text layout" },
            { "AT_TextLink", "Liên kết text" },
            { "AT_TongDienTich_Full", "Tổng diện tích (full)" },
            { "AT_TongDienTich_Replace", "Tổng diện tích (replace)" },
            { "AT_TongDienTich_Replace_CongThem", "Tổng diện tích (replace cộng thêm)" },
            { "AT_TongDienTich_Replace2", "Tổng diện tích (replace 2)" },
            { "AT_TongDoDai_Full", "Tổng độ dài (full)" },
            { "AT_TongDoDai_Replace", "Tổng độ dài (replace)" },
            { "AT_TongDoDai_Replace_CongThem", "Tổng độ dài (replace cộng thêm)" },
            { "AT_TongDoDai_Replace2", "Tổng độ dài (replace 2)" },
            { "AT_UpdateLayout", "Cập nhật layout" },
            { "AT_XoaDoiTuong_3DSolid_Body", "Xóa đối tượng 3D Solid/Body" },
            { "AT_XoaDoiTuong_CungLayer", "Xóa đối tượng cùng layer" },
            { "AT_XoayDoiTuong_Theo2Diem", "Xoay đối tượng theo 2 điểm" },
            { "AT_Surface_frompolyline", "Tạo surface từ polyline" },
            { "XUATBANG_ToaDoPolyline", "Xuất bảng tọa độ polyline" },
            { "AT_XuatBang_Civil3D_ToExcel", "Xuất bảng Civil 3D sang Excel" },
            { "AT_TaoOutline", "Tạo outline" },
            { "CT_Copy_NoiDung_Text", "Copy nội dung text" },
            { "CA", "Copy và dịch tiếng Anh" },
            { "AT_DoDoc", "Đo độ dốc" },
            { "AT_DoDoc_Object", "Đo độ dốc theo object" },
            { "AT_DoDoc_Simple", "Đo độ dốc đơn giản" },
            { "AT_XrefAll", "Xref tất cả file" },
            { "AT_XrefAllOverlay", "Xref tất cả file (overlay)" },
            { "AT_XrefAttachToOverlay", "Chuyển xref attach sang overlay" },
            { "AT_XrefAttachToOverlayFile", "Chuyển xref attach sang overlay (file)" },
            { "AT_XrefToBlock", "Chuyển xref sang block" },
            { "AT_XoayDoiTuong_TheoViewport", "Xoay đối tượng theo viewport" },
            { "AT_XoayDoiTuong_TheoViewport_V2", "Xoay đối tượng theo viewport V2" },
            { "AT_BoTri_ViewPort_TheoHinh", "Bố trí viewport theo hình" },
            { "AT_Xoay_ViewPort_Theo2Diem", "Xoay viewport theo 2 điểm" },
            { "AT_TAOBLOCK_TUNGDOITUONG", "Tạo block từng đối tượng" },
            { "AT_InModel_HangLoat", "In model hàng loạt" },
            { "AT_TextToSolid", "Chuyển text thành solid" },
            { "AT_TextToSolid_Step2", "Chuyển text thành solid bước 2" },
            { "AT_PolysToSolid", "Chuyển polyline thành solid" },
            { "AT_InBanVe_TheoBlock", "In bản vẽ theo block" },
            { "AT_DanhSoThuTu_ChoBlock", "Đánh số thứ tự cho block" },
            { "AT_DIM_DUONGCONG", "Dim đường cong" },
            { "AT_TXTEXP", "Explode text (no text)" },

            // === CIVIL TOOL (94 lệnh) ===
            // Corridor
            { "CTC_AddAllSection", "Thêm tất cả section vào corridor" },
            { "CTC_DieuChinh_PhanDoan", "Điều chỉnh phân đoạn corridor" },
            { "CTC_TaoCorridor_ChoTuyenDuong", "Tạo corridor cho tuyến đường" },
            { "CTPI_Corridor_SetTargets", "Corridor set targets" },
            { "CAC_TaoCooridor_DuongDoThi_RePhai", "Tạo corridor đường đô thị rẽ phải" },
            // Parcel
            { "CTPA_TaoParcel_CacLoaiNha", "Tạo parcel các loại nhà" },
            { "CTPA_DoiTen_Parcel", "Đổi tên parcel" },
            { "CTPA_DoiTen_Parcel_Nhanh", "Đổi tên parcel nhanh" },
            // Pipe
            { "CTPI_ThayDoi_DuongKinhCong", "Thay đổi đường kính cống" },
            { "CTPI_ThayDoi_DoanDocCong", "Thay đổi độ dốc cống" },
            { "CTPI_BangCaoDo_TuNhienHoThu", "Bảng cao độ tự nhiên hố thu" },
            { "CTPI_XoayHoThu_Theo2diem", "Xoay hố thu theo 2 điểm" },
            { "CTPi_ThayDoi_CaoDo_DayCong", "Thay đổi cao độ đáy cống" },
            { "CTPi_DieuChinh_BeMat_ThamChieu", "Điều chỉnh bề mặt tham chiếu" },
            // Point
            { "CTPO_TaoCogoPoint_CaoDo_FromSurface", "Tạo CogoPoint cao độ từ Surface" },
            { "CTPO_TaoCogoPoint_CaoDo_Elevationspot", "Tạo CogoPoint từ Elevation spot" },
            { "CTPO_UpdateAllPointGroup", "Update tất cả Point Group" },
            { "CTPO_CreateCogopointFromText", "Tạo CogoPoint từ Text" },
            { "CTPO_An_CogoPoint", "Ẩn CogoPoint" },
            { "CTPO_TaoCogoPoint_FromExcel", "Tạo CogoPoint từ Excel" },
            { "CTPO_DoiTen_Cogopoint", "Đổi tên CogoPoint" },
            { "CTPo_DoiTen_CogoPoint_fromAlignment", "Đổi tên CogoPoint từ alignment" },
            // Profile & ProfileView
            { "CTP_VeTracDoc_TuNhien", "Vẽ trắc dọc tự nhiên" },
            { "CTP_VeTracDoc_TuNhien_TatCaTuyen", "Vẽ trắc dọc tự nhiên tất cả tuyến" },
            { "CTP_Fix_DuongTuNhien_TheoCoc", "Fix đường tự nhiên theo cọc" },
            { "CTP_GanNhanNutGiao_LenTracDoc", "Gán nhãn nút giao lên trắc dọc" },
            { "CTP_TaoCogoPointTuPVI", "Tạo CogoPoint từ PVI" },
            { "CTP_ThayDoi_profile_Band", "Thay đổi profile band" },
            { "CTP_Polyline_To_Profile", "Chuyển polyline thành profile" },
            { "CTP_Adjust_Profile_By_Polyline", "Điều chỉnh profile theo polyline" },
            // Sampleline
            { "CTS_PhatSinhCoc", "Phát sinh cọc" },
            { "CTS_PhatSinhCoc_TheoBang", "Phát sinh cọc theo bảng" },
            { "CTS_PhatSinhCoc_theoKhoangDelta", "Phát sinh cọc theo khoảng delta" },
            { "CTS_PhatSinhCoc_TuCogoPoint", "Phát sinh cọc từ CogoPoint" },
            { "CTS_PhatSinhCoc_ThuCong", "Phát sinh cọc thủ công" },
            { "CTS_DoiTenCoc", "Đổi tên cọc" },
            { "CTS_DoiTenCoc2", "Đổi tên cọc 2" },
            { "CTS_DoiTenCoc3", "Đổi tên cọc 3" },
            { "CTS_DoiTenCoc_H", "Đổi tên cọc H" },
            { "CTS_DoiTenCoc_TheoThuTu", "Đổi tên cọc theo thứ tự" },
            { "CTS_DoiTenCoc_fromCogoPoint", "Đổi tên cọc từ CogoPoint" },
            { "CTS_DichCoc_TinhTien", "Dịch cọc tịnh tiến" },
            { "CTS_DichCoc_TinhTien_20", "Dịch cọc tịnh tiến 20m" },
            { "CTS_DichCoc_TinhTien40", "Dịch cọc tịnh tiến 40m" },
            { "CTS_Copy_NhomCoc", "Copy nhóm cọc" },
            { "CTS_Copy_BeRong_sampleLine", "Copy bề rộng sample line" },
            { "CTS_Offset_BeRong_sampleLine", "Offset bề rộng sample line" },
            { "CTS_DongBo_2_NhomCoc", "Đồng bộ 2 nhóm cọc" },
            { "CTS_DongBo_2_NhomCoc_TheoDoan", "Đồng bộ 2 nhóm cọc theo đoạn" },
            { "CTS_ChenCoc_TrenTracDoc", "Chèn cọc trên trắc dọc" },
            { "CTS_CHENCOC_TRENTRACNGANG", "Chèn cọc trên trắc ngang" },
            { "CTS_TaoBang_ToaDoCoc", "Tạo bảng tọa độ cọc" },
            { "CTS_TaoBang_ToaDoCoc2", "Tạo bảng tọa độ cọc 2" },
            { "CTS_TaoBang_ToaDoCoc3", "Tạo bảng tọa độ cọc 3" },
            { "AT_UPdate2Table", "Update 2 bảng" },
            { "CTS_ThayDoi_BeRong_Sampleline", "Thay đổi bề rộng sample line" },
            { "CTS_HieuChinh_KhoangCachCoc", "Hiệu chỉnh khoảng cách cọc" },
            // Section View
            { "CTSV_DanhCap", "Đánh cấp" },
            { "CTSV_DanhCap_CapNhat", "Đánh cấp - cập nhật" },
            { "CTSV_DanhCap_VeThem", "Đánh cấp - vẽ thêm" },
            { "CTSV_DanhCap_VeThem1", "Đánh cấp - vẽ thêm 1" },
            { "CTSV_DanhCap_VeThem2", "Đánh cấp - vẽ thêm 2" },
            { "CTSV_DanhCap_XoaBo", "Đánh cấp - xóa bỏ" },
            { "CTSV_HieuChinh_Section", "Hiệu chỉnh section" },
            { "CTSV_HieuChinh_Section_Dynamic", "Hiệu chỉnh section (dynamic)" },
            { "CTSV_ThayDoi_GioiHan_traiPhai", "Thay đổi giới hạn trái/phải" },
            { "CTSV_ThayDoi_KhungIn", "Thay đổi khung in" },
            { "CTSV_ThayDoi_MSS_Min_Max", "Thay đổi MSS min/max" },
            { "CTSV_fit_KhungIn", "Fit khung in" },
            { "CTSV_fit_KhungIn_5_10_top", "Fit khung in 5-10 top" },
            { "CTSV_fit_KhungIn_5_5_top", "Fit khung in 5-5 top" },
            { "CTSV_An_DuongDiaChat", "Ẩn đường địa chất" },
            { "CTSV_ChuyenDoi_TNTK_TNTN", "Chuyển đổi TNTK/TNTN" },
            { "CTSV_KhoaCatNgang_AddPoint", "Khóa cắt ngang - thêm điểm" },
            { "CTSV_ThemVatLieu_TrenCatNgang", "Thêm vật liệu trên cắt ngang" },
            { "CTSV_VeTracNgangThietKe", "Vẽ trắc ngang thiết kế" },

            { "CTSV_KhoiLuongCatNgang", "Khối lượng cắt ngang" },
            { "CTSV_XuatKhoiLuongRaExcel", "Xuất khối lượng ra Excel" },
            { "CTSV_TaoCorridorSurface", "Tạo corridor surface" },
            { "CTSV_Them_BangKL_CatNgang", "Thêm bảng khối lượng cắt ngang" },
            { "CTSV_ChonSection_Static", "Chọn section static" },
            // Surface
            { "CTS_TaoSpotElevation_OnSurface_TaiTim", "Tạo Spot Elevation trên Surface tại tim" },
            { "CTSU_CaoDoMatPhang_TaiCogopoint", "Cao độ mặt phẳng tại CogoPoint" },
            // Alignment
            { "AT_OffsetAlignment", "Offset alignment" },
            { "CTA_BangThongKeCacTuyenDuong", "Bảng thống kê các tuyến đường" },
            { "CTA_DieuChinhBanKinh_ConnectedAlignment", "Điều chỉnh bán kính connected alignment" },
            { "CTA_DieuChinhBanKinh_Help", "Điều chỉnh bán kính - help" },
            { "CTA_GanNhan_DauCuoiTyten", "Gán nhãn đầu cuối tỷ lệ" },
            // Property Sets
            { "AT_Solid_Set_PropertySet", "Set property set cho solid" },
            { "AT_Solid_Show_Info", "Hiển thị thông tin solid" },
            // Other Civil

            { "CT_ThongTinDoiTuong", "Thông tin đối tượng Civil 3D" },

            // === CIVIL TOOL 2 ===
            { "CTS_Them_MaterialList", "Thêm material list" },
            { "CTS_Xem_MaterialList", "Xem material list" },
            { "CTS_Xoa_MaterialList", "Xóa material list" },
            { "AT_XuatBang_SangExcel", "Xuất bảng sang Excel" },
            { "AT_BoTri_ViewPort_Theo2Diem", "Bố trí viewport theo 2 điểm" },

            // === HELP & MENU ===
            { "SHOW_MENU", "Hiển thị menu classic" },
            { "AT_HelpList", "Danh sách tất cả lệnh" },
            { "AT_Help", "Tra cứu help cho lệnh" },
            { "AT_HelpSearch", "Tìm kiếm lệnh" },
            { "AT_HelpForm", "Hiển thị form help" },
            { "SHORTCUT_MANAGER", "Quản lý phím tắt" },
        };

        /// <summary>
        /// Khởi động WebSocket server
        /// </summary>
        public async Task StartAsync()
        {
            if (_isRunning)
            {
                A.Ed.WriteMessage("\n⚠ Chatbot server đang chạy rồi trên port " + Port);
                return;
            }

            _cts = new CancellationTokenSource();
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://localhost:{Port}/");
            _httpListener.Prefixes.Add($"http://127.0.0.1:{Port}/");

            try
            {
                _httpListener.Start();
                _isRunning = true;
                A.Ed.WriteMessage($"\n✅ Chatbot server đã khởi động! Port: {Port}");
                A.Ed.WriteMessage($"\n🌐 Mở file WebUI/index.html trong trình duyệt để sử dụng chatbot");

                _ = Task.Run(() => AcceptClientsAsync(_cts.Token), _cts.Token);
            }
            catch (HttpListenerException ex)
            {
                A.Ed.WriteMessage($"\n❌ Không thể khởi động server: {ex.Message}");
                A.Ed.WriteMessage($"\n💡 Thử chạy: netsh http add urlacl url=http://localhost:{Port}/ user=Everyone");
            }
        }

        /// <summary>
        /// Dừng server
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;

            _cts?.Cancel();
            _httpListener?.Stop();
            _httpListener?.Close();
            _isRunning = false;
            _clients.Clear();
            A.Ed.WriteMessage("\n🛑 Chatbot server đã dừng.");
        }

        /// <summary>
        /// Chấp nhận kết nối từ client
        /// </summary>
        private async Task AcceptClientsAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var context = await _httpListener!.GetContextAsync();

                    // Serve static files (HTML/CSS/JS)
                    if (!context.Request.IsWebSocketRequest)
                    {
                        await HandleHttpRequest(context);
                        continue;
                    }

                    var wsContext = await context.AcceptWebSocketAsync(null);
                    var ws = wsContext.WebSocket;
                    _clients.Add(ws);

                    A.Ed.WriteMessage("\n🔗 Chatbot client đã kết nối!");

                    // Gửi danh sách lệnh cho client
                    await SendCommandList(ws);

                    _ = Task.Run(() => HandleClientAsync(ws, ct), ct);
                }
                catch (System.Exception) when (ct.IsCancellationRequested) { break; }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Accept error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Xử lý HTTP request (serve static files cho WebUI)
        /// </summary>
        private async Task HandleHttpRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // CORS headers
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            // API endpoint: GET /api/commands
            if (request.Url?.AbsolutePath == "/api/commands")
            {
                var json = JsonSerializer.Serialize(SupportedCommands);
                var buffer = Encoding.UTF8.GetBytes(json);
                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer);
                response.Close();
                return;
            }

            // Serve WebUI files
            string basePath = Path.Combine(
                Path.GetDirectoryName(typeof(ChatbotServer).Assembly.Location) ?? "",
                "..", "..", "..", "Chatbot", "WebUI"
            );

            // Fallback: try relative to csproj location
            if (!Directory.Exists(basePath))
            {
                basePath = @"c:\Dropbox\DATA\AI Agent\Autocad 2026_API\MyFirstProject\Chatbot\WebUI";
            }

            string requestPath = request.Url?.AbsolutePath ?? "/";
            if (requestPath == "/") requestPath = "/index.html";

            string filePath = Path.Combine(basePath, requestPath.TrimStart('/').Replace('/', '\\'));

            if (File.Exists(filePath))
            {
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                response.ContentType = GetContentType(filePath);
                response.ContentLength64 = fileBytes.Length;
                await response.OutputStream.WriteAsync(fileBytes);
            }
            else
            {
                response.StatusCode = 404;
                var msg = Encoding.UTF8.GetBytes("404 Not Found");
                await response.OutputStream.WriteAsync(msg);
            }

            response.Close();
        }

        private static string GetContentType(string filePath)
        {
            return Path.GetExtension(filePath).ToLower() switch
            {
                ".html" => "text/html; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".js" => "application/javascript; charset=utf-8",
                ".json" => "application/json; charset=utf-8",
                ".png" => "image/png",
                ".ico" => "image/x-icon",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Gửi danh sách lệnh cho client khi kết nối
        /// </summary>
        private async Task SendCommandList(WebSocket ws)
        {
            var msg = new
            {
                type = "commands",
                commands = SupportedCommands
            };
            string json = JsonSerializer.Serialize(msg);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// Xử lý tin nhắn từ client
        /// </summary>
        private async Task HandleClientAsync(WebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[4096];

            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                try
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                        A.Ed.WriteMessage("\n🔌 Chatbot client đã ngắt kết nối.");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessMessage(ws, message);
                }
                catch (WebSocketException) { break; }
                catch (OperationCanceledException) { break; }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Client error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Xử lý message từ chatbot client
        /// </summary>
        private async Task ProcessMessage(WebSocket ws, string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                string type = root.GetProperty("type").GetString() ?? "";

                switch (type)
                {
                    case "execute":
                        string command = root.GetProperty("command").GetString() ?? "";
                        await ExecuteCommand(ws, command);
                        break;

                    case "ping":
                        await SendResponse(ws, "pong", "ok", "Server đang hoạt động");
                        break;

                    default:
                        await SendResponse(ws, "error", "unknown", $"Không hiểu loại tin nhắn: {type}");
                        break;
                }
            }
            catch (JsonException)
            {
                await SendResponse(ws, "error", "parse_error", "Tin nhắn không đúng định dạng JSON");
            }
        }

        /// <summary>
        /// Thực thi lệnh AutoCAD trên main thread
        /// Hỗ trợ chuỗi lệnh động chứa \n (ví dụ: "CIRCLE\n10,20\n15\n")
        /// </summary>
        private async Task ExecuteCommand(WebSocket ws, string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                await SendResponse(ws, "result", "error", "Lệnh trống!");
                return;
            }

            try
            {
                // Chuẩn hóa chuỗi lệnh:
                // JSON parsing đã chuyển \n thành newline thực
                // Nhưng nếu vẫn còn literal "\n" (escaped), cũng chuyển luôn
                string cmdToSend = command;
                cmdToSend = cmdToSend.Replace("\\n", "\n");

                // Đảm bảo kết thúc bằng newline
                if (!cmdToSend.EndsWith("\n"))
                {
                    cmdToSend += "\n";
                }

                // Thực thi trên main thread của AutoCAD
                Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(
                    cmdToSend, true, false, false
                );

                // Lấy tên lệnh chính để hiển thị mô tả
                string cmdName = cmdToSend.Split('\n')[0].Trim();
                string description = "";
                if (SupportedCommands.TryGetValue(cmdName, out var desc))
                {
                    description = desc;
                }

                // Hiển thị log trên AutoCAD (rút gọn cho dễ đọc)
                string displayCmd = cmdToSend.Replace("\n", " ").Trim();
                A.Ed.WriteMessage($"\n🤖 Chatbot → {displayCmd}");
                await SendResponse(ws, "result", "ok",
                    $"✅ Đã gửi lệnh: {displayCmd}" + (description != "" ? $" ({description})" : ""));
            }
            catch (System.Exception ex)
            {
                await SendResponse(ws, "result", "error", $"❌ Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Gửi phản hồi JSON cho client
        /// </summary>
        private async Task SendResponse(WebSocket ws, string type, string status, string message)
        {
            var response = new { type, status, message };
            string json = JsonSerializer.Serialize(response);
            var bytes = Encoding.UTF8.GetBytes(json);

            if (ws.State == WebSocketState.Open)
            {
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
