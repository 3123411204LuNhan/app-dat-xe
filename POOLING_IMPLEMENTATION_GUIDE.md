# Hướng Dẫn Triển Khai Chức Năng Ghép Cuốc

## ✅ Những gì đã hoàn thành

### Backend (RideHailingApi)
1. **Models** mới trong [Models/TripDtos.cs](RideHailingApi/Models/TripDtos.cs):
   - `PoolingCandidateItem` - Thông tin cuốc có thể ghép
   - `PoolTripsRequest` - Request ghép 2 cuốc
   - `PooledTripInfo` - Info cuốc ghép

2. **API Endpoints** trong [Controllers/TripsController.cs](RideHailingApi/Controllers/TripsController.cs):
   - `GET /api/trips/pool-candidates/{tripId}` - Tìm cuốc phù hợp để ghép
   - `POST /api/trips/pool` - Ghép 2 cuốc
   - `GET /api/trips/pooled/{tripId}` - Xem info cuốc ghép

3. **Services**:
   - [Services/GeoDistanceHelper.cs](RideHailingApi/Services/GeoDistanceHelper.cs) - Tính khoảng cách (Haversine formula)

4. **Database Migration** trong [sql_server_script/migration_pooling_schema.sql](sql_server_script/migration_pooling_schema.sql):
   ```sql
   ALTER TABLE Trips ADD PooledWithTripID INT, MaxPassengers INT, CurrentPassengers INT
   ```

### Frontend (RideHailingApp - MAUI)
1. **Models** trong [Services/ApiModels.cs](RideHailingApp/Services/ApiModels.cs):
   - `PoolingCandidateItem` - Candidate trip
   - `PooledTripInfo` - Pooled trip info

2. **API Methods** trong [Services/ApiService.cs](RideHailingApp/Services/ApiService.cs):
   - `GetPoolCandidatesAsync()` - Tìm candidates
   - `PoolTripsAsync()` - Ghép cuốc
   - `GetPooledTripInfoAsync()` - Lấy info

3. **SignalR Event** trong [Services/TripHubService.cs](RideHailingApp/Services/TripHubService.cs):
   - `PoolingNotification` event - Nhận thông báo ghép cuốc

4. **UI Components**:
   - [PoolingTabView.xaml](RideHailingApp/PoolingTabView.xaml) - Tab ghép cuốc
   - [PoolingTabView.xaml.cs](RideHailingApp/PoolingTabView.xaml.cs) - Logic tab ghép

---

## 🔧 Các Bước Triển Khai

### Step 1: Cập nhật Database Schema
Chạy script này trên cả 4 databases (North Primary, North Replica, South Primary, South Replica):

```bash
sqlcmd -S <SERVER_NAME> -d <DATABASE_NAME> -i migration_pooling_schema.sql
```

Hoặc chạy trực tiếp trong SQL Server Management Studio.

### Step 2: Cập nhật Backend API (Optional - nếu chưa làm)
Backend đã có tất cả các endpoint cần thiết. Kiểm tra:
- ✅ GeoDistanceHelper tính khoảng cách
- ✅ TripsController có 3 pool endpoints
- ✅ TripDtos có các model mới

### Step 3: Cập nhật Frontend XAML (DriverHomePage)

**Option A: Thay thế toàn bộ** (Khuyến nghị)
1. Backup file hiện tại: `DriverHomePage.xaml` → `DriverHomePage.xaml.bak`
2. Xóa [DriverHomePage_Updated.xaml](DriverHomePage_Updated.xaml) thành [DriverHomePage.xaml](DriverHomePage.xaml)
3. Thêm namespace vào đầu file XAML:
   ```xml
   xmlns:local="clr-namespace:RideHailingApp"
   ```

**Option B: Thêm từng phần** (Nếu muốn giữ code hiện tại)
1. Thêm `<local:PoolingTabView x:Name="PoolingTab"/>` vào một nơi phù hợp
2. Hoặc thêm 1 Button "Ghép Cuốc" để hiển thị/ẩn pooling UI

### Step 4: Cập nhật DriverHomePage.xaml.cs

Thêm các phần sau vào file code-behind:

```csharp
private PoolingTabView _poolingTab;

// Trong constructor sau InitializeComponent():
_poolingTab = this.FindByName<PoolingTabView>("PoolingTab");
if (_poolingTab != null)
    _hub.PoolingNotification += OnPoolingNotification;

// Khi tài xế chấp nhận cuốc, thêm dòng này vào OnAcceptTripClicked():
if (_poolingTab != null)
{
    // Parse GPS từ pickup/dropoff nếu có (hoặc dùng GPS hiện tại)
    var currentLoc = await Geolocation.GetLastKnownLocationAsync();
    if (currentLoc != null)
    {
        _poolingTab.Initialize(tripId, 
            currentLoc.Latitude, currentLoc.Longitude,
            currentLoc.Latitude, currentLoc.Longitude); // Simplify: dùng vị trí hiện tại
    }
}

// Event handler cho pooling notification
private void OnPoolingNotification(string poolingType, string message)
{
    MainThread.BeginInvokeOnMainThread(() =>
    {
        DisplayAlert("📢 Thông báo ghép cuốc", message, "OK");
    });
}
```

### Step 5: Kiểm Tra & Test

1. **Chạy Backend**:
   ```bash
   dotnet run --project RideHailingApi
   ```

2. **Chạy MAUI App**:
   - Android: F5 hoặc `dotnet maui build -f net9.0-android -c Debug`
   - Windows: `dotnet maui build -f net9.0-windows10.0.19041.0 -c Debug`

3. **Test Flow**:
   - ✅ Tài xế bật "Nhận Cuốc"
   - ✅ Nhận 1-2 cuốc mới
   - ✅ Chấp nhận 1 cuốc (Nhận)
   - ✅ Chuyển sang tab "Ghép Cuốc"
   - ✅ Click "Tìm cuốc phù hợp"
   - ✅ Xem danh sách candidates
   - ✅ Click "Ghép 🚗" để ghép 2 cuốc

---

## 📊 Tiêu Chí Ghép Cuốc

Hai cuốc sẽ được gợi ý ghép nếu thỏa:

| Tiêu chí | Giá trị |
|---------|--------|
| Khoảng cách Pickup | ≤ 1.0 km |
| Khoảng cách Dropoff | ≤ 1.0 km |
| Thời gian tạo | ≤ 5 phút |
| Cùng khu vực (Region) | Yes |
| Cùng loại xe | Yes (optional - code chưa implement) |
| Hành khách tối đa | 2 |

---

## 🔧 Tùy Chỉnh Tiêu Chí

Để thay đổi tiêu chí ghép, chỉnh sửa các hằng số trong [TripsController.cs](RideHailingApi/Controllers/TripsController.cs):

```csharp
const double MaxPickupDistanceKm = 1.0;      // Thay đổi ở đây
const double MaxDropoffDistanceKm = 1.0;    // Thay đổi ở đây
const int MaxMinutesOld = 5;                 // Thay đổi ở đây
```

---

## 🐛 Troubleshooting

### 1. "Không tìm thấy cuốc phù hợp"
- **Nguyên nhân**: Không có cuốc nào thỏa tiêu chí
- **Giải pháp**: 
  - Tăng khoảng cách cho phép (MaxPickupDistanceKm)
  - Tăng thời gian tạo (MaxMinutesOld)
  - Kiểm tra GPS có bật không

### 2. Lỗi "Server không khả dụng (503)"
- **Nguyên nhân**: Primary DB down, app ở chế độ Read-Only
- **Giải pháp**: Chỉ có thể xem candidates, không thể ghép. Chờ Primary khôi phục.

### 3. SignalR notification không nhận được
- **Nguyên nhân**: Kết nối SignalR bị đứt
- **Giải pháp**: Kiểm tra kết nối mạng, JWT token hợp lệ

---

## 📝 API Contract

### GET /api/trips/pool-candidates/{tripId}

**Query Parameters**:
```
mainPickupLat=10.7605
mainPickupLon=106.7035
mainDropoffLat=10.8
mainDropoffLon=106.8
```

**Response**:
```json
[
  {
    "tripID": 102,
    "userID": 5,
    "pickupLocation": "Tân Bình District",
    "dropoffLocation": "Phú Nhuận District",
    "pickupDistance": 0.8,
    "dropoffDistance": 0.9,
    "minutesOld": 2,
    "createdAt": "2026-05-03T10:30:00Z"
  }
]
```

### POST /api/trips/pool

**Body**:
```json
{
  "mainTripID": 101,
  "secondaryTripID": 102
}
```

**Response**:
```json
{
  "success": true,
  "mainTripId": 101,
  "secondaryTripId": 102,
  "message": "Ghép cuốc thành công!"
}
```

---

## 📌 Lưu Ý Quan Trọng

1. **Cần cập nhật Database trước** - Script migration không thể auto-run
2. **JWT Token phải hợp lệ** - Ghép cuốc là protected endpoint
3. **GPS phải chính xác** - Sử dụng Haversine formula để tính khoảng cách
4. **SignalR cần kết nối WebSocket** - Bật SSL/TLS đúng cách
5. **Maxpassengers = 2** - Hiện tại chỉ hỗ trợ 2 hành khách. Để mở rộng, cần thay đổi logic

---

## 🎯 Bước Tiếp Theo (Future Enhancements)

- [ ] Hỗ trợ 3+ hành khách trên 1 xe
- [ ] Tính toán giá tiền chia cho multiple passengers
- [ ] Thống kê doanh thu từ ghép cuốc
- [ ] Rating + feedback sau khi ghép cuốc
- [ ] Unpool động khi 1 hành khách bị hủy cuốc
- [ ] ML/AI để predict best pooling combinations

---

Chúc mừng! Bạn đã có chức năng ghép cuốc hoàn chỉnh 🎉
