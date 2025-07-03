# PowerShell script to fix AutoFixture circular reference issues in Auth command handler tests
$testFiles = @(
    "d:\programming\GitHub\Footex\Footex.UnitTests\CQRS\Auth\Commands\ConfirmEmailCommandHandlerTests.cs",
    "d:\programming\GitHub\Footex\Footex.UnitTests\CQRS\Auth\Commands\ForgotPasswordCommandHandlerTests.cs",
    "d:\programming\GitHub\Footex\Footex.UnitTests\CQRS\Auth\Commands\RefreshTokenCommandHandlerTests.cs",
    "d:\programming\GitHub\Footex\Footex.UnitTests\CQRS\Auth\Commands\ResendEmailConfirmationCommandHandlerTests.cs",
    "d:\programming\GitHub\Footex\Footex.UnitTests\CQRS\Auth\Commands\ResetPasswordCommandHandlerTests.cs",
    "d:\programming\GitHub\Footex\Footex.UnitTests\CQRS\Auth\Commands\RevokeTokenCommandHandlerTests.cs",
    "d:\programming\GitHub\Footex\Footex.UnitTests\CQRS\Auth\Commands\UpdateUserCommandHandlerTests.cs"
)

foreach ($file in $testFiles) {
    if (Test-Path $file) {
        Write-Host "Processing $file"
        
        # Read the file content
        $content = Get-Content $file -Raw
        
        # Replace Fixture with NoRecursionFixture
        $content = $content -replace "private readonly Fixture _fixture;", "private readonly NoRecursionFixture _fixture;"
        $content = $content -replace "_fixture = new Fixture\(\);", "_fixture = new NoRecursionFixture();"
        
        # Add using statement for NoRecursionFixture if not present
        if ($content -notmatch "using Footex\.UnitTests\.Common;") {
            $content = $content -replace "(using [^;]+;[\r\n]+)+", "$&using Footex.UnitTests.Common;`r`n"
        }
        
        # Write the updated content back to the file
        Set-Content $file -Value $content -NoNewline
        
        Write-Host "Updated $file"
    } else {
        Write-Host "File not found: $file"
    }
}

Write-Host "Completed updating Auth command handler tests"
