# Quick Start - Build & Test (Windows Batch)
# Chạy: .\BUILD_AND_TEST.bat hoặc double-click file này

@echo off
setlocal enabledelayedexpansion

echo.
echo ======================================================
echo    RIDE-HAILING APP - BUILD & TEST QUICK START
echo ======================================================
echo.

REM Check if .NET is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found
    echo Please install from: https://dotnet.microsoft.com/download/dotnet/10.0
    pause
    exit /b 1
)

echo ✅ .NET SDK detected: %dotnet version%
echo.

REM ===== STEP 1: Check SQL Server =====
echo [1/6] Checking SQL Server connection...
(
    echo sqlcmd -S localhost -U sa -P YourStrong!Pass123 -Q "SELECT @@VERSION"
) | sqlcmd -S localhost -U sa -P YourStrong!Pass123 >nul 2>&1

if errorlevel 1 (
    echo ❌ Cannot connect to SQL Server
    echo.
    echo Please ensure SQL Server is running:
    echo   Option A: SQL Server Express / SQL Server 2022
    echo   Option B: Docker - run this command:
    echo      docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Pass123" ^
    echo         -p 1433:1433 --name mssql -d mcr.microsoft.com/mssql/server:2022-latest
    echo.
    pause
    exit /b 1
)
echo ✅ SQL Server connected
echo.

REM ===== STEP 2: Update Connection Strings =====
echo [2/6] Connection strings status:
echo.
echo   Ensure RideHailingApi\appsettings.Development.json has:
echo   - North_Primary: Server=localhost; Database=north
echo   - North_Replica: Server=localhost; Database=north_rep
echo   - South_Primary: Server=localhost; Database=south
echo   - South_Replica: Server=localhost; Database=south_rep
echo.
pause

REM ===== STEP 3: Build Backend =====
echo [3/6] Building Backend API...
cd RideHailingApi

dotnet restore
if errorlevel 1 (
    echo ❌ Backend restore failed
    pause
    exit /b 1
)

dotnet build --configuration Debug
if errorlevel 1 (
    echo ❌ Backend build failed
    pause
    exit /b 1
)
echo ✅ Backend built successfully
cd ..
echo.

REM ===== STEP 4: Build Frontend =====
echo [4/6] Building MAUI App...
cd RideHailingApp

echo Installing MAUI workload...
dotnet workload restore
dotnet workload install maui

dotnet restore
if errorlevel 1 (
    echo ❌ MAUI restore failed
    pause
    exit /b 1
)

dotnet build -f net9.0-windows10.0.19041.0 --configuration Debug
if errorlevel 1 (
    echo ❌ MAUI build failed
    pause
    exit /b 1
)
echo ✅ MAUI built successfully
cd ..
echo.

REM ===== STEP 5: Start Backend =====
echo [5/6] Starting Backend API...
echo.
echo Launching Backend on port 5108...
echo URL: http://localhost:5108
echo Swagger: http://localhost:5108/swagger/index.html
echo.
echo IMPORTANT: Keep this window open while testing!
echo To stop: Ctrl+C
echo.
pause

cd RideHailingApi
dotnet run --configuration Debug --no-build

REM ===== STEP 6: Start MAUI (in new window) =====
echo.
echo [6/6] Starting MAUI App in new window...
cd ..\RideHailingApp

REM Open new window for MAUI
start cmd /k "dotnet maui run -f net9.0-windows10.0.19041.0 --configuration Debug && pause"

echo.
echo ======================================================
echo    Backend and App should be running now!
echo ======================================================
echo.
echo Next steps:
echo   1. Open MAUI app window (opened automatically)
echo   2. Try to login/register
echo   3. Test pooling feature in tab "Ghép Cuốc"
echo.
pause
