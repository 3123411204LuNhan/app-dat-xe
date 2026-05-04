using SQLite;

namespace RideHailingApp.Services
{
    [Table("SyncQueue")]
    public class SyncQueueItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string RequestType { get; set; } = "";
        public string PayloadJson { get; set; } = "";
        public string Status { get; set; } = "Pending"; // Pending | Retrying | Synced | Failed
        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastAttemptAt { get; set; }
    }

    public class OfflineQueueService
    {
        private SQLiteAsyncConnection? _db;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private async Task<SQLiteAsyncConnection> GetDbAsync()
        {
            if (_db != null) return _db;
            var path = Path.Combine(FileSystem.AppDataDirectory, "offline_queue.db3");
            _db = new SQLiteAsyncConnection(path);
            await _db.CreateTableAsync<SyncQueueItem>();
            return _db;
        }

        public async Task EnqueueAsync(string requestType, string payloadJson)
        {
            var db = await GetDbAsync();
            await db.InsertAsync(new SyncQueueItem
            {
                RequestType = requestType,
                PayloadJson = payloadJson,
                Status      = "Pending",
                CreatedAt   = DateTime.UtcNow
            });
        }

        public async Task<List<SyncQueueItem>> GetPendingAsync()
        {
            var db = await GetDbAsync();
            return await db.Table<SyncQueueItem>()
                .Where(x => x.Status == "Pending" || x.Status == "Retrying")
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkSyncedAsync(int id)
        {
            var db = await GetDbAsync();
            await db.ExecuteAsync("UPDATE SyncQueue SET Status='Synced' WHERE Id=?", id);
        }

        public async Task MarkFailedAsync(int id, int currentRetryCount)
        {
            var db  = await GetDbAsync();
            string newStatus = currentRetryCount < 3 ? "Retrying" : "Failed";
            await db.ExecuteAsync(
                "UPDATE SyncQueue SET Status=?, RetryCount=?, LastAttemptAt=? WHERE Id=?",
                newStatus, currentRetryCount + 1, DateTime.UtcNow, id);
        }

        public async Task<int> GetPendingCountAsync()
        {
            var db = await GetDbAsync();
            return await db.Table<SyncQueueItem>()
                .CountAsync(x => x.Status == "Pending" || x.Status == "Retrying");
        }

        // Processed by ApiService when connectivity is restored
        public async Task ProcessQueueAsync(Func<SyncQueueItem, Task<bool>> sender)
        {
            await _lock.WaitAsync();
            try
            {
                var pending = await GetPendingAsync();
                foreach (var item in pending)
                {
                    bool success = false;
                    try { success = await sender(item); }
                    catch { }

                    if (success)
                        await MarkSyncedAsync(item.Id);
                    else
                        await MarkFailedAsync(item.Id, item.RetryCount);
                }
            }
            finally { _lock.Release(); }
        }
    }
}
