# Hướng Dẫn Sử Dụng Các Lệnh AutoCAD/Civil3D - T27 Tools

> **Phiên bản:** 1.0  
> **Cập nhật:** 30/12/2024  
> **Tác giả:** T27 Engineering

---

## Mục Lục

1. [Tổng Quan](#tổng-quan)
2. [Cách Sử Dụng Hệ Thống Help](#cách-sử-dụng-hệ-thống-help)
3. [Danh Sách Lệnh CAD](#danh-sách-lệnh-cad)
4. [Danh Sách Lệnh Civil 3D](#danh-sách-lệnh-civil-3d)
5. [Các Lệnh Hay Dùng](#các-lệnh-hay-dùng)

---

## Tổng Quan

Bộ công cụ T27 Tools cung cấp hơn **80 lệnh** hỗ trợ thiết kế trong AutoCAD và Civil 3D, được phân chia thành các nhóm:

| Nhóm | Số lệnh | Mô tả |
|------|---------|-------|
| CAD - Tổng hợp | 8 | Tính tổng độ dài, diện tích |
| CAD - Text | 5 | Xử lý text, copy, dịch |
| CAD - Transform | 2 | Xoay, di chuyển đối tượng |
| CAD - Layout | 6 | Làm việc với Layout, Viewport |
| CAD - In ấn | 2 | In hàng loạt |
| CAD - 3D | 2 | Tạo 3D Solid |
| Civil - Corridor | 4 | Corridor, Assembly |
| Civil - SectionView | 20+ | Mặt cắt ngang |
| Civil - Sampleline | 8 | Cọc, Sample Line |
| Civil - Profile | 2 | Trắc dọc |
| Civil - Pipe | 2 | Hệ thống cống |
| Civil - Surface | 2 | Bề mặt địa hình |
| Civil - Point | 1 | CoGo Point |
| Civil - Alignment | 3 | Tuyến đường |

---

## Cách Sử Dụng Hệ Thống Help

### 📋 AT_HelpList - Xem danh sách tất cả lệnh

```
Command: AT_HelpList
```

Hiển thị danh sách tất cả các lệnh được nhóm theo chức năng.

### 🔍 AT_Help - Xem chi tiết một lệnh

```
Command: AT_Help
Nhập tên lệnh cần tra cứu: AT_DoDoc
```

Hiển thị hướng dẫn chi tiết bao gồm:
- Mô tả chức năng
- Cú pháp sử dụng
- Các bước thực hiện
- Ví dụ và lưu ý

### 🔎 AT_HelpSearch - Tìm kiếm lệnh

```
Command: AT_HelpSearch
Nhập từ khóa tìm kiếm: khối lượng
```

Tìm kiếm lệnh theo tên, mô tả hoặc nhóm chức năng.

---

## Danh Sách Lệnh CAD

### 📐 Tổng hợp Độ dài / Diện tích

| Lệnh | Mô tả |
|------|-------|
| `AT_TongDoDai_Full` | Tính tổng độ dài các đối tượng và ghi ra text mới |
| `AT_TongDoDai_Replace` | Tính tổng độ dài và thay thế vào text có sẵn |
| `AT_TongDoDai_Replace2` | Tính tổng độ dài và thay thế (phiên bản 2) |
| `AT_TongDoDai_Replace_CongThem` | Tính tổng độ dài và cộng thêm vào giá trị hiện có |
| `ET_TongDienTich_Full` | Tính tổng diện tích các đối tượng |
| `AT_TongDienTich_Replace` | Tính tổng diện tích và thay thế vào text |
| `AT_TongDienTich_Replace2` | Tính tổng diện tích và thay thế (phiên bản 2) |
| `AT_TongDienTich_Replace_CongThem` | Tính tổng diện tích và cộng thêm |

### 📝 Text

| Lệnh | Mô tả |
|------|-------|
| `AT_TextLink` | Liên kết nội dung giữa các text |
| `ET_DanhSoThuTu` | Đánh số thứ tự tự động cho các text |
| `CT_Copy_NoiDung_Text` | Copy nội dung từ text này sang text khác |
| `CA_CopyVaDichTiengAnh` | Copy text và dịch sang tiếng Anh |
| `AT_TextToSolid` | Chuyển Text thành Solid Hatch hoặc 3D Solid |

### 🔄 Transform

| Lệnh | Mô tả |
|------|-------|
| `AT_XoayDoiTuong_Theo2Diem` | Xoay đối tượng theo hướng của 2 điểm |
| `AT_Offset_2Ben` | Offset đối tượng về cả 2 bên cùng lúc |

### 📄 Layout & Viewport

| Lệnh | Mô tả |
|------|-------|
| `AT_TextLayout` | Chuyển text từ Model sang Layout |
| `ET_TaoMoi_TextLayout` | Tạo mới text trong Layout |
| `ET_DimLayout` | Chuyển Dimension từ Model sang Layout |
| `ET_DimLayout2` | Chuyển Dimension (phiên bản 2) |
| `ET_BlockLayout` | Chuyển Block từ Model sang Layout |
| `AT_UpdateLayout` | Cập nhật tất cả các Layout |
| `AT_BoTri_ViewPort_TheoHinh` | Tự động bố trí Viewport theo hình dạng |
| `AT_XoayDoiTuong_TheoViewport` | Xoay đối tượng theo góc của Viewport |
| `AT_Xoay_ViewPortHienHanh_Theo2Diem` | Xoay Viewport hiện hành theo 2 điểm |

### 🖨️ In ấn

| Lệnh | Mô tả |
|------|-------|
| `AT_InModel_HangLoat` | In hàng loạt các bản vẽ trong Model Space |
| `AT_InBanVe_TheoBlock` | In bản vẽ theo Block trong Layout |

### 📊 Đo lường

| Lệnh | Mô tả |
|------|-------|
| `AT_DoDoc` | Tính và hiển thị độ dốc giữa 2 điểm |
| `AT_DoDoc_Simple` | Tính độ dốc đơn giản |
| `AT_DoDoc_Object` | Tính độ dốc từ Line hoặc Polyline |

### 🗂️ Khác

| Lệnh | Mô tả |
|------|-------|
| `AT_TaoOutline` | Tạo outline cho các đối tượng |
| `AT_TaoBlock_TungDoiTuong` | Tạo Block riêng cho từng đối tượng |
| `AT_Solid_frompolyline` | Tạo 3D Solid từ Polyline bằng extrude |
| `AT_XoaDoiTuong_CungLayer` | Xóa tất cả đối tượng cùng layer |
| `AT_XoaDoiTuong_3DSolid_Body` | Xóa các 3D Solid và Body |
| `AT_annotive_scale_currentOnly` | Chỉ giữ lại annotation scale hiện tại |
| `AT_Xref_all_file` | Quản lý Xref cho tất cả file |
| `AT_XuatXref` | Xuất thông tin Xref |
| `AT_XuatBangToaDo_Polyline` | Xuất bảng tọa độ Polyline ra Excel |
| `AT_Label_FromText` | Tạo Label từ nội dung Text |

---

## Danh Sách Lệnh Civil 3D

### 🛤️ Corridor

| Lệnh | Mô tả |
|------|-------|
| `CTC_TaoCorridor_ChoTuyenDuong` | Tạo Corridor cho tuyến đường |
| `CTC_DieuChinh_PhanDoan` | Điều chỉnh phân đoạn (Region) của Corridor |
| `CTPI_Corridor_SetTargets` | Thiết lập Targets cho Corridor |
| `CTC_TaoCooridor_DuongDoThi_RePhai` | Tạo Corridor đường đô thị với rẽ phải |

### 📊 SectionView (Mặt cắt ngang)

| Lệnh | Mô tả |
|------|-------|
| `CTSV_ChuyenDoi_TNTK_TNTN` | Chuyển đổi trắc ngang TK và TN |
| `CTSV_DanhCap` | Đánh cấp (Grade) trên mặt cắt ngang |
| `CTSV_DanhCap_XoaBo` | Xóa bỏ các đường đánh cấp |
| `CTSV_DanhCap_VeThem` | Vẽ thêm đường đánh cấp |
| `CTSV_DanhCap_VeThem1` | Vẽ thêm đường đánh cấp (v1) |
| `CTSV_DanhCap_VeThem2` | Vẽ thêm đường đánh cấp (v2) |
| `CTSV_DanhCap_CapNhat` | Cập nhật đường đánh cấp |
| `CTSV_ThemVatLieu_TrenCatNgang` | Thêm vật liệu trên mặt cắt ngang |
| `CTSV_ThayDoi_MSS_Min_Max` | Thay đổi Min/Max của MSS |
| `CTSV_ThayDoi_GioiHan_traiPhai` | Thay đổi giới hạn trái/phải |
| `CTSV_ThayDoi_KhungIn` | Thay đổi khung in |
| `CTSV_KhoaCatNgang_AddPoint` | Thêm điểm vào khóa cắt ngang |
| `CTSV_fit_KhungIn` | Fit Section View vào khung in |
| `CTSV_fit_KhungIn_5_5_top` | Fit với margin 5-5 |
| `CTSV_fit_KhungIn_5_10_top` | Fit với margin 5-10 |
| `CTSV_An_DuongDiaChat` | Ẩn các đường địa chất |
| `CTSV_HieuChinh_Section` | Hiệu chỉnh Section View |
| `CTSV_HieuChinh_Section_Dynamic` | Hiệu chỉnh Section View động |
| `CTSV_DieuChinh_DuongTuNhien` | Điều chỉnh đường tự nhiên |
| `CTSV_KhoiLuongCatNgang` | Tính khối lượng từ mặt cắt ngang |
| `CTSV_XuatKhoiLuongRaExcel` | Xuất khối lượng ra Excel |
| `CTSV_VeTracNgangThietKe` | Vẽ trắc ngang thiết kế |
| `CTSV_TaoCorridorSurface` | Tạo Corridor Surface |
| `CTSV_TaoCorridorSurfaceMultiple` | Tạo nhiều Corridor Surface |
| `CTSV_TaoCorridorSurfaceSingle` | Tạo một Corridor Surface |
| `AT_PolylineFromSection` | Tạo Polyline từ Section View |

### 📍 Sampleline (Cọc)

| Lệnh | Mô tả |
|------|-------|
| `CTS_DoiTenCoc` | Đổi tên cọc (Sample Line) |
| `CTS_DoiTenCoc2` | Đổi tên cọc (phiên bản 2) |
| `CTS_DoiTenCoc3` | Đổi tên cọc (phiên bản 3) |
| `CTS_TaoBang_ToaDoCoc` | Tạo bảng tọa độ các cọc |
| `CTS_TaoBang_ToaDoCoc2` | Tạo bảng tọa độ cọc (v2) |
| `CTS_TaoBang_ToaDoCoc3` | Tạo bảng tọa độ cọc (v3) |
| `CTS_ThayDoi_BeRong_Sampleline` | Thay đổi bề rộng Sample Line |
| `AT_UPdate2Table` | Cập nhật thông tin vào bảng |

### 📈 Profile

| Lệnh | Mô tả |
|------|-------|
| `CTP_ThayDoi_profile_Band` | Thay đổi Profile Band |

### 🔧 Pipe & Structure

| Lệnh | Mô tả |
|------|-------|
| `CTPi_DieuChinh_BeMat_ThamChieu` | Điều chỉnh bề mặt tham chiếu |
| `CTPi_ThayDoi_CaoDo_DayCong` | Thay đổi cao độ đáy cống |

### 🗺️ Surface

| Lệnh | Mô tả |
|------|-------|
| `CTS_TaoSpotElevation_OnSurface_TaiTim` | Tạo Spot Elevation trên Surface |
| `CTSU_CaoDoMatPhang_TaiCogopoint` | Lấy cao độ mặt phẳng tại CoGo Point |

### 📌 Point

| Lệnh | Mô tả |
|------|-------|
| `CTPo_DoiTen_CogoPoint_fromAlignment` | Đổi tên CoGo Point theo Alignment |

### 🛣️ Alignment

| Lệnh | Mô tả |
|------|-------|
| `AT_OffsetAlignment` | Tạo Offset Alignment |
| `CTA_BangThongKeCacTuyenDuong` | Tạo bảng thống kê các tuyến đường |
| `CT_TaoDuong_ConnectedAlignment_NutGiao` | Tạo đường nối tại nút giao |

### ℹ️ Thông tin

| Lệnh | Mô tả |
|------|-------|
| `CT_ThongTinDoiTuong` | Hiển thị thông tin đối tượng Civil 3D |
| `AT_Solid_Set_PropertySet` | Thiết lập Property Set cho 3D Solid |
| `AT_Solid_Show_Info` | Hiển thị thông tin Property |
| `AT_XuatBang_Civil3D_ToExcel` | Xuất các bảng Civil 3D ra Excel |

---

## Các Lệnh Hay Dùng

### 🌟 Top 10 lệnh được sử dụng nhiều nhất

1. **AT_DoDoc** - Tính độ dốc giữa 2 điểm
2. **AT_TongDoDai_Full** - Tính tổng độ dài
3. **ET_TongDienTich_Full** - Tính tổng diện tích
4. **AT_InModel_HangLoat** - In hàng loạt
5. **CTSV_KhoiLuongCatNgang** - Tính khối lượng cắt ngang
6. **CTC_TaoCorridor_ChoTuyenDuong** - Tạo Corridor
7. **CTS_DoiTenCoc** - Đổi tên cọc
8. **CTSV_DanhCap** - Đánh cấp trên cắt ngang
9. **AT_Offset_2Ben** - Offset 2 bên
10. **AT_TaoBlock_TungDoiTuong** - Tạo Block từ đối tượng

---

## Liên Hệ & Hỗ Trợ

Nếu gặp vấn đề khi sử dụng, vui lòng liên hệ:
- **Email:** support@t27.vn
- **Hotline:** 0909 xxx xxx

---

*© 2024 T27 Engineering. All rights reserved.*
