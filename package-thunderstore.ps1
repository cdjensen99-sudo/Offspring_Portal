param(
    [string]$ValheimPath = "D:\SteamLibrary\steamapps\common\Valheim"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

& (Join-Path $root "build.ps1") -ValheimPath $ValheimPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$staging = Join-Path $root "artifacts\thunderstore-staging"
if (Test-Path $staging) {
    Remove-Item $staging -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $staging | Out-Null

Copy-Item (Join-Path $root "thunderstore\manifest.json") $staging
Copy-Item (Join-Path $root "thunderstore\README.md") $staging
Copy-Item (Join-Path $root "thunderstore\CHANGELOG.md") $staging
Copy-Item (Join-Path $root "artifacts\OffspringPortal.dll") $staging

$iconSource = Join-Path $root "thunderstore\icon.png"
if (Test-Path $iconSource) {
    Copy-Item $iconSource $staging
}
else {
    Write-Warning "thunderstore\icon.png is missing. Add a 256x256 PNG before uploading to Thunderstore."
}

$manifest = Get-Content (Join-Path $staging "manifest.json") -Raw | ConvertFrom-Json
$zipName = "HW-Offspring_Portal-$($manifest.version_number).zip"
$zipPath = Join-Path $root "artifacts\$zipName"

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

$files = Get-ChildItem $staging -File | ForEach-Object { $_.FullName }
Compress-Archive -Path $files -DestinationPath $zipPath -Force

Write-Host "Thunderstore package: $zipPath"
Write-Host "Upload as team HW. Thunderstore will show the name as 'Offspring Portal'."
