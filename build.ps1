<#
.SYNOPSIS
    Build and packaging script for Todo Widget WPF desktop application.

.DESCRIPTION
    1. Converts desktop PNG icon into Windows ICO format (if not already present).
    2. Runs 'dotnet publish' for win-x64, self-contained, ReadyToRun Release build.
    3. Prints the published application output path.
    4. Automatically compiles the Inno Setup installer script (setup.iss) if ISCC.exe
       is installed in PATH or standard Program Files locations.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Release.

.PARAMETER Runtime
    Target runtime identifier. Default is win-x64.

.PARAMETER SkipInstaller
    Switch to skip Inno Setup installer compilation even if ISCC.exe is found.

.PARAMETER Clean
    Switch to clean previous build artifacts before publishing.

.EXAMPLE
    .\build.ps1
    Publishes Release win-x64 and builds the Inno Setup installer.

.EXAMPLE
    .\build.ps1 -Clean -SkipInstaller
    Cleans previous builds and publishes the self-contained app without building the installer.
#>

[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [string]$Runtime = "win-x64",

    [switch]$SkipInstaller,

    [switch]$Clean
)

$ErrorActionPreference = "Stop"

# Set script paths
$ScriptDir = $PSScriptRoot
$ProjectDir = Join-Path $ScriptDir "src\TodoWidget"
$ProjectFile = Join-Path $ProjectDir "TodoWidget.csproj"
$PublishDir = Join-Path $ProjectDir "bin\$Configuration\net8.0-windows10.0.19041.0\$Runtime\publish"
$InstallerScript = Join-Path $ScriptDir "installer\setup.iss"
$InstallerOutputDir = Join-Path $ScriptDir "installer\Output"
$LogosDir = Join-Path $ScriptDir "Logos"
$DesktopPngPath = Join-Path $LogosDir "Icon when app is in desktop.png"
$AppIcoPath = Join-Path $LogosDir "app.ico"
$AssetsIcoPath = Join-Path $ProjectDir "Assets\app.ico"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "             Todo Widget - Build & Package                " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Configuration : $Configuration" -ForegroundColor Gray
Write-Host "Runtime       : $Runtime" -ForegroundColor Gray
Write-Host "Script Root   : $ScriptDir" -ForegroundColor Gray
Write-Host "Project File  : $ProjectFile" -ForegroundColor Gray
Write-Host ""

# ---------------------------------------------------------------------
# Step 1: Icon Generation / Verification (.png -> .ico)
# ---------------------------------------------------------------------
function Convert-PngToMultiResIco {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourcePngPath,
        [Parameter(Mandatory = $true)]
        [string]$TargetIcoPath
    )

    if (-not (Test-Path $SourcePngPath)) {
        Write-Warning "Source icon not found at: $SourcePngPath"
        return
    }

    $targetDir = Split-Path -Parent $TargetIcoPath
    if (-not (Test-Path $targetDir)) {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    }

    try {
        Add-Type -AssemblyName System.Drawing

        $srcImage = [System.Drawing.Image]::FromFile($SourcePngPath)
        $iconSizes = @(256, 48, 32, 16)
        $pngStreams = [System.Collections.Generic.List[byte[]]]::new()

        foreach ($size in $iconSizes) {
            $destBmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $graphics = [System.Drawing.Graphics]::FromImage($destBmp)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.DrawImage($srcImage, 0, 0, $size, $size)
            $graphics.Dispose()

            $ms = New-Object System.IO.MemoryStream
            $destBmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
            $destBmp.Dispose()
            $pngStreams.Add($ms.ToArray())
            $ms.Dispose()
        }
        $srcImage.Dispose()

        # Write Windows ICO file structure
        $fs = [System.IO.File]::Create($TargetIcoPath)
        $writer = New-Object System.IO.BinaryWriter($fs)

        # ICONDIR header (6 bytes)
        $writer.Write([uint16]0)                # Reserved (0)
        $writer.Write([uint16]1)                # Type (1 = ICO)
        $writer.Write([uint16]$pngStreams.Count)# Image Count

        # Header size + Directory entries size (16 bytes per image)
        $dataOffset = 6 + ($pngStreams.Count * 16)

        for ($i = 0; $i -lt $pngStreams.Count; $i++) {
            $size = $iconSizes[$i]
            $bytes = $pngStreams[$i]

            # ICONDIRENTRY (16 bytes)
            $w = if ($size -ge 256) { [byte]0 } else { [byte]$size }
            $h = if ($size -ge 256) { [byte]0 } else { [byte]$size }
            $writer.Write($w)                    # Width (0 = 256)
            $writer.Write($h)                    # Height (0 = 256)
            $writer.Write([byte]0)               # Color count
            $writer.Write([byte]0)               # Reserved
            $writer.Write([uint16]1)             # Color planes
            $writer.Write([uint16]32)            # Bits per pixel
            $writer.Write([uint32]$bytes.Length) # Image bytes count
            $writer.Write([uint32]$dataOffset)   # Offset of data

            $dataOffset += $bytes.Length
        }

        # Write actual image data payloads
        foreach ($bytes in $pngStreams) {
            $writer.Write($bytes)
        }

        $writer.Flush()
        $writer.Close()
        $fs.Close()

        Write-Host "[Icon] Successfully created icon: $TargetIcoPath" -ForegroundColor Green
    }
    catch {
        Write-Warning "Could not convert PNG to ICO: $_"
    }
}

Write-Host ">>> [1/3] Checking application icon files..." -ForegroundColor Yellow
if (-not (Test-Path $AppIcoPath)) {
    if (Test-Path $DesktopPngPath) {
        Write-Host "Converting '$DesktopPngPath' to '$AppIcoPath'..." -ForegroundColor Gray
        Convert-PngToMultiResIco -SourcePngPath $DesktopPngPath -TargetIcoPath $AppIcoPath
    } else {
        Write-Warning "Desktop PNG icon not found at: $DesktopPngPath"
    }
} else {
    Write-Host "[Icon] Found existing icon at: $AppIcoPath" -ForegroundColor Gray
}

# Ensure WPF project Assets\app.ico exists
if (Test-Path $AppIcoPath) {
    $assetsDir = Split-Path -Parent $AssetsIcoPath
    if (-not (Test-Path $assetsDir)) {
        New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null
    }
    if (-not (Test-Path $AssetsIcoPath)) {
        Copy-Item -Path $AppIcoPath -Destination $AssetsIcoPath -Force
        Write-Host "[Icon] Copied icon to: $AssetsIcoPath" -ForegroundColor Gray
    }
}

# ---------------------------------------------------------------------
# Step 2: Dotnet Publish (Release, win-x64, self-contained, ReadyToRun)
# ---------------------------------------------------------------------
Write-Host ""
Write-Host ">>> [2/3] Publishing TodoWidget WPF application..." -ForegroundColor Yellow

if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

# Check for dotnet CLI
$dotnetExe = "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $dotnetExe)) {
    $found = Get-Command "dotnet" -ErrorAction SilentlyContinue
    if ($found) { $dotnetExe = $found.Source }
}
if (-not (Test-Path $dotnetExe)) {
    Write-Error "The .NET CLI ('dotnet') is not found in PATH or LocalAppData. Please install .NET 8 SDK."
    exit 1
}

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous build output..." -ForegroundColor Gray
    & $dotnetExe clean "$ProjectFile" -c $Configuration | Out-Null
    if (Test-Path $PublishDir) {
        Remove-Item -Path $PublishDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$publishArgs = @(
    "publish",
    "$ProjectFile",
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-p:PublishReadyToRun=true"
)

Write-Host "Executing: $dotnetExe $($publishArgs -join ' ')" -ForegroundColor DarkGray
& $dotnetExe @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " [SUCCESS] Application successfully published!           " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Publish Output Directory:" -ForegroundColor Cyan
Write-Host "  $PublishDir" -ForegroundColor White
Write-Host ""

$exePath = Join-Path $PublishDir "TodoWidget.exe"
if (Test-Path $exePath) {
    $exeItem = Get-Item $exePath
    $fileSizeMB = [math]::Round($exeItem.Length / 1MB, 2)
    Write-Host "Primary Executable : $exePath ($fileSizeMB MB)" -ForegroundColor Gray
}

# ---------------------------------------------------------------------
# Step 3: Inno Setup Compilation (Optional / If ISCC.exe is found)
# ---------------------------------------------------------------------
Write-Host ""
Write-Host ">>> [3/3] Checking Inno Setup Compiler (ISCC.exe)..." -ForegroundColor Yellow

if ($SkipInstaller) {
    Write-Host "Skipping installer compilation (-SkipInstaller specified)." -ForegroundColor Yellow
    exit 0
}

# Find ISCC.exe
$isccLocations = @(
    (Get-Command "iscc.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 5\ISCC.exe",
    "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
)

$isccPath = $null
foreach ($loc in $isccLocations) {
    if ($loc -and (Test-Path $loc)) {
        $isccPath = $loc
        break
    }
}

if (-not $isccPath) {
    # Check Registry for Inno Setup installation directory
    $regPaths = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1"
    )
    foreach ($rp in $regPaths) {
        if (Test-Path $rp) {
            $installDir = (Get-ItemProperty -Path $rp -Name "InstallLocation" -ErrorAction SilentlyContinue).InstallLocation
            if ($installDir) {
                $candidate = Join-Path $installDir "ISCC.exe"
                if (Test-Path $candidate) {
                    $isccPath = $candidate
                    break
                }
            }
        }
    }
}

if ($isccPath) {
    Write-Host "Found Inno Setup Compiler at: $isccPath" -ForegroundColor Green

    if (-not (Test-Path $InstallerScript)) {
        Write-Error "Inno Setup script not found at: $InstallerScript"
        exit 1
    }

    if (-not (Test-Path $InstallerOutputDir)) {
        New-Item -ItemType Directory -Path $InstallerOutputDir -Force | Out-Null
    }

    Write-Host "Compiling installer: $InstallerScript..." -ForegroundColor Gray
    & "$isccPath" "$InstallerScript"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Inno Setup compiler failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    Write-Host ""
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host " [SUCCESS] Installer successfully compiled!              " -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host "Installer Output Directory:" -ForegroundColor Cyan
    Write-Host "  $InstallerOutputDir" -ForegroundColor White

    $setupExe = Get-ChildItem -Path $InstallerOutputDir -Filter "ToDoSetup-*.exe" | Select-Object -First 1
    if ($setupExe) {
        $setupSizeMB = [math]::Round($setupExe.Length / 1MB, 2)
        Write-Host "Installer Executable       : $($setupExe.FullName) ($setupSizeMB MB)" -ForegroundColor White
    }
    Write-Host ""
} else {
    Write-Host "Inno Setup compiler (ISCC.exe) was not found on this system." -ForegroundColor Yellow
    Write-Host "The application has been published successfully, but the installer executable could not be built automatically." -ForegroundColor Gray
    Write-Host ""
    Write-Host "To build the installer:" -ForegroundColor Cyan
    Write-Host "  1. Download and install Inno Setup 6 from: https://jrsoftware.org/isdl.php" -ForegroundColor Gray
    Write-Host "  2. Run: & 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' '$InstallerScript'" -ForegroundColor Gray
    Write-Host "  3. Or re-run this script: .\build.ps1" -ForegroundColor Gray
    Write-Host ""
}
