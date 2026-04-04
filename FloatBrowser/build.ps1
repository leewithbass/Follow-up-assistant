[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$NoRestore,

    [switch]$KillRunning,

    [string]$StageRoot = "$env:TEMP\FloatBrowser-build"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Join-Path $scriptDir "FloatBrowser.sln"
$projectPath = Join-Path $scriptDir "src\FloatBrowser.App\FloatBrowser.App.csproj"
$projectName = "FloatBrowser.App"
$buildRoot = $scriptDir
$buildSolutionPath = $solutionPath
$artifactRoot = Join-Path (Split-Path -Parent $scriptDir) "dist"

if (-not (Test-Path $solutionPath)) {
    throw "Solution file not found: $solutionPath"
}

if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

function Use-StagingBuild([string]$Path) {
    return $Path.StartsWith("\\")
}

Write-Host "Building $projectName" -ForegroundColor Cyan
Write-Host "Solution: $solutionPath"
Write-Host "Configuration: $Configuration"

if ($KillRunning) {
    $runningProcess = Get-Process $projectName -ErrorAction SilentlyContinue
    if ($null -ne $runningProcess) {
        Write-Host "Stopping running process: $projectName" -ForegroundColor Yellow
        $runningProcess | Stop-Process -Force
    }
}

if (Use-StagingBuild $scriptDir) {
    $buildRoot = Join-Path $StageRoot "workspace"
    $buildSolutionPath = Join-Path $buildRoot "FloatBrowser.sln"

    Write-Host "Source is on a UNC path. Staging to local Windows folder..." -ForegroundColor Yellow
    Write-Host "Stage: $buildRoot"

    if (Test-Path $buildRoot) {
        Remove-Item $buildRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $buildRoot | Out-Null

    & robocopy $scriptDir $buildRoot /MIR /XD .git bin obj .vs WebView2UserData /XF "*.user" | Out-Null
    $robocopyExitCode = $LASTEXITCODE
    if ($robocopyExitCode -ge 8) {
        throw "robocopy staging failed with exit code $robocopyExitCode"
    }
}

Push-Location $buildRoot
try {
    if (-not $NoRestore) {
        Write-Host "Restoring packages..." -ForegroundColor Cyan
        dotnet restore $buildSolutionPath
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore failed with exit code $LASTEXITCODE"
        }
    }

    Write-Host "Building solution..." -ForegroundColor Cyan
    dotnet build $buildSolutionPath -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }

    $outputPath = Join-Path $buildRoot "src\FloatBrowser.App\bin\$Configuration\net8.0-windows10.0.19041.0"
    $projectXml = [xml](Get-Content $projectPath)
    $version = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "Could not read project version from $projectPath"
    }

    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
    $artifactPath = Join-Path $artifactRoot "FloatBrowser-v$version-win-x64.zip"
    if (Test-Path $artifactPath) {
        Remove-Item $artifactPath -Force
    }
    Compress-Archive -Path (Join-Path $outputPath "*") -DestinationPath $artifactPath

    Write-Host "Build succeeded." -ForegroundColor Green
    Write-Host "Output: $outputPath"
    Write-Host "Package: $artifactPath"

    if (Use-StagingBuild $scriptDir) {
        Write-Host "This build was produced from a staged Windows-local copy." -ForegroundColor Yellow
    }
}
finally {
    Pop-Location
}
