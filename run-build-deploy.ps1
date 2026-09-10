$log = Join-Path $PSScriptRoot "build-output.txt"
try {
    & (Join-Path $PSScriptRoot "build.ps1") -Deploy *>&1 | Out-File -FilePath $log -Encoding utf8
    "EXIT=$LASTEXITCODE" | Out-File -Append -FilePath $log -Encoding utf8
    $paths = @(
        (Join-Path $PSScriptRoot "artifacts\OffspringPortal.dll"),
        "C:\Users\cdjen\AppData\Roaming\com.kesomannen.gale\valheim\profiles\New Release\BepInEx\plugins\Hardwire99-Offspring_Portal\OffspringPortal.dll"
    )
    foreach ($p in $paths) {
        if (Test-Path $p) {
            $i = Get-Item $p
            "DLL: $p" | Out-File -Append -FilePath $log -Encoding utf8
            "  LastWriteTime: $($i.LastWriteTime)" | Out-File -Append -FilePath $log -Encoding utf8
            "  Size: $($i.Length) bytes" | Out-File -Append -FilePath $log -Encoding utf8
        }
        else {
            "MISSING: $p" | Out-File -Append -FilePath $log -Encoding utf8
        }
    }
    exit $LASTEXITCODE
}
catch {
    $_ | Out-File -Append -FilePath $log -Encoding utf8
    exit 1
}
