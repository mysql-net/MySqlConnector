$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$generatedDir = Join-Path $scriptRoot 'generated'
$containerName = 'mysqlconnector-skipcrl'
$imageName = 'mysqlconnector-skipcrl-server'
$port = 3308

function Get-OpenSslPath
{
	$command = Get-Command openssl -ErrorAction SilentlyContinue
	if ($command)
	{
		return $command.Source
	}

	$gitOpenSsl = 'C:\Program Files\Git\usr\bin\openssl.exe'
	if (Test-Path $gitOpenSsl)
	{
		return $gitOpenSsl
	}

	throw 'OpenSSL was not found. Install it or use Git for Windows.'
}

function Wait-ForMySql
{
	param(
		[string] $ContainerName,
		[int] $Attempts = 60
	)

	for ($attempt = 0; $attempt -lt $Attempts; $attempt++)
	{
		$logs = docker logs $ContainerName 2>&1 | Out-String
		if ($logs -match 'Channel mysql_main configured to support TLS' -and
			$logs -match 'ready for connections')
		{
			return
		}

		Start-Sleep -Seconds 1
	}

	throw "MySQL container '$ContainerName' did not become ready."
}

$openSsl = Get-OpenSslPath

New-Item -ItemType Directory -Force -Path $generatedDir | Out-Null
Remove-Item -Force `
	(Join-Path $generatedDir 'ca-key.pem'), `
	(Join-Path $generatedDir 'ca-cert.pem'), `
	(Join-Path $generatedDir 'ca-cert.srl'), `
	(Join-Path $generatedDir 'server-key.pem'), `
	(Join-Path $generatedDir 'server.csr'), `
	(Join-Path $generatedDir 'server-cert.pem') `
	-ErrorAction SilentlyContinue

& $openSsl genrsa -out (Join-Path $generatedDir 'ca-key.pem') 4096
& $openSsl req -x509 -new -nodes `
	-key (Join-Path $generatedDir 'ca-key.pem') `
	-sha256 -days 3650 `
	-out (Join-Path $generatedDir 'ca-cert.pem') `
	-subj '/C=US/ST=WA/L=Seattle/O=MySqlConnector/CN=MySqlConnector Skip CRL Test CA'
& $openSsl genrsa -out (Join-Path $generatedDir 'server-key.pem') 2048
& $openSsl req -new `
	-key (Join-Path $generatedDir 'server-key.pem') `
	-out (Join-Path $generatedDir 'server.csr') `
	-config (Join-Path $scriptRoot 'server-cert.cnf')
& $openSsl x509 -req `
	-in (Join-Path $generatedDir 'server.csr') `
	-CA (Join-Path $generatedDir 'ca-cert.pem') `
	-CAkey (Join-Path $generatedDir 'ca-key.pem') `
	-CAcreateserial `
	-out (Join-Path $generatedDir 'server-cert.pem') `
	-days 365 `
	-sha256 `
	-extensions v3_req `
	-extfile (Join-Path $scriptRoot 'server-cert.cnf')

& $openSsl verify `
	-CAfile (Join-Path $generatedDir 'ca-cert.pem') `
	(Join-Path $generatedDir 'server-cert.pem')

docker rm -f $containerName 2>$null | Out-Null
docker image rm $imageName 2>$null | Out-Null

docker build --quiet -f (Join-Path $scriptRoot 'MySql.Dockerfile') -t $imageName $scriptRoot | Out-Null
docker run --rm -d `
	--name $containerName `
	-e MYSQL_ROOT_PASSWORD=pass `
	-e MYSQL_ROOT_HOST=% `
	-p "${port}:3306" `
	$imageName | Out-Null

Wait-ForMySql -ContainerName $containerName

Write-Host ''
Write-Host "Skip CRL server is ready on port $port."
Write-Host "Next: .\\tests\\SkipCrl\\repro.ps1"
