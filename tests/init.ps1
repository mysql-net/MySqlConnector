# Starts a MySQL Server (or MariaDB) container for the integration tests (see README.md) and creates the users, schemas, and components they use.
# Uses wslc if it's installed, otherwise Docker. An existing container with the same name is replaced.
param(
	[string] $Image = 'mysql:26.10',
	[string] $Name = 'mysqlconnector',
	[int] $Port = 3306
)

$ErrorActionPreference = 'Stop'

if (Get-Command wslc -ErrorAction SilentlyContinue)
{
	$containerCli = 'wslc'
}
elseif (Get-Command docker -ErrorAction SilentlyContinue)
{
	$containerCli = 'docker'
}
else
{
	throw 'Neither wslc nor docker was found.'
}

$rootPassword = 'pass'
$isMariaDb = $Image -match 'mariadb'
$mysql = $isMariaDb ? 'mariadb' : 'mysql'

# the CI init scripts and TLS certificates are mounted into the container
$serverPath = (Resolve-Path (Join-Path $PSScriptRoot '../.ci/server')).Path
$serverMount = '/etc/mysqlconnector'

$serverArguments = @(
	'--max-allowed-packet=96M',
	'--character-set-server=utf8mb4',
	'--disable-log-bin',
	'--local-infile=1',
	'--max-connections=250',
	"--ssl-ca=$serverMount/certs/ssl-ca-cert.pem",
	"--ssl-cert=$serverMount/certs/ssl-server-cert.pem",
	"--ssl-key=$serverMount/certs/ssl-server-key.pem"
)
if ($isMariaDb)
{
	$serverArguments += '--in-predicate-conversion-threshold=100000', '--plugin-maturity=beta'
}

& $containerCli rm -f $Name 2>$null | Out-Null

$PSNativeCommandUseErrorActionPreference = $true

Write-Output "Starting $Image as $containerCli container '$Name' on port $Port"
& $containerCli run -d --rm --pull always --name $Name -e MYSQL_ROOT_PASSWORD=$rootPassword -p "${Port}:3306" --tmpfs /var/lib/mysql -v "${serverPath}:${serverMount}:ro" $Image @serverArguments | Out-Null

# the image's entrypoint initializes the database with a temporary server that doesn't listen on TCP, so wait for a TCP connection to succeed
$PSNativeCommandUseErrorActionPreference = $false
for ($i = 0; $i -lt 60; $i++)
{
	& $containerCli exec -e MYSQL_PWD=$rootPassword $Name $mysql --protocol=tcp -h 127.0.0.1 -uroot -e 'SELECT 1' 2>&1 | Out-Null
	if ($LASTEXITCODE -eq 0)
	{
		break
	}
	Start-Sleep -Seconds 1
}
if ($LASTEXITCODE -ne 0)
{
	throw "The server in container '$Name' didn't start; run '$containerCli logs $Name' for details."
}
$PSNativeCommandUseErrorActionPreference = $true

if ($isMariaDb)
{
	$initScripts = @('init.sql', 'init_ed25519.sql')

	# the parsec plugin was added in MariaDB 11.6
	$hasParsec = & $containerCli exec -e MYSQL_PWD=$rootPassword $Name $mysql -uroot -N -e "SELECT COUNT(*) FROM information_schema.all_plugins WHERE plugin_name = 'parsec';"
	if ($hasParsec -eq '1')
	{
		$initScripts += 'init_parsec.sql'
	}
	else
	{
		Write-Warning "$Image doesn't have the parsec plugin; add ParsecAuthentication to Data.UnsupportedFeatures."
	}
}
else
{
	$initScripts = @('init.sql', 'init_sha256.sql', 'init_caching_sha2.sql', 'init_query_attributes.sql')
}

foreach ($script in $initScripts)
{
	Write-Output "Running $script"
	& $containerCli exec -e MYSQL_PWD=$rootPassword $Name bash -c "$mysql -uroot < $serverMount/$script"
}

# the server certificate is issued to 'localhost', so connect by that name to verify it
Write-Output 'Verifying SSL connection'
$sslModeArguments = $isMariaDb ? @('--ssl', '--ssl-verify-server-cert') : @('--ssl-mode=VERIFY_IDENTITY')
& $containerCli exec -e MYSQL_PWD=test $Name $mysql --protocol=tcp -h localhost -ussltest @sslModeArguments "--ssl-ca=$serverMount/certs/ssl-ca-cert.pem" "--ssl-cert=$serverMount/certs/ssl-client-cert.pem" "--ssl-key=$serverMount/certs/ssl-client-key.pem" -e 'SELECT 1' | Out-Null

Write-Output "$Image is ready: server=127.0.0.1;port=$Port;user id=root;password=$rootPassword"
