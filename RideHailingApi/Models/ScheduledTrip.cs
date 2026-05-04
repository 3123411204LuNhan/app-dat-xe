using System;
using System.ComponentModel.DataAnnotations;

namespace RideHailingApi.Models
{
    public class ScheduledTrip
    {
        [Key]
        public int ScheduledTripId { get; set; }

        public int UserId { get; set; }

        public string PickupAddress { get; set; } = string.Empty;
        public double? PickupLat { get; set; }
        public double? PickupLng { get; set; }

        public string DropoffAddress { get; set; } = string.Empty;
        public double? DropoffLat { get; set; }
        public double? DropoffLng { get; set; }

        public string VehicleType { get; set; } = "Xe máy";

        public DateTime ScheduledPickupTime { get; set; }

        public string Status { get; set; } = "Scheduled";

        public double? DistanceKm { get; set; }

        public decimal? EstimatedFare { get; set; }

        public int? TripId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public string Region { get; set; } = "South";
    }
}