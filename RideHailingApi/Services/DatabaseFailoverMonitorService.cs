namespace RideHailingApi.Services
{
    public class DatabaseFailoverMonitorService : BackgroundService
    {
        private static readonly string[] Regions = { "South", "North" };

        private readonly IConfiguration        _config;
        private readonly DatabaseRuntimeState   _state;
        private readonly IDatabaseProbe         _probe;
        private readonly FailoverSimulator      _simulator;   // Giữ tích hợp với admin manual override
        private readonly ILogger<DatabaseFailoverMonitorService> _logger;
        public DatabaseFailoverMonitorService(
            IConfiguration config,
            DatabaseRuntimeState state,
            IDatabaseProbe probe,
            FailoverSimulator simulator,
            ILogger<DatabaseFailoverMonitorService> logger)
        {
            _config    = config;
            _state     = state;
            _probe     = probe;
            _simulator = simulator;
            _logger    = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int intervalSec       = _config.GetValue("DatabaseFailover:HealthCheckIntervalSeconds", 10);
            int recoveryThreshold = _config.GetValue("DatabaseFailover:RecoverySuccessThreshold", 3);
            // By default do NOT perform automatic failover unless explicitly enabled in configuration.
            bool enableAutoFailoverGlobal = _config.GetValue("DatabaseFailover:EnableAutoFailover", false);
            _logger.LogInformation(
                "DatabaseFailoverMonitor started — interval={Interval}s, recoveryThreshold={Threshold}",
                intervalSec, recoveryThreshold);
            if (!enableAutoFailoverGlobal)
            {
                _logger.LogWarning("Automatic failover is DISABLED by configuration. Only admin manual override will trigger failover.");
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var region in Regions)
                {
                    await CheckRegionAsync(region, recoveryThreshold, stoppingToken, enableAutoFailoverGlobal);
                }
                await Task.Delay(TimeSpan.FromSeconds(intervalSec), stoppingToken);
            }
        }
        private async Task CheckRegionAsync(string region, int recoveryThreshold, CancellationToken ct, bool enableAutoFailover)
        {
            string primaryCs = _config.GetConnectionString($"{region}_Primary") ?? "";
            string backupCs  = _config.GetConnectionString($"{region}_Replica")  ?? "";

            // Admin manual override trumps real check
            bool manualDown    = _simulator.IsPrimaryDown(region);
            bool primaryAlive  = !manualDown && await _probe.CanConnectAsync(primaryCs, ct);
            bool backupAlive   = await _probe.CanConnectAsync(backupCs, ct);

            // Note: enableAutoFailover is passed from ExecuteAsync (global flag)

            _state.Update(region, s =>
            {
                s.PrimaryHealthy   = primaryAlive;
                s.BackupHealthy    = backupAlive;
                s.ManualOverrideDown = manualDown;
                s.LastChecked      = DateTime.UtcNow;

                if (primaryAlive)
                    s.PrimaryRecoveryCount++;
                else
                    s.PrimaryRecoveryCount = 0;

                if (manualDown)
                {
                    // Admin explicitly requested failover -> switch to backup
                    if (s.CurrentTarget != DatabaseTarget.Backup)
                    {
                        _logger.LogWarning(
                            "[{Region}] Primary DB xuống (admin manual override). Chuyển sang Backup — DegradedMode ON.",
                            region);
                        _simulator.Append(region,
                            $"⚠ Manual-failover [{region}]: Admin bật chế độ failover — chuyển sang Backup DB.");
                    }
                    s.CurrentTarget = DatabaseTarget.Backup;
                    s.IsDegradedMode = true;
                }
                else if (enableAutoFailover && !manualDown && !primaryAlive && backupAlive)
                {
                    // Automatic failover enabled and primary down -> switch to backup
                    if (s.CurrentTarget != DatabaseTarget.Backup)
                    {
                        string reason = "connection failure";
                        _logger.LogWarning(
                            "[{Region}] Primary DB xuống ({Reason}). Chuyển sang Backup — DegradedMode ON.",
                            region, reason);
                        _simulator.Append(region,
                            $"⚠ Auto-failover [{region}]: Primary xuống ({reason}) — chuyển sang Backup DB.");
                    }
                    s.CurrentTarget = DatabaseTarget.Backup;
                    s.IsDegradedMode = true;
                }
                else if (primaryAlive && s.PrimaryRecoveryCount >= recoveryThreshold)
                {
                    if (s.CurrentTarget != DatabaseTarget.Primary)
                    {
                        _logger.LogInformation(
                            "[{Region}] Primary DB hồi phục (liên tiếp {Count}/{Threshold} lần). Quay về Primary — DegradedMode OFF.",
                            region, s.PrimaryRecoveryCount, recoveryThreshold);
                        _simulator.Append(region,
                            $"✅ Auto-recovery [{region}]: Primary hồi phục — quay về Primary DB.");
                    }
                    s.CurrentTarget  = DatabaseTarget.Primary;
                    s.IsDegradedMode = false;
                }
                else if (!primaryAlive && !backupAlive)
                {
                    if (s.CurrentTarget != DatabaseTarget.None)
                    {
                        _logger.LogError(
                            "[{Region}] CẢ Primary VÀ Backup đều không khả dụng!", region);
                        _simulator.Append(region,
                            $"🔴 CRITICAL [{region}]: Cả Primary và Backup đều XUỐNG!");
                    }
                    s.CurrentTarget  = DatabaseTarget.None;
                    s.IsDegradedMode = true;
                }
            });
        }
    }
}
