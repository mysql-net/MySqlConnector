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
pushd tests\MySqlConnector.DependencyInjection.Tests
dotnet test -c Release
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}
popd

pushd .\tests\IntegrationTests

echo "Executing integration tests with No Compression, No SSL"
Copy-Item -Force ..\..\.ci\config\config.json config.json
dotnet test -c Release -f net462
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}
dotnet test -c Release -f net10.0
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}

echo "Executing integration tests with Compression, No SSL"
Copy-Item -Force ..\..\.ci\config\config.compression.json config.json
dotnet test -c Release -f net8.0
if ($LASTEXITCODE -ne 0){
    exit $LASTEXITCODE;
}

popd
