# PowerShell script to fix all AutoFixture circular reference issues in all test files
$testFiles = Get-ChildItem -Path "d:\programming\GitHub\Footex\Footex.UnitTests" -Filter "*.cs" -Recurse | Where-Object { $_.Name -like "*Tests.cs" }

foreach ($file in $testFiles) {
    Write-Host "Processing $($file.FullName)"
    
    # Read the file content
    $content = Get-Content $file.FullName -Raw
    
    # Skip if file is empty or doesn't contain Fixture
    if ([string]::IsNullOrWhiteSpace($content) -or $content -notmatch "Fixture") {
        Write-Host "Skipping $($file.FullName) - no Fixture usage found"
        continue
    }
    
    # Replace Fixture with NoRecursionFixture
    $content = $content -replace "private readonly Fixture _fixture;", "private readonly NoRecursionFixture _fixture;"
    $content = $content -replace "_fixture = new Fixture\(\);", "_fixture = new NoRecursionFixture();"
    
    # Add using statement for NoRecursionFixture if not present and Fixture is used
    if ($content -match "NoRecursionFixture" -and $content -notmatch "using Footex\.UnitTests\.Common;") {
        # Find the last using statement and add after it
        $content = $content -replace "(using [^;]+;[\r\n]*)+", "$&using Footex.UnitTests.Common;`r`n"
    }
    
    # Write the updated content back to the file
    Set-Content $file.FullName -Value $content -NoNewline
    
    Write-Host "Updated $($file.FullName)"
}

Write-Host "Completed updating all test files"
