# Đặc tả yêu cầu chức năng bổ sung cho hệ thống đặt xe

> Tài liệu mô tả yêu cầu cho 3 nhóm tính năng bổ sung của hệ thống đặt xe:
>
> 1. **Offline-first & sync cho client**
> 2. **Báo cáo & analytics thời gian thực (heatmap cầu, doanh thu)**
> 3. **Lịch đặt trước (scheduling / pre-book)**

---

## 1. Mục tiêu
- Bổ sung các tính năng giúp hệ thống đặt xe hoạt động ổn định hơn trong điều kiện mạng yếu.
- Tăng khả năng giám sát, phân tích dữ liệu theo thời gian thực cho vận hành và marketing.
- Hỗ trợ người dùng đặt chuyến trước theo thời gian mong muốn.
- Làm cơ sở cho thiết kế API, database, UI và kế hoạch triển khai.

---

## 2. Phạm vi
Tài liệu này tập trung vào 3 nhóm tính năng:
- Offline-first và đồng bộ lại khi có mạng
- Dashboard báo cáo và analytics thời gian thực
- Đặt chuyến trước (scheduling / pre-book)

---

# 3. Tính năng 1: Offline-first & sync cho client

## 3.1 Mô tả
Tính năng này cho phép ứng dụng client vẫn thực hiện được một số thao tác cơ bản khi mất mạng, lưu yêu cầu cục bộ và tự động đồng bộ khi có mạng trở lại.

---

## 3.2 Mục tiêu nghiệp vụ
- Tránh mất thao tác người dùng khi mạng yếu hoặc gián đoạn.
- Tăng độ ổn định và trải nghiệm sử dụng của ứng dụng mobile.
- Cho phép người dùng tiếp tục thao tác với dữ liệu cục bộ trong giới hạn hợp lý.

---

## 3.3 Chức năng chi tiết

### 3.3.1 Hỗ trợ thao tác khi mất mạng
Ứng dụng phải cho phép người dùng tiếp tục một số thao tác ở chế độ offline, ví dụ:
- xem dữ liệu đã cache gần nhất
- tạo yêu cầu tạm thời chưa gửi được
- xem lịch sử truy cập gần nhất
- chỉnh sửa bản nháp một số form chưa submit được

### 3.3.2 Lưu queue cục bộ
Khi người dùng thực hiện thao tác cần gọi API nhưng thiết bị đang offline:
- request không được gửi ngay lên server
- request được lưu vào **local queue** trên thiết bị

### 3.3.3 Đồng bộ lại khi có mạng
Khi mạng trở lại:
- hệ thống tự động duyệt queue
- gửi lại các request chưa đồng bộ theo thứ tự
- cập nhật trạng thái từng request sau khi xử lý

### 3.3.4 Trạng thái của bản ghi queue
Mỗi request trong queue nên có trạng thái:
- `Pending`
- `Retrying`
- `Synced`
- `Failed`

### 3.3.5 Retry policy
Hệ thống phải hỗ trợ retry tự động khi:
- mất mạng tạm thời
- timeout tạm thời
- backend trả lỗi có thể retry

### 3.3.6 Conflict handling (gợi ý mở rộng)
Nếu dữ liệu local và server khác nhau sau khi sync:
- hệ thống phải có chiến lược xử lý conflict
- có thể chọn một trong các hướng:
  - server wins
  - client wins
  - merge có điều kiện

---

## 3.4 Thành phần kỹ thuật đề xuất
- **SQLite** hoặc local database trên client
- **Sync Queue Table**
- **Sync Service** chạy nền
- **Network Connectivity Monitor**
- Retry policy / backoff strategy

---

## 3.5 Dữ liệu gợi ý
### Bảng local: SyncQueue
- `Id`
- `RequestType`
- `PayloadJson`
- `EntityType`
- `EntityId`
- `Status`
- `RetryCount`
- `CreatedAt`
- `LastAttemptAt`
- `LastError`

### Bảng local: CachedData (tuỳ chọn)
- `Key`
- `JsonValue`
- `UpdatedAt`

---

## 3.6 UI/UX đề xuất
- Hiển thị trạng thái mạng: `Online` / `Offline`
- Hiển thị thông báo khi request được lưu tạm:
  - “Bạn đang offline. Yêu cầu sẽ được gửi khi có mạng.”
- Có icon hoặc trạng thái đồng bộ nhỏ trong app
- Có thể thêm màn hình debug queue cho môi trường dev/test

---

## 3.7 API / backend liên quan
Backend cần hỗ trợ:
- idempotent endpoints cho các request có thể gửi lại
- phân biệt lỗi retry được và lỗi không retry được
- log theo request id nếu muốn theo dõi việc sync

---

## 3.8 Effort
- **Medium**

---

# 4. Tính năng 2: Báo cáo & analytics thời gian thực (heatmap cầu, doanh thu)

## 4.1 Mô tả
Tính năng này cung cấp dashboard thời gian thực dành cho vận hành và marketing nhằm theo dõi:
- heatmap nhu cầu đặt xe
- doanh thu
- số chuyến đi
- số tài xế hoạt động
- các KPI quan trọng theo thời gian thực hoặc gần thời gian thực

---

## 4.2 Mục tiêu nghiệp vụ
- Hỗ trợ đội vận hành theo dõi nhu cầu đặt xe theo khu vực.
- Hỗ trợ đội marketing theo dõi hành vi sử dụng và hiệu quả hoạt động.
- Giúp ra quyết định nhanh trong giờ cao điểm, khuyến mãi, phân bổ tài xế.

---

## 4.3 Chức năng chi tiết

### 4.3.1 Heatmap cầu theo khu vực
Hệ thống phải hiển thị heatmap dựa trên dữ liệu:
- điểm đón
- điểm đến
- số lượt tìm xe
- số chuyến tạo mới
- số chuyến bị huỷ

### 4.3.2 Dashboard doanh thu
Dashboard phải hiển thị:
- doanh thu theo ngày / tuần / tháng
- doanh thu theo khu vực
- doanh thu theo loại xe
- doanh thu theo tài xế (nếu cần)

### 4.3.3 KPI thời gian thực
Các KPI đề xuất:
- số chuyến mới theo phút / giờ
- số user đang hoạt động
- số tài xế online
- tỷ lệ nhận cuốc thành công
- tỷ lệ huỷ chuyến
- ETA trung bình

### 4.3.4 Bộ lọc & drill-down
Người dùng dashboard (admin / operator / marketing) phải có thể:
- lọc theo thời gian
- lọc theo khu vực
- lọc theo loại xe
- drill-down từ KPI tổng quan xuống danh sách chi tiết

### 4.3.5 Cảnh báo vận hành (gợi ý mở rộng)
Có thể bổ sung:
- cảnh báo khu vực thiếu tài xế
- cảnh báo tỷ lệ huỷ tăng cao
- cảnh báo cầu vượt cung trong thời gian thực

---

## 4.4 Kiến trúc kỹ thuật đề xuất
- **Event pipeline** để thu thập sự kiện nghiệp vụ
- **Timeseries DB / warehouse / event store** hoặc lớp lưu trữ phù hợp
- **Dashboard frontend** cho hiển thị biểu đồ và heatmap
- **Aggregation jobs / stream processing** để tính toán chỉ số

---

## 4.5 Nguồn dữ liệu / event đề xuất
Các event nên ghi nhận:
- `TripCreated`
- `TripMatched`
- `TripCancelled`
- `TripCompleted`
- `DriverOnline`
- `DriverOffline`
- `PaymentSucceeded`
- `SearchRideRequested`

---

## 4.6 API đề xuất
```text
GET /api/admin/dashboard/kpis
GET /api/admin/dashboard/revenue
GET /api/admin/dashboard/heatmap-demand
GET /api/admin/dashboard/trips/realtime
GET /api/admin/dashboard/drivers/online
```

---

## 4.7 Giao diện đề xuất
### Dashboard tổng quan
- Card KPI
- Biểu đồ doanh thu theo thời gian
- Biểu đồ số chuyến theo thời gian
- Danh sách tài xế online

### Heatmap cầu
- Bản đồ heatmap theo khu vực
- Có filter thời gian
- Có khả năng zoom / pan

### Bảng báo cáo chi tiết
- Danh sách khu vực
- Số chuyến
- Doanh thu
- Tỷ lệ huỷ

---

## 4.8 Dữ liệu gợi ý
### Bảng / stream events
- `EventId`
- `EventType`
- `OccurredAt`
- `UserId`
- `DriverId`
- `TripId`
- `Region`
- `VehicleType`
- `PayloadJson`

### Bảng tổng hợp (tuỳ chọn)
- `DemandHeatmapAgg`
- `RevenueAgg`
- `TripKpiAgg`

---

## 4.9 Effort
- **High**

## Migration instructions (quick)

1. Add EF Core packages to `RideHailingApi` project (done in csproj):
   - `Microsoft.EntityFrameworkCore`
   - `Microsoft.EntityFrameworkCore.SqlServer`

2. Create `DataContext` deriving from `DbContext` and register it in `Program.cs`.

3. Create model `ScheduledTrip` and add `DbSet<ScheduledTrip>` to `DataContext`.

4. From repository root run (PowerShell):

```powershell
cd RideHailingApi
dotnet tool install --global dotnet-ef --version 8.* --add-path $env:USERPROFILE\.dotnet\tools
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add AddScheduledTrips -o Migrations\ScheduledTrips
dotnet ef database update
```

Note: adjust connection string in `appsettings.json` if necessary before running `database update`.

---

# 5. Tính năng 3: Lịch đặt trước (scheduling / pre-book)

## 5.1 Mô tả
Tính năng này cho phép người dùng **đặt chuyến trước** cho một thời điểm trong tương lai, ví dụ:
- trước vài giờ
- trước một ngày
- trước một khoảng thời gian cấu hình cho phép

---

## 5.2 Mục tiêu nghiệp vụ
- Tăng tính tiện lợi cho người dùng có nhu cầu lên lịch trước.
- Tạo thêm use case thực tế cho hệ thống.
- Hỗ trợ điều phối tài xế tốt hơn ở một số tình huống.

---

## 5.3 Chức năng chi tiết

### 5.3.1 Tạo lịch đặt trước
Người dùng phải có thể nhập:
- điểm đón
- điểm đến
- loại xe
- ngày / giờ mong muốn đón

### 5.3.2 Kiểm tra hợp lệ thời gian đặt trước
Hệ thống phải kiểm tra:
- thời gian đặt trước phải nằm trong tương lai
- không vượt quá giới hạn cho phép (nếu có)
- không nhỏ hơn khoảng tối thiểu (ví dụ ít nhất 15 phút sau hiện tại)

### 5.3.3 Quản lý lịch đặt trước
Người dùng phải có thể:
- xem danh sách lịch đã đặt trước
- xem chi tiết lịch đặt trước
- hủy lịch đặt trước (nếu chưa đến thời điểm xử lý)

### 5.3.4 Xử lý chuyến đặt trước
Đến gần thời điểm khởi hành:
- hệ thống chuyển lịch đặt trước sang trạng thái sẵn sàng xử lý
- bắt đầu tìm tài xế theo logic matching bình thường

### 5.3.5 Trạng thái đề xuất
- `Scheduled`
- `PendingDispatch`
- `SearchingDriver`
- `DriverAssigned`
- `Cancelled`
- `Completed`

---

## 5.4 Thành phần kỹ thuật đề xuất
- API Bookings / Scheduled Trips
- Bảng `ScheduledTrips`
- Background job / scheduler để kích hoạt matching đúng giờ
- UI trang đặt trước trên mobile app

---

## 5.5 API đề xuất
```text
POST   /api/bookings/scheduled
GET    /api/bookings/scheduled
GET    /api/bookings/scheduled/{id}
DELETE /api/bookings/scheduled/{id}
POST   /api/bookings/scheduled/{id}/confirm
```

---

## 5.6 Giao diện đề xuất
### Màn hình đặt xe trước
- Input điểm đón
- Input điểm đến
- Chọn loại xe
- Chọn ngày
- Chọn giờ
- Nút xác nhận đặt trước

### Màn hình danh sách lịch đặt trước
- Danh sách booking đã lên lịch
- Thời gian đón dự kiến
- Trạng thái booking
- Nút hủy (nếu hợp lệ)

---

## 5.7 Dữ liệu gợi ý
### Bảng ScheduledTrips
- `ScheduledTripId`
- `UserId`
- `PickupAddress`
- `PickupLat`
- `PickupLng`
- `DropoffAddress`
- `DropoffLat`
- `DropoffLng`
- `VehicleType`
- `ScheduledPickupTime`
- `Status`
- `CreatedAt`
- `UpdatedAt`

---

## 5.8 Luồng xử lý đề xuất
```text
[User tạo booking đặt trước]
        ↓
[Hệ thống lưu ScheduledTrip = Scheduled]
        ↓
[Scheduler/background service theo dõi thời gian]
        ↓
[Đến gần giờ đón]
        ↓
[Chuyển sang PendingDispatch / SearchingDriver]
        ↓
[Thực hiện matching như trip thường]
```

---

## 5.9 Effort
- **Medium**

---

# 6. Tóm tắt effort

| Tính năng | Effort |
|---|---|
| Offline-first & sync cho client | Medium |
| Báo cáo & analytics thời gian thực | High |
| Lịch đặt trước (pre-book) | Medium |

---

# 7. Gợi ý thứ tự triển khai

## Giai đoạn 1
- Lịch đặt trước (pre-book)
- Offline-first & sync cho client

## Giai đoạn 2
- Báo cáo & analytics thời gian thực

---

# 8. Kết luận
Ba nhóm tính năng này giúp hệ thống đặt xe:
- linh hoạt hơn trong trải nghiệm người dùng,
- ổn định hơn ở phía client khi mạng yếu,
- mạnh hơn về giám sát vận hành và phân tích dữ liệu,
- thực tế hơn với nhu cầu đặt chuyến trước.

Tài liệu này có thể dùng tiếp để:
- viết FR/NFR chi tiết,
- thiết kế API,
- thiết kế DB,
- thiết kế UI,
- và lập kế hoạch triển khai theo sprint.
