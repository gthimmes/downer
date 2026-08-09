# Captures a screenshot of the Downer window for design iteration.
# Usage: .\tools\capture-window.ps1 -OutPng shot.png [-FileArg sample.md] [-WaitSec 6] [-ExePath path]
param(
    [Parameter(Mandatory = $true)][string]$OutPng,
    [string]$FileArg = "",
    [double]$WaitSec = 6,
    [string]$ExePath = "src\Downer\bin\Debug\net10.0\Downer.exe"
)

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32Cap {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

[Win32Cap]::SetProcessDPIAware() | Out-Null

if ($FileArg) {
    $p = Start-Process -FilePath $ExePath -ArgumentList "`"$FileArg`"" -PassThru
} else {
    $p = Start-Process -FilePath $ExePath -PassThru
}

Start-Sleep -Seconds $WaitSec
$p.Refresh()
$h = $p.MainWindowHandle
if ($h -eq [IntPtr]::Zero) { Stop-Process -Id $p.Id -Force; throw "No main window handle" }

[Win32Cap]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 600

$r = New-Object Win32Cap+RECT
[Win32Cap]::GetWindowRect($h, [ref]$r) | Out-Null
$w = $r.Right - $r.Left
$ht = $r.Bottom - $r.Top

$bmp = New-Object System.Drawing.Bitmap($w, $ht)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[Win32Cap]::PrintWindow($h, $hdc, 2) | Out-Null  # 2 = PW_RENDERFULLCONTENT
$g.ReleaseHdc($hdc)
$g.Dispose()
$bmp.Save($OutPng, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Stop-Process -Id $p.Id -Force
Write-Output "Captured ${w}x${ht} -> $OutPng"
