$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$generatedDir = Join-Path $scriptRoot 'generated'
$containerName = 'mysqlconnector-skipcrl'
$imageName = 'mysqlconnector-skipcrl-client'

if (-not (Test-Path (Join-Path $generatedDir 'ca-cert.pem')))
{
	throw 'Run .\tests\SkipCrl\setup.ps1 first.'
}

$containerStatus = docker ps --filter "name=^$containerName$" --format '{{.Names}}'
if (@($containerStatus | Where-Object { $_ -eq $containerName }).Count -ne 1)
{
	throw 'MySQL container is not running. Run .\tests\SkipCrl\setup.ps1 first.'
}

docker image rm $imageName 2>$null | Out-Null
docker build --quiet -f (Join-Path $scriptRoot 'Client.Dockerfile') -t $imageName $repoRoot | Out-Null
docker run --rm `
	-e "MYSQL_CONNECTION_STRING=Server=host.docker.internal;Port=3308;User ID=root;Password=pass;Database=mysql;Pooling=false;Connection Timeout=10" `
	$imageName
