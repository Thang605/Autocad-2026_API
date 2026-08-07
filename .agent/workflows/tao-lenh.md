---
description: Quy trình tạo lệnh mới cho AutoCAD/Civil3D plugin
---

# Quy trình tạo lệnh mới

## Bước 1: Tạo file lệnh & Form UI (nếu lệnh cần Form)
- **File Lệnh**: Tạo file `.cs` mới trong thư mục `Acad Tool` hoặc `Civil Tool`.
  - Đặt tên file theo format: `XX.AT_TenLenh.cs` hoặc `XX.CT_TenLenh.cs` (Ví dụ: `36.CTP_DieuChinhDuongDo.cs`).
  - Đặt `[assembly: CommandClass(typeof(Civil3DCsharp.TenClass))]` ở đầu file.
- **File Form UI** (Nếu lệnh có giao diện Form):
  - Tạo file Form theo format: `TenLenhForm.cs` (Ví dụ: `DieuChinhDuongDoForm.cs`).
  - Form kế thừa `System.Windows.Forms.Form`.
- **Ghi nhớ đối tượng & thông số đã chọn cho lần thực hiện sau**:
  - Khai báo các `private static ObjectId _lastObjectId = ObjectId.Null;` trong Command Class để lưu vết đối tượng đã chọn.
  - Khi lệnh chạy lần sau, kiểm tra đối tượng cũ (`!_lastObjectId.IsNull && _lastObjectId.IsValid && !_lastObjectId.IsErased`) để tự động nạp lại thông tin vào Form.
  - Khai báo static variables (`_last...`) trong Form để nhớ thông số nhập gần nhất.
  - Sử dụng `ed.StartUserInteraction(form)` khi ấn nút pick trên màn hình để ẩn Form tạm thời và lấy dữ liệu.
  - Gọi Form bằng `Application.ShowModalDialog(form)` trong phương thức lệnh.

## Bước 2: Kiểm tra xung đột namespace
Các xung đột thường gặp cần alias:
```csharp
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using DrawingFont = System.Drawing.Font;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Label = Autodesk.Civil.DatabaseServices.Label;
using WinFormsLabel = System.Windows.Forms.Label;
```

## Bước 3: Kiểm tra Transaction & OpenMode
- Luôn sử dụng `OpenMode.ForWrite` khi sửa đổi đối tượng Civil 3D/AutoCAD trong `Transaction`.
- Bọc logic sửa đổi bản vẽ trong khối `using (Transaction tr = db.TransactionManager.StartTransaction())`.

## Bước 4: Build để kiểm tra lỗi
```powershell
cd "c:\Dropbox\0.AI AGENT\6.C#\Autocad 2026_API\MyFirstProject"
dotnet build
```

**QUAN TRỌNG**: Sau khi tạo lệnh xong, PHẢI build lại để kiểm tra còn lỗi gì không!

## Bước 5: Sửa lỗi (nếu có)
- Xem chi tiết lỗi từ output build.
- Sửa các lỗi xung đột namespace, syntax, v.v.
- Build lại cho đến khi thành công (`0 Error(s)`).

