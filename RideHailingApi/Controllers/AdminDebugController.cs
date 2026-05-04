using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RideHailingApi.Data;

namespace RideHailingApi.Controllers
{
    [ApiController]
    [Route("api/admin/debug")]
    [Authorize(Roles = "USER")] // keep restricted; accessible with valid JWT
    public class AdminDebugController : ControllerBase
    {
        private readonly DataConnect _db;
        private readonly ILogger<AdminDebugController> _logger;

        public AdminDebugController(DataConnect db, ILogger<AdminDebugController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /api/admin/debug/user?userName=... or ?phone=...
        [HttpGet("user")]
        public IActionResult GetUser([FromQuery] string? userName, [FromQuery] string? phone)
        {
            if (string.IsNullOrWhiteSpace(userName) && string.IsNullOrWhiteSpace(phone))
                return BadRequest(new { error = "Provide userName or phone" });

            // Use RegionMiddleware helper extension
            string region = (HttpContext.Items["Region"] as string) ?? "South";
            try
            {
                var table = _db.ExecuteReader(region,
                    "SELECT TOP 5 UserID, UserName, Phone, PassWord, RegisteredRegion, IsLocked FROM Users WHERE UserName = @u OR Phone = @ph",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@u", userName ?? "");
                        cmd.Parameters.AddWithValue("@ph", phone ?? "");
                    });
                var list = new List<object>();
                foreach (System.Data.DataRow row in table.Rows)
                {
                    list.Add(new {
                        userId = row["UserID"],
                        userName = row["UserName"],
                        phone = row["Phone"],
                        passwordHash = row["PassWord"],
                        registeredRegion = row["RegisteredRegion"],
                        isLocked = row.Table.Columns.Contains("IsLocked") ? row["IsLocked"] : 0
                    });
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Debug user lookup failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
