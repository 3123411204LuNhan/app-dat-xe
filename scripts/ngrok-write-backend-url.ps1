<#
Script: scripts/ngrok-write-backend-url.ps1
Purpose: Query local ngrok API to get public HTTPS URL and write it to
         RideHailingApp Resources/Raw/backend_url.txt (for packaged asset)
         and to app data folder (FileSystem.AppDataDirectory) so app reads runtime URL.

Usage:
  1) Ensure ngrok is running: ngrok http 5108
  2) Run this script (PowerShell):
       .\scripts\ngrok-write-backend-url.ps1 -Port 5108 -AppProjectPath "RideHailingApp"

Notes:
  - This script calls ngrok local API at http://127.0.0.1:4040/api/tunnels
  - It finds the first https tunnel and writes its public_url into two locations:
      - RideHailingApp/Resources/Raw/backend_url.txt (for packaged asset)
      - %LOCALAPPDATA%\{AppName}\backend_url.txt (FileSystem.AppDataDirectory at runtime)
  - Requires PowerShell 7+ for cross-platform path handling; works on Windows PowerShell too.
#>
param(
    [int]$Port = 5108,
    [string]$AppProjectPath = "RideHailingApp"
)

function Write-FileSafely($path, $content) {
    $dir = Split-Path $path -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    Set-Content -Path $path -Value $content -Encoding UTF8
}

# Query ngrok local API
$apiUrl = "http://127.0.0.1:4040/api/tunnels"
try {
    $resp = Invoke-RestMethod -Method Get -Uri $apiUrl -ErrorAction Stop
} catch {
    Write-Error "Cannot reach ngrok local API at $apiUrl. Is ngrok running?"
    exit 1
}

$httpsTunnel = $resp.tunnels | Where-Object { $_.public_url -like 'https:*' -and ($_.config? .addr -like "*:$Port") }
if (-not $httpsTunnel) {
    # fallback: pick any https tunnel
    $httpsTunnel = $resp.tunnels | Where-Object { $_.public_url -like 'https:*' } | Select-Object -First 1
}

if (-not $httpsTunnel) {
    Write-Error "No HTTPS tunnel found in ngrok API response."
    exit 2
}

$publicUrl = $httpsTunnel.public_url
Write-Host "Found ngrok public URL: $publicUrl"

# Write to project Resources/Raw/backend_url.txt
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectRawPath = Join-Path $repoRoot "${AppProjectPath}\Resources\Raw\backend_url.txt"
Write-FileSafely $projectRawPath $publicUrl
Write-Host "Wrote packaged asset: $projectRawPath"

# Write to local app data (FileSystem.AppDataDirectory equivalent)
$localAppData = Join-Path $env:LOCALAPPDATA $AppProjectPath
$appDataFile = Join-Path $localAppData "backend_url.txt"
Write-FileSafely $appDataFile $publicUrl
Write-Host "Wrote runtime override: $appDataFile"

Write-Host "Done. Rebuild/install app on device if needed; app will read runtime file on startup."