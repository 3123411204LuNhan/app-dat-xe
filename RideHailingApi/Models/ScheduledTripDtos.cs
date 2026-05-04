namespace RideHailingApi.Models
{
    public class CreateScheduledTripRequest
    {
        public string PickupAddress       { get; set; } = "";
        public double? PickupLat          { get; set; }
        public double? PickupLng          { get; set; }
        public string DropoffAddress      { get; set; } = "";
        public double? DropoffLat         { get; set; }
        public double? DropoffLng         { get; set; }
        public string VehicleType         { get; set; } = "Xe máy";
        public DateTime ScheduledPickupTime { get; set; }
        public double? DistanceKm         { get; set; }
    }

    public class ScheduledTripDto
    {
        public int    ScheduledTripId     { get; set; }
        public int    UserId              { get; set; }
        public string PickupAddress       { get; set; } = "";
        public string DropoffAddress      { get; set; } = "";
        public string VehicleType         { get; set; } = "";
        public string ScheduledPickupTime { get; set; } = "";
        public string Status              { get; set; } = "";
        public string Region              { get; set; } = "";
        public decimal? EstimatedFare     { get; set; }
        public double? DistanceKm         { get; set; }
        public int?   TripId              { get; set; }
        public string CreatedAt           { get; set; } = "";
        public bool   CanCancel           { get; set; }
    }
}
