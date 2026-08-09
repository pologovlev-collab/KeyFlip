param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\assets\KeyFlip.ico')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath {
    param(
        [System.Drawing.RectangleF]$Rectangle,
        [float]$Radius
    )

    $diameter = $Radius * 2
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc($Rectangle.X, $Rectangle.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($Rectangle.Right - $diameter, $Rectangle.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($Rectangle.Right - $diameter, $Rectangle.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($Rectangle.X, $Rectangle.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconPng {
    param([int]$Size)

    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.Clear([System.Drawing.Color]::Transparent)

        $outerMargin = [Math]::Max(1.0, $Size * 0.045)
        $outerRect = [System.Drawing.RectangleF]::new($outerMargin, $outerMargin, $Size - 2 * $outerMargin, $Size - 2 * $outerMargin)
        $outerPath = New-RoundedRectanglePath $outerRect ($Size * 0.22)
        $outerBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 5, 108, 255))
        try { $graphics.FillPath($outerBrush, $outerPath) }
        finally { $outerBrush.Dispose(); $outerPath.Dispose() }

        $innerMargin = [Math]::Max(2.0, $Size * 0.12)
        $innerRect = [System.Drawing.RectangleF]::new($innerMargin, $innerMargin, $Size - 2 * $innerMargin, $Size - 2 * $innerMargin)
        $innerPath = New-RoundedRectanglePath $innerRect ($Size * 0.15)
        $innerBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 5, 25, 65))
        try { $graphics.FillPath($innerBrush, $innerPath) }
        finally { $innerBrush.Dispose(); $innerPath.Dispose() }

        $white = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
        $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, [Math]::Max(1.5, $Size * 0.095))
        try {
            $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

            $left = $Size * 0.25
            $right = $Size * 0.75
            $top = $Size * 0.38
            $bottom = $Size * 0.62
            $arrow = $Size * 0.15

            $graphics.DrawLine($pen, $left, $top, $right - $arrow * 0.35, $top)
            $graphics.FillPolygon($white, [System.Drawing.PointF[]]@(
                [System.Drawing.PointF]::new($right, $top),
                [System.Drawing.PointF]::new($right - $arrow, $top - $arrow * 0.72),
                [System.Drawing.PointF]::new($right - $arrow, $top + $arrow * 0.72)
            ))

            $graphics.DrawLine($pen, $right, $bottom, $left + $arrow * 0.35, $bottom)
            $graphics.FillPolygon($white, [System.Drawing.PointF[]]@(
                [System.Drawing.PointF]::new($left, $bottom),
                [System.Drawing.PointF]::new($left + $arrow, $bottom - $arrow * 0.72),
                [System.Drawing.PointF]::new($left + $arrow, $bottom + $arrow * 0.72)
            ))
        }
        finally {
            $pen.Dispose()
            $white.Dispose()
        }

        $stream = [System.IO.MemoryStream]::new()
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return ,$stream.ToArray()
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$sizes = @(16, 20, 24, 32, 48, 64, 128, 256)
$images = @($sizes | ForEach-Object { New-IconPng $_ })
$iconStream = [System.IO.MemoryStream]::new()
$writer = [System.IO.BinaryWriter]::new($iconStream)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$sizes.Count)

    $offset = 6 + 16 * $sizes.Count
    for ($index = 0; $index -lt $sizes.Count; $index++) {
        $size = $sizes[$index]
        $image = $images[$index]
        $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$image.Length)
        $writer.Write([uint32]$offset)
        $offset += $image.Length
    }

    foreach ($image in $images) { $writer.Write($image) }
    $writer.Flush()

    $resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($resolvedOutputPath)) | Out-Null
    [System.IO.File]::WriteAllBytes($resolvedOutputPath, $iconStream.ToArray())
    Write-Host "Icon created: $resolvedOutputPath"
}
finally {
    $writer.Dispose()
    $iconStream.Dispose()
}
