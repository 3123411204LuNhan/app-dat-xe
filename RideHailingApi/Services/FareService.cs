namespace RideHailingApi.Services
{
    public class FareService
    {
        private static readonly Dictionary<string, decimal> _ratesPerKm = new()
        {
            ["Xe máy"]     = 3_000m,
            ["Ô tô 4 chỗ"] = 5_000m,
            ["Ô tô 7 chỗ"] = 7_000m,
        };

        private const decimal BaseFee = 10_000m;

        public decimal Calculate(string vehicleType, double distanceKm)
        {
            if (!_ratesPerKm.TryGetValue(vehicleType, out decimal rate))
                rate = 3_000m;
            decimal raw = BaseFee + rate * (decimal)distanceKm;
            return Math.Round(raw / 1000m) * 1000m;
        }

        public IReadOnlyDictionary<string, decimal> Rates => _ratesPerKm;
        public decimal BaseFeeValue => BaseFee;
    }
}
