@echo off
echo Executing tests with No Compression, No SSL && copy /y .ci\config\config.json tests\SideBySide\config.json && dotnet test tests/SideBySide/SideBySide.csproj --configuration Release
echo Executing tests with Compression, No SSL && copy /y .ci\config\config.compression.json tests\SideBySide\config.json && dotnet test tests/SideBySide/SideBySide.csproj --configuration Release
echo Executing tests with No Compression, SSL && copy /y .ci\config\config.ssl.json tests\SideBySide\config.json && dotnet test tests/SideBySide/SideBySide.csproj --configuration Release
echo Executing tests with Compression, SSL && copy /y ".ci\config\config.compression+ssl.json" tests\SideBySide\config.json && dotnet test tests/SideBySide/SideBySide.csproj --configuration Release
