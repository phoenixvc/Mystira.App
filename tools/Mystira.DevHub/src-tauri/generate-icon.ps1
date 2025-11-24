# Generate a simple icon for Tauri
# This creates a basic icon.ico file using .NET

$iconPath = "$PSScriptRoot\icons"
if (-not (Test-Path $iconPath)) {
    New-Item -ItemType Directory -Path $iconPath -Force | Out-Null
}

# Create a simple 256x256 PNG icon using .NET
Add-Type -AssemblyName System.Drawing

$size = 256
$bitmap = New-Object System.Drawing.Bitmap($size, $size)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Set high quality
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Draw background (dark blue/purple gradient)
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    [System.Drawing.Point]::new(0, 0),
    [System.Drawing.Point]::new($size, $size),
    [System.Drawing.Color]::FromArgb(79, 70, 229),  # Indigo
    [System.Drawing.Color]::FromArgb(139, 92, 246)  # Purple
)
$graphics.FillEllipse($brush, 0, 0, $size, $size)
$brush.Dispose()

# Draw "M" letter in white
$font = New-Object System.Drawing.Font("Arial", 180, [System.Drawing.FontStyle]::Bold)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center

$graphics.DrawString("M", $font, $textBrush, $size/2, $size/2, $format)

# Clean up
$font.Dispose()
$textBrush.Dispose()
$format.Dispose()
$graphics.Dispose()

# Save as PNG first
$pngPath = "$iconPath\app-icon.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()

Write-Host "Created app-icon.png" -ForegroundColor Green

# Generate all Tauri icons from the PNG
Write-Host "Generating Tauri icons..." -ForegroundColor Yellow
$parentDir = Split-Path -Parent $PSScriptRoot
Set-Location $parentDir
npx @tauri-apps/cli icon "$pngPath" 2>&1 | Out-String

Write-Host "Icons generated successfully!" -ForegroundColor Green

