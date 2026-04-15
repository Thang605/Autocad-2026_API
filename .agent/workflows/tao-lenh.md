---
description: Quy trình tạo lệnh mới cho AutoCAD/Civil3D plugin
---

# Quy trình tạo lệnh mới

## Bước 1: Tạo file lệnh
- Tạo file `.cs` mới trong thư mục `Acad Tool` hoặc `Civil Tool`
- Đặt tên file theo format: `XX.AT_TenLenh.cs` hoặc `XX.CT_TenLenh.cs`
- Sử dụng template từ các file có sẵn

## Bước 2: Kiểm tra xung đột namespace
Các xung đột thường gặp cần alias:
```csharp
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using DrawingFont = System.Drawing.Font;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Label = Autodesk.Civil.DatabaseServices.Label;
```

## Bước 3: Build để kiểm tra lỗi
// turbo
```powershell
cd "c:\Onedrive\0.AI AGENT\6.C#\Autocad 2026_API\MyFirstProject"
dotnet build
```

**QUAN TRỌNG (NHIỆM VỤ BẮT BUỘC CHO AI AGENT)**: 
Ngay sau khi AI Agent tự động viết code xong một lệnh mới (hoặc có chỉnh sửa bất kỳ nội dung C# nào), BẠN BẮT BUỘC PHẢI tự rigger công cụ `run_command` để chạy lệnh `dotnet build` bên trong thư mục `MyFirstProject` nhằm kiểm tra lỗi biên dịch! Nếu có lỗi (như Ambiguous reference `Section`), bạn phải tự sửa và build lại cho đến khi `0 errors`. Không đợi user yêu cầu.

## Bước 4: Sửa lỗi (nếu có)
- Xem chi tiết lỗi từ output build
- Sửa các lỗi xung đột namespace, syntax, v.v.
- Build lại cho đến khi thành công

## ⛔ KHÔNG cập nhật ToolPalette.xlsx
- **KHÔNG** tự động thêm lệnh mới vào file `ToolPalette.xlsx`.
- User sẽ tự thêm lệnh vào file Excel khi cần.
