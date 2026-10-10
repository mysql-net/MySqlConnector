$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$testDirectory = Join-Path $repoRoot 'artifacts\framework-package-test'
$testRunDirectory = Join-Path $testDirectory ([guid]::NewGuid().ToString('N'))
$feedDirectory = Join-Path $testRunDirectory 'feed'
$testProject = 'tests\MySqlConnector.FrameworkPackageTests\MySqlConnector.FrameworkPackageTests.csproj'

Push-Location $repoRoot
try
{
	New-Item -ItemType Directory -Path $feedDirectory -Force | Out-Null

	& dotnet pack src\MySqlConnector\MySqlConnector.csproj --configuration Release --no-restore --no-build --output $feedDirectory
	if ($LASTEXITCODE -ne 0)
	{
		throw "dotnet pack failed with exit code $LASTEXITCODE"
	}

	$packages = @(Get-ChildItem -LiteralPath $feedDirectory -Filter 'MySqlConnector.*.nupkg' -File)
	if ($packages.Count -ne 1)
	{
		throw "Expected one MySqlConnector package in $feedDirectory but found $($packages.Count)"
	}

	$packageVersion = $packages[0].BaseName.Substring('MySqlConnector.'.Length)
	& dotnet restore $testProject --source $feedDirectory --force "-p:MySqlConnectorTestPackageVersion=$packageVersion"
	if ($LASTEXITCODE -ne 0)
	{
		throw "dotnet restore failed with exit code $LASTEXITCODE"
	}

	foreach ($framework in @('net462', 'net48'))
	{
		$redirectOutput = Join-Path $testRunDirectory "$framework\redirects-enabled"
		New-Item -ItemType Directory -Path $redirectOutput -Force | Out-Null

		& dotnet build $testProject --configuration Release --framework $framework --no-restore --output $redirectOutput "-p:AutoGenerateBindingRedirects=true" "-p:MySqlConnectorTestPackageVersion=$packageVersion"
		if ($LASTEXITCODE -ne 0)
		{
			throw "dotnet build with binding redirects enabled failed for $framework with exit code $LASTEXITCODE"
		}

		$configPath = Join-Path $redirectOutput 'MySqlConnector.FrameworkPackageTests.exe.config'
		if (-not (Test-Path -LiteralPath $configPath))
		{
			throw "$framework did not generate $configPath with AutoGenerateBindingRedirects enabled"
		}

		[xml] $config = Get-Content -LiteralPath $configPath -Raw
		$redirects = $config.SelectNodes("//*[local-name()='bindingRedirect']")
		if ($redirects.Count -gt 0)
		{
			throw "$framework generated $($redirects.Count) binding redirect(s) in $configPath"
		}
		Write-Output "$framework generated no binding redirects"

		$noRedirectOutput = Join-Path $testRunDirectory "$framework\redirects-disabled"
		New-Item -ItemType Directory -Path $noRedirectOutput -Force | Out-Null

		& dotnet build $testProject --configuration Release --framework $framework --no-restore --output $noRedirectOutput "-p:AutoGenerateBindingRedirects=false" "-p:MySqlConnectorTestPackageVersion=$packageVersion"
		if ($LASTEXITCODE -ne 0)
		{
			throw "dotnet build with binding redirects disabled failed for $framework with exit code $LASTEXITCODE"
		}

		$executablePath = Join-Path $noRedirectOutput 'MySqlConnector.FrameworkPackageTests.exe'
		& $executablePath
		if ($LASTEXITCODE -ne 0)
		{
			throw "Running the $framework package smoke test failed with exit code $LASTEXITCODE"
		}
		Write-Output "$framework package smoke test passed without binding redirects"
	}
}
finally
{
	Pop-Location
}
