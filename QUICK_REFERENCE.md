# 🚀 QUICK REFERENCE - BUILD & TEST CHEAT SHEET

## 🎯 1-MINUTE QUICK START (Windows)

### Prerequisite Check:
```powershell
# Check .NET
dotnet --version

# Check SQL Server
sqlcmd -S localhost -U sa -P YourStrong!Pass123 -Q "SELECT @@VERSION"

# Or use Docker
docker ps  # Should see "mssql" container running
```

### If SQL Server not running (Docker):
```powershell
docker run -e 'ACCEPT_EULA=Y' `
           -e 'MSSQL_SA_PASSWORD=YourStrong!Pass123' `
           -p 1433:1433 `
           --name mssql `
           -d mcr.microsoft.com/mssql/server:2022-latest

# Wait 15 seconds for startup
```

### Quick Build:
```powershell
# Terminal 1: Backend
cd RideHailingApi
dotnet run --configuration Debug

# Terminal 2: MAUI App (new window)
cd RideHailingApp
dotnet maui run -f net9.0-windows10.0.19041.0

# Terminal 3: Test API (or use Postman)
curl http://localhost:5108/swagger/index.html
```

---

## 📋 ESSENTIAL COMMANDS

### Database Setup

```powershell
# Create databases
sqlcmd -S localhost -U sa -P YourStrong!Pass123 -Q @"
CREATE DATABASE north;
CREATE DATABASE north_rep;
CREATE DATABASE south;
CREATE DATABASE south_rep;
"@

# Run migration on all
$dbs = "north", "north_rep", "south", "south_rep"
foreach ($db in $dbs) {
    sqlcmd -S localhost -U sa -P YourStrong!Pass123 -d $db -i "sql_server_script/migration_pooling_schema.sql"
}

# Verify
sqlcmd -S localhost -U sa -P YourStrong!Pass123 -d north -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;"
```

### Backend Build & Run

```powershell
# Build only
cd RideHailingApi
dotnet build --configuration Debug

# Run with hot reload
dotnet watch run --configuration Debug

# Build + Run (clean)
dotnet clean
dotnet restore
dotnet build --configuration Debug
dotnet run --configuration Debug
```

### MAUI Build & Run

```powershell
# Install workload (first time only)
dotnet workload install maui android ios windows

# Build only
cd RideHailingApp
dotnet build -f net9.0-windows10.0.19041.0 --configuration Debug

# Run (builds + runs)
dotnet maui run -f net9.0-windows10.0.19041.0 --configuration Debug

# Run on Android emulator
dotnet maui run -f net9.0-android

# Debug (attach debugger)
dotnet maui build -f net9.0-windows10.0.19041.0 --configuration Debug
# Then run app from VS
```

---

## 🧪 API TEST COMMANDS

### Auth
```bash
# Login driver 1
curl -X POST http://localhost:5108/api/auth/login \
  -H "Content-Type: application/json" \
  -H "X-Region: South" \
  -d '{"userName":"driver1","password":"pass123"}'

# Save token (replace with actual token)
$token = "eyJhbGc..."
```

### Book Trips
```bash
# Trip 1
curl -X POST http://localhost:5108/api/trips/book-trip \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $token" \
  -H "X-Region: South" \
  -d '{
    "userID": 1,
    "pickupLocation": "10.7605,106.7035",
    "dropoffLocation": "10.8,106.8"
  }'

# Trip 2 (nearby)
curl -X POST http://localhost:5108/api/trips/book-trip \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $token" \
  -H "X-Region: South" \
  -d '{
    "userID": 2,
    "pickupLocation": "10.7610,106.7040",
    "dropoffLocation": "10.8010,106.8010"
  }'
```

### Pooling
```bash
# Get candidates
curl "http://localhost:5108/api/trips/pool-candidates/101?mainPickupLat=10.7605&mainPickupLon=106.7035&mainDropoffLat=10.8&mainDropoffLon=106.8" \
  -H "Authorization: Bearer $token" \
  -H "X-Region: South"

# Pool trips
curl -X POST http://localhost:5108/api/trips/pool \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $token" \
  -d '{"MainTripID":101,"SecondaryTripID":102}'

# Get pooled info
curl http://localhost:5108/api/trips/pooled/101 \
  -H "Authorization: Bearer $token"
```

### Health Check
```bash
curl http://localhost:5108/api/trips/health/South
```

---

## 🔧 COMMON ISSUES & FIXES

| Issue | Cause | Fix |
|-------|-------|-----|
| **DB Connection Failed** | SQL Server not running | `docker start mssql` or check SSMS |
| **Port 5108 in use** | Another app using port | `netstat -ano \| findstr :5108` then kill process |
| **JWT Token Invalid** | Token expired or missing | Login again, ensure `Authorization: Bearer <token>` |
| **No Pool Candidates** | Trips too far apart | Use provided GPS coords (< 1 km apart) |
| **MAUI build error** | Workload not installed | `dotnet workload install maui` |
| **SignalR connection fails** | Wrong token or URL | Check `ApiService.cs` URL config |
| **Android emulator can't reach backend** | Localhost issue | Use `10.0.2.2` instead of `localhost` |

---

## 📊 TESTING WORKFLOW

### ✅ Scenario 1: Simple Pooling Test (5 min)

```
1. Terminal 1: dotnet run (Backend)
   → Listening on http://localhost:5108 ✅

2. Postman: Login
   → Copy token to variable {{token}} ✅

3. Postman: Book Trip 1
   → Receive trip_id_1 (e.g., 101) ✅

4. Postman: Book Trip 2
   → Receive trip_id_2 (e.g., 102) ✅

5. Postman: Get Pool Candidates
   → Should see trip 102 as candidate ✅

6. Postman: Pool Trips
   → Response: "success": true ✅

7. Postman: Get Pooled Info
   → Shows main + secondary trips ✅
```

### ✅ Scenario 2: Full App Test (15 min)

```
1. Start Backend: dotnet run ✅

2. Start MAUI App: dotnet maui run ✅

3. Register Tài xế 1
   → Success ✅

4. Click "🚦 Bắt đầu nhận cuốc xe"
   → Status: "● Online" ✅

5. (Another user) Book Trip
   → Tài xế 1 receives notification ✅

6. Tài xế 1: Click "Nhận" on trip
   → Status: "Accepted" ✅

7. Switch to "Ghép Cuốc" tab
   → See stats ✅

8. Click "🔍 Tìm cuốc phù hợp"
   → See candidates list (if available) ✅

9. Click "Ghép 🚗" on candidate
   → Confirmation dialog ✅

10. Confirm
    → "Ghép cuốc thành công!" popup ✅
    → Shows "Cuốc ghép đang hoạt động" ✅
```

---

## 📱 MAUI DEPLOYMENT

### Windows Desktop:
```powershell
# Development
dotnet maui run -f net9.0-windows10.0.19041.0 --configuration Debug

# Release build
dotnet maui publish -f net9.0-windows10.0.19041.0 --configuration Release
# Output: RideHailingApp/bin/Release/net9.0-windows10.0.19041.0/publish
```

### Android:
```powershell
# Requires Android SDK + emulator

# List emulators
dotnet maui list

# Run on specific emulator
dotnet maui run -f net9.0-android --device [device-id]

# Create APK
dotnet maui publish -f net9.0-android --configuration Release
```

---

## 🔐 SECURITY NOTES

**Development Only**:
- `TrustServerCertificate=true` in connection string
- JWT key is example (change in production)
- CORS allows any origin

**Before Production**:
- Use proper SSL certificates
- Update JWT secret (32+ chars)
- Restrict CORS to specific origins
- Enable HTTPS only
- Use environment variables for secrets

---

## 📈 PERFORMANCE MONITORING

### Check Backend Logs:
```
Look for in debug output:
  ✅ "Now listening on: http://0.0.0.0:5108"
  ✅ "Application started"
  ⚠️  Errors: Look for SQL exceptions, JWT issues
```

### Check MAUI Logs:
```powershell
# Android
adb logcat | grep "RideHailing"

# Windows (check output window in VS)
Debug → Windows → Output
```

### SQL Server Logs:
```powershell
# Docker
docker logs mssql

# Local (SQL Server Agent)
# Event Viewer → Windows Logs → Application
```

---

## 🎯 SUCCESS CRITERIA

- [ ] Backend starts: "listening on :5108"
- [ ] Database connected: No connection errors
- [ ] Login works: Get JWT token
- [ ] Book trip: Receive tripId
- [ ] Pool candidates: Get non-empty list (if 2+ trips)
- [ ] Pool trips: Success response
- [ ] MAUI app: Opens without crashes
- [ ] Tab "Ghép Cuốc": Visible and responsive
- [ ] SignalR: Notifications received

---

## 🚨 Emergency Reset

If everything breaks:

```powershell
# 1. Stop all processes (Ctrl+C in terminals)

# 2. Reset databases
docker stop mssql
docker rm mssql
docker run -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=YourStrong!Pass123' -p 1433:1433 --name mssql -d mcr.microsoft.com/mssql/server:2022-latest

# 3. Clean builds
cd RideHailingApi && dotnet clean
cd ../RideHailingApp && dotnet clean

# 4. Restart from Step 1
```

---

**Last Updated**: May 3, 2026
**Version**: 1.0
