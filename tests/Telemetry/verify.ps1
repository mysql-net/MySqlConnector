param(
	[int] $TimeoutSeconds = 30,
	[int] $PollIntervalMilliseconds = 500
)

$ErrorActionPreference = 'Stop'

$mysqlContainer = 'mysqlconnector-telemetry'
$dashboardContainer = 'aspire-dashboard'
$dashboardTelemetryApi = 'http://localhost:18888/api/telemetry'
$setupScript = Join-Path $PSScriptRoot 'setup.ps1'
$telemetryApp = Join-Path $PSScriptRoot 'Telemetry.cs'

function Test-ContainerRunning
{
	param(
		[string] $ContainerName
	)

	$isRunning = docker inspect --format '{{.State.Running}}' $ContainerName 2>$null
	return $LASTEXITCODE -eq 0 -and $isRunning.Trim() -eq 'true'
}

function Test-TelemetryApiAvailable
{
	try
	{
		$response = Invoke-WebRequest -Uri "$dashboardTelemetryApi/resources" -UseBasicParsing
		$response.Content | ConvertFrom-Json | Out-Null
		return $true
	}
	catch
	{
		return $false
	}
}

function Get-RequiredMatch
{
	param(
		[string] $Text,
		[string] $Pattern,
		[string] $Description
	)

	$normalizedText = $Text -replace "\r\n?", "`n"
	$match = [regex]::Match($normalizedText, $Pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
	if (-not $match.Success)
	{
		throw "Could not find $Description in Telemetry.cs output."
	}
	return $match
}

function Get-AttributeStringValue
{
	param(
		[object[]] $Attributes,
		[string] $Key
	)

	$attribute = $Attributes | Where-Object { $_.key -eq $Key } | Select-Object -First 1
	if ($null -eq $attribute)
	{
		return $null
	}

	return $attribute.value.stringValue
}

function Get-TraceSpanRecords
{
	param(
		[object] $TraceResponse
	)

	$spans = [System.Collections.Generic.List[object]]::new()
	foreach ($resourceSpan in $TraceResponse.data.resourceSpans)
	{
		$serviceName = Get-AttributeStringValue -Attributes $resourceSpan.resource.attributes -Key 'service.name'
		foreach ($scopeSpan in $resourceSpan.scopeSpans)
		{
			foreach ($span in $scopeSpan.spans)
			{
				$spans.Add([pscustomobject]@{
					ServiceName = $serviceName
					ScopeName = $scopeSpan.scope.name
					Name = $span.name
					SpanId = $span.spanId
					ParentSpanId = $span.parentSpanId
					TraceId = $span.traceId
				})
			}
		}
	}

	return $spans
}

function Test-TraceGraph
{
	param(
		[object] $TraceResponse,
		[string] $TraceId,
		[string[]] $ClientSpanIds
	)

	$spans = Get-TraceSpanRecords -TraceResponse $TraceResponse
	$issues = [System.Collections.Generic.List[string]]::new()

	$rootSpans = @($spans | Where-Object {
		$_.TraceId -eq $TraceId -and
		$_.ServiceName -eq 'MySqlConnector.TelemetrySample' -and
		$_.Name -eq 'TelemetryScenario' -and
		[string]::IsNullOrEmpty($_.ParentSpanId)
	})
	if ($rootSpans.Count -ne 1)
	{
		$issues.Add("Expected 1 TelemetryScenario root span but found $($rootSpans.Count).")
	}

	foreach ($clientSpanId in $ClientSpanIds)
	{
		$clientSpans = @($spans | Where-Object {
			$_.TraceId -eq $TraceId -and
			$_.ServiceName -eq 'MySqlConnector.TelemetrySample' -and
			$_.Name -eq 'Execute' -and
			$_.SpanId -eq $clientSpanId
		})
		if ($clientSpans.Count -ne 1)
		{
			$issues.Add("Expected 1 MySqlConnector Execute span with SpanId '$clientSpanId' but found $($clientSpans.Count).")
			continue
		}

		$mysqlChildren = @($spans | Where-Object {
			$_.TraceId -eq $TraceId -and
			$_.ServiceName -eq 'mysql-telemetry-demo' -and
			$_.Name -eq 'stmt' -and
			$_.ParentSpanId -eq $clientSpanId
		})
		if ($mysqlChildren.Count -lt 1)
		{
			$issues.Add("Expected a MySQL stmt child span for Execute span '$clientSpanId'.")
		}
	}

	return [pscustomobject]@{
		Passed = $issues.Count -eq 0
		Issues = $issues
		Spans = $spans
	}
}

$runStartedAt = Get-Date
Write-Host "Verify run started: $($runStartedAt.ToString('O'))"

if (-not (Test-ContainerRunning -ContainerName $mysqlContainer) -or -not (Test-ContainerRunning -ContainerName $dashboardContainer))
{
	Write-Host 'Starting telemetry environment...'
	& $setupScript
}
elseif (-not (Test-TelemetryApiAvailable))
{
	throw "Aspire telemetry API is unavailable. Restart the telemetry environment with '.\tests\Telemetry\setup.ps1'."
}
else
{
	Write-Host 'Reusing running telemetry environment.'
}

$telemetryOutput = & dotnet $telemetryApp 2>&1 | ForEach-Object { $_.ToString() }
$telemetryOutput | ForEach-Object { Write-Host $_ }
if ($LASTEXITCODE -ne 0)
{
	throw "Telemetry.cs exited with code $LASTEXITCODE."
}

$telemetryText = [string]::Join([Environment]::NewLine, $telemetryOutput)
$traceId = (Get-RequiredMatch -Text $telemetryText -Pattern '^TRACE_ID=(?<TraceId>[0-9a-f]{32}|<null>)$' -Description 'TRACE_ID').Groups['TraceId'].Value
if ($traceId -eq '<null>')
{
	throw 'Telemetry.cs did not produce a trace ID.'
}

$comQueryMatch = Get-RequiredMatch -Text $telemetryText -Pattern '^COM_QUERY traceparent: 00-(?<TraceId>[0-9a-f]{32})-(?<SpanId>[0-9a-f]{16})-01$' -Description 'COM_QUERY traceparent'
$preparedMatch = Get-RequiredMatch -Text $telemetryText -Pattern '^COM_STMT_EXECUTE traceparent: 00-(?<TraceId>[0-9a-f]{32})-(?<SpanId>[0-9a-f]{16})-01$' -Description 'COM_STMT_EXECUTE traceparent'
if ($comQueryMatch.Groups['TraceId'].Value -ne $traceId -or $preparedMatch.Groups['TraceId'].Value -ne $traceId)
{
	throw "Telemetry.cs printed mismatched trace IDs. Expected '$traceId'."
}

$clientSpanIds = @(
	$comQueryMatch.Groups['SpanId'].Value
	$preparedMatch.Groups['SpanId'].Value
)

$deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
$lastResult = $null
do
{
	try
	{
		$traceResponse = Invoke-RestMethod -Uri "$dashboardTelemetryApi/traces/$traceId" -UseBasicParsing
		$lastResult = Test-TraceGraph -TraceResponse $traceResponse -TraceId $traceId -ClientSpanIds $clientSpanIds
		if ($lastResult.Passed)
		{
			break
		}
	}
	catch
	{
		$lastResult = $null
	}

	Start-Sleep -Milliseconds $PollIntervalMilliseconds
}
while ([DateTimeOffset]::UtcNow -lt $deadline)

if ($null -eq $lastResult -or -not $lastResult.Passed)
{
	$message = if ($null -eq $lastResult)
	{
		"Trace '$traceId' was not returned by the Aspire telemetry API within $TimeoutSeconds seconds."
	}
	else
	{
		$issueText = $lastResult.Issues -join [Environment]::NewLine
		$spanTable = $lastResult.Spans | Sort-Object ServiceName, Name, SpanId | Format-Table -AutoSize | Out-String -Width 220
		"$issueText`n$spanTable"
	}

	throw "Telemetry verification failed.`n$message"
}

Write-Host ''
Write-Host "Verified trace $traceId."
foreach ($clientSpanId in $clientSpanIds)
{
	$mysqlChildCount = @($lastResult.Spans | Where-Object {
		$_.TraceId -eq $traceId -and
		$_.ServiceName -eq 'mysql-telemetry-demo' -and
		$_.Name -eq 'stmt' -and
		$_.ParentSpanId -eq $clientSpanId
	}).Count
	Write-Host "Execute span $clientSpanId has $mysqlChildCount MySQL child span(s)."
}
Write-Host ''
Write-Host ($lastResult.Spans | Where-Object { $_.TraceId -eq $traceId } | Sort-Object ServiceName, Name, SpanId | Format-Table -AutoSize | Out-String -Width 220)
