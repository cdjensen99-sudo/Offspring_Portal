param(
    [string]$ValheimPath = "D:\SteamLibrary\steamapps\common\Valheim",
    [string]$GaleProfilePath = "C:\Users\cdjen\AppData\Roaming\com.kesomannen.gale\valheim\profiles\New Release",
    [string]$BepInExPath = "",
    [string]$JotunnPath = "",
    [switch]$Deploy
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\OffspringPortal\OffspringPortal.csproj"

if ([string]::IsNullOrWhiteSpace($BepInExPath)) {
    $BepInExPath = Join-Path $GaleProfilePath "BepInEx"
}

if ([string]::IsNullOrWhiteSpace($JotunnPath)) {
    $JotunnPath = Join-Path $BepInExPath "plugins\ValheimModding-Jotunn\Jotunn.dll"
}

if (-not (Test-Path (Join-Path $ValheimPath "valheim_Data\Managed\assembly_valheim.dll"))) {
    Write-Error "Valheim assemblies not found at $ValheimPath\valheim_Data\Managed"
}

if (-not (Test-Path (Join-Path $BepInExPath "core\BepInEx.dll"))) {
    Write-Error "BepInEx core not found at $BepInExPath\core. Point -GaleProfilePath or -BepInExPath at your active profile."
}

if (-not (Test-Path $JotunnPath)) {
    Write-Error "Jotunn.dll not found at $JotunnPath"
}

Write-Host "ValheimPath:   $ValheimPath"
Write-Host "BepInExPath:   $BepInExPath"
Write-Host "JotunnPath:    $JotunnPath"

dotnet build $project `
    -p:ValheimPath=$ValheimPath `
    -p:BepInExPath=$BepInExPath `
    -p:JotunnPath=$JotunnPath `
    -p:GaleProfilePath=$GaleProfilePath `
    -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Built: $(Join-Path $root 'artifacts\OffspringPortal.dll')"

if (-not $Deploy) {
    Write-Host "Skipped deploy. Pass -Deploy to copy into the Gale profile."
    exit 0
}

$dll = Join-Path $root "artifacts\OffspringPortal.dll"
$pluginDir = Join-Path $GaleProfilePath "BepInEx\plugins\Hardwire99-Offspring_Portal"
$legacyDir = Join-Path $GaleProfilePath "BepInEx\plugins\OffspringPortal-OffspringPortal"
$devDeployDir = Join-Path $GaleProfilePath "BepInEx\plugins\HW-Offspring_Portal"
$duplicateDir = Join-Path $GaleProfilePath "BepInEx\plugins\Unknown-OffspringPortal.dll"
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
