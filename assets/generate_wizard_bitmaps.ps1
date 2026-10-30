Add-Type -AssemblyName System.Drawing

$assetsDir = "j:\VC Directory Project\multi-headphone-app\assets"
$srcImgPath = Join-Path $assetsDir "user_logo.png"

$src = [System.Drawing.Image]::FromFile($srcImgPath)
Write-Host "Source Image loaded: $($src.Width)x$($src.Height)" -ForegroundColor Cyan

# 1. WizardImageFile: 164 x 314 px (24-bit BMP)
$wLarge = 164
$hLarge = 314
$bmpLarge = New-Object System.Drawing.Bitmap $wLarge, $hLarge, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$gLarge = [System.Drawing.Graphics]::FromImage($bmpLarge)
$gLarge.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gLarge.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gLarge.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

# Dark background
$bgColor = [System.Drawing.Color]::FromArgb(18, 22, 30)
$gLarge.Clear($bgColor)

# Scale source image to fit width (150px) while maintaining aspect ratio
$drawWidth = 154
$drawHeight = [int]($src.Height * ($drawWidth / $src.Width))
$drawX = [int](($wLarge - $drawWidth) / 2)
$drawY = [int](($hLarge - $drawHeight) / 2)

$gLarge.DrawImage($src, $drawX, $drawY, $drawWidth, $drawHeight)
$gLarge.Dispose()

$largeBmpPath = Join-Path $assetsDir "wizard_large.bmp"
$bmpLarge.Save($largeBmpPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
$bmpLarge.Dispose()
Write-Host "Created WizardImage: $largeBmpPath" -ForegroundColor Green

# 2. WizardSmallImageFile: 55 x 58 px (24-bit BMP)
$wSmall = 55
$hSmall = 58
$bmpSmall = New-Object System.Drawing.Bitmap $wSmall, $hSmall, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$gSmall = [System.Drawing.Graphics]::FromImage($bmpSmall)
$gSmall.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gSmall.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gSmall.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

$gSmall.Clear($bgColor)

# Scale source image to fit 50x50 centered
$drawWSmall = 50
$drawHSmall = [int]($src.Height * ($drawWSmall / $src.Width))
$drawXSmall = [int](($wSmall - $drawWSmall) / 2)
$drawYSmall = [int](($hSmall - $drawHSmall) / 2)

$gSmall.DrawImage($src, $drawXSmall, $drawYSmall, $drawWSmall, $drawHSmall)
$gSmall.Dispose()

$smallBmpPath = Join-Path $assetsDir "wizard_small.bmp"
$bmpSmall.Save($smallBmpPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
$bmpSmall.Dispose()
Write-Host "Created WizardSmallImage: $smallBmpPath" -ForegroundColor Green

# 3. Create Multi-Resolution ICO from user_logo.png
$icoPath = Join-Path $assetsDir "app_icon.ico"
$iconSizes = @(16, 32, 48, 64, 128, 256)
$iconBitmaps = @()

foreach ($sz in $iconSizes) {
    $b = New-Object System.Drawing.Bitmap $sz, $sz, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($b)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.DrawImage($src, 0, 0, $sz, $sz)
    $g.Dispose()
    $iconBitmaps += $b
}

# Write standard ICO file
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter $fs

# ICONDIR
$bw.Write([uint16]0) # Reserved
$bw.Write([uint16]1) # Type 1 = ICO
$bw.Write([uint16]$iconBitmaps.Count) # Count

# Save PNG streams for each size
$pngStreams = @()
foreach ($b in $iconBitmaps) {
    $ms = New-Object System.IO.MemoryStream
    $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngStreams += $ms
}

# Calculate offsets
$headerSize = 6 + ($iconBitmaps.Count * 16)
$currentOffset = $headerSize

for ($i = 0; $i -lt $iconBitmaps.Count; $i++) {
    $sz = $iconSizes[$i]
    $dimByte = if ($sz -eq 256) { [byte]0 } else { [byte]$sz }
    $bw.Write($dimByte) # Width
    $bw.Write($dimByte) # Height
    $bw.Write([byte]0) # Colors
    $bw.Write([byte]0) # Reserved
    $bw.Write([uint16]1) # Planes
    $bw.Write([uint16]32) # Bits
    $bw.Write([uint32]$pngStreams[$i].Length) # Size
    $bw.Write([uint32]$currentOffset) # Offset
    $currentOffset += $pngStreams[$i].Length
}

for ($i = 0; $i -lt $pngStreams.Count; $i++) {
    $bytes = $pngStreams[$i].ToArray()
    $bw.Write($bytes)
    $pngStreams[$i].Dispose()
    $iconBitmaps[$i].Dispose()
}

$bw.Close()
$fs.Close()
Write-Host "Created Multi-Res ICO: $icoPath" -ForegroundColor Green

$src.Dispose()
