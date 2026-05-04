<#
Start-Dev-With-Ngrok.ps1

Starts the backend (RideHailingApi) and ngrok, waits for ngrok public HTTPS url,
writes it to the two runtime locations the MAUI app reads, and opens the admin page.

Usage (from repo root):
  powershell -ExecutionPolicy Bypass -File .\scripts\start-dev-ngrok.ps1

Parameters:
  -NgrokExePath : path to ngrok executable or command name (default: ngrok)
  -ApiProject   : relative path to API project (default: RideHailingApi)
  -Port         : local HTTP port backend listens on (default: 5108)
  -Configuration: dotnet build configuration (default: Debug)

Notes:
 - This script starts two new Terminal windows (PowerShell) for backend and ngrok so you can see logs.
 - Requires ngrok installed and available in PATH (or pass -NgrokExePath full path).
 - After tunnel available, script writes backend_url into:
     1) ./RideHailingApp/Resources/Raw/backend_url.txt (packaged asset)
     2) %LOCALAPPDATA%\RideHailingApp\backend_url.txt (runtime override)
 - Finally it opens the admin page: https://<ngrok>/admin.html
 - To stop backend/ngrok, close the opened windows or find processes started by this script.
#>
[CmdletBinding()]
param(
    [string]$NgrokExePath = "ngrok",
    [string]$ApiProject = "RideHailingApi",
    [int]$Port = 5108,
    [string]$Configuration = "Debug"
)

function Write-FileSafely($path, $content) {
    $dir = Split-Path $path -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    Set-Content -Path $path -Value $content -Encoding UTF8
}

$repoRoot = (Resolve-Path "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\..")
$repoRoot = (Resolve-Path $repoRoot).ProviderPath

# Start backend in new PowerShell window
$apiFullPath = Join-Path $repoRoot $ApiProject
if (-not (Test-Path $apiFullPath)) {
    Write-Error "API project path not found: $apiFullPath"; exit 1
}

$dotnetCmd = "dotnet run --project `"$apiFullPath`" -c $Configuration --urls http://0.0.0.0:$Port"
Write-Host "Starting backend: $dotnetCmd"
Start-Process pwsh -ArgumentList "-NoExit","-Command","cd '$apiFullPath'; $dotnetCmd" -WindowStyle Normal

Start-Sleep -Seconds 2

# Start ngrok in new PowerShell window
try {
    Write-Host "Starting ngrok: $NgrokExePath http $Port"
    Start-Process $NgrokExePath -ArgumentList "http $Port" -NoNewWindow:$false -WindowStyle Normal
} catch {
    Write-Warning "Failed to start ngrok via '$NgrokExePath'. Make sure ngrok is installed and in PATH, or pass -NgrokExePath the full path to ngrok.exe."
    Write-Host "You can start ngrok manually: ngrok http $Port" -ForegroundColor Yellow
}

# Poll ngrok local API for tunnel
$apiUrl = 'http://127.0.0.1:4040/api/tunnels'
$publicUrl = $null
Write-Host "Waiting for ngrok tunnel to become available (http://127.0.0.1:4040)..."
$deadline = [DateTime]::UtcNow.AddMinutes(3)
while (-not $publicUrl -and [DateTime]::UtcNow -lt $deadline) {
    try {
        $resp = Invoke-RestMethod -Uri $apiUrl -UseBasicParsing -ErrorAction Stop
        if ($resp.tunnels) {
            # prefer https tunnel matching port
            $https = $resp.tunnels | Where-Object { $_.public_url -like 'https:*' -and ($_.config? .addr -like "*:$Port") }
            if (-not $https) { $https = $resp.tunnels | Where-Object { $_.public_url -like 'https:*' } | Select-Object -First 1 }
            if ($https) { $publicUrl = $https.public_url }
        }
    } catch {
        # ngrok UI not ready yet
    }
    if (-not $publicUrl) { Start-Sleep -Seconds 1 }
}

if (-not $publicUrl) {
    Write-Error "Ngrok tunnel did not appear within timeout. Check ngrok process and http://127.0.0.1:4040 for details."; exit 2
}

Write-Host "Found ngrok public URL: $publicUrl"

# Write into packaged Resources/Raw/backend_url.txt
$projectBackendFile = Join-Path $repoRoot "RideHailingApp\Resources\Raw\backend_url.txt"
Write-FileSafely $projectBackendFile $publicUrl
Write-Host "Wrote packaged asset: $projectBackendFile"

# Write into local appdata (runtime override)
$localAppData = Join-Path $env:LOCALAPPDATA "RideHailingApp"
$appDataFile = Join-Path $localAppData "backend_url.txt"
Write-FileSafely $appDataFile $publicUrl
Write-Host "Wrote runtime override: $appDataFile"

# Open admin page
$adminUrl = "$publicUrl/admin.html"
Write-Host "Opening admin page: $adminUrl"
Start-Process $adminUrl

Write-Host "Done. Backend and ngrok started in new windows. Close those windows to stop them." -ForegroundColor Green
