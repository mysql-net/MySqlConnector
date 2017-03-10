@echo off
echo Executing tests with No Compression, No SSL && copy /y tests\SideBySide\config.json.example tests\SideBySide\config.json && dotnet test tests/SideBySide --configuration Release
echo Executing tests with Compression, No SSL && copy /y .ci\config\config.compression.json tests\SideBySide\config.json && dotnet test tests/SideBySide --configuration Release
echo Executing tests with No Compression, SSL && copy /y .ci\config\config.ssl.json tests\SideBySide\config.json && dotnet test tests/SideBySide --configuration Release
echo Executing tests with Compression, SSL && copy /y ".ci\config\config.compression+ssl.json" tests\SideBySide\config.json && dotnet test tests/SideBySide --configuration Release
