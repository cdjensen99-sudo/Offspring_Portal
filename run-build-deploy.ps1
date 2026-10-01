$log = Join-Path $PSScriptRoot "build-deploy-log.txt"
$exitFile = Join-Path $PSScriptRoot "build-deploy-exit.txt"
try {
    & (Join-Path $PSScriptRoot "build.ps1") -Deploy *>&1 | Out-File -FilePath $log -Encoding utf8
    $code = $LASTEXITCODE
} catch {
    $_ | Out-File -FilePath $log -Append -Encoding utf8
    $code = 1
}
Set-Content -Path $exitFile -Value $code

$deployDir = "C:\Users\cdjen\AppData\Roaming\com.kesomannen.gale\valheim\profiles\New Release\BepInEx\plugins\Hardwire99-Offspring_Portal"
$dll = Join-Path $deployDir "OffspringPortal.dll"
$pending = Join-Path $deployDir "OffspringPortal.dll.pending"
$verify = Join-Path $PSScriptRoot "build-deploy-verify.txt"
$lines = @()
if (Test-Path $dll) {
    $item = Get-Item $dll
    $lines += "DLL_EXISTS: true"
    $lines += "DLL_PATH: $($item.FullName)"
    $lines += "DLL_SIZE: $($item.Length)"
    $lines += "DLL_LASTWRITE: $($item.LastWriteTime.ToString('o'))"
} else {
    $lines += "DLL_EXISTS: false"
}
$lines += "PENDING_EXISTS: $(Test-Path $pending)"
Set-Content -Path $verify -Value $lines
