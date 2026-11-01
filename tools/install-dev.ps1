# Builds the MSIX and registers its unpacked layout for local testing. Needs Developer Mode (no signing).
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$out = Join-Path $root 'AppPackages'
$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter makeappx.exe | ? { $_.DirectoryName -like '*\x64' } | Sort FullName | Select -Last 1 -Expand FullName
$env:PATH += ";${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer"

Get-Process SonarFlyout -ErrorAction SilentlyContinue | Stop-Process
dotnet publish $root -c Release -p:GenerateAppxPackageOnBuild=true -p:AppxPackageDir="$out\" | Out-Null
if ($LASTEXITCODE) { throw 'Build failed' }

$package = Get-ChildItem $out -Recurse -Filter '*_x64.msix' | Sort LastWriteTime | Select -Last 1
Remove-Item "$out\layout" -Recurse -Force -ErrorAction SilentlyContinue
& $makeappx unpack /p $package.FullName /d "$out\layout" /o | Out-Null

try {
  Add-AppxPackage -Register "$out\layout\AppxManifest.xml" -ForceApplicationShutdown
} catch {
  # Development registration refuses manifest changes over an existing install of the same version.
  Get-AppxPackage SonarFlyout | Remove-AppxPackage
  Add-AppxPackage -Register "$out\layout\AppxManifest.xml" -ForceApplicationShutdown
}
Start-Process "shell:AppsFolder\$((Get-AppxPackage SonarFlyout).PackageFamilyName)!App"
