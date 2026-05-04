# ✅ BUILD & TEST CHECKLIST

## 📋 PRE-SETUP REQUIREMENTS

### Environment Preparation
- [ ] Windows 10/11 hoặc Linux VM (Ubuntu 22.04+)
- [ ] RAM: 8GB+ available
- [ ] Disk: 30GB+ free space
- [ ] Internet connection (để download packages)

### Software Installation
- [ ] .NET SDK 10.0 installed (`dotnet --version` works)
- [ ] Visual Studio 2022 (optional, nhưng recommend)
- [ ] SQL Server 2022 hoặc Docker installed
- [ ] Git installed (để clone repo)

---

## 🔧 STEP 1: Database Setup (Estimated: 15 minutes)

- [ ] SQL Server running (check with SSMS hoặc `docker ps`)
- [ ] Test connection: `sqlcmd -S localhost -U sa -P YourStrong!Pass123 -Q "SELECT @@VERSION"`
- [ ] Create 4 databases: `north`, `north_rep`, `south`, `south_rep`
- [ ] Run north.sql on `north` database
- [ ] Run north.sql on `north_rep` database
- [ ] Run south.sql on `south` database
- [ ] Run south.sql on `south_rep` database
- [ ] Run migration_pooling_schema.sql on all 4 databases
- [ ] Verify tables created: 
  ```sql
  SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;
  -- Should see: Users, Trips, PoolingHistory (if included)
  ```

---

## 🔨 STEP 2: Backend Build (Estimated: 5-10 minutes)

### Code Preparation
- [ ] Clone/Download project repository
- [ ] Navigate to: `cd RideHailingApi`
- [ ] Update `appsettings.Development.json` connection strings
  - [ ] North_Primary points to localhost/north
  - [ ] North_Replica points to localhost/north_rep
  - [ ] South_Primary points to localhost/south
  - [ ] South_Replica points to localhost/south_rep
  - [ ] JWT Key >= 32 characters
  - [ ] JWT Issuer: "ride-hailing-api"
  - [ ] JWT Audience: "ride-hailing-app"

### Build Execution
- [ ] `dotnet restore` - Restore NuGet packages
- [ ] `dotnet build --configuration Debug` - Build solution
- [ ] ✅ No build errors displayed
- [ ] ✅ No warnings (optional)

### Run Backend
- [ ] Open Terminal in `RideHailingApi` folder
- [ ] Run: `dotnet run --configuration Debug`
- [ ] ✅ Output shows: "Now listening on: http://0.0.0.0:5108"
- [ ] ✅ Status: "Application started"
- [ ] ✅ No errors in log output
- [ ] **IMPORTANT**: Keep this terminal open!
- [ ] Test endpoint (new terminal): `curl http://localhost:5108/swagger`

---

## 📱 STEP 3: MAUI App Build (Estimated: 10-15 minutes)

### Setup
- [ ] Navigate to: `cd RideHailingApp`
- [ ] Install MAUI workload (first time only):
  ```powershell
  dotnet workload restore
  dotnet workload install maui
  dotnet workload install android ios windows
  ```

### Update Config
- [ ] Check `Services/ApiService.cs`:
  - [ ] Base URL for localhost correct: `http://192.168.1.45:5108` (Android) or `https://localhost:7285` (Windows)
  - [ ] Adjust IP to your VM IP if needed

### Build Execution
- [ ] `dotnet restore` - Restore packages
- [ ] `dotnet build -f net9.0-windows10.0.19041.0 --configuration Debug`
- [ ] ✅ No build errors
- [ ] ✅ Output shows "Build succeeded"

### Run Application
- [ ] Execute: `dotnet maui run -f net9.0-windows10.0.19041.0`
- [ ] ✅ MAUI app window opens
- [ ] ✅ Dark theme loads correctly (#0F1117 background)
- [ ] ✅ No crash/exception

---

## 🧪 STEP 4: API Testing with Postman (Estimated: 10 minutes)

### Import Collection
- [ ] Download/Open Postman
- [ ] Import: `Postman_Pooling_Test_Collection.json`
- [ ] Verify variables set:
  - [ ] `base_url`: http://localhost:5108
  - [ ] `region`: South
  - [ ] Other variables empty initially

### Test 1: Auth - Login Driver 1
- [ ] Run: "1️⃣ AUTH - Login Driver 1"
- [ ] ✅ Status: 200 OK
- [ ] ✅ Response has `accessToken`
- [ ] ✅ Variable `token_driver1` auto-set
- [ ] ✅ Variable `user_id_1` auto-set

### Test 2: Auth - Login Driver 2
- [ ] Run: "1️⃣ AUTH - Login Driver 2"
- [ ] ✅ Status: 200 OK
- [ ] ✅ `token_driver2` auto-set
- [ ] ✅ `user_id_2` auto-set

### Test 3: Book Trip 1
- [ ] Run: "2️⃣ TRIPS - Book Trip 1"
- [ ] ✅ Status: 200 OK
- [ ] ✅ Response: `{ "tripId": 101, ... }`
- [ ] ✅ Variable `trip_id_1` auto-set to 101

### Test 4: Book Trip 2
- [ ] Run: "2️⃣ TRIPS - Book Trip 2 (Nearby)"
- [ ] ✅ Status: 200 OK
- [ ] ✅ Response: `{ "tripId": 102, ... }`
- [ ] ✅ Variable `trip_id_2` auto-set to 102

### Test 5: Get Pool Candidates
- [ ] Run: "3️⃣ POOLING - Get Pool Candidates for Trip 1"
- [ ] ✅ Status: 200 OK
- [ ] ✅ Response is array (not empty if trips are close)
- [ ] ✅ Should see Trip 102 in candidates
- [ ] ✅ Trip 102 has:
  - [ ] `tripID`: 102
  - [ ] `pickupDistance`: ~0.077 (< 1 km ✅)
  - [ ] `dropoffDistance`: ~0.111 (< 1 km ✅)
  - [ ] `minutesOld`: 1 (< 5 min ✅)

### Test 6: Pool Trips
- [ ] Run: "4️⃣ POOLING - Pool Trip 1 with Trip 2"
- [ ] ✅ Status: 200 OK
- [ ] ✅ Response: `{ "success": true, ... }`
- [ ] ✅ Message: "Ghép cuốc thành công!"

### Test 7: Get Pooled Info
- [ ] Run: "5️⃣ POOLING - Get Pooled Trip Info"
- [ ] ✅ Status: 200 OK
- [ ] ✅ Response shows:
  - [ ] `mainTripID`: 101
  - [ ] `secondaryTripID`: 102
  - [ ] `currentPassengers`: 2
  - [ ] `mainPickup`, `mainDropoff` (Trip 1)
  - [ ] `secondaryPickup`, `secondaryDropoff` (Trip 2)

### Test 8: Health Check
- [ ] Run: "HEALTH - Check API Status"
- [ ] ✅ Status: 200 OK
- [ ] ✅ Response shows:
  - [ ] `region`: South
  - [ ] `primaryOk`: true
  - [ ] `replicaOk`: true
  - [ ] `isFailover`: false

---

## 📱 STEP 5: MAUI App Manual Testing (Estimated: 15 minutes)

### App Startup
- [ ] MAUI app window is open
- [ ] ✅ No crash on startup
- [ ] ✅ Shows login screen (MainPage)

### Auth & Profile Setup
- [ ] Click "Đăng ký" (Register)
- [ ] ✅ Register page opens
- [ ] Fill in:
  - [ ] Username: `testtaxi`
  - [ ] Full Name: `Test Tài Xế`
  - [ ] Phone: `0912345678`
  - [ ] Password: `pass123`
- [ ] Click "Đăng ký"
- [ ] ✅ Success message
- [ ] Back to login
- [ ] Login with `testtaxi` / `pass123`
- [ ] ✅ Logged in successfully
- [ ] ✅ Shows MainPage (customer view) OR redirected to profile

### Switch to Driver Mode
- [ ] (If on customer page) Click profile/settings
- [ ] Look for "Switch to Driver Mode" option
- [ ] ✅ DriverHomePage opens
- [ ] ✅ Header shows: Driver name, avatar, status badge
- [ ] ✅ Status badge shows: "● Offline"

### Online Mode
- [ ] Click "🚦 Bắt đầu nhận cuốc xe"
- [ ] ✅ Button changes to "⛔ Dừng nhận cuốc xe"
- [ ] ✅ Status badge: "● Online" (green)
- [ ] ✅ "Cuốc đang chờ" count: 0 (or any pending trips)

### Tab Layout
- [ ] ✅ Two tabs visible:
  - [ ] Tab 1: "Nhận Cuốc" (active)
  - [ ] Tab 2: "Ghép Cuốc 🚗"

### Switch to Pooling Tab
- [ ] Click "Ghép Cuốc 🚗" tab
- [ ] ✅ Tab switches
- [ ] ✅ Shows:
  - [ ] Info banner (💡 text about pooling)
  - [ ] Status cards: "Cuốc ghép thành công", "Đang tìm kiếm"
  - [ ] "🔍 Tìm cuốc phù hợp" button
  - [ ] "Danh sách cuốc có thể ghép" section

### Find Candidates
- [ ] (Need to accept a trip first)
- [ ] Back to "Nhận Cuốc" tab
- [ ] If no pending trips, create mock data or wait for real trip
- [ ] For now, just verify tab exists and is responsive
- [ ] ✅ No crash when clicking elements

### Offline Mode
- [ ] Click "⛔ Dừng nhận cuốc xe"
- [ ] ✅ Button changes back to "🚦 Bắt đầu nhận cuốc xe"
- [ ] ✅ Status badge: "● Offline" (gray)

---

## 🔗 STEP 6: Full Integration Test (Estimated: 20 minutes)

### Setup
- [ ] Backend running on port 5108 ✅
- [ ] MAUI app open ✅
- [ ] Postman ready ✅

### Test Booking Flow
- [ ] (Customer device/app) Book a trip:
  - In MainPage, fill pickup/dropoff
  - Click "Tìm tài xế"
  - ✅ Trip booked, shows trip details
  
- [ ] (Driver phone/tab) Driver app:
  - ✅ Trip appears in "Cuốc đang chờ" list
  - ✅ Shows pickup, dropoff, vehicle type, estimated fare

### Test Acceptance
- [ ] Driver clicks "Nhận" on the trip
- [ ] ✅ Trip moves to "Chuyến đang thực hiện"
- [ ] ✅ Shows:
  - [ ] Trip info: #101 Pickup → Dropoff
  - [ ] Status: "🚗 Đang trên đường đến điểm đón"
  - [ ] Buttons: "📍 Đã đến điểm đón", "🧑 Đã đón khách", "✅ Hoàn thành chuyến"

### Test Pooling Integration
- [ ] ✅ Switch to "Ghép Cuốc" tab (trip accepted)
- [ ] ✅ Tab shows trip info in "Tìm kiếm cuốc có thể ghép" section
- [ ] Click "🔍 Tìm cuốc phù hợp"
- [ ] ⏳ Loading indicator appears
- [ ] ✅ Candidates list populates (if available)
- [ ] If candidates exist:
  - [ ] Click "Ghép 🚗"
  - [ ] Confirmation dialog appears
  - [ ] Confirm
  - [ ] ✅ "Ghép cuốc thành công!" popup
  - [ ] ✅ Active pooling card shows both trips
  - [ ] ✅ Passengers count: 👥 2/2

### Test SignalR Notifications
- [ ] After pooling:
  - [ ] (Customer 1) Should receive: "Chuyến của bạn đã được ghép..."
  - [ ] (Customer 2) Should receive: "Chuyến của bạn đã được ghép..."
  - [ ] ✅ Notifications show (toast/alert)

---

## 🐛 STEP 7: Failure Mode Testing (Optional, Estimated: 10 minutes)

### Failover Test
- [ ] (Simulate) Stop SQL Server Primary
- [ ] Driver attempts to find candidates
- [ ] ✅ Either:
  - [ ] API returns "Server ở chế độ Read-Only" (503 status)
  - [ ] OR API still works if using Replica
- [ ] Restart SQL Server
- [ ] ✅ API works again

### Token Expiration
- [ ] Make Postman request after JWT expires
- [ ] ✅ Response: 401 Unauthorized
- [ ] Re-login to get new token
- [ ] ✅ Works again

### Invalid GPS Format
- [ ] Postman: Send pool-candidates with invalid GPS
- [ ] ✅ Returns empty list (or error, as designed)

---

## ✅ FINAL VALIDATION

### Checklist Before Deployment
- [ ] ✅ All 8 Postman tests passed (green)
- [ ] ✅ MAUI app doesn't crash
- [ ] ✅ Pooling tab visible and responsive
- [ ] ✅ Can find candidates (if trips available)
- [ ] ✅ Can pool 2 trips
- [ ] ✅ SignalR notifications received
- [ ] ✅ Database has PooledWithTripID column
- [ ] ✅ No console errors (check debug output)
- [ ] ✅ No database connection errors
- [ ] ✅ Health check returns OK

### Known Working State
- [ ] Backend: Listening on :5108
- [ ] Database: 4 databases with pooling schema
- [ ] MAUI: App open with 2 tabs
- [ ] API: All pooling endpoints responsive
- [ ] SignalR: Connected and receiving messages

---

## 📝 NOTES

Date Tested: ________________
Tester Name: ________________
Environment: Windows / Linux / Mac
Test Result: ✅ PASS / ❌ FAIL

Issues Found:
```
(List any issues, error messages, or improvements)


```

---

**Congratulations!** 🎉 If you've completed all checks, your Ride-Hailing app with Pooling feature is ready for use!

For production deployment, refer to deployment guides.
