using Microsoft.Data.SqlClient;

namespace RideHailingApi.Services
{
    public interface IDatabaseProbe
    {
        Task<bool> CanConnectAsync(string connectionString, CancellationToken ct = default);
    }

    public class SqlDatabaseProbe : IDatabaseProbe
    {
        public async Task<bool> CanConnectAsync(string connectionString, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(connectionString)) return false;
            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(ct);
                await using var cmd = new SqlCommand("SELECT 1", conn);
                await cmd.ExecuteScalarAsync(ct);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
