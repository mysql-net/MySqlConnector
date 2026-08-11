$ErrorActionPreference = 'Stop'

foreach ($container in @('mysqlconnector-telemetry', 'aspire-dashboard'))
{
	$existingContainers = docker container ls -a --filter "name=^$container$" --format '{{.Names}}'
	if (@($existingContainers | Where-Object { $_ -eq $container }).Count -eq 1)
	{
		docker rm -f $container | Out-Null
	}
}

Write-Host 'Telemetry containers stopped.'
