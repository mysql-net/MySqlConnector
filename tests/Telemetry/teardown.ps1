$ErrorActionPreference = 'Stop'

foreach ($container in @('mysqlconnector-telemetry', 'aspire-dashboard'))
{
	docker rm -f $container 2>$null | Out-Null
}

Write-Host 'Telemetry containers stopped.'
