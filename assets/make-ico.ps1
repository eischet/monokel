# Erzeugt aus assets/Monokel.png das Programmsymbol src/Monokel/Monokel.ico und das kleinere Bild
# assets/Monokel-512.png für die README (Aufruf: pwsh assets/make-ico.ps1).
param(
    [string]$Source = (Join-Path $PSScriptRoot 'Monokel.png'),
    [string]$Target = (Join-Path $PSScriptRoot '..\src\Monokel\Monokel.ico'),
    [string]$Readme = (Join-Path $PSScriptRoot 'Monokel-512.png')
)
Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Image]::FromFile((Resolve-Path $Source))

function Resize([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.PixelOffsetMode = 'HighQuality'
    $g.CompositingQuality = 'HighQuality'
    $g.SmoothingMode = 'HighQuality'
    $g.DrawImage($src, 0, 0, $s, $s)
    $g.Dispose()
    $bmp
}

# README zeigt 256 px breit, 512 px bleiben auf hochauflösenden Bildschirmen scharf
$bmp = Resize 512
$bmp.Save([System.IO.Path]::GetFullPath($Readme), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

$sizes = 16, 20, 24, 32, 40, 48, 64, 256
$pngs = foreach ($s in $sizes) {
    $bmp = Resize $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    , $ms.ToArray()
}
$src.Dispose()

# ICO: Header, Verzeichnis, dann die PNG-Daten (PNG-Einträge versteht Windows seit Vista)
$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $out
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $len = $pngs[$i].Length
    $w.Write([byte]($s % 256)); $w.Write([byte]($s % 256)); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]$len); $w.Write([uint32]$offset)
    $offset += $len
}
foreach ($p in $pngs) { $w.Write($p) }
$w.Flush()
[System.IO.File]::WriteAllBytes([System.IO.Path]::GetFullPath($Target), $out.ToArray())
