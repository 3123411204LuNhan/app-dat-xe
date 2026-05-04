namespace RideHailingApi.Services
{
    public interface IConnectionStringResolver
    {
        string GetConnectionString(string region);
        bool IsDegradedMode(string region);
        DatabaseTarget GetCurrentTarget(string region);
    }

    // Resolve connection string thực tế dựa trên DatabaseRuntimeState
    public class ConnectionStringResolver : IConnectionStringResolver
    {
        private readonly IConfiguration _config;
        private readonly DatabaseRuntimeState _state;

        public ConnectionStringResolver(IConfiguration config, DatabaseRuntimeState state)
        {
            _config = config;
            _state  = state;
        }

        public string GetConnectionString(string region)
        {
            var target = _state.GetTarget(region);
            return target switch
            {
                DatabaseTarget.Primary => _config.GetConnectionString($"{region}_Primary") ?? "",
                DatabaseTarget.Backup  => _config.GetConnectionString($"{region}_Replica") ?? "",
                _                      => throw new InvalidOperationException(
                                              $"[{region}] Cả Primary và Backup đều không khả dụng.")
            };
        }

        public bool IsDegradedMode(string region)  => _state.IsDegraded(region);
        public DatabaseTarget GetCurrentTarget(string region) => _state.GetTarget(region);
    }
}
