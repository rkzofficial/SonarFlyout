# Builds store\images\ from the app captures in docs\images\. Run with Windows PowerShell 5.1 (System.Drawing).
param($root = (Split-Path $PSScriptRoot))
Add-Type -AssemblyName System.Drawing
$out = Join-Path $root 'store\images'
New-Item -ItemType Directory -Force $out | Out-Null
$captures = Join-Path $root 'docs\images'

function New-Canvas($w, $h) {
  $bmp = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = 'AntiAlias'; $g.InterpolationMode = 'HighQualityBicubic'; $g.PixelOffsetMode = 'HighQuality'; $g.TextRenderingHint = 'AntiAliasGridFit'
  $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush ((New-Object System.Drawing.Point 0, 0), (New-Object System.Drawing.Point $w, $h), ([System.Drawing.Color]::FromArgb(255, 14, 22, 48)), ([System.Drawing.Color]::FromArgb(255, 30, 58, 128)))
  $g.FillRectangle($bg, 0, 0, $w, $h)
  $glow = New-Object System.Drawing.Drawing2D.GraphicsPath
  $glow.AddEllipse([float]($w * 0.45), [float](-$h * 0.35), [float]($w * 0.9), [float]($h * 1.3))
  $pgb = New-Object System.Drawing.Drawing2D.PathGradientBrush $glow
  $pgb.CenterColor = [System.Drawing.Color]::FromArgb(90, 80, 150, 255); $pgb.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 80, 150, 255))
  $g.FillPath($pgb, $glow)
  @{ Bitmap = $bmp; Graphics = $g }
}

function Get-RoundedPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
  $p = New-Object System.Drawing.Drawing2D.GraphicsPath
  $p.AddArc($x, $y, 2 * $r, 2 * $r, 180, 90); $p.AddArc($x + $w - 2 * $r, $y, 2 * $r, 2 * $r, 270, 90)
  $p.AddArc($x + $w - 2 * $r, $y + $h - 2 * $r, 2 * $r, 2 * $r, 0, 90); $p.AddArc($x, $y + $h - 2 * $r, 2 * $r, 2 * $r, 90, 90)
  $p.CloseFigure(); $p
}

# Rounded-corner screenshot with a soft shadow, scaled to $scale.
function Draw-Window($g, $image, [float]$x, [float]$y, [float]$scale, [float]$radius) {
  $w = $image.Width * $scale; $h = $image.Height * $scale
  for ($i = 24; $i -ge 1; $i -= 3) {
    $shadow = Get-RoundedPath ($x - $i + 6) ($y - $i + 14) ($w + 2 * $i) ($h + 2 * $i) ($radius + $i)
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(10, 0, 0, 0))), $shadow)
  }
  $scaled = New-Object System.Drawing.Bitmap ([int]$w), ([int]$h)
  $sg = [System.Drawing.Graphics]::FromImage($scaled); $sg.InterpolationMode = 'HighQualityBicubic'; $sg.PixelOffsetMode = 'HighQuality'
  $sg.DrawImage($image, 0, 0, [int]$w, [int]$h)
  $brush = New-Object System.Drawing.TextureBrush $scaled; $brush.TranslateTransform($x, $y)
  $g.FillPath($brush, (Get-RoundedPath $x $y $w $h $radius))
  $g.DrawPath((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(70, 255, 255, 255)), 1.5), (Get-RoundedPath $x $y $w $h $radius))
}

function New-Font([float]$size, [bool]$bold = $false) {
  $style = if ($bold) { [System.Drawing.FontStyle]::Bold } else { [System.Drawing.FontStyle]::Regular }
  New-Object System.Drawing.Font ([string]'Segoe UI'), $size, $style, ([System.Drawing.GraphicsUnit]::Pixel)
}

function Draw-Text($g, $text, $size, $bold, [float]$x, [float]$y, [float]$width, $alpha = 255) {
  $font = New-Font $size $bold
  $g.DrawString($text, $font, (New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb($alpha, 255, 255, 255))), (New-Object System.Drawing.RectangleF $x, $y, $width, 1000))
  $g.MeasureString($text, $font, [int]$width).Height
}

function Draw-Logo($g, [float]$x, [float]$y, [float]$side) {
  $path = Get-RoundedPath $x $y $side $side ($side * 0.22)
  $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush ((New-Object System.Drawing.PointF $x, $y), (New-Object System.Drawing.PointF ($x + $side), ($y + $side)), ([System.Drawing.Color]::FromArgb(255, 64, 140, 235)), ([System.Drawing.Color]::FromArgb(255, 22, 82, 180)))
  $g.FillPath($brush, $path)
  $font = New-Object System.Drawing.Font 'Segoe Fluent Icons', ($side * 0.58), ([System.Drawing.GraphicsUnit]::Pixel)
  $fmt = [System.Drawing.StringFormat]::GenericTypographic; $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'
  $g.DrawString([string][char]0xE995, $font, [System.Drawing.Brushes]::White, (New-Object System.Drawing.RectangleF $x, $y, $side, $side), $fmt)
}

function Crop($path, $x, $y, $w, $h) {
  $src = [System.Drawing.Image]::FromFile($path)
  $dst = New-Object System.Drawing.Bitmap $w, $h; [System.Drawing.Graphics]::FromImage($dst).DrawImage($src, (New-Object System.Drawing.Rectangle 0, 0, $w, $h), (New-Object System.Drawing.Rectangle $x, $y, $w, $h), 'Pixel')
  $dst
}

$flyout = [System.Drawing.Image]::FromFile((Join-Path $captures 'flyout.png'))
$perChannelFull = [System.Drawing.Image]::FromFile((Join-Path $captures 'per-channel.png'))
$perChannel = Crop (Join-Path $captures 'per-channel.png') 40 0 ($perChannelFull.Width - 40) $perChannelFull.Height
$trayMenu = Crop (Join-Path $captures 'tray-menu.png') 250 60 310 180

$screens = @(
  @{ File = 'screenshot-1-flyout.png'; Image = $flyout; Scale = 1.4; Title = "Sonar, one click away"; Body = "Volume, mute and output for every Sonar channel, right from your taskbar." },
  @{ File = 'screenshot-2-per-channel.png'; Image = $perChannel; Scale = 1.45; Title = "Route each channel anywhere"; Body = "Unlink outputs to send game, chat and media to different devices." },
  @{ File = 'screenshot-3-tray.png'; Image = $trayMenu; Scale = 2.4; Title = "Feels like part of Windows"; Body = "Fluent design, follows your theme and your language, and can start with Windows." }
)
foreach ($s in $screens) {
  $c = New-Canvas 1920 1080; $g = $c.Graphics
  $titleHeight = $g.MeasureString($s.Title, (New-Font 72 $true), 820).Height
  $bodyHeight = $g.MeasureString($s.Body, (New-Font 34), 760).Height
  $top = (1080 - (96 + 40 + $titleHeight + 24 + $bodyHeight)) / 2
  Draw-Logo $g 140 $top 96
  $null = Draw-Text $g $s.Title 72 $true 136 ($top + 136) 820
  $null = Draw-Text $g $s.Body 34 $false 140 ($top + 136 + $titleHeight + 24) 760 200
  $w = $s.Image.Width * $s.Scale; $h = $s.Image.Height * $s.Scale
  Draw-Window $g $s.Image (1440 - $w / 2) ((1080 - $h) / 2) $s.Scale 12
  $c.Bitmap.Save((Join-Path $out $s.File))
}

foreach ($size in 1080, 2160) {
  $c = New-Canvas $size $size; $g = $c.Graphics; $u = $size / 1080
  Draw-Logo $g (340 * $u) (250 * $u) (400 * $u)
  $fmt = New-Object System.Drawing.StringFormat; $fmt.Alignment = 'Center'
  $g.DrawString('Sonar Flyout', (New-Font (96 * $u) $true), [System.Drawing.Brushes]::White, (New-Object System.Drawing.RectangleF 0, (720 * $u), $size, (200 * $u)), $fmt)
  $c.Bitmap.Save((Join-Path $out "boxart-${size}x${size}.png"))
}

foreach ($size in @(@(720, 1080), @(1440, 2160))) {
  $w = $size[0]; $h = $size[1]; $u = $w / 720
  $c = New-Canvas $w $h; $g = $c.Graphics
  Draw-Logo $g (260 * $u) (110 * $u) (200 * $u)
  $fmt = New-Object System.Drawing.StringFormat; $fmt.Alignment = 'Center'
  $g.DrawString('Sonar Flyout', (New-Font (64 * $u) $true), [System.Drawing.Brushes]::White, (New-Object System.Drawing.RectangleF 0, (330 * $u), $w, (100 * $u)), $fmt)
  $g.DrawString('A tray mixer for SteelSeries Sonar', (New-Font (30 * $u)), (New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 255, 255, 255))), (New-Object System.Drawing.RectangleF 0, (420 * $u), $w, (60 * $u)), $fmt)
  $scale = 0.95 * $u
  Draw-Window $g $flyout (($w - $flyout.Width * $scale) / 2) (520 * $u) $scale (12 * $u)
  $c.Bitmap.Save((Join-Path $out "poster-${w}x${h}.png"))
}

# Super hero art carries no text: the Store overlays the app name on it.
$c = New-Canvas 1920 1080; $g = $c.Graphics
Draw-Window $g $flyout 1220 ((1080 - $flyout.Height * 1.3) / 2) 1.3 12
Draw-Logo $g 260 380 320
$c.Bitmap.Save((Join-Path $out 'hero-1920x1080.png'))

$c = New-Canvas 300 300; Draw-Logo $c.Graphics 30 30 240; $c.Bitmap.Save((Join-Path $out 'storelogo-300x300.png'))
Get-ChildItem $out | % { "{0} {1}x{2}" -f $_.Name, ([System.Drawing.Image]::FromFile($_.FullName)).Width, ([System.Drawing.Image]::FromFile($_.FullName)).Height }
