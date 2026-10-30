param(
    [string]$FilePath,
    [string]$Target,
    [string]$Password = "RPAAudio2026!"
)

if (-not $FilePath -and $Target) { $FilePath = $Target }
if (-not $FilePath) {
    Write-Host "Usage: sign_binaries.ps1 -FilePath <path_to_exe_or_dll>" -ForegroundColor Yellow
    exit 1
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PfxPath = Join-Path $ScriptDir "K_Vanlalrengpuia_CodeSigning.pfx"

if (-not (Test-Path $PfxPath)) {
    Write-Error "PFX certificate not found at $PfxPath"
    exit 1
}

if (-not (Test-Path $FilePath)) {
    Write-Error "Target file not found at $FilePath"
    exit 1
}

$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($PfxPath, $Password, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)

Write-Host "Signing file: $FilePath" -ForegroundColor Cyan
Write-Host "Publisher:    $($cert.Subject)" -ForegroundColor White

try {
    # Attempt timestamping via DigiCert; if offline or sandboxed fallback without timestamp
    $sig = Set-AuthenticodeSignature -FilePath $FilePath -Certificate $cert -HashAlgorithm SHA256 -TimestampServer "http://timestamp.digicert.com" -ErrorAction Stop
}
catch {
    Write-Host "Note: Timestamp server unreachable, signing with local certificate timestamp..." -ForegroundColor Yellow
    $sig = Set-AuthenticodeSignature -FilePath $FilePath -Certificate $cert -HashAlgorithm SHA256
}

$color = "Green"
if ($sig.Status -ne "Valid") { $color = "Yellow" }
Write-Host "Signature Status: $($sig.Status)" -ForegroundColor $color
Write-Host "Status Message:   $($sig.StatusMessage)" -ForegroundColor White

# SIG # Begin signature block
# MIIGagYJKoZIhvcNAQcCoIIGWzCCBlcCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCDuat/Y0jKWxNPN
# Lt7sFTgO6mh5dIIdV/fTd1pNl5RxIKCCA50wggOZMIICgaADAgECAgkA2TGo7KM/
# /qIwDQYJKoZIhvcNAQELBQAwZDEmMCQGCSqGSIb3DQEJARYXcmVuZ2FraGlhbmd0
# ZUBnbWFpbC5jb20xHzAdBgNVBAoTFlJQQSBBdWRpbyBUZWNobm9sb2dpZXMxGTAX
# BgNVBAMTEEsgVmFubGFscmVuZ3B1aWEwHhcNMjYxMDMwMDMyODQyWhcNMzYxMDMw
# MDMyODQyWjBkMSYwJAYJKoZIhvcNAQkBFhdyZW5nYWtoaWFuZ3RlQGdtYWlsLmNv
# bTEfMB0GA1UEChMWUlBBIEF1ZGlvIFRlY2hub2xvZ2llczEZMBcGA1UEAxMQSyBW
# YW5sYWxyZW5ncHVpYTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAKye
# 2Kr0emZBqLXOWipqW3uZwkKdcm0hqdwyso2x+8TvhBk+qyYjRrbcQW16g9JeZPnG
# jxs3S761zgOvl2oKGcEYiAJ/oubQH7fRSs64hSoPREjjTjFrkdKC/+qMS+woFMb8
# 2znvFPvCz6Z6D0VnTNY4LMvqk4UsvPL2SM0mnLcYwJ0OJMmbP9D3xLi9KghTk2cd
# rrkl9yKvMrSyYxTVo6oO/X/neb7fg0Zp5/t83JKGrFs+mVrowYdG/qetMsU/zDhM
# HEAfBjGqvl6ZLiCJ6noWZxvrNgcs1l751aWLEflJbZN3SrMB9Cie2bq5fiIp2APY
# NlN/j443tyi38dSBoxECAwEAAaNOMEwwFgYDVR0lAQH/BAwwCgYIKwYBBQUHAwMw
# DgYDVR0PAQH/BAQDAgeAMCIGA1UdEQQbMBmBF3Jlbmdha2hpYW5ndGVAZ21haWwu
# Y29tMA0GCSqGSIb3DQEBCwUAA4IBAQAOie2wBuM3ZIs1Bpy2zyyr3VNXhDGieV6s
# sWYn8bF6Wbp1Q4Q7zT+7h7QSfxB9c8X1b56HNMxW0cwk/nQlPksSpuPlSFZ3S4Tk
# +UC32JJ0AXHq3EXXL0iATmeR1pVPZGU+AcMjhugK40X4U2snWesi5JO+hkqgW53r
# /3UQn+/ohjZV2ND1DFxucwbARhsiH+JTGvl+1e9ItxjjsE+kSyD8W8Zf7RttSz7d
# AG6ddK47pBcOWSvA6OmhBMb2CWXyzfn7OuU0ix+4nkLNK5pC1xLeyqQIbmeQfcj1
# YPIVPTvijihOIzXstXO4Y+8iHjW6vYbohx5/orxizUiGjx/hIDBHMYICIzCCAh8C
# AQEwcTBkMSYwJAYJKoZIhvcNAQkBFhdyZW5nYWtoaWFuZ3RlQGdtYWlsLmNvbTEf
# MB0GA1UEChMWUlBBIEF1ZGlvIFRlY2hub2xvZ2llczEZMBcGA1UEAxMQSyBWYW5s
# YWxyZW5ncHVpYQIJANkxqOyjP/6iMA0GCWCGSAFlAwQCAQUAoIGEMBgGCisGAQQB
# gjcCAQwxCjAIoAKAAKECgAAwGQYJKoZIhvcNAQkDMQwGCisGAQQBgjcCAQQwHAYK
# KwYBBAGCNwIBCzEOMAwGCisGAQQBgjcCARUwLwYJKoZIhvcNAQkEMSIEIGsugBeE
# cAIqG+Ny+yRkkOHKhDDlRCDEIjQb7GWKvBuGMA0GCSqGSIb3DQEBAQUABIIBAEg0
# S+OC3KIBeE6jw8a2qDHSTILRflYbW4xMDG0Bff89o7X+Jtr54k6557/rceD0iWXF
# ZKGdzVjaNVhulA6Oue3JuGDvcbdCcftA4uEZBdSQjwRIqsSkxtZ0e9kS2px0vRgV
# qeAYDAhaWEtnTDbsHwjago+5waNAjc50oAVfcdLJafOmt4qHcCr/5OW6LYF6REce
# zE+1okK49WknFO//XIFOk2/pvu36Mliv8qd5ulk9PdQr9bxN2kYuoYmqXddGPU+I
# EKo4fD2aKsdJzkgK555qCbaZo0m3jEKZi8Tkn9L3quQlLINT2BvZAz46OPtMT9GI
# l1W7HUDvs/tKiB72eOs=
# SIG # End signature block
