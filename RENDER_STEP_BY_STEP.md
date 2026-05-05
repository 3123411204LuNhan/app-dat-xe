# 🚀 HƯỚNG DẪN DEPLOY LÊN RENDER - CHI TIẾT TỪNG BƯỚC

## 📋 Mục Lục
1. [Chuẩn bị](#chuẩn-bị)
2. [Tạo tài khoản Render](#tạo-tài-khoản-render)
3. [Kết nối GitHub](#kết-nối-github)
4. [Tạo Web Service](#tạo-web-service)
5. [Cấu hình Build & Start](#cấu-hình-build--start)
6. [Environment Variables](#environment-variables)
7. [Deploy](#deploy)
8. [Kiểm tra & Troubleshooting](#kiểm-tra--troubleshooting)

---

## 🔧 Chuẩn bị

### Bước 1: Đảm bảo code sẵn sàng
```bash
# 1. Pull code mới nhất
git pull origin main

# 2. Verify build
cd RideHailingApi
dotnet clean
dotnet build

# 3. Test local
dotnet run
# Kiểm tra: http://localhost:5108/admin.html

# 4. Commit final changes (nếu có)
git add .
git commit -m "Ready for Render deployment"
git push origin main
```

### Bước 2: Chuẩn bị thông tin
- ✅ GitHub account (đã có)
- ✅ Repository: `App-Dat-Xe/app-dat-xe`
- ✅ Branch: `main`
- ✅ SQL Server connection strings (chuẩn bị sẵn)

---

## 🌐 Tạo Tài Khoản Render

### Bước 1: Truy cập Render.com
1. **Mở trình duyệt** → nhập: https://render.com
2. **Nhấn "Sign Up"** (góc phải trên)

### Bước 2: Chọn GitHub Sign Up
```
[Sign Up with GitHub] ← Click cái này
```
- Nhập email GitHub (hoặc tài khoản GitHub)
- GitHub sẽ hỏi xác nhận - Click "Authorize"
- Render sẽ tạo tài khoản tự động

### Bước 3: Verify Email
- Render gửi email xác nhận
- Click link trong email để verify
- ✅ Tài khoản sẵn sàng!

**Lưu ý**: Không cần nhập credit card cho Free tier

---

## 🔗 Kết Nối GitHub

### Bước 1: Authorize GitHub
1. Sau khi đăng nhập Render, click **"Dashboard"**
2. Click **"Connect GitHub"** (nếu chưa)
3. GitHub hỏi permissions:
   - Chọn **"Authorize"**
   - Render được quyền access repo của bạn

### Bước 2: Chọn Repository
Render hiển thị các repo:
```
✅ App-Dat-Xe/app-dat-xe (repository của bạn)
```
- Click để chọn
- Hoặc tích "All repositories"

**Kết quả**: GitHub connected ✅

---

## 🏗️ Tạo Web Service

### Bước 1: Vào Dashboard
```
1. Render Homepage → "Dashboard" (top right)
2. Hoặc: render.com/dashboard
```

### Bước 2: Tạo Web Service Mới
```
Click: [New +] → [Web Service]
```

**Hiển thị lựa chọn:**
```
✓ GitHub (Select a repository)
  Web Services (existing)
  Databases
```

### Bước 3: Chọn Repository
1. Tìm: **`app-dat-xe`**
2. Click chọn
3. Render sẽ load repo info

**Kết quả:**
```
Repository: App-Dat-Xe/app-dat-xe
Branch: main (tự chọn)
```

---

## ⚙️ Cấu Hình Build & Start

### Bước 1: Điền Thông Tin Cơ Bản

**Màn hình Render sẽ có:**

#### 1️⃣ Name
```
Tên ứng dụng: ride-hailing-api
(Sẽ dùng trong URL: ride-hailing-api.onrender.com)

💡 Mẹo: Dùng tên ngắn, không có space
```

#### 2️⃣ Region
```
Chọn: Singapore
(Gần Việt Nam nhất, tốc độ tốt)

Lựa chọn khác:
- Frankfurt (Europe)
- Ohio (North America)
- Oregon (West Coast US)
```

#### 3️⃣ Branch
```
Chọn: main
(Branch bạn push code lên)
```

#### 4️⃣ Root Directory
```
Để trống hoặc: RideHailingApi
(Folder chứa .csproj file)
```

#### 5️⃣ Runtime
```
Chọn: .NET
(Render auto-detect từ .csproj)
```

### Bước 2: Build Command
```bash
Dán cái này:

dotnet publish RideHailingApi/RideHailingApi.csproj -c Release -o out
```

**Giải thích:**
- `dotnet publish` - Compile release
- `RideHailingApi/RideHailingApi.csproj` - Path to project file
- `-c Release` - Release configuration
- `-o out` - Output to `out` folder

### Bước 3: Start Command
```bash
Dán cái này:

dotnet out/RideHailingApi.dll
```

**Giải thích:**
- Chạy file .dll đã compile
- `out/` - Folder từ build step
- Render sẽ auto-set PORT từ environment

---

## 🔐 Environment Variables

### Bước 1: Scroll xuống "Environment"

### Bước 2: Thêm Environment Variables
Click **"Add Environment Variable"** để thêm từng cái:

#### ⭐ REQUIRED Variables:

**1. ASPNETCORE_ENVIRONMENT**
```
Key: ASPNETCORE_ENVIRONMENT
Value: Production
```

**2. ASPNETCORE_URLS**
```
Key: ASPNETCORE_URLS
Value: http://0.0.0.0:$PORT
(Render tự thay $PORT với port thực)
```

#### 📊 Database Connections:

Bạn cần SQL Server connection strings. Cách lấy:

**Lấy SQL Server Connection String:**
```
Server=tcp:[your-server].database.windows.net,1433;
Database=RideHailingDB;
User ID=admin;
Password=your-password;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

**Thêm vào Render:**

**3. South_Primary** (Database chính)
```
Key: ConnectionStrings:South_Primary
Value: [your-sql-connection-string]
```

**4. South_Replica** (Database backup)
```
Key: ConnectionStrings:South_Replica
Value: [your-replica-connection-string]
(Hoặc giống South_Primary nếu chưa có Replica)
```

**5. North_Primary**
```
Key: ConnectionStrings:North_Primary
Value: [your-north-connection-string]
```

**6. North_Replica**
```
Key: ConnectionStrings:North_Replica
Value: [your-north-replica-connection-string]
```

#### 🔑 JWT Settings:

**7. JWT Secret Key**
```
Key: JwtSettings:SecretKey
Value: [Generate long random string]

💡 Generate:
Mở PowerShell:
[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((New-Guid).ToString() + (New-Guid).ToString()))

Hoặc dùng: https://www.uuidgenerator.net/
```

**8. JWT Issuer**
```
Key: JwtSettings:Issuer
Value: ride-hailing-api
```

**9. JWT Audience**
```
Key: JwtSettings:Audience
Value: ride-hailing-app
```

#### 🌐 CORS Settings:

**10. AllowedOrigins** (Optional)
```
Key: AllowedOrigins:0
Value: https://[your-app].onrender.com

Hoặc multiple:
Key: AllowedOrigins:0
Value: https://ride-hailing-api.onrender.com
Key: AllowedOrigins:1
Value: https://localhost:7285
Key: AllowedOrigins:2
Value: http://localhost:5108
```

---

## 📤 Deploy

### Bước 1: Review Cấu Hình
```
✅ Name: ride-hailing-api
✅ Region: Singapore
✅ Branch: main
✅ Build Command: ✓
✅ Start Command: ✓
✅ Environment Variables: ✓ (All added)
```

### Bước 2: Click "Create Web Service"
```
Nút to dưới cùng: [Create Web Service]
Render sẽ:
1. Clone repository
2. Run build command
3. Deploy
4. Start application
```

### Bước 3: Chờ Deploy
```
Render Dashboard sẽ hiển thị logs:

✓ Building...
✓ Installing .NET...
✓ dotnet publish...
✓ Starting...
✓ Health check passed
✓ Live on: https://ride-hailing-api.onrender.com
```

**Thời gian**: ~5-10 phút

### Bước 4: Deploy Hoàn Thành ✅
```
Render sẽ hiển thị:
🎉 Deploy successful!

URL: https://ride-hailing-api.onrender.com
Admin: https://ride-hailing-api.onrender.com/admin.html
```

---

## ✅ Kiểm Tra & Testing

### Bước 1: Test Homepage
```
Mở: https://ride-hailing-api.onrender.com

Kết quả expected:
- Trang welcome hiển thị
- Welcome page của bạn (index.html)
```

### Bước 2: Test Admin Dashboard
```
Mở: https://ride-hailing-api.onrender.com/admin.html

Kết quả expected:
- Admin dashboard load được
- Stats cards hiển thị
- Menu hoạt động
```

### Bước 3: Test API Endpoints
```
Mở: https://ride-hailing-api.onrender.com/api/admin/stats

Kết quả expected (JSON):
{
  "totalUsers": 10,
  "totalDrivers": 5,
  "totalTrips": 20,
  "totalRevenue": 1000000,
  ...
}
```

### Bước 4: Kiểm tra Build Logs
```
Render Dashboard:
1. Click vào service
2. Tab "Logs"
3. Xem chi tiết build/runtime logs
```

---

## 🆘 Troubleshooting

### ❌ Lỗi: Build failed

**Kiểm tra:**
1. Click vào service trong dashboard
2. Vào tab "Logs"
3. Xem error message

**Nguyên nhân phổ biến:**

#### A. Build Command sai
```
❌ Sai:
dotnet publish -c Release -o out

✅ Đúng:
dotnet publish RideHailingApi/RideHailingApi.csproj -c Release -o out
```

#### B. Project file không tìm thấy
```
❌ Sai path:
dotnet publish RideHailingApi.csproj -c Release -o out

✅ Đúng path:
dotnet publish RideHailingApi/RideHailingApi.csproj -c Release -o out
```

#### C. Dependencies missing
```
Fix:
1. Verify .csproj file locally
2. Run: dotnet restore
3. Commit changes
4. Re-deploy
```

### ❌ Lỗi: Application startup failed

**Kiểm tra Logs:**
```
Error: Unable to connect to database
```

**Giải pháp:**
1. Verify connection strings trong environment variables
2. Kiểm tra SQL Server firewall rules
3. Kiểm tra server accessible từ internet
4. Test connection string locally

**Add firewall rule cho Render IP:**
```
Render IP: 34.120.x.x (search "Render IP whitelist")
SQL Server: Allow outbound từ Render
```

### ❌ Lỗi: Admin dashboard không load

**Kiểm tra:**
```
1. Trang home có load không?
2. Static files có serve không?
3. Kiểm tra logs
```

**Giải pháp:**
```csharp
// Verify Program.cs có:
app.UseDefaultFiles();    ✅
app.UseStaticFiles();     ✅

// Build & Push lại:
git add .
git commit -m "Fix static files"
git push origin main

// Render sẽ auto redeploy
```

### ❌ Lỗi: Application times out

**Kiểm tra:**
```
1. Database connection timeout?
2. External API timeout?
3. Heavy processing?
```

**Giải pháp:**
```
1. Increase timeout in config
2. Optimize queries
3. Add caching
4. Check database health
```

---

## 📊 Monitoring & Logs

### Xem Logs Real-time
```
1. Render Dashboard
2. Click service name
3. Tab "Logs"
4. Scroll xem real-time logs

💡 Logs update mỗi 30 giây
```

### Xem Events
```
1. Tab "Events"
2. Xem deployment history
3. Auto-redeploy khi push
```

### Health Check
```
Render tự động monitor:
- Healthy (green) ✅
- Starting (yellow)
- Failed (red) ❌
```

---

## 🔄 Auto-Deploy Setup

### Render có tính năng Auto-Deploy:
```
Khi bạn:
1. Commit code
2. Push lên GitHub (main branch)

Render sẽ:
3. Auto detect changes
4. Re-build (5-10 phút)
5. Auto deploy
6. Zero downtime
```

### Disable Auto-Deploy (nếu cần):
```
1. Service Settings
2. Toggle "Auto-Deploy" → OFF
3. Deploy manually khi cần
```

---

## 💡 Best Practices

### 1. Environment Variables
```
✅ Lưu sensitive data (password, keys) trong env vars
❌ Không commit secrets vào git
✅ Sử dụng .gitignore để exclude
```

### 2. Database
```
✅ Dùng Azure SQL / AWS RDS (cloud)
❌ Không để database ở máy local
✅ Whitelist Render IP
✅ Dùng connection pooling
```

### 3. Monitoring
```
✅ Monitor logs thường xuyên
✅ Set up alerts (nếu có)
✅ Test health endpoints: /health
✅ Kiểm tra admin dashboard định kỳ
```

### 4. Updates
```
✅ Test locally trước khi push
✅ Commit messages rõ ràng
✅ Push small commits (dễ rollback)
❌ Không push huge changes
```

---

## 📝 Thông Tin Hữu Ích

### Render Dashboard URL
```
https://dashboard.render.com
```

### Your App URL
```
https://[your-service-name].onrender.com
```

### Admin Dashboard
```
https://[your-service-name].onrender.com/admin.html
```

### API Base
```
https://[your-service-name].onrender.com/api/
```

### Health Check
```
https://[your-service-name].onrender.com/health
```

---

## 🎯 Checklist Cuối Cùng

- [ ] GitHub repo ready (pushed)
- [ ] Code builds locally ✓
- [ ] Render account created
- [ ] GitHub authorized
- [ ] Web Service created
- [ ] Build Command set
- [ ] Start Command set
- [ ] All env vars added
- [ ] Database connection tested
- [ ] Deploy clicked
- [ ] Build successful (logs checked)
- [ ] Homepage loads ✓
- [ ] Admin dashboard loads ✓
- [ ] API working ✓
- [ ] Share URL with team 🎉

---

## 🚀 Sau Khi Deploy

### 1. Share URL
```
Admin: https://ride-hailing-api.onrender.com/admin.html
API: https://ride-hailing-api.onrender.com/api/
```

### 2. Update Mobile App
```
Thay API URL:
const API_BASE_URL = "https://ride-hailing-api.onrender.com";
```

### 3. Set Up Monitoring
```
- Health checks
- Performance monitoring
- Error logging
- Database health
```

### 4. Document Deployment
```
- Save all env vars (secure)
- Document build/start commands
- Document any special setup
- Create runbook
```

---

## 📞 Cần Giúp Đỡ?

### Resources:
- **Render Docs**: https://render.com/docs
- **Render Support**: support@render.com
- **.NET Deploy**: https://learn.microsoft.com/aspnet/core/host-and-deploy

### Troubleshooting:
- Xem `RENDER_DEPLOYMENT_GUIDE.md` (general guide)
- Xem Logs trong Render Dashboard
- Check GitHub repo status
- Test locally: `dotnet run`

---

**✅ Ready to deploy! Follow steps trên và thành công!** 🚀

**Time: ~15-20 phút từ start đến deploy thành công**
