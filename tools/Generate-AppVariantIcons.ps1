[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$baseIconPath = Join-Path $repositoryRoot "GuacamoleClient-WinForms\guac.ico"
$sizes = @(16, 32, 48, 64)

$variants = @(
    @{ App = "winforms"; Letter = "W"; Channel = "stable"; Background = "#FFFFFF"; Foreground = "#111111" },
    @{ App = "winforms"; Letter = "W"; Channel = "dev"; Background = "#F28C00"; Foreground = "#111111" },
    @{ App = "winforms"; Letter = "W"; Channel = "debug"; Background = "#C62828"; Foreground = "#FFFFFF" },
    @{ App = "avalonia"; Letter = "A"; Channel = "stable"; Background = "#FFFFFF"; Foreground = "#111111" },
    @{ App = "avalonia"; Letter = "A"; Channel = "dev"; Background = "#F28C00"; Foreground = "#111111" },
    @{ App = "avalonia"; Letter = "A"; Channel = "debug"; Background = "#C62828"; Foreground = "#FFFFFF" }
)

function New-BadgedPngFrame {
    param(
        [Parameter(Mandatory)] [int] $Size,
        [Parameter(Mandatory)] [string] $Letter,
        [Parameter(Mandatory)] [string] $Background,
        [Parameter(Mandatory)] [string] $Foreground
    )

    $sourceIcon = [System.Drawing.Icon]::new($baseIconPath, $Size, $Size)
    $sourceBitmap = $sourceIcon.ToBitmap()
    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.DrawImage($sourceBitmap, 0, 0, $Size, $Size)

        $badgeSize = [Math]::Max(7, [int][Math]::Round($Size * 0.44))
        $borderWidth = if ($Size -ge 48) { 2 } else { 1 }
        $badgeBounds = [System.Drawing.Rectangle]::new(
            $Size - $badgeSize,
            $Size - $badgeSize,
            $badgeSize - 1,
            $badgeSize - 1)

        $backgroundBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($Background))
        $foregroundBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($Foreground))
        $borderPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(220, 32, 32, 32), $borderWidth)
        $font = [System.Drawing.Font]::new(
            "Arial",
            [single]($badgeSize * 0.78),
            [System.Drawing.FontStyle]::Bold,
            [System.Drawing.GraphicsUnit]::Pixel)
        $format = [System.Drawing.StringFormat]::new()

        try {
            $graphics.FillRectangle($backgroundBrush, $badgeBounds)
            $graphics.DrawRectangle($borderPen, $badgeBounds)
            $format.Alignment = [System.Drawing.StringAlignment]::Center
            $format.LineAlignment = [System.Drawing.StringAlignment]::Center
            $format.FormatFlags = [System.Drawing.StringFormatFlags]::NoWrap
            $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
            if ($Size -le 16) {
                $pixelPattern = if ($Letter -eq "W") {
                    @("10001", "10001", "10101", "10101", "01010")
                }
                else {
                    @("01110", "10001", "11111", "10001", "10001")
                }

                $startX = $badgeBounds.X + 1
                $startY = $badgeBounds.Y + 1
                for ($row = 0; $row -lt $pixelPattern.Count; $row++) {
                    for ($column = 0; $column -lt $pixelPattern[$row].Length; $column++) {
                        if ($pixelPattern[$row][$column] -eq "1") {
                            $graphics.FillRectangle($foregroundBrush, $startX + $column, $startY + $row, 1, 1)
                        }
                    }
                }
            }
            else {
                $textBounds = [System.Drawing.RectangleF]::new(
                    $badgeBounds.X,
                    $badgeBounds.Y,
                    $badgeBounds.Width,
                    $badgeBounds.Height)
                $graphics.DrawString($Letter, $font, $foregroundBrush, $textBounds, $format)
            }
        }
        finally {
            $format.Dispose()
            $font.Dispose()
            $borderPen.Dispose()
            $foregroundBrush.Dispose()
            $backgroundBrush.Dispose()
        }

        $stream = [System.IO.MemoryStream]::new()
        try {
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            return $stream.ToArray()
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
        $sourceBitmap.Dispose()
        $sourceIcon.Dispose()
    }
}

function Write-IcoFile {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [object[]] $Frames
    )

    $directory = Split-Path -Parent $Path
    [System.IO.Directory]::CreateDirectory($directory) | Out-Null

    $stream = [System.IO.File]::Create($Path)
    $writer = [System.IO.BinaryWriter]::new($stream)

    try {
        $writer.Write([uint16]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]$Frames.Count)

        $offset = 6 + (16 * $Frames.Count)
        foreach ($frame in $Frames) {
            [byte[]] $bytes = $frame.Bytes
            $writer.Write([byte]$frame.Size)
            $writer.Write([byte]$frame.Size)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            $writer.Write([uint16]1)
            $writer.Write([uint16]32)
            $writer.Write([uint32]$bytes.Length)
            $writer.Write([uint32]$offset)
            $offset += $bytes.Length
        }

        foreach ($frame in $Frames) {
            [byte[]] $bytes = $frame.Bytes
            $writer.BaseStream.Write($bytes, 0, $bytes.Length)
        }
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

foreach ($variant in $variants) {
    $frames = @(
        foreach ($size in $sizes) {
            @{
                Size = $size
                Bytes = New-BadgedPngFrame `
                    -Size $size `
                    -Letter $variant.Letter `
                    -Background $variant.Background `
                    -Foreground $variant.Foreground
            }
        }
    )

    $projectDirectory = if ($variant.App -eq "winforms") {
        "GuacamoleClient-WinForms"
    }
    else {
        "GuacamoleClient-Avalonia"
    }

    $outputPath = Join-Path $repositoryRoot "$projectDirectory\icons\guac-$($variant.App)-$($variant.Channel).ico"
    Write-IcoFile -Path $outputPath -Frames $frames
    Write-Host "Generated $outputPath"
}
