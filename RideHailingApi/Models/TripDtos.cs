namespace RideHailingApi.Models
{
    public class PendingTripItem
    {
        public int TripID { get; set; }
        public int UserID { get; set; }
        public string PickupLocation { get; set; } = "";
        public string DropoffLocation { get; set; } = "";
        public string Region { get; set; } = "";
        public string VehicleType { get; set; } = "";
        public decimal? EstimatedFare { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class TripStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class TripHistoryItem
    {
        public int TripID { get; set; }
        public int UserID { get; set; }
        public int? DriverID { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public decimal? Fare { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
