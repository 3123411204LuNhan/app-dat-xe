using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RideHailingApi.Data;
using RideHailingApi.Hubs;
using RideHailingApi.Middleware;
using RideHailingApi.Models;
using RideHailingApi.Services;
namespace RideHailingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripsController : ControllerBase
    {
        private readonly DataConnect _db;
        private readonly IHubContext<TripHub> _hub;
        private readonly FareService _fareService;

        private int ResolveLocationId(string region, string locationName)
        {
            if (string.IsNullOrWhiteSpace(locationName)) return 1;
            try
            {
                var idObj = _db.ExecuteScalar(region, 
                    "SELECT LocationID FROM Locations WHERE LocationName = @name", 
                    cmd => cmd.Parameters.AddWithValue("@name", locationName));
                if (idObj != null && idObj != DBNull.Value) return Convert.ToInt32(idObj);

                var newId = _db.ExecuteScalarWrite(region,
                    "INSERT INTO Locations (LocationName, Address, Latitude, Longitude) VALUES (@name, @name, 0, 0); SELECT SCOPE_IDENTITY();",
                    cmd => cmd.Parameters.AddWithValue("@name", locationName));
                return Convert.ToInt32(newId);
            }
            catch { return 1; }
        }
        public TripsController(DataConnect db, IHubContext<TripHub> hub, FareService fareService)
        {
            _db = db;
            _hub = hub;
            _fareService = fareService;
        }

        // GET /api/trips/estimate-fare?vehicleType=Xe+máy&distanceKm=5.5
        [HttpGet("estimate-fare")]
        public IActionResult EstimateFare([FromQuery] string vehicleType = "Xe máy", [FromQuery] double distanceKm = 0)
        {
            decimal fare = _fareService.Calculate(vehicleType, distanceKm);
            return Ok(new { vehicleType, distanceKm, fare });
        }

        // POST /api/trips/book-trip — Protected: yêu cầu JWT hợp lệ
        [Authorize]
        [HttpPost("book-trip")]
        public async Task<IActionResult> BookTrip([FromBody] TripRequest request)
        {
            string region = HttpContext.GetRegion();
            if (!string.IsNullOrWhiteSpace(request.Region))
                region = request.Region;

            decimal fare = _fareService.Calculate(request.VehicleType, request.DistanceKm);
            int pickupId = request.PickupLocationID > 0 ? request.PickupLocationID : ResolveLocationId(region, request.PickupLocation);
            int dropoffId = request.DropoffLocationID > 0 ? request.DropoffLocationID : ResolveLocationId(region, request.DropoffLocation);

            try
            {
                var newId = _db.ExecuteScalarWrite(region,
                    "INSERT INTO Trips (UserID, PickupLocationID, DropoffLocationID, Region, DistanceKm, Price, Status) " +
                    "VALUES (@Uid, @PickId, @DropId, @Reg, @DistanceKm, @Price, 'Pending'); SELECT SCOPE_IDENTITY();",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@Uid", request.UserID);
                        cmd.Parameters.AddWithValue("@PickId", pickupId);
                        cmd.Parameters.AddWithValue("@DropId", dropoffId);
                        cmd.Parameters.AddWithValue("@Reg", region);
                        cmd.Parameters.AddWithValue("@DistanceKm", request.DistanceKm);
                        cmd.Parameters.AddWithValue("@Price", fare);
                    });

                int tripId = Convert.ToInt32(newId);
                await _hub.Clients.Group($"Trip_{tripId}")
                    .SendAsync("OnTripStatusChanged", "Pending", "Đang tìm tài xế cho bạn...");
                await _hub.Clients.Group($"DriverPool_{region}")
                    .SendAsync("OnNewTripRequest", tripId, request.PickupLocationID, request.DropoffLocationID);

                return Ok(new { tripId, message = $"Đặt xe thành công tại Server Chính ({region})", fare });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(503, new
                {
                    error   = "Server Chính đang bảo trì.",
                    message = "Hệ thống đang ở chế độ Read-Only. Bạn chỉ có thể xem lịch sử, không thể đặt xe mới lúc này."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        // POST /api/trips/{tripId}/notify-status — Tài xế báo trạng thái chuyến (Accepted/Arrived/Completed)
        [Authorize]
        [HttpPost("{tripId:int}/notify-status")]
        public async Task<IActionResult> NotifyTripStatus(int tripId, [FromBody] TripStatusRequest req)
        {
            await _hub.Clients.Group($"Trip_{tripId}")
                .SendAsync("OnTripStatusChanged", req.Status, req.Message);
            return Ok(new { sent = true });
        }
        // GET /api/trips/history/{userId} — lịch sử chuyến đi (có failover sang Replica)
        [HttpGet("history/{userId:int}")]
        public IActionResult GetHistory(int userId)
        {
            string region = HttpContext.GetRegion();
            try
            {
                var table = _db.ExecuteReader(region,
                    "SELECT t.TripID, t.UserID, t.DriverID, p.LocationName AS PickupLocation, d.LocationName AS DropoffLocation, t.Region, t.Status, " +
                    "'Xe máy' AS VehicleType, t.Price AS Fare, t.CreatedAt " +
                    "FROM Trips t " +
                    "LEFT JOIN Locations p ON t.PickupLocationID = p.LocationID " +
                    "LEFT JOIN Locations d ON t.DropoffLocationID = d.LocationID " +
                    "WHERE t.UserID = @id ORDER BY t.CreatedAt DESC",
                    cmd => cmd.Parameters.AddWithValue("@id", userId));

                var trips = new List<TripHistoryItem>();
                foreach (System.Data.DataRow row in table.Rows)
                {
                    trips.Add(new TripHistoryItem
                    {
                        TripID          = (int)row["TripID"],
                        UserID          = (int)row["UserID"],
                        DriverID        = row["DriverID"] is DBNull ? null : (int?)row["DriverID"],
                        PickupLocation  = row["PickupLocation"].ToString() ?? "",
                        DropoffLocation = row["DropoffLocation"].ToString() ?? "",
                        Region          = row["Region"].ToString() ?? "",
                        Status          = row["Status"].ToString() ?? "",
                        VehicleType     = row["VehicleType"].ToString() ?? "",
                        Fare            = row["Fare"] is DBNull ? null : Convert.ToDecimal(row["Fare"]),
                        CreatedAt       = row["CreatedAt"] is DBNull ? null : (DateTime?)row["CreatedAt"]
                    });
                }
                return Ok(trips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        // GET /api/trips/test-connection/{region} — kiểm tra kết nối DB của 1 region
        [HttpGet("test-connection/{region}")]
        public IActionResult TestDBconnection(string region)
        {
            try
            {
                var serverName = _db.ExecuteScalar(region, "SELECT @@SERVERNAME")?.ToString();
                return Ok(new
                {
                    TrangThai  = "Kết nối thành công",
                    KhuVuc     = region,
                    ServerName = serverName,
                    LoiNhan    = "API của bạn đã đâm xuyên qua SQL Server rồi đó!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    TrangThai = "Kết nối thất bại",
                    KhuVuc    = region,
                    LoiNhan   = $"Không thể kết nối đến SQL Server: {ex.Message}"
                });
            }
        }

        // GET /api/trips/pending/{region} — tài xế lấy danh sách cuốc đang chờ
        [Authorize]
        [HttpGet("pending/{region}")]
        public IActionResult GetPendingTrips(string region)
        {
            try
            {
                var table = _db.ExecuteReader(region,
                    "SELECT t.TripID, t.UserID, p.LocationName AS PickupLocation, d.LocationName AS DropoffLocation, t.Region, " +
                    "'Xe máy' AS VehicleType, t.Price AS Fare, t.CreatedAt " +
                    "FROM Trips t " +
                    "LEFT JOIN Locations p ON t.PickupLocationID = p.LocationID " +
                    "LEFT JOIN Locations d ON t.DropoffLocationID = d.LocationID " +
                    "WHERE t.Status='Pending' AND t.Region=@region ORDER BY t.CreatedAt DESC",
                    cmd => cmd.Parameters.AddWithValue("@region", region));

                var trips = new List<PendingTripItem>();
                foreach (System.Data.DataRow row in table.Rows)
                {
                    trips.Add(new PendingTripItem
                    {
                        TripID          = (int)row["TripID"],
                        UserID          = (int)row["UserID"],
                        PickupLocation  = row["PickupLocation"].ToString() ?? "",
                        DropoffLocation = row["DropoffLocation"].ToString() ?? "",
                        Region          = row["Region"].ToString() ?? "",
                        VehicleType     = row["VehicleType"].ToString() ?? "",
                        EstimatedFare   = row["Fare"] is DBNull ? null : Convert.ToDecimal(row["Fare"]),
                        CreatedAt       = row["CreatedAt"] is DBNull ? null : (DateTime?)row["CreatedAt"]
                    });
                }
                return Ok(trips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /api/trips/{tripId}/accept — tài xế nhận cuốc
        [Authorize]
        [HttpPost("{tripId:int}/accept")]
        public async Task<IActionResult> AcceptTrip(int tripId)
        {
            string region = HttpContext.GetRegion();
            var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
            int driverId = int.Parse(subClaim ?? "0");
            string driverName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                             ?? User.FindFirst("unique_name")?.Value ?? "Tài xế";

            try
            {
                int rows = _db.ExecuteNonQuery(region,
                    "UPDATE Trips SET Status='Accepted', DriverID=@driverId " +
                    "WHERE TripID=@tripId AND Status='Pending'",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@driverId", driverId);
                        cmd.Parameters.AddWithValue("@tripId", tripId);
                    });

                if (rows == 0)
                    return Conflict(new { error = "Chuyến đi đã được nhận bởi tài xế khác." });

                await _hub.Clients.Group($"Trip_{tripId}")
                    .SendAsync("OnTripStatusChanged", "Accepted",
                        $"Tài xế {driverName} đã nhận chuyến của bạn!");

                return Ok(new { accepted = true, driverId, driverName });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(503, new { error = "Server Chính đang bảo trì." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        // POST /api/trips/{tripId}/arrive — tài xế đến điểm đón
        [Authorize]
        [HttpPost("{tripId:int}/arrive")]
        public async Task<IActionResult> ArriveAtPickup(int tripId)
        {
            string region = HttpContext.GetRegion();
            try
            {
                _db.ExecuteNonQuery(region,
                    "UPDATE Trips SET Status='Arrived' WHERE TripID=@tripId",
                    cmd => cmd.Parameters.AddWithValue("@tripId", tripId));

                await _hub.Clients.Group($"Trip_{tripId}")
                    .SendAsync("OnTripStatusChanged", "Arrived", "Tài xế đã đến điểm đón!");

                return Ok(new { arrived = true });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(503, new { error = "Server Chính đang bảo trì." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /api/trips/{tripId}/pickup — tài xế đón khách, bắt đầu di chuyển
        [Authorize]
        [HttpPost("{tripId:int}/pickup")]
        public async Task<IActionResult> PickupPassenger(int tripId)
        {
            string region = HttpContext.GetRegion();
            try
            {
                _db.ExecuteNonQuery(region,
                    "UPDATE Trips SET Status='InProgress' WHERE TripID=@tripId",
                    cmd => cmd.Parameters.AddWithValue("@tripId", tripId));

                await _hub.Clients.Group($"Trip_{tripId}")
                    .SendAsync("OnTripStatusChanged", "InProgress", "Tài xế đã đón khách. Đang di chuyển đến điểm đến.");

                return Ok(new { inProgress = true });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(503, new { error = "Server Chính đang bảo trì." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /api/trips/{tripId}/complete — hoàn thành chuyến
        [Authorize]
        [HttpPost("{tripId:int}/complete")]
        public async Task<IActionResult> CompleteTrip(int tripId)
        {
            string region = HttpContext.GetRegion();
            try
            {
                // Read fare before marking completed
                decimal? fare = null;
                try
                {
                    var fareObj = _db.ExecuteScalar(region,
                        "SELECT Price FROM Trips WHERE TripID=@tripId",
                        cmd => cmd.Parameters.AddWithValue("@tripId", tripId));
                    if (fareObj is not null and not DBNull)
                        fare = Convert.ToDecimal(fareObj);
                }
                catch { /* Fare column may not exist on older DB */ }

                _db.ExecuteNonQuery(region,
                    "UPDATE Trips SET Status='Completed' WHERE TripID=@tripId",
                    cmd => cmd.Parameters.AddWithValue("@tripId", tripId));

                string fareMsg = fare.HasValue ? $" Tổng tiền: {fare.Value:#,##0}đ." : "";
                await _hub.Clients.Group($"Trip_{tripId}")
                    .SendAsync("OnTripStatusChanged", "Completed", $"Chuyến đi hoàn thành.{fareMsg} Cảm ơn bạn!");

                return Ok(new { completed = true, fare });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(503, new { error = "Server Chính đang bảo trì." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /api/trips/{tripId}/cancel — hủy chuyến (user hoặc driver)
        [Authorize]
        [HttpPost("{tripId:int}/cancel")]
        public async Task<IActionResult> CancelTrip(int tripId)
        {
            string region = HttpContext.GetRegion();
            try
            {
                _db.ExecuteNonQuery(region,
                    "UPDATE Trips SET Status='Cancelled' WHERE TripID=@tripId AND Status IN ('Pending','Accepted','Arrived')",
                    cmd => cmd.Parameters.AddWithValue("@tripId", tripId));

                await _hub.Clients.Group($"Trip_{tripId}")
                    .SendAsync("OnTripStatusChanged", "CancelledByDriver", "Chuyến đi đã bị hủy.");

                return Ok(new { cancelled = true });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(503, new { error = "Server Chính đang bảo trì." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /api/trips/{tripId}/rating — khách hàng đánh giá tài xế
        [Authorize]
        [HttpPost("{tripId:int}/rating")]
        public IActionResult SubmitRating(int tripId, [FromBody] RatingRequest req)
        {
            string region = HttpContext.GetRegion();
            var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
            int userId = int.Parse(subClaim ?? "0");

            if (req.Score < 1 || req.Score > 5)
                return BadRequest(new { error = "Score phải từ 1 đến 5." });

            try
            {
                _db.ExecuteNonQuery(region,
                    "INSERT INTO Ratings (TripID, UserID, Score, Comment, CreatedAt) " +
                    "VALUES (@tripId, @userId, @score, @comment, GETDATE())",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@tripId",  tripId);
                        cmd.Parameters.AddWithValue("@userId",  userId);
                        cmd.Parameters.AddWithValue("@score",   req.Score);
                        cmd.Parameters.AddWithValue("@comment", (object?)req.Comment ?? DBNull.Value);
                    });
                return Ok(new { rated = true, score = req.Score });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(503, new { error = "Server Chính đang bảo trì." });
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                return Conflict(new { error = "Chuyến này đã được đánh giá." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET /api/trips/{tripId}/invoice — chi tiết hoá đơn chuyến
        [HttpGet("{tripId:int}/invoice")]
        public IActionResult GetInvoice(int tripId)
        {
            string region = HttpContext.GetRegion();
            try
            {
                var table = _db.ExecuteReader(region,
                    "SELECT t.TripID, t.UserID, t.DriverID, p.LocationName AS PickupLocation, d.LocationName AS DropoffLocation, " +
                    "t.Region, t.Status, 'Xe máy' AS VehicleType, " +
                    "ISNULL(t.DistanceKm,0) AS DistanceKm, t.Price AS Fare, t.CreatedAt, " +
                    "ISNULL(r.Score,0) AS RatingScore, ISNULL(r.Comment,'') AS RatingComment " +
                    "FROM Trips t " +
                    "LEFT JOIN Ratings r ON t.TripID = r.TripID " +
                    "LEFT JOIN Locations p ON t.PickupLocationID = p.LocationID " +
                    "LEFT JOIN Locations d ON t.DropoffLocationID = d.LocationID " +
                    "WHERE t.TripID = @tripId",
                    cmd => cmd.Parameters.AddWithValue("@tripId", tripId));

                if (table.Rows.Count == 0)
                    return NotFound(new { error = "Không tìm thấy chuyến đi." });

                var row = table.Rows[0];
                decimal fare = row["Fare"] is DBNull ? 0m : Convert.ToDecimal(row["Fare"]);
                double distKm = Convert.ToDouble(row["DistanceKm"]);

                return Ok(new
                {
                    tripId        = (int)row["TripID"],
                    userId        = (int)row["UserID"],
                    driverId      = row["DriverID"] is DBNull ? null : (int?)Convert.ToInt32(row["DriverID"]),
                    pickup        = row["PickupLocation"].ToString(),
                    dropoff       = row["DropoffLocation"].ToString(),
                    vehicleType   = row["VehicleType"].ToString(),
                    distanceKm    = distKm,
                    baseFare      = 10_000m,
                    distanceFare  = Math.Max(0m, fare - 10_000m),
                    totalFare     = fare,
                    status        = row["Status"].ToString(),
                    createdAt     = row["CreatedAt"] is DBNull ? null : (DateTime?)row["CreatedAt"],
                    ratingScore   = Convert.ToInt32(row["RatingScore"]),
                    ratingComment = row["RatingComment"].ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET /api/trips/driver/history — lịch sử cuốc của tài xế đang đăng nhập
        [Authorize]
        [HttpGet("driver/history")]
        public IActionResult GetDriverHistory()
        {
            string region = HttpContext.GetRegion();
            var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
            int driverId = int.Parse(subClaim ?? "0");

            try
            {
                var table = _db.ExecuteReader(region,
                    "SELECT t.TripID, t.UserID, t.DriverID, p.LocationName AS PickupLocation, d.LocationName AS DropoffLocation, t.Region, t.Status, " +
                    "'Xe máy' AS VehicleType, t.Price AS Fare, t.CreatedAt " +
                    "FROM Trips t " +
                    "LEFT JOIN Locations p ON t.PickupLocationID = p.LocationID " +
                    "LEFT JOIN Locations d ON t.DropoffLocationID = d.LocationID " +
                    "WHERE t.DriverID=@driverId AND t.Status IN ('Completed','Cancelled') " +
                    "ORDER BY t.CreatedAt DESC",
                    cmd => cmd.Parameters.AddWithValue("@driverId", driverId));

                var trips = new List<TripHistoryItem>();
                foreach (System.Data.DataRow row in table.Rows)
                {
                    trips.Add(new TripHistoryItem
                    {
                        TripID          = (int)row["TripID"],
                        UserID          = (int)row["UserID"],
                        DriverID        = driverId,
                        PickupLocation  = row["PickupLocation"].ToString() ?? "",
                        DropoffLocation = row["DropoffLocation"].ToString() ?? "",
                        Region          = row["Region"].ToString() ?? "",
                        Status          = row["Status"].ToString() ?? "",
                        VehicleType     = row["VehicleType"].ToString() ?? "",
                        Fare            = row["Fare"] is DBNull ? null : Convert.ToDecimal(row["Fare"]),
                        CreatedAt       = row["CreatedAt"] is DBNull ? null : (DateTime?)row["CreatedAt"]
                    });
                }
                return Ok(trips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET /api/trips/health/{region}
        // Trả về trạng thái Primary và Replica — client dùng để tự phát hiện failover khi khởi động.
        [HttpGet("health/{region}")]
        public IActionResult Health(string region)
        {
            bool primaryOk = _db.IsPrimaryAlive(region);
            bool replicaOk = _db.IsReplicaAlive(region);
            return Ok(new
            {
                Region    = region,
                PrimaryOk = primaryOk,
                ReplicaOk = replicaOk,
                IsFailover = !primaryOk && replicaOk,
                Message   = primaryOk
                    ? $"Server chính ({region}) hoạt động bình thường."
                    : replicaOk
                        ? $"Server chính ({region}) KHÔNG khả dụng — đang dùng Replica."
                        : $"Cả Primary lẫn Replica ({region}) đều không phản hồi!"
            });
        }
    }
}
