<#
.SYNOPSIS
  Builds and runs the Space Missions MCP server and Chatbot projects.

.DESCRIPTION
  By default, builds both projects and starts the Chatbot. The MCP server uses stdio
  and is normally spawned by the Chatbot when a user message arrives (see
  SpaceMissionsAgent:SpaceMissionsMcp in src/Chatbot/appsettings.json).

  Use -McpServerOnly to run the MCP server alone (e.g. with MCP Inspector).
  Use -SeparateWindows to build once, then open two consoles (MCP + Chatbot).

.PARAMETER Configuration
  dotnet build/run configuration (Debug or Release).

.PARAMETER AspNetCoreEnvironment
  Chatbot ASP.NET Core environment (Playground uses the M365 Agents Playground profile).

.PARAMETER BuildOnly
  Build both projects and exit.

.PARAMETER NoBuild
  Skip dotnet build.

.PARAMETER McpServerOnly
  Run only SpaceMissions.McpServer (stdio; blocks until the MCP client disconnects).

.PARAMETER ChatbotOnly
  Run only Chatbot (default when neither -McpServerOnly nor -SeparateWindows is set).

.PARAMETER SeparateWindows
  Build both projects, then start MCP server and Chatbot in separate PowerShell windows.

.EXAMPLE
  .\devTools\run-space-missions-chatbot.ps1

.EXAMPLE
  .\devTools\run-space-missions-chatbot.ps1 -SeparateWindows

.EXAMPLE
  .\devTools\run-space-missions-chatbot.ps1 -McpServerOnly
#>
[CmdletBinding(DefaultParameterSetName = 'Chatbot')]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [ValidateSet('Playground', 'Development')]
    [string] $AspNetCoreEnvironment = 'Playground',

    [switch] $BuildOnly,
    [switch] $NoBuild,
    [switch] $McpServerOnly,
    [switch] $SeparateWindows
)

$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$McpProject = Join-Path $RepoRoot 'src\SpaceMissions.McpServer\SpaceMissions.McpServer.csproj'
$ChatbotProject = Join-Path $RepoRoot 'src\Chatbot\Chatbot.csproj'
$DatasetPath = Join-Path $RepoRoot 'dataset\space_missions.csv'

foreach ($path in @($McpProject, $ChatbotProject, $DatasetPath)) {
    if (-not (Test-Path $path)) {
        throw "Required path not found: $path"
    }
}

$env:SPACE_MISSIONS_DATASET_PATH = $DatasetPath

function Invoke-RepoBuild {
    param([string[]] $Projects)

    foreach ($project in $Projects) {
        Write-Host ""
        Write-Host "Building $project ($Configuration)..." -ForegroundColor Cyan
        dotnet build $project -c $Configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed: $project"
        }
    }
}

function Get-ChatbotLaunchProfile {
    if ($AspNetCoreEnvironment -eq 'Playground') {
        return 'Microsoft 365 Agents Playground'
    }

    return 'Start Project'
}

function Start-McpServerProcess {
    param(
        [switch] $NewWindow
    )

    $arguments = @(
        '-NoExit',
        '-Command',
        @"
`$env:SPACE_MISSIONS_DATASET_PATH = '$DatasetPath'
Set-Location '$RepoRoot'
Write-Host 'SpaceMissions.McpServer (stdio) — waiting for MCP client on stdin/stdout' -ForegroundColor Yellow
dotnet run --project '$McpProject' -c $Configuration --no-launch-profile
"@
    )

    if ($NewWindow) {
        Start-Process -FilePath 'pwsh' -ArgumentList $arguments -WorkingDirectory $RepoRoot | Out-Null
        return
    }

    Set-Location $RepoRoot
    Write-Host ""
    Write-Host "Starting SpaceMissions.McpServer (stdio)..." -ForegroundColor Green
    dotnet run --project $McpProject -c $Configuration --no-launch-profile
}

function Start-ChatbotProcess {
    param(
        [switch] $NewWindow
    )

    $launchProfile = Get-ChatbotLaunchProfile
    $arguments = @(
        '-NoExit',
        '-Command',
        @"
`$env:SPACE_MISSIONS_DATASET_PATH = '$DatasetPath'
`$env:ASPNETCORE_ENVIRONMENT = '$AspNetCoreEnvironment'
Set-Location '$RepoRoot'
Write-Host 'Chatbot — http://localhost:5130 ($AspNetCoreEnvironment)' -ForegroundColor Green
dotnet run --project '$ChatbotProject' -c $Configuration --launch-profile '$launchProfile'
"@
    )

    if ($NewWindow) {
        Start-Process -FilePath 'pwsh' -ArgumentList $arguments -WorkingDirectory $RepoRoot | Out-Null
        return
    }

    Set-Location $RepoRoot
    Write-Host ""
    Write-Host "Starting Chatbot ($AspNetCoreEnvironment) at http://localhost:5130 ..." -ForegroundColor Green
    Write-Host "MCP server project: $McpProject" -ForegroundColor DarkGray
    $env:ASPNETCORE_ENVIRONMENT = $AspNetCoreEnvironment
    dotnet run --project $ChatbotProject -c $Configuration --launch-profile $launchProfile
}

Write-Host "Repository root: $RepoRoot" -ForegroundColor DarkGray
Write-Host "Dataset:         $DatasetPath" -ForegroundColor DarkGray

if (-not $NoBuild) {
    Invoke-RepoBuild -Projects @($McpProject, $ChatbotProject)
}

if ($BuildOnly) {
    Write-Host ""
    Write-Host "Build complete." -ForegroundColor Green
    return
}

if ($McpServerOnly) {
    Start-McpServerProcess
    return
}

if ($SeparateWindows) {
    Write-Host ""
    Write-Host "Launching MCP server and Chatbot in separate windows..." -ForegroundColor Cyan
    Start-McpServerProcess -NewWindow
    Start-Sleep -Seconds 1
    Start-ChatbotProcess -NewWindow
    Write-Host "Both processes started. Close their windows to stop them." -ForegroundColor Green
    return
}

Start-ChatbotProcess
