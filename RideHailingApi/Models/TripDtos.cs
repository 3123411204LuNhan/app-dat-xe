namespace RideHailingApi.Models
{
    public class PendingTripItem
    {
        public int TripID { get; set; }
        public int UserID { get; set; }
        public string PickupLocation { get; set; } = "";
        public string DropoffLocation { get; set; } = "";
        public string Region { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
    }

    public class TripStatusRequest
    {
        public string Status { get; set; } = string.Empty;   // Accepted | Arrived | Completed
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
        public DateTime? CreatedAt { get; set; }
    }

    // ===== Pooling Models =====
    public class PoolingCandidateItem
    {
        public int TripID { get; set; }
        public int UserID { get; set; }
        public string PickupLocation { get; set; } = "";
        public string DropoffLocation { get; set; } = "";
        public double PickupDistance { get; set; }      // Khoảng cách từ main trip pickup (km)
        public double DropoffDistance { get; set; }    // Khoảng cách từ main trip dropoff (km)
        public int MinutesOld { get; set; }             // Bao lâu trip này được tạo
        public DateTime? CreatedAt { get; set; }
    }

    public class PoolTripsRequest
    {
        public int MainTripID { get; set; }
        public int SecondaryTripID { get; set; }
    }

    public class PooledTripInfo
    {
        public int MainTripID { get; set; }
        public int SecondaryTripID { get; set; }
        public int? MainUserID { get; set; }
        public int? SecondaryUserID { get; set; }
        public string MainPickup { get; set; } = "";
        public string MainDropoff { get; set; } = "";
        public string SecondaryPickup { get; set; } = "";
        public string SecondaryDropoff { get; set; } = "";
        public int CurrentPassengers { get; set; } = 2;
        public DateTime? PooledAt { get; set; }
    }
}
