# Regenerates Assets\: tray icons and MSIX logos. Run with Windows PowerShell 5.1 (System.Drawing).
param($outDir = (Join-Path $PSScriptRoot '..\Assets'))
Add-Type -AssemblyName System.Drawing

function Save-Png($bitmap) {
  $ms = New-Object System.IO.MemoryStream
  $bitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
  ,$ms.ToArray()
}

function Write-Ico($path, $pngs, $sizes) {
  $out = New-Object System.IO.MemoryStream; $w = New-Object System.IO.BinaryWriter $out
  $w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
  $offset = 6 + 16 * $sizes.Count
  for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i] % 256
    $w.Write([byte]$s); $w.Write([byte]$s); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]$pngs[$i].Length); $w.Write([uint32]$offset)
    $offset += $pngs[$i].Length
  }
  foreach ($p in $pngs) { $w.Write($p) }
  [System.IO.File]::WriteAllBytes($path, $out.ToArray())
}

# Tray icons match the Windows 11 volume icon: the Segoe Fluent glyph drawn at exactly the icon size from the pixel
# origin (contrast 0 matched the native icon best), with waves the level hasn't reached at ~22%.
function Draw-Tray($size, [System.Drawing.Color]$color, $glyph, [bool]$dimWaves) {
  $bmp = New-Object System.Drawing.Bitmap $size, $size
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.TextRenderingHint = 'AntiAlias'; $g.TextContrast = 0
  $font = New-Object System.Drawing.Font 'Segoe Fluent Icons', $size, ([System.Drawing.GraphicsUnit]::Pixel)
  $fmt = [System.Drawing.StringFormat]::GenericTypographic
  if ($dimWaves) { $g.DrawString([string][char]0xE995, $font, (New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(56, $color))), 0, 0, $fmt) }
  $g.DrawString([string][char]$glyph, $font, (New-Object System.Drawing.SolidBrush $color), 0, 0, $fmt)
  $bmp
}

$traySizes = 16, 20, 24, 32, 40, 48, 64, 256
$states = @{ '0' = 0xE992; '1' = 0xE993; '2' = 0xE994; '3' = 0xE995; 'Muted' = 0xE74F }
foreach ($state in $states.Keys) {
  foreach ($theme in @(@{ Name = 'Dark'; Color = [System.Drawing.Color]::White }, @{ Name = 'Light'; Color = [System.Drawing.Color]::FromArgb(255, 28, 28, 28) })) {
    $pngs = foreach ($s in $traySizes) { Save-Png (Draw-Tray $s $theme.Color $states[$state] ($state -ne 'Muted')) }
    Write-Ico (Join-Path $outDir "Tray$($theme.Name)$state.ico") $pngs $traySizes
  }
}

function Draw-Logo($width, $height) {
  $bmp = New-Object System.Drawing.Bitmap $width, $height
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAlias'
  $side = [Math]::Min($width, $height); $x = ($width - $side) / 2; $y = ($height - $side) / 2
  $r = $side * 0.22
  $path = New-Object System.Drawing.Drawing2D.GraphicsPath
  $path.AddArc($x, $y, 2 * $r, 2 * $r, 180, 90); $path.AddArc($x + $side - 2 * $r, $y, 2 * $r, 2 * $r, 270, 90)
  $path.AddArc($x + $side - 2 * $r, $y + $side - 2 * $r, 2 * $r, 2 * $r, 0, 90); $path.AddArc($x, $y + $side - 2 * $r, 2 * $r, 2 * $r, 90, 90)
  $path.CloseFigure()
  $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush ((New-Object System.Drawing.PointF $x, $y), (New-Object System.Drawing.PointF ($x + $side), ($y + $side)), ([System.Drawing.Color]::FromArgb(255, 64, 140, 235)), ([System.Drawing.Color]::FromArgb(255, 22, 82, 180)))
  $g.FillPath($brush, $path)
  $glyphSize = $side * 0.58
  $font = New-Object System.Drawing.Font 'Segoe Fluent Icons', $glyphSize, ([System.Drawing.GraphicsUnit]::Pixel)
  $fmt = [System.Drawing.StringFormat]::GenericTypographic; $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'
  $g.DrawString([string][char]0xE995, $font, [System.Drawing.Brushes]::White, (New-Object System.Drawing.RectangleF $x, $y, $side, $side), $fmt)
  $bmp
}

$logos = [ordered]@{
  'Square44x44Logo.scale-100.png' = 44, 44; 'Square44x44Logo.scale-200.png' = 88, 88
  'Square44x44Logo.targetsize-16.png' = 16, 16; 'Square44x44Logo.targetsize-24.png' = 24, 24
  'Square44x44Logo.targetsize-32.png' = 32, 32; 'Square44x44Logo.targetsize-48.png' = 48, 48
  'Square44x44Logo.targetsize-256.png' = 256, 256
  'Square44x44Logo.targetsize-16_altform-unplated.png' = 16, 16; 'Square44x44Logo.targetsize-24_altform-unplated.png' = 24, 24
  'Square44x44Logo.targetsize-32_altform-unplated.png' = 32, 32; 'Square44x44Logo.targetsize-48_altform-unplated.png' = 48, 48
  'Square44x44Logo.targetsize-256_altform-unplated.png' = 256, 256
  'Square150x150Logo.scale-100.png' = 150, 150; 'Square150x150Logo.scale-200.png' = 300, 300
  'StoreLogo.scale-100.png' = 50, 50; 'StoreLogo.scale-200.png' = 100, 100
  'SplashScreen.scale-100.png' = 620, 300; 'SplashScreen.scale-200.png' = 1240, 600
}
foreach ($name in $logos.Keys) { (Draw-Logo $logos[$name][0] $logos[$name][1]).Save((Join-Path $outDir $name)) }

$appSizes = 16, 24, 32, 48, 256
$appPngs = foreach ($s in $appSizes) { Save-Png (Draw-Logo $s $s) }
Write-Ico (Join-Path $outDir 'App.ico') $appPngs $appSizes
