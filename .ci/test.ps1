# restore
dotnet restore
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}

# build project
dotnet build src\MySqlConnector -c Release
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}

echo "Executing connection string tests"
dotnet test tests\MySqlConnector.Tests\MySqlConnector.Tests.csproj -c Release
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}

echo "Executing tests with No Compression, No SSL"
Copy-Item -Force .ci\config\config.json tests\SideBySide\config.json
dotnet test tests\SideBySide\SideBySide.csproj -c Release
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}
echo "Executing Debug Only tests"
dotnet test tests\SideBySide\SideBySide.csproj -c Debug --filter "FullyQualifiedName~SideBySide.DebugOnlyTests"
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}

echo "Executing tests with Compression, No SSL"
Copy-Item -Force .ci\config\config.compression.json tests\SideBySide\config.json
dotnet test tests\SideBySide\SideBySide.csproj -c Release
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}

echo "Executing baseline connection string tests"
dotnet restore tests\MySqlConnector.Tests\MySqlConnector.Tests.csproj /p:Configuration=Baseline
dotnet test tests\MySqlConnector.Tests\MySqlConnector.Tests.csproj -c Baseline
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}

echo "Executing baseline tests with No Compression, No SSL"
Copy-Item -Force .ci\config\config.json tests\SideBySide\config.json
dotnet restore tests\SideBySide\SideBySide.csproj /p:Configuration=Baseline
dotnet test tests\SideBySide\SideBySide.csproj -c Baseline
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}

echo "Building Benchmark"

dotnet restore tests\Benchmark\Benchmark.csproj
dotnet run -p tests\Benchmark\Benchmark.csproj -c Release -f net462
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}
