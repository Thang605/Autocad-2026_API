
$projectDir = "c:\Dropbox\DATA\AI Agent\Autocad 2026_API"
$menuFile = "$projectDir\MyFirstProject\Menu form\ClassicMenu.cs"

# 1. Extract commands from ClassicMenu.cs
$menuContent = Get-Content $menuFile
$menuCommands = @()
foreach ($line in $menuContent) {
    if ($line -match 'AddMenuItem\s*\(.*,\s*"(.*)\s*"\s*\)') {
        $cmd = $matches[1].Trim()
        if ($cmd -ne "") { $menuCommands += $cmd }
    }
}
$menuCommands = $menuCommands | Select-Object -Unique

# 2. Extract commands from all .cs files
$allCommands = @()
$csFiles = Get-ChildItem -Path $projectDir -Filter "*.cs" -Recurse
foreach ($file in $csFiles) {
    # Skip ClassicMenu.cs to avoid circular reference (it defines SHOW_MENU)
    if ($file.FullName -like "*ClassicMenu.cs") {
        # Manually add SHOW_MENU as it's the only one there
        $allCommands += "SHOW_MENU"
        continue
    }
    
    $content = Get-Content $file.FullName
    foreach ($line in $content) {
        if ($line -match '\[CommandMethod\s*\(\s*"(.*)"\s*.*\)\]') {
            $cmd = $matches[1].Trim()
            $allCommands += $cmd
        }
    }
}
$allCommands = $allCommands | Select-Object -Unique

# 3. Compare: In Code but NOT in Menu
$missingInMenu = $allCommands | Where-Object { $_ -notin $menuCommands }

Write-Host "--- THONG KE THUC TE ---"
Write-Host "Tong so lenh CommandMethod tim thay: $($allCommands.Count)"
Write-Host "Tong so lenh trong menu: $($menuCommands.Count)"
Write-Host "So lenh thieu trong menu: $($missingInMenu.Count)"
Write-Host ""
Write-Host "--- CAC LENH THIEU TRONG MENU ---"
foreach ($cmd in $missingInMenu | Sort-Object) {
    Write-Host "- $cmd"
}
