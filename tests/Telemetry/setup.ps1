param(
	[string] $MySqlImage = 'mysql:9.7'
)

$ErrorActionPreference = 'Stop'

$mysqlContainer = 'mysqlconnector-telemetry'
$dashboardContainer = 'aspire-dashboard'
$mysqlRootPassword = 'pass'
$telemetryInitScript = Join-Path $PSScriptRoot 'mysql-telemetry-init.sql'
$telemetryApp = Join-Path $PSScriptRoot 'Telemetry.cs'

function Invoke-MySql
{
	param(
		[string] $ContainerName,
		[string] $RootPassword,
		[string[]] $Arguments
	)

	$mysqlArguments = @(
		$ContainerName,
		'mysql',
		'--protocol=tcp',
		'-h127.0.0.1',
		'-P3306',
		'-uroot',
		"-p$RootPassword"
	) + $Arguments

	docker exec @mysqlArguments
}

function Invoke-MySqlWithInput
{
	param(
		[string] $ContainerName,
		[string] $RootPassword,
		[string] $InputText,
		[string[]] $Arguments
	)

	$mysqlArguments = @(
		$ContainerName,
		'mysql',
		'--protocol=tcp',
		'-h127.0.0.1',
		'-P3306',
		'-uroot',
		"-p$RootPassword"
	) + $Arguments

	$InputText | docker exec -i @mysqlArguments
}

function Wait-ForMySql
{
	param(
		[string] $ContainerName,
		[string] $RootPassword,
		[int] $Attempts = 60
	)

	for ($attempt = 0; $attempt -lt $Attempts; $attempt++)
	{
		Invoke-MySql -ContainerName $ContainerName -RootPassword $RootPassword -Arguments @('-N', '-B', '-e', 'SELECT 1;') *> $null
		if ($LASTEXITCODE -eq 0)
		{
			return
		}

		Start-Sleep -Seconds 1
	}

	throw "MySQL in container '$ContainerName' did not become ready within $Attempts seconds."
}

docker version | Out-Null

foreach ($container in @($mysqlContainer, $dashboardContainer))
{
	docker rm -f $container 2>$null | Out-Null
}

docker run --rm -d --pull always `
	-p 18888:18888 `
	-p 4318:18890 `
	--name $dashboardContainer `
	mcr.microsoft.com/dotnet/aspire-dashboard:latest | Out-Null

$dashboardLoginUrl = $null
for ($attempt = 0; $attempt -lt 30; $attempt++)
{
	$dashboardLogs = docker logs $dashboardContainer 2>&1 | Out-String
	if ($dashboardLogs -match 'Login to the dashboard at (?<DashboardUrl>https?://\S+)')
	{
		$dashboardLoginUrl = $Matches.DashboardUrl.TrimEnd('.')
		break
	}

	Start-Sleep -Seconds 1
}

docker run --rm -d --pull always `
	--name $mysqlContainer `
	-e MYSQL_ROOT_PASSWORD=$mysqlRootPassword `
	-p 3306:3306 `
	$MySqlImage `
	--max-allowed-packet=96M `
	--character-set-server=utf8mb4 `
	--disable-log-bin `
	--local-infile=1 `
	--max-connections=250 | Out-Null

Wait-ForMySql -ContainerName $mysqlContainer -RootPassword $mysqlRootPassword

$telemetryInitSql = Get-Content -Raw $telemetryInitScript
Invoke-MySqlWithInput -ContainerName $mysqlContainer -RootPassword $mysqlRootPassword -InputText $telemetryInitSql -Arguments @() | Out-Null
if ($LASTEXITCODE -ne 0)
{
	throw 'Failed to apply MySQL telemetry initialization SQL.'
}

docker restart $mysqlContainer | Out-Null
Wait-ForMySql -ContainerName $mysqlContainer -RootPassword $mysqlRootPassword

$installedComponentCount = Invoke-MySql -ContainerName $mysqlContainer -RootPassword $mysqlRootPassword -Arguments @('-N', '-B', '-e', "SELECT COUNT(*) FROM mysql.component WHERE component_urn IN ('file://component_query_attributes', 'file://component_telemetry');")
if ($LASTEXITCODE -ne 0)
{
	throw 'Failed to verify MySQL component installation.'
}

if ([int] $installedComponentCount -ne 2)
{
	throw 'Expected both component_query_attributes and component_telemetry to be installed.'
}

$telemetryVariableCount = Invoke-MySql -ContainerName $mysqlContainer -RootPassword $mysqlRootPassword -Arguments @('-N', '-B', '-e', "SELECT COUNT(*) FROM performance_schema.global_variables WHERE VARIABLE_NAME LIKE 'telemetry.%';")
if ($LASTEXITCODE -ne 0)
{
	throw 'Failed to verify MySQL telemetry variables.'
}

if ([int] $telemetryVariableCount -le 0)
{
	throw 'MySQL telemetry variables were not found after initialization.'
}

$telemetryConfigurationCount = Invoke-MySql -ContainerName $mysqlContainer -RootPassword $mysqlRootPassword -Arguments @('-N', '-B', '-e', "SELECT COUNT(*) FROM performance_schema.global_variables WHERE (VARIABLE_NAME = 'telemetry.otel_exporter_otlp_traces_endpoint' AND VARIABLE_VALUE = 'http://host.docker.internal:4318/v1/traces') OR (VARIABLE_NAME = 'telemetry.otel_exporter_otlp_traces_protocol' AND VARIABLE_VALUE = 'http/protobuf') OR (VARIABLE_NAME = 'telemetry.otel_resource_attributes' AND VARIABLE_VALUE = 'service.name=mysql-telemetry-demo');")
if ($LASTEXITCODE -ne 0)
{
	throw 'Failed to verify MySQL telemetry OTLP configuration.'
}

if ([int] $telemetryConfigurationCount -ne 3)
{
	throw 'MySQL telemetry OTLP configuration was not applied after restart.'
}

$telemetryEnabledCount = Invoke-MySql -ContainerName $mysqlContainer -RootPassword $mysqlRootPassword -Arguments @('-N', '-B', '-e', "SELECT COUNT(*) FROM performance_schema.global_variables WHERE (VARIABLE_NAME = 'telemetry.trace_enabled' AND VARIABLE_VALUE = 'ON') OR (VARIABLE_NAME = 'telemetry.query_text_enabled' AND VARIABLE_VALUE = 'ON');")
if ($LASTEXITCODE -ne 0)
{
	throw 'Failed to verify MySQL telemetry enable flags.'
}

if ([int] $telemetryEnabledCount -ne 2)
{
	throw 'MySQL telemetry tracing flags were not enabled after restart.'
}

$databaseCount = Invoke-MySql -ContainerName $mysqlContainer -RootPassword $mysqlRootPassword -Arguments @('-N', '-B', '-e', "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = 'telemetry_demo';")
if ($LASTEXITCODE -ne 0)
{
	throw 'Failed to verify the telemetry_demo database.'
}

if ([int] $databaseCount -ne 1)
{
	throw 'The telemetry_demo database was not created.'
}

Invoke-MySql -ContainerName $mysqlContainer -RootPassword $mysqlRootPassword -Arguments @('-D', 'telemetry_demo', '-e', "SELECT mysql_query_attribute_string('traceparent');") | Out-Null
if ($LASTEXITCODE -ne 0)
{
	throw 'mysql_query_attribute_string is not available after initialization.'
}

Write-Host ''
Write-Host 'Telemetry environment is ready.'
if ($dashboardLoginUrl)
{
	Write-Host "Aspire Dashboard login: $dashboardLoginUrl"
}
else
{
	Write-Warning 'Could not extract the Aspire Dashboard login URL from container logs.'
	Write-Host 'Aspire Dashboard UI: http://localhost:18888'
}
Write-Host 'Aspire OTLP endpoint: http://localhost:4318'
Write-Host 'MySQL connection string: Server=127.0.0.1;Port=3306;User ID=root;Password=pass;Database=telemetry_demo;'
Write-Host 'Run: dotnet .\tests\Telemetry\Telemetry.cs'
