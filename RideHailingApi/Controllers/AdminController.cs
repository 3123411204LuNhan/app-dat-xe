using Microsoft.AspNetCore.Mvc;
using RideHailingApi.Data;
using RideHailingApi.Models;
using RideHailingApi.Services;
namespace RideHailingApi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly DataConnect          _db;
        private readonly FailoverSimulator    _failover;
        private readonly DatabaseRuntimeState _runtimeState;
        private static readonly string[]     _regions = { "North", "South" };

        public AdminController(DataConnect db, FailoverSimulator failover, DatabaseRuntimeState runtimeState)
        {
            _db           = db;
            _failover     = failover;
            _runtimeState = runtimeState;
        }

        // ── Analytics ─────────────────────────────────────────────────────────────

        // GET /api/admin/dashboard/kpis
        [HttpGet("dashboard/kpis")]
        public IActionResult GetKpis()
        {
            long    totalTrips = 0, totalUsers = 0, totalDrivers = 0, cancelledTrips = 0;
            decimal totalRevenue = 0;

            foreach (var region in _regions)
            {
                try
                {
                    totalTrips    += Convert.ToInt64(_db.ExecuteScalar(region, "SELECT COUNT(*) FROM Trips")  ?? 0L);
                    totalUsers    += Convert.ToInt64(_db.ExecuteScalar(region, "SELECT COUNT(*) FROM Users")  ?? 0L);
                    totalDrivers  += Convert.ToInt64(_db.ExecuteScalar(region, "SELECT COUNT(*) FROM Drivers") ?? 0L);
                    cancelledTrips += Convert.ToInt64(_db.ExecuteScalar(region,
                        "SELECT COUNT(*) FROM Trips WHERE Status='Cancelled'") ?? 0L);
                    var rev = _db.ExecuteScalar(region, "SELECT ISNULL(SUM(Fare),0) FROM Trips WHERE Status='Completed'");
                    if (rev != null && rev != DBNull.Value) totalRevenue += Convert.ToDecimal(rev);
                }
                catch { /* region unavailable — skip */ }
            }

            double cancelRate = totalTrips > 0 ? Math.Round((double)cancelledTrips / totalTrips * 100, 1) : 0;
            return Ok(new { totalTrips, totalUsers, totalDrivers, totalRevenue, cancelledTrips, cancelRate });
        }

        // GET /api/admin/dashboard/revenue?days=30
        [HttpGet("dashboard/revenue")]
        public IActionResult GetRevenue([FromQuery] int days = 30)
        {
            var rows = new List<object>();
            foreach (var region in _regions)
            {
                try
                {
                    var table = _db.ExecuteReader(region,
                        "SELECT CONVERT(DATE, CreatedAt) AS Day, COUNT(*) AS Trips, ISNULL(SUM(Fare),0) AS Revenue " +
                        "FROM Trips WHERE Status='Completed' AND CreatedAt >= DATEADD(DAY, -@d, GETDATE()) " +
                        "GROUP BY CONVERT(DATE, CreatedAt) ORDER BY Day DESC",
                        cmd => cmd.Parameters.AddWithValue("@d", days));

                    foreach (System.Data.DataRow row in table.Rows)
                    {
                        rows.Add(new
                        {
                            region,
                            day     = ((DateTime)row["Day"]).ToString("yyyy-MM-dd"),
                            trips   = Convert.ToInt32(row["Trips"]),
                            revenue = Convert.ToDecimal(row["Revenue"])
                        });
                    }
                }
                catch { }
            }
            return Ok(rows);
        }

        // ── User Management ───────────────────────────────────────────────────────

        // GET /api/admin/users?search=&region=South&page=1
        [HttpGet("users")]
        public IActionResult GetUsers([FromQuery] string? search, [FromQuery] string region = "South", [FromQuery] int page = 1)
        {
            try
            {
                var table = _db.ExecuteReader(region,
                    "SELECT TOP 50 UserID, UserName, FullName, Phone, RegisteredRegion, ISNULL(IsLocked,0) AS IsLocked " +
                    "FROM Users WHERE (@s='' OR UserName LIKE '%'+@s+'%' OR FullName LIKE '%'+@s+'%') " +
                    "ORDER BY UserID DESC",
                    cmd => cmd.Parameters.AddWithValue("@s", search ?? ""));

                var users = table.Rows.Cast<System.Data.DataRow>().Select(r => new
                {
                    userId    = (int)r["UserID"],
                    userName  = r["UserName"].ToString(),
                    fullName  = r["FullName"].ToString(),
                    phone     = r["Phone"].ToString(),
                    region    = r["RegisteredRegion"].ToString(),
                    isLocked  = Convert.ToBoolean(r["IsLocked"])
                }).ToList();

                return Ok(users);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // PUT /api/admin/users/{id}/lock?region=South
        [HttpPut("users/{id:int}/lock")]
        public IActionResult LockUser(int id, [FromQuery] string region, [FromBody] LockRequest req)
        {
            try
            {
                _db.ExecuteNonQuery(region,
                    "UPDATE Users SET IsLocked=@locked WHERE UserID=@id",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@locked", req.IsLocked ? 1 : 0);
                        cmd.Parameters.AddWithValue("@id", id);
                    });
                return Ok(new { userId = id, isLocked = req.IsLocked });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // ── Driver Management ─────────────────────────────────────────────────────

        // GET /api/admin/drivers?search=&region=South
        [HttpGet("drivers")]
        public IActionResult GetDrivers([FromQuery] string? search, [FromQuery] string region = "South")
        {
            try
            {
                var table = _db.ExecuteReader(region,
                    "SELECT d.DriverID, d.FullName, d.Phone, ISNULL(d.IsLocked,0) AS IsLocked, " +
                    "COUNT(t.TripID) AS TotalTrips, ISNULL(SUM(t.Fare),0) AS TotalEarnings " +
                    "FROM Drivers d LEFT JOIN Trips t ON d.DriverID = t.DriverID AND t.Status='Completed' " +
                    "WHERE @s='' OR d.FullName LIKE '%'+@s+'%' OR d.Phone LIKE '%'+@s+'%' " +
                    "GROUP BY d.DriverID, d.FullName, d.Phone, d.IsLocked " +
                    "ORDER BY d.DriverID DESC",
                    cmd => cmd.Parameters.AddWithValue("@s", search ?? ""));

                var drivers = table.Rows.Cast<System.Data.DataRow>().Select(r => new
                {
                    driverId      = (int)r["DriverID"],
                    fullName      = r["FullName"].ToString(),
                    phone         = r["Phone"].ToString(),
                    isLocked      = Convert.ToBoolean(r["IsLocked"]),
                    totalTrips    = Convert.ToInt32(r["TotalTrips"]),
                    totalEarnings = Convert.ToDecimal(r["TotalEarnings"])
                }).ToList();

                return Ok(drivers);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // PUT /api/admin/drivers/{id}/lock?region=South
        [HttpPut("drivers/{id:int}/lock")]
        public IActionResult LockDriver(int id, [FromQuery] string region, [FromBody] LockRequest req)
        {
            try
            {
                _db.ExecuteNonQuery(region,
                    "UPDATE Drivers SET IsLocked=@locked WHERE DriverID=@id",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@locked", req.IsLocked ? 1 : 0);
                        cmd.Parameters.AddWithValue("@id", id);
                    });
                return Ok(new { driverId = id, isLocked = req.IsLocked });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // GET /api/admin/trips?region=South&status=Completed&page=1
        [HttpGet("trips")]
        public IActionResult GetTrips([FromQuery] string region = "South",
            [FromQuery] string? status = null, [FromQuery] int page = 1)
        {
            try
            {
                string filter = string.IsNullOrEmpty(status) ? "" : "AND Status=@status";
                var table = _db.ExecuteReader(region,
                    $"SELECT TOP 50 TripID, UserID, DriverID, PickupLocation, DropoffLocation, " +
                    $"ISNULL(VehicleType,'') AS VehicleType, Fare, Status, CreatedAt " +
                    $"FROM Trips WHERE 1=1 {filter} ORDER BY CreatedAt DESC",
                    cmd => { if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@status", status); });

                var trips = table.Rows.Cast<System.Data.DataRow>().Select(r => new
                {
                    tripId      = (int)r["TripID"],
                    userId      = (int)r["UserID"],
                    driverId    = r["DriverID"] is DBNull ? null : (int?)Convert.ToInt32(r["DriverID"]),
                    pickup      = r["PickupLocation"].ToString(),
                    dropoff     = r["DropoffLocation"].ToString(),
                    vehicleType = r["VehicleType"].ToString(),
                    fare        = r["Fare"] is DBNull ? 0m : Convert.ToDecimal(r["Fare"]),
                    status      = r["Status"].ToString(),
                    createdAt   = r["CreatedAt"] is DBNull ? null : ((DateTime?)r["CreatedAt"])?.ToString("dd/MM/yyyy HH:mm")
                }).ToList();

                return Ok(trips);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // GET /api/admin/status — trạng thái tất cả server + event log (kết hợp auto-detect + manual)
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var allState = _runtimeState.GetAll();

            // Map to shape expected by admin.html (lowercase keys)
            var servers = allState.Select(r =>
            {
                var s = r.State;
                return new
                {
                    region = r.Region,
                    // primaryReal = can connect to primary
                    primaryReal = s.PrimaryHealthy,
                    // primarySimulated = admin manual override
                    primarySimulated = s.ManualOverrideDown,
                    // replicaReal = can connect to replica/backup
                    replicaReal = s.BackupHealthy,
                    // for display/debug
                    currentTarget = s.CurrentTarget.ToString(),
                    isDegraded = s.IsDegradedMode,
                    lastChecked = s.LastChecked
                };
            }).ToList();

            var logs = _failover.GetLogs()
                .Select(l => new { time = l.Time, region = l.Region, message = l.Message })
                .ToList();

            return Ok(new { servers, logs });
        }

        // POST /api/admin/simulate-down/{region} — giả lập Primary sập
        [HttpPost("simulate-down/{region}")]
        public IActionResult SimulateDown(string region)
        {
            _failover.SetPrimaryDown(region);
            return Ok(new { message = $"Primary [{region}] đã được giả lập SẬP. App chuyển sang Replica." });
        }

        // POST /api/admin/simulate-up/{region} — khôi phục Primary
        [HttpPost("simulate-up/{region}")]
        public IActionResult SimulateUp(string region)
        {
            _failover.SetPrimaryUp(region);
            return Ok(new { message = $"Primary [{region}] đã được khôi phục. App trở lại bình thường." });
        }

        // POST /api/admin/reset-manual-overrides — reset tất cả manual overrides (restore all)
        [HttpPost("reset-manual-overrides")]
        public IActionResult ResetManualOverrides()
        {
            var regions = _failover.GetManualDownRegions();
            foreach (var r in regions)
            {
                _failover.SetPrimaryUp(r);
            }
            return Ok(new { message = "Manual overrides reset for all regions.", regions });
        }

        // POST /api/admin/test-write/{region} — thử ghi vào Primary
        // Sẽ thất bại (503) khi Primary đang giả lập sập
        [HttpPost("test-write/{region}")]
        public IActionResult TestWrite(string region)
        {
            try
            {
                _db.ExecuteNonQuery(region,
                    "INSERT INTO Trips (UserID, PickupLocation, DropoffLocation, Region, Status) " +
                    "VALUES (1, N'[Admin Test] Điểm đón', N'[Admin Test] Điểm đến', @r, 'Test')",
                    cmd => cmd.Parameters.AddWithValue("@r", region));

                _failover.Append(region, $"✅ GHI thành công vào Primary [{region}]");
                return Ok(new { success = true, source = "Primary",
                    message = $"Ghi thành công vào Primary [{region}]." });
            }
            catch (InvalidOperationException ex)
            {
                _failover.Append(region, $"❌ GHI THẤT BẠI [{region}] — Primary không khả dụng");
                return StatusCode(503, new { success = false, source = "—", message = ex.Message });
            }
            catch (Exception ex)
            {
                _failover.Append(region, $"❌ GHI LỖI [{region}]: {ex.Message}");
                return StatusCode(500, new { success = false, source = "—", message = ex.Message });
            }
        }

        // GET /api/admin/test-read/{region} — thử đọc (tự fallover sang Replica nếu Primary sập)
        [HttpGet("test-read/{region}")]
        public IActionResult TestRead(string region)
        {
            try
            {
                var table = _db.ExecuteReader(region,
                    "SELECT TOP 5 TripID, PickupLocation, DropoffLocation, Status, CreatedAt " +
                    "FROM Trips ORDER BY CreatedAt DESC");

                var target  = _runtimeState.GetTarget(region);
                string source = target == DatabaseTarget.Primary ? "Primary" : "Replica";

                var rows = table.Rows.Cast<System.Data.DataRow>().Select(r => new
                {
                    TripID          = (int)r["TripID"],
                    PickupLocation  = r["PickupLocation"].ToString(),
                    DropoffLocation = r["DropoffLocation"].ToString(),
                    Status          = r["Status"].ToString(),
                    CreatedAt       = r["CreatedAt"] is DBNull ? "—" : ((DateTime)r["CreatedAt"]).ToString("dd/MM HH:mm")
                }).ToList();

                _failover.Append(region,
                    $"✅ ĐỌC thành công từ {source} [{region}] — {rows.Count} chuyến đi");

                return Ok(new { success = true, source, rowCount = rows.Count, data = rows });
            }
            catch (Exception ex)
            {
                _failover.Append(region, $"❌ ĐỌC THẤT BẠI [{region}]: {ex.Message}");
                return StatusCode(500, new { success = false, source = "—", message = ex.Message });
            }
        }
    }
}
