@echo off
echo Executing tests with No Compression, No SSL && copy /y .ci\config\config.json tests\IntegrationTests\config.json && dotnet test tests/IntegrationTests/IntegrationTests.csproj --configuration Release
echo Executing tests with Compression, No SSL && copy /y .ci\config\config.compression.json tests\IntegrationTests\config.json && dotnet test tests/IntegrationTests/IntegrationTests.csproj --configuration Release
echo Executing tests with No Compression, SSL && copy /y .ci\config\config.ssl.json tests\IntegrationTests\config.json && dotnet test tests/IntegrationTests/IntegrationTests.csproj --configuration Release
echo Executing tests with Compression, SSL && copy /y ".ci\config\config.compression+ssl.json" tests\IntegrationTests\config.json && dotnet test tests/IntegrationTests/IntegrationTests.csproj --configuration Release
