---
description: Phát hành Civil3D_Tools - Build Release và copy đến thư mục phát hành
---
# Quy trình phát hành Civil3D_Tools

// turbo-all

## Các bước thực hiện

1. Build project ở chế độ Release:
```powershell
dotnet build "c:\Dropbox\DATA\AI Agent\Autocad 2026_API\MyFirstProject\MyFirstProject.csproj" --configuration Release
```

2. Copy file DLL đến thư mục phát hành:
```powershell
Copy-Item "c:\Dropbox\DATA\AI Agent\Autocad 2026_API\MyFirstProject\bin\Release\Civil3D_Tools.dll" -Destination "Y:\5.SOFT T27\1. FOR WORK\1. THIET KE DUONG\2.CIVIL 3D\2026\AutoCAD Civil 3D 2026 Win x64\x64\c3d\Civil3D2026.dll" -Force
```

## Lưu ý
- File DLL nguồn: `bin\Release\Civil3D_Tools.dll`
- File đích: `Y:\5.SOFT T27\1. FOR WORK\1. THIET KE DUONG\2.CIVIL 3D\2026\AutoCAD Civil 3D 2026 Win x64\x64\c3d\Civil3D2026.dll`
- Đảm bảo ổ Y: đã được kết nối trước khi copy
