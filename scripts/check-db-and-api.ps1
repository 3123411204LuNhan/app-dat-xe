<#
scripts/check-db-and-api.ps1
Checks SQL connectivity and backend admin endpoints for each region.
Usage:
  pwsh -File .\scripts\check-db-and-api.ps1 [-AppSettingsPath <path>] [-BaseUrl <url>] [-SqlTimeoutSec <seconds>]

Defaults assume you run from repo root.
#>
[CmdletBinding()]
param(
    [string]$AppSettingsPath = "RideHailingApi\appsettings.json",
    [string]$BaseUrl = "http://localhost:5108",
    [int]$SqlTimeoutSec = 5
)

function Write-Heading($text){ Write-Host "`n=== $text ===" -ForegroundColor Cyan }
function Write-Ok($msg){ Write-Host "[OK]  $msg" -ForegroundColor Green }
function Write-Warn($msg){ Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg){ Write-Host "[ERR]  $msg" -ForegroundColor Red }

# Resolve appsettings
$repoRoot = (Get-Location).ProviderPath
$settingsPath = Join-Path $repoRoot $AppSettingsPath
if (-not (Test-Path $settingsPath)){
    Write-Err "appsettings not found: $settingsPath"; exit 2
}

$cfg = Get-Content -Raw -Path $settingsPath | ConvertFrom-Json
if (-not $cfg.ConnectionStrings){ Write-Warn "No ConnectionStrings section in $settingsPath" }

$connections = @{}
foreach ($prop in $cfg.ConnectionStrings.PSObject.Properties){
    $connections[$prop.Name] = $prop.Value
}

# Helper to extract server host and port
function Parse-ServerFromConnString([string]$cs){
    if (-not $cs) { return $null }
    $m = [regex]::Match($cs, 'Server\s*=\s*([^;]+)', 'IgnoreCase')
    if (-not $m.Success) { return $null }
    $server = $m.Groups[1].Value.Trim()
    # if contains comma -> host,port
    if ($server -match ','){
        $parts = $server.Split(',')
        return @{ Host = $parts[0]; Port = [int]$parts[1] }
    }
    else {
        return @{ Host = $server; Port = $null }
    }
}

Add-Type -AssemblyName System.Data

Write-Heading "SQL Connectivity Checks"
foreach ($name in $connections.Keys){
    $cs = $connections[$name]
    Write-Host "\nConnection: $name" -ForegroundColor White
    $info = Parse-ServerFromConnString $cs
    if ($info -ne $null -and $info.Port){
        Write-Host " Server: $($info.Host) Port: $($info.Port)"
        $res = Test-NetConnection -ComputerName $info.Host -Port $info.Port -InformationLevel Quiet
        if ($res){ Write-Ok "TCP reachable: $($info.Host):$($info.Port)" } else { Write-Warn "TCP NOT reachable: $($info.Host):$($info.Port)" }
    else {
        $hostDisplay = if ($info -ne $null -and $info.Host) { $info.Host } else { 'unknown or named instance' }
        Write-Host " Server: $hostDisplay (named instance or no port)"
        Write-Warn "Cannot perform TCP test for named instance; will try opening SqlConnection." 
    }

    # Ensure connection string has Connection Timeout small for quick test
    $testCs = $cs
    if ($testCs -notmatch 'Connection\s*Timeout\s*='){ $testCs = $testCs + ";Connection Timeout=$SqlTimeoutSec" }

    try{
        $sqlConn = New-Object System.Data.SqlClient.SqlConnection $testCs
        $sqlConn.Open()
        $sqlConn.Close()
        Write-Ok "SqlConnection OPEN succeeded (timeout ${SqlTimeoutSec}s)"
    } catch {
        Write-Err "SqlConnection failed: $($_.Exception.Message)"
    }
}

Write-Heading "Backend Admin API Checks (BaseUrl: $BaseUrl)"
# Check admin/status
$statusUrl = "$BaseUrl/api/admin/status"
try{
    $status = Invoke-RestMethod -Uri $statusUrl -Method Get -ErrorAction Stop
    Write-Ok "Admin status endpoint reachable"
    # print servers summary
    if ($status.servers){
        foreach ($s in $status.servers){
            $r = $s.region
            $pReal = $s.primaryReal
            $pSim = $s.primarySimulated
            $rep = $s.replicaReal
            Write-Host " Region: $r  primaryReal: $pReal  primarySimulated: $pSim  replicaReal: $rep"
        }
    }
} catch {
    Write-Err "Failed to GET admin status: $($_.Exception.Message)"
}

# For each region test read/write
$regions = @('South','North')
foreach ($reg in $regions){
    Write-Heading "Test for region: $reg"
    $readUrl = "$BaseUrl/api/admin/test-read/$reg"
    $writeUrl = "$BaseUrl/api/admin/test-write/$reg"

    try{
        $r = Invoke-RestMethod -Uri $readUrl -Method Get -ErrorAction Stop
        if ($r.success -eq $true){ Write-Ok "Read OK -> source: $($r.source) rows: $($r.rowCount)" }
        else { Write-Warn "Read returned not-success: $($r | ConvertTo-Json -Depth 2)" }
    } catch {
        Write-Err "Read failed: $($_.Exception.Message)"
    }

    try{
        $w = Invoke-RestMethod -Uri $writeUrl -Method Post -ErrorAction Stop
        if ($w.success -eq $true){ Write-Ok "Write OK -> source: $($w.source) message: $($w.message)" }
        else { Write-Warn "Write returned not-success: $($w | ConvertTo-Json -Depth 2)" }
    } catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        if ($resp -ne $null){
            $sr = New-Object System.IO.StreamReader $resp.GetResponseStream()
            $text = $sr.ReadToEnd(); $sr.Close()
            Write-Err "Write failed HTTP: $text"
        } else { Write-Err "Write failed: $($_.Exception.Message)" }
    } catch {
        Write-Err "Write failed: $($_.Exception.Message)"
    }
}

Write-Host "`nSummary: completed checks." -ForegroundColor Cyan
