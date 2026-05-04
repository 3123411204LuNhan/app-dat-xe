using Microsoft.AspNetCore.Mvc;
using RideHailingApi.Services;

namespace RideHailingApi.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly DatabaseRuntimeState   _state;
        private readonly FailoverSimulator       _simulator;

        public HealthController(DatabaseRuntimeState state, FailoverSimulator simulator)
        {
            _state     = state;
            _simulator = simulator;
        }

        // GET /health/db — trạng thái failover của tất cả regions
        [HttpGet("db")]
        public IActionResult GetDbHealth()
        {
            var regions = _state.GetAll().Select(r =>
            {
                var s = r.State;
                string mode = s.CurrentTarget switch
                {
                    DatabaseTarget.Primary => "Normal",
                    DatabaseTarget.Backup  => "Degraded",
                    _                      => "Unavailable"
                };
                return new
                {
                    region          = r.Region,
                    mode,
                    isDegradedMode  = s.IsDegradedMode,
                    currentTarget   = s.CurrentTarget.ToString(),
                    primaryHealthy  = s.PrimaryHealthy,
                    backupHealthy   = s.BackupHealthy,
                    manualOverride  = s.ManualOverrideDown,
                    lastChecked     = s.LastChecked
                };
            });

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                regions
            });
        }

        // GET /health/db/{region} — trạng thái failover của một region cụ thể (dùng bởi MAUI app)
        [HttpGet("db/{region}")]
        public IActionResult GetRegionHealth(string region)
        {
            var s = _state.GetState(region);
            bool isFailover = s.CurrentTarget != DatabaseTarget.Primary;

            // Tương thích với client MAUI (trường IsFailover để app biết đang ở Replica)
            return Ok(new
            {
                region,
                isFailover,
                isDegradedMode  = s.IsDegradedMode,
                currentTarget   = s.CurrentTarget.ToString(),
                primaryHealthy  = s.PrimaryHealthy,
                backupHealthy   = s.BackupHealthy,
                lastChecked     = s.LastChecked
            });
        }

        // GET /health/ping — health check đơn giản cho load balancer
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { status = "ok", time = DateTime.UtcNow });
    }
}
