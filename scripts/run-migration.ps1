<#
PowerShell script to build and run the MigrationHelper tool.
Usage:
  Set environment variable MIGRATION_CONNECTION_STRING or pass -ConnectionString.
  .\scripts\run-migration.ps1 -ConnectionString "Server=...;Database=...;User Id=...;Password=...;"
#>
param(
    [string]$Configuration = 'Release',
    [string]$ConnectionString = $env:MIGRATION_CONNECTION_STRING
)

$projectPath = Join-Path -Path $PSScriptRoot -ChildPath '..\RideHailingApi\MigrationTool\MigrationTool.csproj'
$projectPath = (Resolve-Path $projectPath).ProviderPath

if (-not $ConnectionString) {
    Write-Host "No connection string passed and MIGRATION_CONNECTION_STRING not set. You will be prompted to enter it during runtime." -ForegroundColor Yellow
}
else {
    Write-Host "Using connection string from parameter or env var." -ForegroundColor Green
}

Write-Host "Building MigrationTool project..."
$build = dotnet build `"$projectPath`" -c $Configuration
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed."; exit $LASTEXITCODE }

$exe = Join-Path -Path (Split-Path $projectPath -Parent) -ChildPath "bin\$Configuration\net10.0\MigrationTool.dll"
if (-not (Test-Path $exe)) {
    Write-Error "Built artifact not found: $exe"; exit 1
}

$env:MIGRATION_CONNECTION_STRING = $ConnectionString

Write-Host "Running MigrationHelper..."
dotnet `"$exe`"

if ($LASTEXITCODE -ne 0) { Write-Error "MigrationHelper failed with exit code $LASTEXITCODE"; exit $LASTEXITCODE }

Write-Host "MigrationHelper finished." -ForegroundColor Green
