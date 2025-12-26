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
cd c:\Dropbox\DATA\AI Agent\Autocad 2026_API\MyFirstProject
dotnet build
```

**QUAN TRỌNG**: Sau khi tạo lệnh xong, PHẢI build lại để kiểm tra còn lỗi gì không!

## Bước 4: Sửa lỗi (nếu có)
- Xem chi tiết lỗi từ output build
- Sửa các lỗi xung đột namespace, syntax, v.v.
- Build lại cho đến khi thành công
