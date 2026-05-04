namespace RideHailingApi.Services
{
    public enum DatabaseTarget { Primary, Backup, None }

    // Trạng thái runtime của mỗi region DB (được inject vào các service khác dưới dạng Singleton)
    public class RegionDbState
    {
        public bool PrimaryHealthy          { get; set; } = true;
        public bool BackupHealthy           { get; set; } = true;
        public bool IsDegradedMode          { get; set; } = false;
        public DatabaseTarget CurrentTarget { get; set; } = DatabaseTarget.Primary;
        public int PrimaryRecoveryCount     { get; set; } = 0;
        public DateTime LastChecked         { get; set; } = DateTime.UtcNow;
        public bool ManualOverrideDown      { get; set; } = false;  // Admin giả lập
    }

    // Quản lý trạng thái failover runtime cho tất cả regions (thread-safe)
    public class DatabaseRuntimeState
    {
        private readonly Dictionary<string, RegionDbState> _states;
        private readonly object _lock = new();

        public DatabaseRuntimeState()
        {
            _states = new Dictionary<string, RegionDbState>(StringComparer.OrdinalIgnoreCase)
            {
                ["South"] = new RegionDbState(),
                ["North"] = new RegionDbState()
            };
        }

        public RegionDbState GetState(string region)
        {
            lock (_lock)
            {
                if (!_states.TryGetValue(region, out var s))
                {
                    s = new RegionDbState();
                    _states[region] = s;
                }
                return s;
            }
        }

        public void Update(string region, Action<RegionDbState> updater)
        {
            lock (_lock)
            {
                if (!_states.TryGetValue(region, out var s))
                {
                    s = new RegionDbState();
                    _states[region] = s;
                }
                updater(s);
            }
        }

        public bool IsDegraded(string region)
        {
            lock (_lock) return GetState(region).IsDegradedMode;
        }

        public DatabaseTarget GetTarget(string region)
        {
            lock (_lock) return GetState(region).CurrentTarget;
        }

        public IReadOnlyList<(string Region, RegionDbState State)> GetAll()
        {
            lock (_lock)
                return _states.Select(kv => (kv.Key, kv.Value)).ToList().AsReadOnly();
        }
    }
}
