// ============================================================
// 07.Sampleline - Module quản lý cọc (SampleLine) cho Civil 3D
// ============================================================
//
// File gốc đã được tách ra thành các file nhỏ hơn để dễ quản lý:
//
// 07b.SamplelineOffset.cs   - Offset bề rộng SampleLine (2 lệnh)
//   - CTS_Copy_BeRong_sampleLine
//   - CTS_Offset_BeRong_sampleLine
//
// 07c.SamplelineRename.cs   - Đổi tên cọc (6 lệnh)
//   - CTS_DoiTenCoc
//   - CTS_DoiTenCoc2
//   - CTS_DoiTenCoc3
//   - CTS_DoiTenCoc_fromCogoPoint
//   - CTS_DoiTenCoc_TheoThuTu
//   - CTS_DoiTenCoc_H
//
// 07d.SamplelineTable.cs    - Bảng tọa độ + cập nhật bảng (4 lệnh)
//   - AT_UPdate2Table
//
// 07e.SamplelineCreate.cs   - Chèn / Phát sinh cọc (6 lệnh)
//   - CTS_ChenCoc_TrenTracDoc
//   - CTS_CHENCOC_TRENTRACNGANG
//   - CTS_PhatSinhCoc
//   - CTS_PhatSinhCoc_theoKhoangDelta
//   - CTS_PhatSinhCoc_TuCogoPoint
//   - CTS_PhatSinhCoc_TheoBang
//
// 07f.SamplelineMove.cs     - Dịch cọc tịnh tiến (3 lệnh)
//   - CTS_DichCoc_TinhTien
//   - CTS_DichCoc_TinhTien40
//   - CTS_DichCoc_TinhTien_20
//
// 07g.SamplelineSync.cs     - Copy / Đồng bộ nhóm cọc (3 lệnh)
//   - CTS_Copy_NhomCoc
//   - CTS_DongBo_2_NhomCoc
//   - CTS_DongBo_2_NhomCoc_TheoDoan
//
// Tổng cộng: 24 lệnh trong 6 file
// ============================================================
