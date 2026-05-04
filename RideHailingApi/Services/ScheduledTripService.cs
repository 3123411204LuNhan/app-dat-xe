using Microsoft.EntityFrameworkCore;
using RideHailingApi.Data;
using RideHailingApi.Models;

namespace RideHailingApi.Services
{
    public class ScheduledTripService
    {
        private readonly DataContext _ctx;

        public ScheduledTripService(DataContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<ScheduledTrip> CreateAsync(ScheduledTrip trip)
        {
            _ctx.ScheduledTrips.Add(trip);
            await _ctx.SaveChangesAsync();
            return trip;
        }

        public async Task<List<ScheduledTrip>> GetByUserAsync(int userId)
        {
            return await _ctx.ScheduledTrips
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<ScheduledTrip?> GetByIdAsync(int id, int userId)
        {
            return await _ctx.ScheduledTrips.FirstOrDefaultAsync(s => s.ScheduledTripId == id && s.UserId == userId);
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var ent = await _ctx.ScheduledTrips.FirstOrDefaultAsync(s => s.ScheduledTripId == id && s.UserId == userId);
            if (ent == null) return false;
            if (ent.Status != "Scheduled") return false; // only allow cancel before activation
            _ctx.ScheduledTrips.Remove(ent);
            await _ctx.SaveChangesAsync();
            return true;
        }
    }
}
