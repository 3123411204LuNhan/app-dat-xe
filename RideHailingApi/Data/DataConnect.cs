using Microsoft.Data.SqlClient;
using RideHailingApi.Services;
using Microsoft.Extensions.Logging;
using System.Data;
using Microsoft.EntityFrameworkCore;
using RideHailingApi.Models;

namespace RideHailingApi.Data
{
    public class DataConnect
    {
        private readonly DbContextOptions<DataContext>? _efOptions;
        private readonly IConnectionStringResolver _resolver;
        private readonly DatabaseRuntimeState      _state;
        private readonly ILogger<DataConnect>?     _logger;

        public DataConnect(IConnectionStringResolver resolver, DatabaseRuntimeState state, ILogger<DataConnect>? logger = null)
        {
            _resolver = resolver;
            _state    = state;
            _logger   = logger;
        }

        // ── WRITE ─────────────────────────────────────────────────────────────────
        // Luôn ghi vào Primary. Ném lỗi nếu đang DegradedMode hoặc Primary không khả dụng.

        public object? ExecuteScalarWrite(string region, string sql, Action<SqlCommand>? parameterizer = null)
        {
            EnsureWritable(region);
            string cs = _resolver.GetConnectionString(region);   // Primary khi không degraded
            try
            {
                using var conn = new SqlConnection(cs);
                conn.Open();
                using var cmd = new SqlCommand(sql, conn);
                parameterizer?.Invoke(cmd);
                return cmd.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "ExecuteScalarWrite failed for region {Region}. ConnectionString={Cs}. SqlException: {Msg}", region, cs, ex.Message);
                // Rethrow original SqlException so callers can inspect ex.Number (e.g. unique constraint)
                throw;
            }
        }

        public int ExecuteNonQuery(string region, string sql, Action<SqlCommand>? parameterizer = null)
        {
            EnsureWritable(region);
            string cs = _resolver.GetConnectionString(region);
            try
            {
                using var conn = new SqlConnection(cs);
                conn.Open();
                using var cmd = new SqlCommand(sql, conn);
                parameterizer?.Invoke(cmd);
                return cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "ExecuteNonQuery failed for region {Region}. ConnectionString={Cs}. SqlException: {Msg}", region, cs, ex.Message);
                throw;
            }
        }

        // ── READ ──────────────────────────────────────────────────────────────────
        // Dùng connection string hiện tại (Primary hoặc Backup tuỳ trạng thái).

        public object? ExecuteScalar(string region, string sql, Action<SqlCommand>? parameterizer = null)
            => ExecuteRead(region, sql, parameterizer, cmd => cmd.ExecuteScalar());

        // Helper to execute scalar and return int safely
        public int ExecuteScalarInt(string region, string sql, Action<SqlCommand>? parameterizer = null)
        {
            var obj = ExecuteScalar(region, sql, parameterizer);
            try { return Convert.ToInt32(obj ?? 0); } catch { return 0; }
        }

        public DataTable ExecuteReader(string region, string sql, Action<SqlCommand>? parameterizer = null)
            => ExecuteRead(region, sql, parameterizer, cmd =>
            {
                var table = new DataTable();
                using var reader = cmd.ExecuteReader();
                table.Load(reader);
                return table;
            });

        // ── HEALTH ────────────────────────────────────────────────────────────────

        public bool IsPrimaryAlive(string region)
            => _state.GetState(region).PrimaryHealthy;

        public bool IsReplicaAlive(string region)
            => _state.GetState(region).BackupHealthy;

        // ── PRIVATE ───────────────────────────────────────────────────────────────

        private void EnsureWritable(string region)
        {
            if (_resolver.IsDegradedMode(region))
            {
                var target = _resolver.GetCurrentTarget(region);
                string reason = target == DatabaseTarget.None
                    ? $"[{region}] Cả Primary và Backup đều không khả dụng."
                    : $"[{region}] Đang chạy trên Backup DB (DegradedMode). Không thể ghi dữ liệu.";
                throw new InvalidOperationException(reason);
            }
        }

        private T ExecuteRead<T>(string region, string sql,
            Action<SqlCommand>? parameterizer, Func<SqlCommand, T> execute)
        {
            // Dùng connection string do resolver quyết định (Primary hoặc Backup theo trạng thái hiện tại)
            string cs = _resolver.GetConnectionString(region);
            using var conn = new SqlConnection(cs);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            parameterizer?.Invoke(cmd);
            return execute(cmd);
        }
    }
}
