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

echo "Executing unit tests"
pushd tests\MySqlConnector.Tests
dotnet test -c Release
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}
popd
pushd tests\Conformance.Tests
dotnet test -c Release
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}
popd

pushd .\tests\SideBySide

echo "Executing tests with No Compression, No SSL"
Copy-Item -Force ..\..\.ci\config\config.json config.json
dotnet test -c Release
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}

echo "Executing tests with Compression, No SSL"
Copy-Item -Force ..\..\.ci\config\config.compression.json config.json
dotnet test -c Release -f netcoreapp2.1
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}

popd

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
