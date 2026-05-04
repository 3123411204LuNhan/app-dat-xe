# 📋 BUILD & TEST GUIDE - Ride-Hailing App với Ghép Cuốc

## 🎯 Mục Tiêu
Build dự án lên máy ảo và test chức năng ghép cuốc từ đầu đến cuối.

---

## 📋 Yêu Cầu Hệ Thống

### Hardware
- **RAM**: Tối thiểu 8GB (recommended 16GB)
- **Disk**: 30GB trống (cho VM + dependencies)
- **CPU**: 4 cores (recommended 8)

### Software
| Component | Version | Ghi Chú |
|-----------|---------|---------|
| .NET SDK | 10.0+ | Backend + MAUI |
| SQL Server | 2019+ | Database |
| Visual Studio | 2022 | IDE (optional, nhưng recommend) |
| MAUI Workload | Latest | `dotnet workload install maui` |
| Windows SDK | 10.0.19041+ | Cho Windows MAUI target |

---

## 🖥️ SETUP MÁYA ẢO - HƯỚNG DẪN WINDOWS

### Step 1️⃣: Cài đặt .NET SDK 10

**Cách 1: Download từ official**
```
👉 https://dotnet.microsoft.com/download/dotnet/10.0
   → Chọn "Installer" cho Windows
   → Run installer, chọn "Development"
```

**Cách 2: Chocolatey (nếu có)**
```powershell
choco install dotnet-sdk-10.0
```

**Verify**:
```powershell
dotnet --version
# Expected output: 10.0.x
```

### Step 2️⃣: Cài đặt SQL Server

**Option A: SQL Server Express** (Free)
```
👉 https://www.microsoft.com/sql-server/sql-server-downloads
   → Chọn "Express"
   → Run installer
   → Authentication: Mixed mode
   → sa password: YourStrong!Pass123
```

**Option B: Docker** (Recommended - easier)
```powershell
# Install Docker Desktop
# https://www.docker.com/products/docker-desktop

# Pull & run SQL Server
docker run -e 'ACCEPT_EULA=Y' `
           -e 'MSSQL_SA_PASSWORD=YourStrong!Pass123' `
           -p 1433:1433 `
           --name mssql `
           -d mcr.microsoft.com/mssql/server:2022-latest

# Verify (wait ~15 seconds)
docker logs mssql
```

**Verify Connection**:
```powershell
# Using sqlcmd (comes with SQL Server)
sqlcmd -S localhost -U sa -P YourStrong!Pass123
> SELECT @@VERSION
> GO
> EXIT
```

### Step 3️⃣: Tạo Databases

**Option A: SSMS** (SQL Server Management Studio)
1. Mở SSMS
2. Connect: `localhost` (Windows Auth) hoặc `.\SQLEXPRESS`
3. Right-click "Databases" → New Database
4. Create: `north`, `north_rep`, `south`, `south_rep`

**Option B: Command Line**
```powershell
$query = @"
CREATE DATABASE north;
CREATE DATABASE north_rep;
CREATE DATABASE south;
CREATE DATABASE south_rep;
"@

sqlcmd -S localhost -U sa -P YourStrong!Pass123 -Q $query
```

### Step 4️⃣: Run Database Migrations

**Manual Approach** (Recommended cho lần đầu):
```powershell
# Mở SSMS, chọn database "north", rồi:
# File → Open → sql_server_script/north.sql
# Click "Execute"
# Repeat cho north_rep, south, south_rep
```

**Script Approach**:
```powershell
$server = "localhost"
$sa_password = "YourStrong!Pass123"
$databases = @("north", "north_rep", "south", "south_rep")

foreach ($db in $databases) {
    Write-Host "Running north.sql (hoặc south.sql) on $db..."
    
    sqlcmd -S $server -U sa -P $sa_password -d $db `
           -i "sql_server_script/north.sql"  # or south.sql
}

# Then run pooling migration on all
foreach ($db in $databases) {
    Write-Host "Running pooling migration on $db..."
    
    sqlcmd -S $server -U sa -P $sa_password -d $db `
           -i "sql_server_script/migration_pooling_schema.sql"
}
```

### Step 5️⃣: Update Connection Strings

**Edit**: `RideHailingApi/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "North_Primary": "Server=localhost;Database=north;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=true;",
    "North_Replica": "Server=localhost;Database=north_rep;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=true;",
    "South_Primary": "Server=localhost;Database=south;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=true;",
    "South_Replica": "Server=localhost;Database=south_rep;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=true;"
  },
  "JwtSettings": {
    "Key": "your-secret-key-at-least-32-characters-long-1234567890abc",
    "Issuer": "ride-hailing-api",
    "Audience": "ride-hailing-app",
    "ExpiryMinutes": 60
  }
}
```

### Step 6️⃣: Install MAUI Workload

```powershell
dotnet workload restore
dotnet workload install maui
dotnet workload install android ios windows
```

---

## 🔨 BUILD BACKEND

### Terminal:
```powershell
cd RideHailingApi

# Restore packages
dotnet restore

# Build
dotnet build --configuration Debug

# Or direct run
dotnet run --configuration Debug
```

**Expected Output**:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://0.0.0.0:5108
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

✅ Backend ready tại `http://localhost:5108`

---

## 📱 BUILD MAUI APP

### Validate Setup:
```powershell
dotnet maui -h
```

### Build for Windows Desktop:
```powershell
cd RideHailingApp

# Build
dotnet maui build -f net9.0-windows10.0.19041.0 --configuration Debug

# Or run directly
dotnet maui run -f net9.0-windows10.0.19041.0 --configuration Debug
```

### Build for Android:
```powershell
# Require Android SDK + emulator
dotnet maui run -f net9.0-android --configuration Debug
```

**Expected Result**: App window mở lên trên desktop.

---

## ✅ TEST FLOW - GHÉP CUỐC

### 📍 Test Scenario 1: Manual API Test

**Endpoint 1: Register + Login**
```bash
curl -X POST http://localhost:5108/api/auth/login \
  -H "Content-Type: application/json" \
  -H "X-Region: South" \
  -d '{"userName":"user1","password":"pass123"}'

# Response:
{
  "accessToken": "eyJhbGc...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "user": {
    "id": 1,
    "userName": "user1",
    "registeredRegion": "South",
    "roles": ["USER"]
  }
}

# Save token
$token = "eyJhbGc..."
```

**Endpoint 2: Book 2 Trips**
```bash
# Trip 1
curl -X POST http://localhost:5108/api/trips/book-trip \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $token" \
  -H "X-Region: South" \
  -d '{
    "userID": 1,
    "pickupLocation": "10.7605,106.7035",
    "dropoffLocation": "10.8,106.8",
    "region": "South"
  }'

# Response: { "tripId": 101, "message": "..." }

# Trip 2 (khác người dùng)
curl -X POST http://localhost:5108/api/trips/book-trip \
  -H "Authorization: Bearer $token2" \
  -H "X-Region: South" \
  -d '{
    "userID": 2,
    "pickupLocation": "10.7610,106.7040",
    "dropoffLocation": "10.8010,106.8010",
    "region": "South"
  }'

# Response: { "tripId": 102, "message": "..." }
```

**Endpoint 3: Find Pool Candidates**
```bash
curl "http://localhost:5108/api/trips/pool-candidates/101?mainPickupLat=10.7605&mainPickupLon=106.7035&mainDropoffLat=10.8&mainDropoffLon=106.8" \
  -H "Authorization: Bearer $token" \
  -H "X-Region: South"

# Response:
[
  {
    "tripID": 102,
    "userID": 2,
    "pickupLocation": "10.7610,106.7040",
    "dropoffLocation": "10.8010,106.8010",
    "pickupDistance": 0.077,    # < 1km ✅
    "dropoffDistance": 0.111,   # < 1km ✅
    "minutesOld": 1,            # < 5 min ✅
    "createdAt": "2026-05-03T10:30:00Z"
  }
]
```

**Endpoint 4: Pool 2 Trips**
```bash
curl -X POST http://localhost:5108/api/trips/pool \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $token" \
  -d '{
    "mainTripID": 101,
    "secondaryTripID": 102
  }'

# Response:
{
  "success": true,
  "mainTripId": 101,
  "secondaryTripId": 102,
  "message": "Ghép cuốc thành công!"
}
```

✅ **Ghép thành công!** 🎉

---

### 🎮 Test Scenario 2: Full App Test (MAUI)

#### Tài xế 1:
1. ✅ Run app trên Windows/Android
2. ✅ Register/Login → User: "Driver1"
3. ✅ Bấm "🚦 Bắt đầu nhận cuốc xe"
4. ✅ Status: "● Online" ✅

#### Hành khách:
1. ✅ Mở MainPage
2. ✅ Click "Tìm tài xế" 
3. ✅ Nhập pickup/dropoff 
4. ✅ Confirm booking → Trip 101 created

#### Tài xế 1 (tiếp tục):
1. ✅ Danh sách cuốc refresh
2. ✅ Thấy cuốc #101 
3. ✅ Click "Nhận" → Status: "Accepted"
4. ✅ **Chuyển tab "Ghép Cuốc"**
5. ✅ Click "🔍 Tìm cuốc phù hợp"
6. ✅ Danh sách candidates hiển thị (nếu có)

#### Hành khách 2:
1. ✅ Book trip #102 (nearby pickup/dropoff)

#### Tài xế 1 (ghép):
1. ✅ Refresh "Tìm cuốc" → Candidates list update
2. ✅ Thấy trip #102
3. ✅ Click "Ghép 🚗"
4. ✅ Confirm dialog
5. ✅ ✅ **"Ghép cuốc thành công!" popup**
6. ✅ Tab "Ghép Cuốc" shows "Cuốc ghép đang hoạt động"
7. ✅ Main trip: #101
8. ✅ Secondary trip: #102
9. ✅ Passengers: 👥 2/2

#### Hành khách 1 & 2:
1. ✅ Nhận SignalR notification: "Chuyến của bạn đã được ghép..."

---

## 🐛 DEBUGGING & TROUBLESHOOTING

### Backend Issues

#### Lỗi 1: Database Connection Failed
```
Error: System.Data.SqlClient.SqlException: Cannot connect to SQL Server
```

**Giải pháp**:
1. Check SQL Server running: `docker ps` hoặc SSMS
2. Check connection string có đúng password không
3. Ensure `TrustServerCertificate=true` cho dev

#### Lỗi 2: JWT Token Invalid
```
"error":"Unauthorized","message":"Missing or invalid Authorization header"
```

**Giải pháp**:
1. Ensure token trong `Authorization: Bearer <token>`
2. Check token chưa hết hạn (ExpiryMinutes)
3. Verify `JwtSettings:Key` >= 32 ký tự

#### Lỗi 3: Candidates List Empty
```
"No matching candidates found"
```

**Giải pháp**:
1. Check GPS coordinates format: "10.7605,106.7035" (lat,lon)
2. Ensure candidates đủ gần (< 1 km)
3. Check candidates còn "Pending" status
4. Check time difference < 5 phút

### Frontend Issues

#### MAUI Build Fails
```
Error: MAUI workload not installed
```

**Giải pháp**:
```powershell
dotnet workload restore
dotnet workload install maui
```

#### App Can't Connect to Backend
```
"Network error" hoặc "Timeout"
```

**Giải pháp**:
1. Check backend running: `dotnet run` output
2. Check firewall allows port 5108
3. Check API URL trong ApiService.cs (localhost:5108)
4. For Android emulator: Use `10.0.2.2` instead of `localhost`

---

## 📊 TESTING TOOLS

### Postman Collection

```
# Import vào Postman

POST /api/auth/login
Headers:
  Content-Type: application/json
  X-Region: South

Body:
{
  "userName": "user1",
  "password": "pass123"
}

---

POST /api/trips/book-trip
Headers:
  Authorization: Bearer {{token}}
  X-Region: South
  
Body:
{
  "userID": 1,
  "pickupLocation": "10.7605,106.7035",
  "dropoffLocation": "10.8,106.8"
}

---

GET /api/trips/pool-candidates/101
    ?mainPickupLat=10.7605&mainPickupLon=106.7035
    &mainDropoffLat=10.8&mainDropoffLon=106.8

Headers:
  Authorization: Bearer {{token}}

---

POST /api/trips/pool
Headers:
  Authorization: Bearer {{token}}
  Content-Type: application/json

Body:
{
  "mainTripID": 101,
  "secondaryTripID": 102
}
```

### Swagger/OpenAPI

```
Khi backend running:
👉 http://localhost:5108/swagger/index.html

Tất cả endpoints có sẵn để test interactively!
```

---

## ✅ VALIDATION CHECKLIST

- [ ] SQL Server running + 4 databases created
- [ ] Migrations executed trên tất cả databases
- [ ] Connection strings correct trong appsettings.Development.json
- [ ] Backend builds without error
- [ ] Backend runs successfully (listening on port 5108)
- [ ] MAUI app builds without error
- [ ] MAUI app connects to backend successfully
- [ ] Can login & get JWT token
- [ ] Can book trips
- [ ] Can find pool candidates (API endpoint)
- [ ] Can pool 2 trips successfully
- [ ] Pooled trip info shows correctly
- [ ] SignalR notifications received
- [ ] Failover simulation works (Admin panel)

---

## 🎯 NEXT STEPS

After testing pooling feature:
1. ✅ Test failover scenario (simulate Primary down)
2. ✅ Test read-only mode (app still works on Replica)
3. ✅ Stress test with multiple drivers
4. ✅ Test GPS distance calculation accuracy
5. ✅ Measure latency for pooling candidates search

---

## 📞 QUICK COMMANDS REFERENCE

```powershell
# Start backend
cd RideHailingApi
dotnet run --configuration Debug

# Start MAUI app (Windows)
cd RideHailingApp
dotnet maui run -f net9.0-windows10.0.19041.0

# Query API with curl
curl http://localhost:5108/api/trips/health/South

# Check SQL Server
docker ps
docker logs mssql

# Stop SQL Server
docker stop mssql

# Start SQL Server again
docker start mssql

# View MAUI output logs
adb logcat | grep "RideHailing"  # Android only
```

---

Chúc bạn build và test thành công! 🚀
