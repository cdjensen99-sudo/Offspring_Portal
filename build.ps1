param(
    [string]$ValheimPath = "D:\SteamLibrary\steamapps\common\Valheim",
    [string]$DeployProfile = "C:\Users\cdjen\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Default",
    [switch]$Deploy
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\OffspringPortal\OffspringPortal.csproj"

dotnet build $project -p:ValheimPath=$ValheimPath -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Built: $(Join-Path $root 'artifacts\OffspringPortal.dll')"

if (-not $Deploy) {
    Write-Host "Skipped deploy. Pass -Deploy to copy into the r2modman profile."
    exit 0
}

$dll = Join-Path $root "artifacts\OffspringPortal.dll"
$pluginDir = Join-Path $DeployProfile "BepInEx\plugins\Hardwire99-Offspring_Portal"
$legacyDir = Join-Path $DeployProfile "BepInEx\plugins\OffspringPortal-OffspringPortal"
$devDeployDir = Join-Path $DeployProfile "BepInEx\plugins\HW-Offspring_Portal"
$duplicateDir = Join-Path $DeployProfile "BepInEx\plugins\Unknown-OffspringPortal.dll"
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
$dest = Join-Path $pluginDir "OffspringPortal.dll"
try {
    Copy-Item $dll $dest -Force
    Copy-Item (Join-Path $root "thunderstore\manifest.json") (Join-Path $pluginDir "manifest.json") -Force
    Copy-Item (Join-Path $root "thunderstore\CHANGELOG.md") (Join-Path $pluginDir "CHANGELOG.md") -Force
    Write-Host "Deployed to $dest"
}
catch {
    $pending = Join-Path $pluginDir "OffspringPortal.dll.pending"
    Copy-Item $dll $pending -Force
    Write-Warning "Valheim has the plugin locked. Close the game, then replace OffspringPortal.dll with OffspringPortal.dll.pending"
    Write-Host "Built update saved to $pending"
}
if (Test-Path $duplicateDir) {
    Remove-Item $duplicateDir -Recurse -Force
    Write-Host "Removed duplicate plugin folder: $duplicateDir"
}
if (Test-Path $legacyDir) {
    Remove-Item $legacyDir -Recurse -Force
    Write-Host "Removed legacy plugin folder: $legacyDir"
}
if (Test-Path $devDeployDir) {
    Remove-Item $devDeployDir -Recurse -Force
    Write-Host "Removed dev-only deploy folder: $devDeployDir"
}
