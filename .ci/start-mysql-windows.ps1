# Downloads MySQL Server for Windows, runs it as a service on port 3306, and creates the users needed by the integration tests.
param(
	[string] $Version = '8.4.13',
	[string] $InstallPath = (Join-Path ([System.IO.Path]::GetTempPath()) 'mysql')
)

$ErrorActionPreference = 'Stop'

$serverPath = (Join-Path $PSScriptRoot 'server').Replace('\', '/')
$zipName = "mysql-$Version-winx64.zip"
$zipPath = Join-Path $InstallPath $zipName
$series = ($Version.Split('.')[0..1]) -join '.'
New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

# the current release is in Downloads; superseded releases move to archives
foreach ($url in "https://cdn.mysql.com/Downloads/MySQL-$series/$zipName", "https://cdn.mysql.com/archives/mysql-$series/$zipName")
{
	Write-Output "Downloading $url"
	& curl.exe --fail --silent --show-error --location --retry 3 --output $zipPath $url
	if ($LASTEXITCODE -eq 0)
	{
		break
	}
}
if ($LASTEXITCODE -ne 0)
{
	throw "Couldn't download $zipName"
}

$PSNativeCommandUseErrorActionPreference = $true

# use the Windows tar (bsdtar), which can extract .zip files
& "$env:SystemRoot\System32\tar.exe" -xf $zipPath -C $InstallPath

$baseDirectory = (Join-Path $InstallPath "mysql-$Version-winx64").Replace('\', '/')
$dataDirectory = (Join-Path $InstallPath 'data').Replace('\', '/')
$mysqld = "$baseDirectory/bin/mysqld.exe"
$mysql = "$baseDirectory/bin/mysql.exe"
$defaultsFile = Join-Path $InstallPath 'my.ini'
@"
[mysqld]
basedir=$baseDirectory
datadir=$dataDirectory
port=3306
disable-log-bin
local-infile=1
max-connections=250
ssl-ca=$serverPath/certs/ssl-ca-cert.pem
ssl-cert=$serverPath/certs/ssl-server-cert.pem
ssl-key=$serverPath/certs/ssl-server-key.pem
"@ | Set-Content -Path $defaultsFile -Encoding ascii

Write-Output "Initializing MySQL $Version"
& $mysqld "--defaults-file=$defaultsFile" --initialize-insecure --console
& $mysqld --install MySQL "--defaults-file=$defaultsFile"
Start-Service MySQL

$PSNativeCommandUseErrorActionPreference = $false
for ($i = 0; $i -lt 60; $i++)
{
	& $mysql -uroot --skip-password -e 'SELECT 1' 2>&1 | Out-Null
	if ($LASTEXITCODE -eq 0)
	{
		break
	}
	Start-Sleep -Seconds 1
}
$PSNativeCommandUseErrorActionPreference = $true

Write-Output 'Creating test users'
& $mysql -uroot --skip-password -e "ALTER USER 'root'@'localhost' IDENTIFIED BY 'test';"
foreach ($script in 'init.sql', 'init_sha256.sql', 'init_caching_sha2.sql', 'init_query_attributes.sql')
{
	& $mysql -uroot -ptest -e "source $serverPath/$script"
}

Write-Output 'Verifying SSL connection'
& $mysql -ussltest -ptest --ssl-mode=REQUIRED "--ssl-ca=$serverPath/certs/ssl-ca-cert.pem" "--ssl-cert=$serverPath/certs/ssl-client-cert.pem" "--ssl-key=$serverPath/certs/ssl-client-key.pem" -e 'SELECT 1'
