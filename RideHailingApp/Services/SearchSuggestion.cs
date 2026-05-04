namespace RideHailingApp.Services
{
    // Model thống nhất cho cả kết quả từ DB và Google Places
    public class SearchSuggestion
    {
        public string  DisplayText    { get; set; } = "";   // Tên địa điểm
        public string  SubText        { get; set; } = "";   // Địa chỉ phụ
        public bool    IsFromDatabase { get; set; } = false;
        public int?    LocationID     { get; set; }
        public double? Latitude       { get; set; }
        public double? Longitude      { get; set; }

        // Icon phân biệt nguồn
        public string Icon => IsFromDatabase ? "🏢" : "🔍";

        // Factory từ DB location
        public static SearchSuggestion FromDb(LocationItem item) => new()
        {
            DisplayText    = item.LocationName,
            SubText        = item.Address,
            IsFromDatabase = true,
            LocationID     = item.LocationID,
            Latitude       = item.Latitude,
            Longitude      = item.Longitude
        };

        // Factory từ Google Places
        public static SearchSuggestion FromGoogle(string displayText) => new()
        {
            DisplayText    = displayText,
            SubText        = "Google Maps",
            IsFromDatabase = false
        };
    }

    public record LocationItem(
        int    LocationID,
        string LocationName,
        string Address,
        double Latitude,
        double Longitude);
}
