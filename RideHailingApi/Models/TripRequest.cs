namespace RideHailingApi.Models
{
    public class TripRequest
    {
        public int UserID { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public int PickupLocationID { get; set; } // Added for trip booking
        public int DropoffLocationID { get; set; } // Added for trip booking
        public string Region { get; set; } = string.Empty;
        public string VehicleType { get; set; } = "Xe máy";
        public double DistanceKm { get; set; }
    }
}