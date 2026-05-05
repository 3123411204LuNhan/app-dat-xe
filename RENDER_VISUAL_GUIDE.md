# 🖼️ HÌNH MINH HỌA - HƯỚNG DẪN RENDER

## 📌 Render Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  Render.com Dashboard                                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  👤 Your Account (top right)  │  [Dashboard]  │  [Settings]    │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  📊 Services                                                     │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ ✅ ride-hailing-api (Web Service)                       │   │
│  │                                                          │   │
│  │ Status: 🟢 Live & Running                              │   │
│  │ URL: https://ride-hailing-api.onrender.com            │   │
│  │ Last Deploy: 2 hours ago                                │   │
│  │ CPU: 45% | RAM: 120MB / 512MB                          │   │
│  │                                                          │   │
│  │ [View Logs] [Settings] [Redeploy] [Delete]           │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  [New +] [Web Service] [Database] [Cron Job]                  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📋 Tạo Web Service - Step 1: Name & Region

```
┌─────────────────────────────────────────────────────────────────┐
│  Create a New Web Service                                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1️⃣ Name (Service name)                                         │
│     ┌────────────────────────────────────────┐                 │
│     │ ride-hailing-api                        │  ← Tên service │
│     └────────────────────────────────────────┘                 │
│     💡 Sẽ là: ride-hailing-api.onrender.com                  │
│                                                                  │
│  2️⃣ Region (Khu vực)                                            │
│     ┌────────────────────────────────────────┐                 │
│     │ [Singapore ▼]   ← Chọn Singapore      │                 │
│     └────────────────────────────────────────┘                 │
│     Lựa chọn:                                                    │
│     - Singapore (Gần VN nhất) ✅                                │
│     - Frankfurt (Europe)                                        │
│     - Ohio (North America)                                      │
│     - Oregon (West Coast)                                       │
│                                                                  │
│  3️⃣ Branch                                                       │
│     ┌────────────────────────────────────────┐                 │
│     │ [main ▼]                                │  ← Main branch │
│     └────────────────────────────────────────┘                 │
│                                                                  │
│  4️⃣ Root Directory (Folder)                                     │
│     ┌────────────────────────────────────────┐                 │
│     │                                         │  ← Để trống    │
│     └────────────────────────────────────────┘                 │
│     (Hoặc: RideHailingApi)                                     │
│                                                                  │
│  5️⃣ Runtime                                                      │
│     ┌────────────────────────────────────────┐                 │
│     │ [.NET ▼]                                │  ← .NET       │
│     └────────────────────────────────────────┘                 │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🛠️ Cấu Hình Build & Start Command

```
┌─────────────────────────────────────────────────────────────────┐
│  Build & Deploy Configuration                                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Build Command                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ dotnet publish RideHailingApi/RideHailingApi.csproj \   │  │
│  │   -c Release -o out                                      │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                  │
│  Start Command                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ dotnet out/RideHailingApi.dll                            │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                  │
│  Instance Type                                                   │
│  [Standard (512MB RAM)] ← Free tier                            │
│                                                                  │
│  Auto-Deploy                                                     │
│  [✓] Yes (tự deploy khi push)                                │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔐 Environment Variables - Phần 1

```
┌─────────────────────────────────────────────────────────────────┐
│  Environment Variables                                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [Add Environment Variable]  ← Click để thêm                   │
│                                                                  │
│  Variable 1:                                                     │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ Key:   ASPNETCORE_ENVIRONMENT                         │    │
│  │ Value: Production                                      │    │
│  │        [✓ Save]                                        │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Variable 2:                                                     │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ Key:   ASPNETCORE_URLS                                │    │
│  │ Value: http://0.0.0.0:$PORT                           │    │
│  │        [✓ Save]                                        │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Variable 3:                                                     │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ Key:   ConnectionStrings:South_Primary                │    │
│  │ Value: Server=tcp:your-server.database.windows.net... │    │
│  │        [✓ Save]                                        │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Variable 4:                                                     │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ Key:   ConnectionStrings:South_Replica                │    │
│  │ Value: Server=tcp:your-replica.database.windows.net..│    │
│  │        [✓ Save]                                        │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
│  [Còn nhiều variables khác...]                                 │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📊 Deployment Process

```
Timeline:

1️⃣ Click [Create Web Service]
   |
   ↓
2️⃣ Render clones GitHub repo
   |
   ├─ Downloading code...
   └─ ~30 seconds
   |
   ↓
3️⃣ Install .NET & Dependencies
   |
   ├─ Installing .NET 10...
   ├─ Restoring NuGet packages...
   └─ ~2 minutes
   |
   ↓
4️⃣ Build (dotnet publish)
   |
   ├─ Compiling code...
   ├─ Building release...
   ├─ Creating output...
   └─ ~3-5 minutes
   |
   ↓
5️⃣ Start Application
   |
   ├─ Starting dotnet app...
   ├─ Listening on port...
   ├─ Health check passed ✅
   └─ ~30 seconds
   |
   ↓
6️⃣ Live! 🎉
   |
   └─ URL: https://ride-hailing-api.onrender.com
      Status: 🟢 Running
      Time: ~5-10 minutes total
```

---

## 📂 Logs Display (Success)

```
┌─────────────────────────────────────────────────────────────────┐
│  Logs - ride-hailing-api                                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  === Deploy Log ===                                             │
│                                                                  │
│  [2024-XX-XX XX:XX:XX] Building...                            │
│  [2024-XX-XX XX:XX:XX] Cloning repository                     │
│  [2024-XX-XX XX:XX:XX] $ dotnet publish RideHailingApi...    │
│  [2024-XX-XX XX:XX:XX] Restoring packages                     │
│  [2024-XX-XX XX:XX:XX] MSBuild version                        │
│  [2024-XX-XX XX:XX:XX] Build succeeded                        │
│  [2024-XX-XX XX:XX:XX] Starting service                       │
│  [2024-XX-XX XX:XX:XX] info: Microsoft.Hosting.Lifetime[0]    │
│  [2024-XX-XX XX:XX:XX] Now listening on: http://0.0.0.0:xxxx │
│  [2024-XX-XX XX:XX:XX] Application started                    │
│  [2024-XX-XX XX:XX:XX] Health check OK                        │
│  [2024-XX-XX XX:XX:XX] Service is Live                        │
│  [2024-XX-XX XX:XX:XX] Healthy ✅                             │
│                                                                  │
│  🟢 Status: Live & Running                                     │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## ✅ Successful Deployment

```
┌─────────────────────────────────────────────────────────────────┐
│  🎉 Deployment Successful!                                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Service: ride-hailing-api                                      │
│  Status: 🟢 Live & Running                                      │
│                                                                  │
│  URLs:                                                           │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ Homepage:                                              │    │
│  │ https://ride-hailing-api.onrender.com                │    │
│  │                                                        │    │
│  │ Admin Dashboard:                                       │    │
│  │ https://ride-hailing-api.onrender.com/admin.html    │    │
│  │                                                        │    │
│  │ API Base:                                              │    │
│  │ https://ride-hailing-api.onrender.com/api/           │    │
│  │                                                        │    │
│  │ Health Check:                                          │    │
│  │ https://ride-hailing-api.onrender.com/health         │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Resources:                                                      │
│  CPU: 45% | RAM: 150MB / 512MB                                │
│  Last Deploy: Just now                                         │
│                                                                  │
│  Next Deploy: Auto (when code pushed)                         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔍 Testing After Deploy

```
Step 1: Test Homepage
┌──────────────────────────────────┐
│ Browser:                         │
│ https://ride-hailing-api.onrender.com
│                                  │
│ Result:                          │
│ ✅ Welcome page hiển thị        │
│ ✅ Responsive design            │
│ ✅ Static files load            │
└──────────────────────────────────┘


Step 2: Test Admin Dashboard
┌──────────────────────────────────┐
│ Browser:                         │
│ https://ride-hailing-api.onrender.com/admin.html
│                                  │
│ Result:                          │
│ ✅ Dashboard loads              │
│ ✅ Stats cards show data        │
│ ✅ Charts render                │
│ ✅ Menu works                   │
│ ✅ Dark mode toggle works       │
└──────────────────────────────────┘


Step 3: Test API
┌──────────────────────────────────┐
│ Browser or Postman:             │
│ https://ride-hailing-api.onrender.com/api/admin/stats
│                                  │
│ Result (JSON):                   │
│ {                               │
│   "totalUsers": 10,             │
│   "totalDrivers": 5,            │
│   "totalTrips": 20,             │
│   "totalRevenue": 1000000       │
│ }                               │
│                                  │
│ ✅ API working                  │
└──────────────────────────────────┘
```

---

## 🆘 Troubleshooting - Build Failed

```
❌ Build Error Example:

Error: 'RideHailingApi/RideHailingApi.csproj' not found

Check:
1. Build Command correct?
   ✅ Correct:
      dotnet publish RideHailingApi/RideHailingApi.csproj -c Release -o out
   
   ❌ Wrong:
      dotnet publish RideHailingApi.csproj -c Release -o out
      (Path missing)

2. Root Directory correct?
   ✅ Leave empty (Render finds it)
   ❌ Don't put wrong path

3. Check logs:
   Render Dashboard → Logs tab
   Scroll and find actual error

Solution:
1. Fix Build Command
2. Commit & Push: git push origin main
3. Render auto-redeploy
4. Check logs again
```

---

## 🆘 Troubleshooting - Startup Failed

```
❌ Startup Error Example:

Error: Unable to connect to database

Check:
1. Connection string correct?
   ✅ Test locally first
   
2. Database accessible?
   ✅ Check SQL Server firewall
   ✅ Allow Render IP
   
3. Env var set correctly?
   ✅ ConnectionStrings:South_Primary = [full-connection-string]
   ❌ Don't use $variables in connection string

4. Check logs:
   Render Dashboard → Logs tab
   Look for database error

Solution:
1. Test connection string locally
2. Whitelist Render IP in SQL Server
3. Update env vars
4. Redeploy: [Redeploy] button
5. Check logs
```

---

## 🔄 Auto-Redeploy On Push

```
Workflow:

1. You make changes locally
   git add .
   git commit -m "Add new feature"
   
2. Push to GitHub
   git push origin main
   
3. GitHub notifies Render
   
4. Render auto-detects changes
   
5. Render starts new build
   Status: Building...
   
6. New deployment starts
   Status: Deploying...
   
7. Live update (no downtime)
   Status: Live ✅
   
8. Zero downtime deployment!

Timeline: ~5-10 minutes for full redeploy
```

---

## 📊 Dashboard After Deploy

```
┌─────────────────────────────────────────────────────────────────┐
│  Render Dashboard - Your Service                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  🟢 ride-hailing-api (Live)                                    │
│                                                                  │
│  Tabs: [Overview] [Logs] [Events] [Settings] [Metrics]        │
│                                                                  │
│  ├─ URL: https://ride-hailing-api.onrender.com               │
│  ├─ Status: 🟢 Running                                         │
│  ├─ Instance: web-1                                             │
│  ├─ Memory: 150MB / 512MB (29%)                               │
│  ├─ CPU: 45%                                                    │
│  └─ Uptime: 2h 30m                                             │
│                                                                  │
│  Events:                                                         │
│  • 2024-XX-XX XX:XX - Deployment successful ✅               │
│  • 2024-XX-XX XX:XX - Build completed                        │
│  • 2024-XX-XX XX:XX - Health check passed                    │
│                                                                  │
│  Actions:                                                        │
│  [View Logs] [Redeploy] [Restart] [Settings] [Delete]        │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

**🎉 Deployment successful! Your app is live!** 🚀
