---
description: Publish (phathanh) the Civil3D_Tools.dll to the Y: drive
---

1. Build the project to ensure the DLL is up to date.
   ```powershell
   dotnet build "c:\Dropbox\DATA\AI Agent\Autocad 2026_API\MyFirstProject\MyFirstProject.csproj"
   ```

2. Copy the DLL to the release location.
   // turbo
   ```powershell
   copy "c:\Dropbox\DATA\AI Agent\Autocad 2026_API\MyFirstProject\bin\Debug\Civil3D_Tools.dll" "Y:\5.SOFT T27\1. FOR WORK\1. THIET KE DUONG\2.CIVIL 3D\2026\AutoCAD Civil 3D 2026 Win x64\x64\c3d\Civil3D2026.dll"
   ```

3. Notify the user that the file has been published.
