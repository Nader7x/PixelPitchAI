# Clean-Solution.ps1
# Script to delete all bin and obj folders from all projects in the solution

param(
    [string]$SolutionPath = ".",
    [switch]$WhatIf = $false
)

Write-Host "Cleaning solution at: $SolutionPath" -ForegroundColor Green

# Find all bin and obj directories recursively
$foldersToDelete = @()
$foldersToDelete += Get-ChildItem -Path $SolutionPath -Recurse -Directory -Name "bin" -ErrorAction SilentlyContinue | ForEach-Object { Get-Item (Join-Path $SolutionPath $_) }
$foldersToDelete += Get-ChildItem -Path $SolutionPath -Recurse -Directory -Name "obj" -ErrorAction SilentlyContinue | ForEach-Object { Get-Item (Join-Path $SolutionPath $_) }

if ($foldersToDelete.Count -eq 0) {
    Write-Host "No bin or obj folders found." -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $($foldersToDelete.Count) folders to delete:" -ForegroundColor Yellow

foreach ($folder in $foldersToDelete) {
    Write-Host "  - $($folder.FullName)" -ForegroundColor Cyan
}

if ($WhatIf) {
    Write-Host "`nWhatIf mode: No folders will be deleted." -ForegroundColor Yellow
    exit 0
}

# Prompt for confirmation
$confirmation = Read-Host "`nDo you want to delete these folders? (y/N)"
if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
    Write-Host "Operation cancelled." -ForegroundColor Yellow
    exit 0
}

# Delete the folders
$deletedCount = 0
$errorCount = 0

foreach ($folder in $foldersToDelete) {
    try {
        Remove-Item -Path $folder.FullName -Recurse -Force
        Write-Host "Deleted: $($folder.FullName)" -ForegroundColor Green
        $deletedCount++
    }
    catch {
        Write-Host "Error deleting $($folder.FullName): $($_.Exception.Message)" -ForegroundColor Red
        $errorCount++
    }
}

Write-Host "`nCleanup completed!" -ForegroundColor Green
Write-Host "Successfully deleted: $deletedCount folders" -ForegroundColor Green
if ($errorCount -gt 0) {
    Write-Host "Errors encountered: $errorCount folders" -ForegroundColor Red
}