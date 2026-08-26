# Build & Packaging script for Quick Access Hub

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Building Quick Access Hub Windows App   " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Locate dotnet SDK executable
$env:DOTNET_ROOT = "C:\Users\mayan\.dotnet"
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$dotnetExe = "$env:DOTNET_ROOT\dotnet.exe"

if (-not $dotnetExe) {
    Write-Error "dotnet.exe could not be found! Please ensure .NET 8 SDK is installed."
    exit 1
}

Write-Host "Using dotnet executable: $dotnetExe" -ForegroundColor Green

# 2. Clean & Publish WPF App
Write-Host "Publishing QuickAccessHub WPF application..." -ForegroundColor Yellow
$projectPath = Join-Path $PSScriptRoot "QuickAccessHub\QuickAccessHub.csproj"

& $dotnetExe publish $projectPath -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed!"
    exit 1
}

Write-Host "App successfully published!" -ForegroundColor Green

# 3. Locate Inno Setup Compiler (ISCC.exe)
$isccExe = Get-Command ISCC.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source

if (-not $isccExe) {
    $isccPaths = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    foreach ($path in $isccPaths) {
        if (Test-Path $path) {
            $isccExe = $path
            break
        }
    }
}

if (-not $isccExe) {
    Write-Warning "ISCC.exe (Inno Setup Compiler) was not found. Skipping installer creation step."
    Write-Host "Standalone executable is ready in: QuickAccessHub\bin\Release\net8.0-windows\publish\" -ForegroundColor Green
    exit 0
}

Write-Host "Using Inno Setup Compiler: $isccExe" -ForegroundColor Green

# 4. Compile Installer setup.iss
Write-Host "Compiling setup installer (QuickAccessHub-Setup.exe)..." -ForegroundColor Yellow
$issPath = Join-Path $PSScriptRoot "installer\setup.iss"

& $isccExe $issPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup compilation failed!"
    exit 1
}

$outputSetup = Join-Path $PSScriptRoot "installer_output\QuickAccessHub-Setup.exe"
if (Test-Path $outputSetup) {
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host " SUCCESS! Installer created:             " -ForegroundColor Green
    Write-Host " $outputSetup" -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Green
} else {
    Write-Warning "Setup output file not found at expected path: $outputSetup"
}
