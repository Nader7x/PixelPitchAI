# Run all performance tests
./run-performance-tests.ps1 -TestType all

# Run specific test types
./run-performance-tests.ps1 -TestType load
./run-performance-tests.ps1 -TestType stress
./run-performance-tests.ps1 -TestType benchmark

# Using dotnet CLI
dotnet test --filter Category=LoadTest
dotnet test --filter Category=Benchmark