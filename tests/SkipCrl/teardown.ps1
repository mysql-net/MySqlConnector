$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$generatedDir = Join-Path $scriptRoot 'generated'

docker rm -f mysqlconnector-skipcrl 2>$null | Out-Null
docker image rm mysqlconnector-skipcrl-server mysqlconnector-skipcrl-client 2>$null | Out-Null

if (Test-Path $generatedDir)
{
	Remove-Item -Recurse -Force $generatedDir
}

Write-Host 'Skip CRL repro cleaned up.'
