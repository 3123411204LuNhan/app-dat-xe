using Microsoft.EntityFrameworkCore;
using RideHailingApi.Models;

namespace RideHailingApi.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<ScheduledTrip> ScheduledTrips { get; set; } = null!;

        // Keep legacy DataConnect usage for raw SQL operations
    }
}
