---
description: Build MyFirstProject - Tạo file DLL cho AutoCAD/Civil3D
---

# Workflow: Build Project (_b)

Khi người dùng nhập `/ _b` hoặc yêu cầu "build", thực hiện lệnh sau:

// turbo
1. Build project:
```powershell
dotnet build
```
Cwd: `c:\Dropbox\DATA\AI Agent\Autocad 2026_API\MyFirstProject`

2. Thông báo kết quả build và vị trí file DLL:
Debug: `c:\Dropbox\DATA\AI Agent\Autocad 2026_API\MyFirstProject\bin\Debug\Civil3D_Tools.dll`
Release: `c:\Dropbox\DATA\AI Agent\Autocad 2026_API\MyFirstProject\bin\Release\Civil3D_Tools.dll`
