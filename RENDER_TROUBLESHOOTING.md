# 🆘 RENDER TROUBLESHOOTING - GIẢI QUYẾT CÁC LỖI

## 📋 Danh Sách Lỗi Phổ Biến

1. [Build Failed](#build-failed)
2. [Application Startup Error](#application-startup-error)
3. [Database Connection Failed](#database-connection-failed)
4. [Static Files Not Serving](#static-files-not-serving)
5. [API Timeout](#api-timeout)
6. [Memory Issues](#memory-issues)
7. [CORS Error](#cors-error)
8. [Admin Dashboard Not Loading](#admin-dashboard-not-loading)

---

## ❌ BUILD FAILED

### Triệu chứng:
```
Build Error during deployment
Status: Failed
Logs show compilation errors
```

### 🔍 Nguyên nhân & Giải pháp:

#### ❌ Lỗi 1: Build Command Sai

**Error log:**
```
error: 'RideHailingApi.csproj' not found
```

**❌ Sai:**
```
dotnet publish -c Release -o out
dotnet publish RideHailingApi.csproj -c Release -o out
```

**✅ Đúng:**
```
dotnet publish RideHailingApi/RideHailingApi.csproj -c Release -o out
```

**Giải pháp:**
```
1. Render Dashboard → Service → Settings
2. Sửa Build Command:
   dotnet publish RideHailingApi/RideHailingApi.csproj -c Release -o out
3. Click [Save]
4. Render auto-redeploy
5. Kiểm tra logs
```

---

#### ❌ Lỗi 2: Project File Not Found

**Error log:**
```
The path 'RideHailingApi.csproj' does not exist
```

**Nguyên nhân:**
- Path sai đến file .csproj
- Root Directory set sai

**Giải pháp:**

**Option A: Fix Build Command**
```
Sửa từ:
dotnet publish RideHailingApi.csproj -c Release -o out

Sang:
dotnet publish RideHailingApi/RideHailingApi.csproj -c Release -o out

(Thêm folder path)
```

**Option B: Fix Root Directory**
```
1. Settings → Root Directory
2. Nhập: RideHailingApi
3. Click Save
4. Redeploy

Khi đó Build Command có thể:
dotnet publish . -c Release -o out
(Dấu . = current directory)
```

---

#### ❌ Lỗi 3: NuGet Package Not Found

**Error log:**
```
The project or solution does not contain the required packages
Could not restore NuGet packages
```

**Nguyên nhân:**
- Package reference không còn online
- Version mismatch
- Network timeout

**Giải pháp:**
```
1. Test locally trước:
   cd RideHailingApi
   dotnet clean
   dotnet restore
   dotnet build
   
2. Nếu fail locally, fix đó rồi push
   
3. Push to GitHub:
   git add .
   git commit -m "Fix build issues"
   git push origin main
   
4. Render auto-redeploy
```

---

#### ❌ Lỗi 4: Compilation Error

**Error log:**
```
error CS0103: The name 'xyz' does not exist in the current context
error CS1061: 'xyz' does not contain definition for 'abc'
```

**Nguyên nhân:**
- Code error trong project
- Missing using statements
- Breaking changes

**Giải pháp:**
```
1. Check error message cẩn thận
   
2. Fix lỗi locally:
   - Mở file có error
   - Sửa lỗi
   - Build locally: dotnet build
   
3. Test lại:
   dotnet run
   
4. Push fix:
   git add .
   git commit -m "Fix compilation error"
   git push origin main
   
5. Render tự redeploy
```

---

## ❌ APPLICATION STARTUP ERROR

### Triệu chứng:
```
Status: Failed
Logs: "Application failed to start"
App crashes immediately
```

### 🔍 Nguyên nhân & Giải pháp:

#### ❌ Lỗi 1: Start Command Sai

**Error log:**
```
File not found: out/RideHailingApi.dll
```

**❌ Sai:**
```
dotnet RideHailingApi.dll
dotnet RideHailingApi/RideHailingApi.dll
```

**✅ Đúng:**
```
dotnet out/RideHailingApi.dll
```

**Giải pháp:**
```
1. Settings → Start Command
2. Đổi thành: dotnet out/RideHailingApi.dll
3. Save & Redeploy
```

---

#### ❌ Lỗi 2: Missing Environment Variables

**Error log:**
```
NullReferenceException: Object reference not set
Exception: Cannot find configuration key 'ConnectionStrings:South_Primary'
```

**Nguyên nhân:**
- Env var chưa set
- Key name sai

**Giải pháp:**
```
1. Dashboard → Environment Variables
2. Thêm missing variables:
   - ConnectionStrings:South_Primary
   - ConnectionStrings:South_Replica
   - JwtSettings:SecretKey
   - v.v.
3. Save & Redeploy
```

---

#### ❌ Lỗi 3: Port Binding Error

**Error log:**
```
Error: Address already in use
Error: Cannot bind to http://0.0.0.0:5108
```

**Nguyên nhân:**
- Port cứng trong code
- Render port conflict

**Giải pháp:**
```
Verify Program.cs:

❌ Sai:
builder.WebHost.UseUrls("http://0.0.0.0:5108");

✅ Đúng:
var port = Environment.GetEnvironmentVariable("PORT") ?? "5108";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

Hoặc:
builder.WebHost.UseUrls("http://0.0.0.0:$PORT");
```

**Thực hiện:**
```
1. Fix Program.cs
2. Push to GitHub
3. Render redeploy
```

---

#### ❌ Lỗi 4: Unhandled Exception in Main

**Error log:**
```
Unhandled exception: System.Exception
at Program.cs line 123
```

**Giải pháp:**
```
1. Xem chi tiết error trong logs
2. Identify line số
3. Check code ở dòng đó
4. Fix locally
5. Test: dotnet run
6. Push & Redeploy
```

---

## ❌ DATABASE CONNECTION FAILED

### Triệu chứng:
```
Error: Cannot connect to database
Connection timeout
Login failed
```

### 🔍 Nguyên nhân & Giải pháp:

#### ❌ Lỗi 1: Connection String Format Sai

**Error log:**
```
Keyword not supported: 'password=xxx'
Invalid connection string
```

**✅ Đúng Format:**
```
Server=tcp:your-server.database.windows.net,1433;
Database=RideHailingDB;
User ID=admin;
Password=YourPassword;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

**Giải pháp:**
```
1. Check connection string format
2. Env var: ConnectionStrings:South_Primary
3. Paste full connection string
4. Test locally trước:
   
   // Test file
   using System.Data.SqlClient;
   var cs = "Server=...";
   using var conn = new SqlConnection(cs);
   conn.Open(); // Should work
   
5. Update Render env var
6. Redeploy
```

---

#### ❌ Lỗi 2: Database Not Accessible

**Error log:**
```
Connection timeout
Server not reachable
Login failed for user 'admin'
```

**Nguyên nhân:**
- Firewall rules sai
- Database offline
- Credentials sai

**Giải pháp:**

**Step 1: Check Database Status**
```
Azure Portal:
1. Go to SQL Server resource
2. Status: Online? ✅
3. Check if accessible
```

**Step 2: Whitelist Render IP**
```
Azure Portal:
1. SQL Server → Firewalls and virtual networks
2. Click [Add a firewall rule]
3. Add Render IP:
   - Start IP: 34.120.0.0
   - End IP: 34.120.255.255
   (Check Render docs for exact IPs)
4. Save
```

**Step 3: Verify Credentials**
```
1. Username: admin (hoặc whatever)
2. Password: Check carefully (case-sensitive!)
3. Database: RideHailingDB
```

**Step 4: Test Connection Locally**
```
PowerShell:
$cs = "Server=tcp:your-server.database.windows.net,1433;Database=RideHailingDB;User ID=admin;Password=xxx;Encrypt=True;TrustServerCertificate=False;"

(New-Object System.Data.SqlClient.SqlConnection $cs).Open()

# Should succeed without error
```

**Step 5: Update Render**
```
1. Environment → Update connection string
2. Redeploy
3. Check logs
```

---

#### ❌ Lỗi 3: Connection String in Environment Variable Sai

**Error log:**
```
Keyword not supported: 'xyz'
Invalid format
```

**Giải pháp:**
```
❌ Wrong way:
Key: ConnectionStrings:South_Primary
Value: $connectionstring (using variable)

✅ Right way:
Key: ConnectionStrings:South_Primary
Value: Server=tcp:your-server... (full string)

Note: Không dùng $variable trong Render env vars
Phải paste full connection string
```

---

## ❌ STATIC FILES NOT SERVING

### Triệu chứng:
```
Admin dashboard loads but:
- CSS không apply (website xấu)
- JavaScript không chạy
- Images không hiển thị
```

### 🔍 Nguyên nhân & Giải pháp:

#### ❌ Lỗi 1: UseDefaultFiles() và UseStaticFiles() Thiếu

**Giải pháp:**
```csharp
// Program.cs

❌ Sai:
var app = builder.Build();
app.MapControllers();
app.Run();

✅ Đúng:
var app = builder.Build();
app.UseDefaultFiles();    // ← Add this
app.UseStaticFiles();     // ← Add this
app.MapControllers();
app.Run();
```

**Thực hiện:**
```
1. Edit Program.cs
2. Add UseDefaultFiles() & UseStaticFiles()
3. Push: git push origin main
4. Render auto-redeploy
```

---

#### ❌ Lỗi 2: wwwroot Folder Not Included

**Giải pháp:**
```
1. Verify wwwroot folder exists:
   RideHailingApi/
   └── wwwroot/
       ├── admin.html
       ├── index.html
       ├── admin-styles.css
       └── admin-utils.js

2. If missing, create it:
   mkdir RideHailingApi/wwwroot
   
3. Add files there
4. Commit: git add .
5. Push: git push origin main
6. Render redeploy
```

---

#### ❌ Lỗi 3: File Path Case Sensitive

**Error log:**
```
404 Not Found: /admin.html
```

**Giải pháp:**
```
Check wwwroot files:
✅ Correct:
   - admin.html (lowercase)
   - admin-styles.css
   - admin-utils.js

❌ Wrong:
   - Admin.html (capital A)
   - ADMIN.HTML
   - admin-styles.CSS

Linux (Render) is case-sensitive!
Use lowercase filenames
```

---

## ❌ API TIMEOUT

### Triệu chứng:
```
API request takes too long
Endpoint times out after 30s
Response: 504 Gateway Timeout
```

### 🔍 Nguyên nhân & Giải pháp:

#### ❌ Lỗi 1: Database Query Slow

**Giải pháp:**
```
1. Check query performance locally
2. Add indexes if needed
3. Optimize SQL query
4. Test again: dotnet run
5. Push & Redeploy
```

---

#### ❌ Lỗi 2: API Calling External Service

**Giải pháp:**
```
1. Add timeout setting:
   client.Timeout = TimeSpan.FromSeconds(10);
   
2. Add retry logic
3. Implement caching
4. Test locally
5. Push & Redeploy
```

---

## ❌ MEMORY ISSUES

### Triệu chứng:
```
Status: Failed
Error: Out of memory
App restart repeatedly
```

### 🔍 Nguyên nhân & Giải pháp:

#### ❌ Lỗi 1: Memory Leak

**Giải pháp:**
```
1. Monitor memory usage:
   Render Dashboard → Metrics
   
2. Check for memory leaks:
   - Unfinished SqlConnection
   - Large List not cleared
   - Event handlers not unsubscribed
   
3. Fix code:
   using (var conn = new SqlConnection(cs))
   {
       conn.Open();
       // Use connection
   } // Auto dispose
   
4. Test: dotnet run
5. Push & Redeploy
```

---

#### ❌ Lỗi 2: Large Data Load

**Giải pháp:**
```
1. Implement pagination:
   SELECT TOP 50 * FROM Users
   (Not all at once)
   
2. Lazy loading
3. Caching frequent data
4. Push & Redeploy
```

---

## ❌ CORS ERROR

### Triệu chứng:
```
Browser console error:
"Access to XMLHttpRequest blocked by CORS policy"
```

### 🔍 Nguyên nhân & Giải pháp:

**Giải pháp:**
```csharp
// Program.cs

var allowed = new[] 
{ 
    "https://ride-hailing-api.onrender.com",
    "http://localhost:5108",
    "http://localhost:3000"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecific", p => p
        .WithOrigins(allowed)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

app.UseCors("AllowSpecific");
```

**Thực hiện:**
```
1. Update Program.cs
2. Test locally
3. Push & Redeploy
```

---

## ❌ ADMIN DASHBOARD NOT LOADING

### Triệu chứng:
```
Mở: https://ride-hailing-api.onrender.com/admin.html
Kết quả: 404 Not Found hoặc blank page
```

### 🔍 Nguyên nhân & Giải pháp:

#### ❌ Lỗi 1: admin.html Not Found

**Giải pháp:**
```
1. Check file exists:
   RideHailingApi/wwwroot/admin.html ✅
   
2. If missing, create it from backup:
   git checkout origin/main -- RideHailingApi/wwwroot/admin.html
   
3. Commit & Push
4. Redeploy
```

---

#### ❌ Lỗi 2: CSS/JS Not Loading

**Giải pháp:**
```
1. Check paths trong admin.html:
   ✅ <link rel="stylesheet" href="admin-styles.css">
   ✅ <script src="admin-utils.js"></script>
   
   ❌ sai:
   <link rel="stylesheet" href="/admin-styles.css">
   (leading slash causes issues)
   
2. Update paths
3. Push & Redeploy
```

---

#### ❌ Lỗi 3: API Calls Fail

**Giải pháp:**
```javascript
// In admin.html JavaScript

❌ Sai:
const API_BASE_URL = "http://localhost:5108/api/admin";

✅ Đúng:
const API_BASE_URL = window.location.origin + "/api/admin";
// Or:
const API_BASE_URL = "https://ride-hailing-api.onrender.com/api/admin";

// For production, use relative URL:
const API_BASE_URL = "/api/admin";
```

---

## 🔧 Debugging Steps

### Step 1: Check Logs
```
Render Dashboard:
1. Click service
2. Tab "Logs"
3. Scroll đến error
4. Read error message carefully
```

### Step 2: Verify Build Success
```
Logs should show:
✅ Build succeeded
✅ Deployment completed
✅ Health check passed
```

### Step 3: Test Endpoints
```
Browser:
1. Homepage: https://ride-hailing-api.onrender.com
2. Admin: https://ride-hailing-api.onrender.com/admin.html
3. API: https://ride-hailing-api.onrender.com/api/admin/stats
4. Health: https://ride-hailing-api.onrender.com/health
```

### Step 4: Check Environment
```
Render Settings:
1. Verify all env vars present
2. Check values are correct
3. No typos in keys
```

### Step 5: Redeploy
```
Render Dashboard:
1. Click [Redeploy] button
2. Wait for new build
3. Check logs
4. Test again
```

---

## 📞 Cần Hỗ Trợ Thêm?

### Resources:
- **Render Docs**: https://render.com/docs
- **Common Issues**: https://render.com/docs/troubleshooting
- **.NET Docs**: https://learn.microsoft.com/aspnet/core

### Collect Info Để Ask For Help:
```
1. Full error message từ logs
2. Build command bạn dùng
3. Start command bạn dùng
4. Environment variables (except secrets)
5. GitHub repo link
6. Steps bạn đã thử
```

---

**✅ Hopefully giải quyết được vấn đề! Good luck!** 🚀
