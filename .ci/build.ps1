$ErrorActionPreference = "Stop"

function DownloadWithRetry([string] $url, [string] $downloadLocation, [int] $retries)
{
    while($true)
    {
        try
        {
            Invoke-WebRequest $url -OutFile $downloadLocation
            break
        }
        catch
        {
            $exceptionMessage = $_.Exception.Message
            Write-Host "Failed to download '$url': $exceptionMessage"
            if ($retries -gt 0) {
                $retries--
                Write-Host "Waiting 10 seconds before retrying. Retries left: $retries"
                Start-Sleep -Seconds 10

            }
            else
            {
                $exception = $_.Exception
                throw $exception
            }
        }
    }
}

$repoFolder = Join-Path $PSScriptRoot ..
$env:REPO_FOLDER = $repoFolder

$koreBuildZip="https://github.com/aspnet/KoreBuild/archive/rel/2.0.0-preview2.zip"
if ($env:KOREBUILD_ZIP)
{
    $koreBuildZip=$env:KOREBUILD_ZIP
}

$buildFolder = ".build"

if (!(Test-Path $buildFolder)) {
    Write-Host "Downloading KoreBuild from $koreBuildZip"

    $tempFolder=$env:TEMP + "\KoreBuild-" + [guid]::NewGuid()
    New-Item -Path "$tempFolder" -Type directory | Out-Null

    $localZipFile="$tempFolder\korebuild.zip"

    DownloadWithRetry -url $koreBuildZip -downloadLocation $localZipFile -retries 6

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($localZipFile, $tempFolder)

    New-Item -Path "$buildFolder" -Type directory | Out-Null
    copy-item "$tempFolder\**\build\*" $buildFolder -Recurse

    # Cleanup
    if (Test-Path $tempFolder) {
        Remove-Item -Recurse -Force $tempFolder
    }
}

$dotnetArch = 'x64'
$dotnetChannel = "preview"
$dotnetVersion = "2.0.0-preview2-006497"

$dotnetLocalInstallFolder = $env:DOTNET_INSTALL_DIR
if (!$dotnetLocalInstallFolder)
{
    $dotnetLocalInstallFolder = "$env:LOCALAPPDATA\Microsoft\dotnet\"
}

function InstallSharedRuntime([string] $version, [string] $channel)
{
    $sharedRuntimePath = [IO.Path]::Combine($dotnetLocalInstallFolder, 'shared', 'Microsoft.NETCore.App', $version)
    # Avoid redownloading the CLI if it's already installed.
    if (!(Test-Path $sharedRuntimePath))
    {
        & "$buildFolder\dotnet\dotnet-install.ps1" -Channel $channel `
            -SharedRuntime `
            -Version $version `
            -Architecture $dotnetArch `
            -InstallDir $dotnetLocalInstallFolder
    }
}

& "$buildFolder\dotnet\dotnet-install.ps1" -Channel $dotnetChannel -Version $dotnetVersion -Architecture $dotnetArch
InstallSharedRuntime -version "1.1.2" -channel "release/1.1.0"

dotnet --info
