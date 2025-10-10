# Add a directory to PATH environment variable
# Usage: .\add-to-path.ps1 -Directory "C:\path\to\add"

param(
    [Parameter(Mandatory=$false)]
    [string]$Directory = "C:\terraform"
)

Write-Host "Adding '$Directory' to PATH..." -ForegroundColor Green

# Check if directory exists
if (-not (Test-Path $Directory)) {
    Write-Host "Warning: Directory '$Directory' does not exist!" -ForegroundColor Yellow
    $continue = Read-Host "Do you want to add it anyway? (y/n)"
    if ($continue -ne 'y') {
        Write-Host "Cancelled." -ForegroundColor Red
        exit 1
    }
}

# Get current PATH for User
$userPath = [Environment]::GetEnvironmentVariable("Path", "User")

# Check if already in PATH
if ($userPath -like "*$Directory*") {
    Write-Host "Directory is already in PATH!" -ForegroundColor Yellow
    exit 0
}

# Add to PATH
try {
    $newPath = if ($userPath) { "$userPath;$Directory" } else { $Directory }
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    Write-Host "✓ Successfully added to PATH!" -ForegroundColor Green
    Write-Host ""
    Write-Host "IMPORTANT: You need to restart your terminal for changes to take effect." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Current PATH (User): $newPath" -ForegroundColor Cyan
} catch {
    Write-Host "Failed to update PATH: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "To add manually:" -ForegroundColor Yellow
    Write-Host "1. Press Win + X and select 'System'" -ForegroundColor Gray
    Write-Host "2. Click 'Advanced system settings'" -ForegroundColor Gray
    Write-Host "3. Click 'Environment Variables'" -ForegroundColor Gray
    Write-Host "4. Under 'User variables', select 'Path' and click 'Edit'" -ForegroundColor Gray
    Write-Host "5. Click 'New' and add: $Directory" -ForegroundColor Gray
    Write-Host "6. Click 'OK' on all dialogs" -ForegroundColor Gray
    exit 1
}
