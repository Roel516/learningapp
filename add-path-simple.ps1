# Simple script to add C:\terraform to PATH
# Run this in PowerShell

$directory = "C:\terraform"

Write-Host "Adding $directory to PATH..." -ForegroundColor Green

# Get current user PATH
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")

# Check if already in PATH
if ($currentPath -split ';' | Where-Object { $_ -eq $directory }) {
    Write-Host "Already in PATH!" -ForegroundColor Yellow
    exit 0
}

# Add to PATH
$newPath = if ($currentPath) { "$currentPath;$directory" } else { $directory }

try {
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    Write-Host "SUCCESS! Added to PATH." -ForegroundColor Green
    Write-Host ""
    Write-Host "RESTART YOUR TERMINAL for changes to take effect!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "New PATH: $newPath" -ForegroundColor Cyan
} catch {
    Write-Host "ERROR: $_" -ForegroundColor Red
}
