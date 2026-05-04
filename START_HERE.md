# 🎯 BUILD & TEST SUMMARY - TẤT CẢ NHỮNG GÌ BẠN CẦN

## 📦 Files Hướng Dẫn Đã Tạo

| File | Nội Dung | Sử Dụng Khi |
|------|---------|-----------|
| [BUILD_TEST_GUIDE_WINDOWS.md](BUILD_TEST_GUIDE_WINDOWS.md) | ✅ **Bắt đầu từ đây** - Hướng dẫn chi tiết step-by-step cho Windows | Setup từ đầu |
| [QUICK_REFERENCE.md](QUICK_REFERENCE.md) | Cheatsheet lệnh, troubleshooting, commands nhanh | Khi nhớ lệnh |
| [TEST_CHECKLIST.md](TEST_CHECKLIST.md) | ✅ Checklist kiểm tra từng bước | Xác nhận hoàn thành |
| [BUILD_AND_TEST.bat](BUILD_AND_TEST.bat) | Batch script tự động build (Windows) | Chạy một lần để test |
| [Postman_Pooling_Test_Collection.json](Postman_Pooling_Test_Collection.json) | Postman collection - test API | Test API endpoints |

---

## 🚀 QUICK START (5 phút)

### Nếu bạn có tất cả tools sẵn:

**Terminal 1:**
```powershell
cd RideHailingApi
dotnet run --configuration Debug
# Chờ: "Now listening on: http://0.0.0.0:5108"
```

**Terminal 2:**
```powershell
cd RideHailingApp
dotnet maui run -f net9.0-windows10.0.19041.0
# App mở lên
```

**Terminal 3 (hoặc Postman):**
```bash
# Test API
curl http://localhost:5108/api/trips/health/South
```

✅ **DONE!** Backend + App running!

---

## 📖 STEP-BY-STEP GUIDE (30 phút)

### Cho người mới:

**👉 Start here:** [BUILD_TEST_GUIDE_WINDOWS.md](BUILD_TEST_GUIDE_WINDOWS.md)

Theo dõi:
1. ✅ Requirement Check (5 min)
2. ✅ Database Setup (15 min)
3. ✅ Backend Build (5 min)
4. ✅ MAUI Build (5 min)
5. ✅ Test Flow (5 min)

---

## 🧪 TESTING

### Option A: API Testing (Postman)
1. Import: [Postman_Pooling_Test_Collection.json](Postman_Pooling_Test_Collection.json)
2. Chạy 8 tests theo thứ tự
3. ✅ All green = Success!

### Option B: Full App Testing
1. Mở MAUI app
2. Đăng ký / Đăng nhập
3. Chuyển sang "Chế độ Tài xế"
4. Bấm "🚦 Bắt đầu nhận cuốc"
5. Chuyển tab "Ghép Cuốc"
6. Click "🔍 Tìm cuốc phù hợp"
7. ✅ Ghép cuốc thành công!

### Option C: Automated Checklist
👉 Use: [TEST_CHECKLIST.md](TEST_CHECKLIST.md)
- Đánh dấu ✅ từng step
- Đảm bảo không bỏ sót

---

## 🔧 PREREQUISITES

### Minimum Requirements:
- ✅ .NET SDK 10.0+
- ✅ SQL Server 2019+ (hoặc Docker)
- ✅ 8GB RAM
- ✅ 30GB disk

### Installation:

**1. .NET SDK:**
```
👉 https://dotnet.microsoft.com/download/dotnet/10.0
   Download installer → Run → Done
```

**2. SQL Server (Pick ONE):**

**Option A: Express (Free)**
```
👉 https://www.microsoft.com/sql-server/sql-server-downloads
   → Chọn "Express" → Download → Run
```

**Option B: Docker (Recommended)**
```powershell
docker run -e 'ACCEPT_EULA=Y' `
           -e 'MSSQL_SA_PASSWORD=YourStrong!Pass123' `
           -p 1433:1433 --name mssql -d mcr.microsoft.com/mssql/server:2022-latest
```

---

## 📋 WHAT TO DO

### Step 1: Setup (30 min)
```
1. Create 4 databases: north, north_rep, south, south_rep
2. Run north.sql on north & north_rep
3. Run south.sql on south & south_rep
4. Run migration_pooling_schema.sql on all 4
5. Update appsettings.Development.json with connection strings
```

👉 **Detailed:** [BUILD_TEST_GUIDE_WINDOWS.md - Step 1-5](BUILD_TEST_GUIDE_WINDOWS.md)

### Step 2: Build & Run (10 min)
```
Terminal 1:
  cd RideHailingApi
  dotnet run --configuration Debug

Terminal 2:
  cd RideHailingApp
  dotnet maui run -f net9.0-windows10.0.19041.0
```

👉 **Troubleshooting:** [QUICK_REFERENCE.md](QUICK_REFERENCE.md)

### Step 3: Test (10-20 min)
```
Option 1: Postman
  Import collection → Run 8 tests

Option 2: MAUI App
  Login → Bất nhận cuốc → Tab ghép → Find → Pool

Option 3: Checklist
  Mark each step ✅
```

👉 **Detailed:** [TEST_CHECKLIST.md](TEST_CHECKLIST.md)

---

## 🎯 EXPECTED RESULTS

### ✅ Success Indicators:

**Backend:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://0.0.0.0:5108
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Database:**
```
Trips table has columns:
  ✅ PooledWithTripID
  ✅ MaxPassengers
  ✅ CurrentPassengers
```

**MAUI App:**
```
✅ Starts without crash
✅ 2 tabs visible: "Nhận Cuốc" + "Ghép Cuốc"
✅ Online/Offline toggle works
✅ Tab "Ghép Cuốc" responsive
```

**API Testing:**
```
✅ Login: 200 OK + token
✅ Book trips: 200 OK + tripId
✅ Pool candidates: 200 OK + list
✅ Pool trips: 200 OK + success
✅ Pooled info: 200 OK + details
```

---

## ⚠️ COMMON MISTAKES

### ❌ Mistake 1: Forgot to run migration
**Symptom:** "PooledWithTripID column not found"
**Fix:** Run `migration_pooling_schema.sql` on all 4 databases

### ❌ Mistake 2: Wrong connection string
**Symptom:** "Cannot connect to SQL Server"
**Fix:** Update appsettings.Development.json

### ❌ Mistake 3: Backend not running
**Symptom:** MAUI app shows "Network error"
**Fix:** Check Terminal 1 - `dotnet run` should show "listening on :5108"

### ❌ Mistake 4: MAUI workload not installed
**Symptom:** Build fails with "MAUI workload not found"
**Fix:** `dotnet workload install maui`

### ❌ Mistake 5: SQL Server not running
**Symptom:** Backend starts but crashes on first DB access
**Fix:** Start SQL Server (SSMS, SQL Server Express, or Docker)

---

## 🆘 NEED HELP?

### Check These Files:
1. **Setup issues?** → [BUILD_TEST_GUIDE_WINDOWS.md](BUILD_TEST_GUIDE_WINDOWS.md)
2. **Command forgotten?** → [QUICK_REFERENCE.md](QUICK_REFERENCE.md)
3. **Step-by-step test?** → [TEST_CHECKLIST.md](TEST_CHECKLIST.md)
4. **API test?** → [Postman Collection](Postman_Pooling_Test_Collection.json)

### Errors Reference:
- **"Cannot connect to SQL Server"** → QUICK_REFERENCE.md - Common Issues
- **"JWT Token Invalid"** → Relogin in app
- **"No pool candidates"** → Check GPS coordinates are < 1 km apart
- **"Port 5108 in use"** → Kill existing process or change port

---

## 📝 TESTING FLOW (Recommended Order)

```
1️⃣  Setup Database (15 min) ✅
    └─→ Verify: 4 DBs exist + migration ran

2️⃣  Build Backend (5 min) ✅
    └─→ Verify: Running on :5108

3️⃣  Build MAUI (5 min) ✅
    └─→ Verify: App opens + no crash

4️⃣  API Test with Postman (10 min) ✅
    └─→ Verify: All 8 tests green

5️⃣  MAUI App Manual Test (15 min) ✅
    └─→ Verify: Login → Ghép cuốc works

6️⃣  Full Integration (20 min) ✅
    └─→ Verify: E2E flow successful
```

**Total Time: ~1 hour** ⏱️

---

## 🎓 KEY CONCEPTS

### Ghép Cuốc (Ride Pooling):
- **Purpose**: Kết hợp 2 cuốc cùng hướng để tiết kiệm chi phí
- **Criteria**: Pickup < 1km, Dropoff < 1km, Age < 5 min
- **Max**: 2 hành khách/xe
- **Result**: Cả 2 hành khách được thông báo + tiết kiệm 20-30%

### Architecture:
```
Frontend (MAUI)
    ↓ API calls + SignalR
Backend (ASP.NET)
    ↓ SQL queries
Database (SQL Server)
    ├─ Primary (Write)
    └─ Replica (Read)
```

### Flow:
```
1. User books trip
2. Driver accepts trip
3. Driver opens "Ghép Cuốc" tab
4. Click "Tìm cuốc phù hợp"
5. API calculates candidates using Haversine distance
6. Show list
7. Driver selects 1 + confirms
8. API pools trips + SignalR notifies both users
9. Both users see pooled trip info
```

---

## ✨ SUCCESS!

Once you've completed all steps:

- ✅ Backend running on port 5108
- ✅ MAUI app fully functional
- ✅ Database with pooling schema
- ✅ API endpoints tested
- ✅ Pooling feature working

### Next (Optional):
- 🚀 Deploy to production
- 📊 Add metrics & monitoring
- 🔄 Set up CI/CD pipeline
- 🧪 Load testing

---

## 📞 QUICK LINKS

| Resource | Link |
|----------|------|
| Detailed Setup | [BUILD_TEST_GUIDE_WINDOWS.md](BUILD_TEST_GUIDE_WINDOWS.md) |
| Quick Commands | [QUICK_REFERENCE.md](QUICK_REFERENCE.md) |
| Test Checklist | [TEST_CHECKLIST.md](TEST_CHECKLIST.md) |
| Postman API | [Postman_Pooling_Test_Collection.json](Postman_Pooling_Test_Collection.json) |
| Pooling Guide | [POOLING_IMPLEMENTATION_GUIDE.md](POOLING_IMPLEMENTATION_GUIDE.md) |

---

**Ready to build? Start with:** [BUILD_TEST_GUIDE_WINDOWS.md](BUILD_TEST_GUIDE_WINDOWS.md) 🚀

Good luck! 🍀
