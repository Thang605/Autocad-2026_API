$vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    Write-Host "vswhere found!"
    & $vswhere -latest -requires Microsoft.Component.MSBuild -property installationPath
} else {
    Write-Host "vswhere not found at standard path."
}

# Search all drives / folders for MSBuild.exe
$drives = Get-PSDrive -PSProvider FileSystem
foreach ($d in $drives) {
    Write-Host "Searching drive $($d.Name)..."
    Get-ChildItem -Path "$($d.Name):\" -Filter "MSBuild.exe" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "FOUND: $($_.FullName)"
    }
}
