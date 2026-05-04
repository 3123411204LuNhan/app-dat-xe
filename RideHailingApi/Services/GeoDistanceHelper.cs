namespace RideHailingApi.Services
{
    /// <summary>
    /// Helper để tính khoảng cách giữa 2 tọa độ dùng Haversine formula.
    /// Đơn vị: Kilometer
    /// </summary>
    public static class GeoDistanceHelper
    {
        private const double EarthRadiusKm = 6371.0;

        /// <summary>
        /// Tính khoảng cách giữa 2 điểm GPS (lat/lon) bằng Haversine formula
        /// </summary>
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
