# =========================================================================
# Generate Code Signing Certificate & PFX for K Vanlalrengpuia
# Developer: K Vanlalrengpuia (rengakhiangte@gmail.com)
# =========================================================================

param (
    [string]$Password = "RPAAudio2026!"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Add-Type -AssemblyName System.Security

Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host " Generating Code Signing Certificate for K Vanlalrengpuia" -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan

# Generate RSA 2048 key
$rsa = [System.Security.Cryptography.RSA]::Create(2048)

# Subject with Developer name and contact
$dn = [System.Security.Cryptography.X509Certificates.X500DistinguishedName]::new(
    "CN=K Vanlalrengpuia, O=RPA Audio Technologies, E=rengakhiangte@gmail.com"
)

$req = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
    $dn,
    $rsa,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    [System.Security.Cryptography.RSASignaturePadding]::Pkcs1
)

# 1. Enhanced Key Usage: Code Signing (OID 1.3.6.1.5.5.7.3.3)
$oid = [System.Security.Cryptography.Oid]::new("1.3.6.1.5.5.7.3.3", "Code Signing")
$oidCollection = [System.Security.Cryptography.OidCollection]::new()
[void]$oidCollection.Add($oid)
$eku = [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($oidCollection, $true)
[void]$req.CertificateExtensions.Add($eku)

# 2. Key Usage: Digital Signature
$ku = [System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
    [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature,
    $true
)
[void]$req.CertificateExtensions.Add($ku)

# 3. Subject Alternative Name
$sanBuilder = [System.Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
$sanBuilder.AddEmailAddress("rengakhiangte@gmail.com")
[void]$req.CertificateExtensions.Add($sanBuilder.Build())

# 10-year validity
$now = [DateTimeOffset]::UtcNow
$notAfter = $now.AddYears(10)
$cert = $req.CreateSelfSigned($now, $notAfter)

# Export Public Certificate (.cer)
$cerPath = Join-Path $ScriptDir "K_Vanlalrengpuia_CodeSigning.cer"
$cerBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
[System.IO.File]::WriteAllBytes($cerPath, $cerBytes)
Write-Host "[+] Public Certificate: $cerPath" -ForegroundColor Green

# Export PFX with Private Key (.pfx)
$pfxPath = Join-Path $ScriptDir "K_Vanlalrengpuia_CodeSigning.pfx"
$pfxBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $Password)
[System.IO.File]::WriteAllBytes($pfxPath, $pfxBytes)
Write-Host "[+] Signing Key PFX:    $pfxPath" -ForegroundColor Green
Write-Host "[+] PFX Password:       $Password" -ForegroundColor Yellow

Write-Host "`nCertificate Details:" -ForegroundColor White
Write-Host "  Subject:    $($cert.Subject)"
Write-Host "  Thumbprint: $($cert.Thumbprint)"
Write-Host "  Expires:    $($cert.NotAfter.ToString('yyyy-MM-dd'))"
Write-Host "  Key Specs:  RSA 2048 / SHA256 / Code Signing"
