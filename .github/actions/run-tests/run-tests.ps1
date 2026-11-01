param(
	[Parameter(Mandatory)] [string] $Project,
	[Parameter(Mandatory)] [string] $TargetFramework,
	[Parameter(Mandatory)] [string] $Configuration,
	[Parameter(Mandatory)] [string] $Title
)

$ErrorActionPreference = 'Stop'

# xUnit.net v3 test projects are executables: .NET builds are run from the .dll, but .NET Framework builds only produce an .exe
$extension = if ($TargetFramework -match '^net4\d+$') { 'exe' } else { 'dll' }
$testModule = "artifacts/bin/$Project/$($Configuration.ToLowerInvariant())_$TargetFramework/$Project.$extension"
$resultsDirectory = Join-Path $env:RUNNER_TEMP 'TestResults'
$resultsFileName = ($Title -replace '[^\w.-]+', '-').Trim('-') + '.trx'

# global.json opts in to the Microsoft.Testing.Platform mode of 'dotnet test'; arguments after '--' are passed to the test application
dotnet test --test-modules $testModule --results-directory $resultsDirectory -- --report-xunit-trx --report-xunit-trx-filename $resultsFileName
$exitCode = $LASTEXITCODE
if ($exitCode -eq 0)
{
	exit 0
}

# escapes text for a workflow command; see https://github.com/actions/toolkit/blob/main/packages/core/src/command.ts
function Format-CommandValue([string] $value, [switch] $Property)
{
	$value = $value.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
	if ($Property)
	{
		$value = $value.Replace(':', '%3A').Replace(',', '%2C')
	}
	return $value
}

$summary = "### :x: $Title`n`n"
$resultsPath = Join-Path $resultsDirectory $resultsFileName
if (Test-Path $resultsPath)
{
	[xml] $results = Get-Content $resultsPath -Raw
	$counters = $results.TestRun.ResultSummary.Counters
	$summary += "$($counters.passed) passed, $($counters.failed) failed, $([int] $counters.total - [int] $counters.executed) skipped`n`n"

	# GitHub shows at most 10 error annotations per step, and job summaries are limited to 1MiB
	$maxReportedFailures = 10
	$failedTests = @($results.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -eq 'Failed' })
	foreach ($failedTest in $failedTests | Select-Object -First $maxReportedFailures)
	{
		$errorInfo = $failedTest.Output.ErrorInfo
		Write-Output "::error title=$(Format-CommandValue "$Title`: $($failedTest.testName)" -Property)::$(Format-CommandValue $errorInfo.Message)"
		$summary += "<details><summary>$([System.Net.WebUtility]::HtmlEncode($failedTest.testName))</summary>`n`n``````text`n$($errorInfo.Message)`n$($errorInfo.StackTrace)`n```````n</details>`n`n"
	}
	if ($failedTests.Count -gt $maxReportedFailures)
	{
		$summary += "...and $($failedTests.Count - $maxReportedFailures) more; see the log for details.`n"
	}
}
else
{
	$summary += "The test run failed with exit code $exitCode and produced no results.`n"
}
$summary | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8

exit $exitCode
