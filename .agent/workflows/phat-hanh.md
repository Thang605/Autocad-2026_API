---
description: Phát hành Civil3D_Tools - Build Release và copy đến thư mục phát hành
---

# Workflow: Phát hành (Release)

Khi người dùng nói "phát hành", thực hiện các bước sau:

// turbo-all

1. Build project ở chế độ Release:
```
dotnet build -c Release
```
Working directory: `c:\Dropbox\DATA\AI Agent\Autocad 2026_API\MyFirstProject`

2. Copy file DLL đến đường dẫn phát hành:
```
Copy-Item -Path "c:\Dropbox\DATA\AI Agent\Autocad 2026_API\MyFirstProject\bin\Release\Civil3D_Tools.dll" -Destination "Y:\5.SOFT T27\1. FOR WORK\1. THIET KE DUONG\2.CIVIL 3D\2026\AutoCAD Civil 3D 2026 Win x64\x64\c3d\Civil3D2026.dll" -Force
```

3. Thông báo cho người dùng biết file đã được phát hành tại:
`Y:\5.SOFT T27\1. FOR WORK\1. THIET KE DUONG\2.CIVIL 3D\2026\AutoCAD Civil 3D 2026 Win x64\x64\c3d\Civil3D2026.dll`
