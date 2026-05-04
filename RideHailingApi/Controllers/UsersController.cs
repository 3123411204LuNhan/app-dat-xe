using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RideHailingApi.Data;
using RideHailingApi.Middleware;
using RideHailingApi.Models;

namespace RideHailingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly DataConnect _db;
        private readonly ILogger<UsersController> _logger;

        public UsersController(DataConnect db, ILogger<UsersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /api/users/{id} — read profile (can failover to Replica)
        [HttpGet("{id:int}")]
        public IActionResult GetProfile(int id)
        {
            string region = HttpContext.GetRegion();
            try
            {
                var table = _db.ExecuteReader(region,
                    "SELECT UserID, UserName, FullName, Phone, RegisteredRegion FROM Users WHERE UserID = @id",
                    cmd => cmd.Parameters.AddWithValue("@id", id));

                if (table.Rows.Count == 0)
                    return NotFound(new { error = "Không tìm thấy người dùng." });

                var row = table.Rows[0];
                return Ok(new UserDto
                {
                    UserID = (int)row["UserID"],
                    UserName = row["UserName"].ToString() ?? "",
                    FullName = row["FullName"].ToString() ?? "",
                    Phone = row["Phone"].ToString() ?? "",
                    RegisteredRegion = row["RegisteredRegion"].ToString() ?? ""
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT /api/users/{id} — Protected: yêu cầu JWT hợp lệ
        [Authorize]
        [HttpPut("{id:int}")]
        public IActionResult UpdateProfile(int id, [FromBody] UpdateProfileRequest req)
        {
            string region = HttpContext.GetRegion();
            try
            {
                int rows;
                if (string.IsNullOrEmpty(req.NewPassword))
                {
                    rows = _db.ExecuteNonQuery(region,
                        "UPDATE Users SET FullName = @n, Phone = @ph WHERE UserID = @id",
                        cmd =>
                        {
                            cmd.Parameters.AddWithValue("@n", req.FullName);
                            cmd.Parameters.AddWithValue("@ph", req.Phone);
                            cmd.Parameters.AddWithValue("@id", id);
                        });
                }
                else
                {
                    rows = _db.ExecuteNonQuery(region,
                        "UPDATE Users SET FullName = @n, Phone = @ph, PassWord = @pw WHERE UserID = @id",
                        cmd =>
                        {
                            cmd.Parameters.AddWithValue("@n", req.FullName);
                            cmd.Parameters.AddWithValue("@ph", req.Phone);
                            cmd.Parameters.AddWithValue("@pw", req.NewPassword);
                            cmd.Parameters.AddWithValue("@id", id);
                        });
                }

                if (rows == 0)
                    return NotFound(new { error = "Không tìm thấy người dùng." });
                return Ok(new { message = "Cập nhật thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
