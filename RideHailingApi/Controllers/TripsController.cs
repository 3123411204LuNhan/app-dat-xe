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

        public TripsController(DataConnect db, IHubContext<TripHub> hub)
        {
            _db = db;
            _hub = hub;
        }
        // POST /api/trips/book-trip — Protected: yêu cầu JWT hợp lệ
        [Authorize]
        [HttpPost("book-trip")]
        public async Task<IActionResult> BookTrip([FromBody] TripRequest request)
        {
            string region = HttpContext.GetRegion();
            if (!string.IsNullOrWhiteSpace(request.Region))
                region = request.Region;

            try
            {
                var newId = _db.ExecuteScalarWrite(region,
                    "INSERT INTO Trips (UserID, PickupLocation, DropoffLocation, Region, Status) " +
                    "VALUES (@Uid, @Pick, @Drop, @Reg, 'Pending'); SELECT SCOPE_IDENTITY();",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@Uid", request.UserID);
                        cmd.Parameters.AddWithValue("@Pick", request.PickupLocation);
                        cmd.Parameters.AddWithValue("@Drop", request.DropoffLocation);
                        cmd.Parameters.AddWithValue("@Reg", region);
                    });

                int tripId = Convert.ToInt32(newId);
                // Đẩy trạng thái "Pending" ngay lập tức tới group chuyến đi
                await _hub.Clients.Group($"Trip_{tripId}")
                    .SendAsync("OnTripStatusChanged", "Pending", "Đang tìm tài xế cho bạn...");
                // Thông báo tới pool tài xế trong cùng khu vực
                await _hub.Clients.Group($"DriverPool_{region}")
                    .SendAsync("OnNewTripRequest", tripId, request.PickupLocation, request.DropoffLocation);

                return Ok(new { tripId, message = $"Đặt xe thành công tại Server Chính ({region})" });
            }
            catch (InvalidOperationException)
            {
                // Primary sập — DataConnect không cho ghi vào Replica
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
                    "SELECT TripID, UserID, DriverID, PickupLocation, DropoffLocation, Region, Status, CreatedAt " +
                    "FROM Trips WHERE UserID = @id ORDER BY CreatedAt DESC",
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
                    "SELECT TripID, UserID, PickupLocation, DropoffLocation, Region, CreatedAt " +
                    "FROM Trips WHERE Status='Pending' AND Region=@region ORDER BY CreatedAt DESC",
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

        // POST /api/trips/{tripId}/complete — hoàn thành chuyến
        [Authorize]
        [HttpPost("{tripId:int}/complete")]
        public async Task<IActionResult> CompleteTrip(int tripId)
        {
            string region = HttpContext.GetRegion();
            try
            {
                _db.ExecuteNonQuery(region,
                    "UPDATE Trips SET Status='Completed' WHERE TripID=@tripId",
                    cmd => cmd.Parameters.AddWithValue("@tripId", tripId));

                await _hub.Clients.Group($"Trip_{tripId}")
                    .SendAsync("OnTripStatusChanged", "Completed", "Chuyến đi hoàn thành. Cảm ơn bạn!");

                return Ok(new { completed = true });
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

        // ===== POOLING ENDPOINTS =====

        // GET /api/trips/pool-candidates/{tripId}?mainPickupLat=10.7605&mainPickupLon=106.7035&mainDropoffLat=10.8&mainDropoffLon=106.8
        // Tìm danh sách cuốc có thể ghép với cuốc chính (theo tiêu chí khoảng cách, thời gian, loại xe)
        [Authorize]
        [HttpGet("pool-candidates/{tripId:int}")]
        public IActionResult GetPoolCandidates(
            int tripId,
            [FromQuery] double mainPickupLat,
            [FromQuery] double mainPickupLon,
            [FromQuery] double mainDropoffLat,
            [FromQuery] double mainDropoffLon)
        {
            string region = HttpContext.GetRegion();

            try
            {
                // Lấy thông tin trip chính
                var mainTripTable = _db.ExecuteReader(region,
                    "SELECT TripID, PickupLocation, DropoffLocation, CreatedAt FROM Trips WHERE TripID=@tripId",
                    cmd => cmd.Parameters.AddWithValue("@tripId", tripId));

                if (mainTripTable.Rows.Count == 0)
                    return NotFound(new { error = "Trip không tồn tại." });

                var mainTripRow = mainTripTable.Rows[0];
                DateTime mainCreatedAt = mainTripRow["CreatedAt"] is DBNull ? DateTime.Now : (DateTime)mainTripRow["CreatedAt"];

                // Lấy danh sách các trip Pending khác (cùng region, khác userID, không phải trip chính)
                var candidatesTable = _db.ExecuteReader(region,
                    "SELECT TripID, UserID, PickupLocation, DropoffLocation, CreatedAt " +
                    "FROM Trips " +
                    "WHERE Status='Pending' AND Region=@region AND TripID!=@tripId " +
                    "ORDER BY CreatedAt DESC LIMIT 50",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@region", region);
                        cmd.Parameters.AddWithValue("@tripId", tripId);
                    });

                var candidates = new List<PoolingCandidateItem>();

                const double MaxPickupDistanceKm = 1.0;
                const double MaxDropoffDistanceKm = 1.0;
                const int MaxMinutesOld = 5;

                foreach (System.Data.DataRow row in candidatesTable.Rows)
                {
                    int candidateTripId = (int)row["TripID"];
                    DateTime candidateCreatedAt = row["CreatedAt"] is DBNull ? DateTime.Now : (DateTime)row["CreatedAt"];

                    // Kiểm tra thời gian (cuốc phải tạo trong 5 phút gần nhất)
                    int minutesOld = (int)(mainCreatedAt - candidateCreatedAt).TotalMinutes;
                    if (minutesOld < 0 || minutesOld > MaxMinutesOld)
                        continue;

                    // Trong thực tế, cần parse GPS từ PickupLocation/DropoffLocation
                    // Hiện tại giả sử format là: "10.7605,106.7035" hoặc tương tự
                    string pickupStr = row["PickupLocation"].ToString() ?? "";
                    string dropoffStr = row["DropoffLocation"].ToString() ?? "";

                    if (!TryParseCoordinates(pickupStr, out double candPickupLat, out double candPickupLon))
                        continue;
                    if (!TryParseCoordinates(dropoffStr, out double candDropoffLat, out double candDropoffLon))
                        continue;

                    // Tính khoảng cách
                    double pickupDist = GeoDistanceHelper.CalculateDistance(
                        mainPickupLat, mainPickupLon, candPickupLat, candPickupLon);
                    double dropoffDist = GeoDistanceHelper.CalculateDistance(
                        mainDropoffLat, mainDropoffLon, candDropoffLat, candDropoffLon);

                    // Kiểm tra tiêu chí khoảng cách
                    if (pickupDist <= MaxPickupDistanceKm && dropoffDist <= MaxDropoffDistanceKm)
                    {
                        candidates.Add(new PoolingCandidateItem
                        {
                            TripID = candidateTripId,
                            UserID = (int)row["UserID"],
                            PickupLocation = pickupStr,
                            DropoffLocation = dropoffStr,
                            PickupDistance = pickupDist,
                            DropoffDistance = dropoffDist,
                            MinutesOld = minutesOld,
                            CreatedAt = candidateCreatedAt
                        });
                    }
                }

                return Ok(candidates);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /api/trips/pool — ghép 2 cuốc lại
        [Authorize]
        [HttpPost("pool")]
        public async Task<IActionResult> PoolTrips([FromBody] PoolTripsRequest req)
        {
            string region = HttpContext.GetRegion();

            if (req.MainTripID == req.SecondaryTripID)
                return BadRequest(new { error = "Không thể ghép 1 cuốc với chính nó." });

            try
            {
                // Cập nhật PooledWithTripID cho cả 2 cuốc
                int rows = _db.ExecuteNonQuery(region,
                    "UPDATE Trips SET PooledWithTripID=@secondary WHERE TripID=@main AND Status='Pending'; " +
                    "UPDATE Trips SET PooledWithTripID=@main WHERE TripID=@secondary AND Status='Pending';",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@main", req.MainTripID);
                        cmd.Parameters.AddWithValue("@secondary", req.SecondaryTripID);
                    });

                if (rows < 2)
                    return Conflict(new { error = "Một hoặc cả 2 cuốc đã không còn có sẵn để ghép." });

                // Thông báo cho cả 2 hành khách
                await _hub.Clients.Group($"Trip_{req.MainTripID}")
                    .SendAsync("OnPoolingNotification", "pooled",
                        $"Chuyến của bạn đã được ghép với một cuốc khác để tiết kiệm chi phí!");

                await _hub.Clients.Group($"Trip_{req.SecondaryTripID}")
                    .SendAsync("OnPoolingNotification", "pooled",
                        $"Chuyến của bạn đã được ghép với một cuốc khác để tiết kiệm chi phí!");

                return Ok(new
                {
                    success = true,
                    mainTripId = req.MainTripID,
                    secondaryTripId = req.SecondaryTripID,
                    message = "Ghép cuốc thành công!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET /api/trips/pooled/{tripId} — lấy thông tin cuốc ghép
        [Authorize]
        [HttpGet("pooled/{tripId:int}")]
        public IActionResult GetPooledTripInfo(int tripId)
        {
            string region = HttpContext.GetRegion();

            try
            {
                // Lấy trip chính
                var mainTable = _db.ExecuteReader(region,
                    "SELECT TripID, UserID, PickupLocation, DropoffLocation, PooledWithTripID FROM Trips WHERE TripID=@tripId",
                    cmd => cmd.Parameters.AddWithValue("@tripId", tripId));

                if (mainTable.Rows.Count == 0)
                    return NotFound(new { error = "Trip không tồn tại." });

                var mainRow = mainTable.Rows[0];
                int? pooledTripId = mainRow["PooledWithTripID"] is DBNull ? null : (int?)mainRow["PooledWithTripID"];

                if (!pooledTripId.HasValue)
                    return Ok(new { hasPooling = false });

                // Lấy trip ghép
                var pooledTable = _db.ExecuteReader(region,
                    "SELECT TripID, UserID, PickupLocation, DropoffLocation FROM Trips WHERE TripID=@tripId",
                    cmd => cmd.Parameters.AddWithValue("@tripId", pooledTripId.Value));

                if (pooledTable.Rows.Count == 0)
                    return Ok(new { hasPooling = false });

                var pooledRow = pooledTable.Rows[0];

                return Ok(new PooledTripInfo
                {
                    MainTripID = (int)mainRow["TripID"],
                    SecondaryTripID = (int)pooledRow["TripID"],
                    MainUserID = (int)mainRow["UserID"],
                    SecondaryUserID = (int)pooledRow["UserID"],
                    MainPickup = mainRow["PickupLocation"].ToString() ?? "",
                    MainDropoff = mainRow["DropoffLocation"].ToString() ?? "",
                    SecondaryPickup = pooledRow["PickupLocation"].ToString() ?? "",
                    SecondaryDropoff = pooledRow["DropoffLocation"].ToString() ?? "",
                    CurrentPassengers = 2,
                    PooledAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Helper: Parse "lat,lon" string thành double
        private static bool TryParseCoordinates(string coordString, out double lat, out double lon)
        {
            lat = 0;
            lon = 0;

            if (string.IsNullOrEmpty(coordString))
                return false;

            var parts = coordString.Split(',');
            if (parts.Length != 2)
                return false;

            return double.TryParse(parts[0].Trim(), out lat) &&
                   double.TryParse(parts[1].Trim(), out lon);
        }
    }
}
