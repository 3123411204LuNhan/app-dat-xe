using System.Globalization;
using RideHailingApp.Services;

namespace RideHailingApp;

public partial class TripHistoryPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<TripDisplayItem> _items = new();

    public TripHistoryPage()
    {
        InitializeComponent();
        _apiService = MauiProgram.Services.GetRequiredService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTripHistoryAsync();
    }

    private async Task LoadTripHistoryAsync()
    {
        bool isReadOnly = Preferences.Get("isReadOnly", false);
        ReadOnlyBanner.IsVisible = isReadOnly;
        if (isReadOnly)
        {
            string regionName = Preferences.Get("regionName", "HCM");
            ReplicaServerLabel.Text = $"replica-{(regionName.Contains("Nam") ? "hcm" : "hn")}";
        }

        int userId = Preferences.Get("userID", 0);
        if (userId == 0)
        {
            TripList.ItemsSource = new List<TripDisplayItem>();
            return;
        }

        var result = await _apiService.GetTripHistoryAsync(userId);

        if (result.IsSuccess && result.Data != null)
        {
            _items = result.Data.Select(t => new TripDisplayItem
            {
                TripId        = t.TripID,
                StartAddress  = t.PickupLocation,
                DestAddress   = t.DropoffLocation,
                VehicleType   = string.IsNullOrEmpty(t.VehicleType) ? "Xe máy" : t.VehicleType,
                StatusText    = MapStatus(t.Status),
                StatusColor   = MapStatusColor(t.Status),
                PriceText     = t.Fare.HasValue && t.Fare > 0 ? $"{t.Fare.Value:#,##0}đ" : "—",
                DateText      = t.CreatedAt.HasValue
                                    ? t.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm")
                                    : "—",
                DriverName    = t.DriverID.HasValue ? $"Tài xế #{t.DriverID}" : "Chưa phân công",
                DriverInitial = t.DriverID.HasValue ? "T" : "?",
                Rating        = "—",
                CanRate       = t.Status?.ToLower() == "completed" && t.DriverID.HasValue
            }).ToList();

            TripList.ItemsSource = _items;
        }
        else if (result.IsReadOnlyMode)
        {
            Preferences.Set("isReadOnly", true);
            ReadOnlyBanner.IsVisible = true;
            await DisplayAlert("Chế độ dự phòng",
                "Đang kết nối server dự phòng (Replica). Chỉ xem được lịch sử cũ.", "OK");
        }
        else
        {
            await DisplayAlert("Không thể tải lịch sử",
                result.ErrorMessage ?? "Kiểm tra lại kết nối mạng.", "OK");
            TripList.ItemsSource = new List<TripDisplayItem>();
        }
    }

    private async void OnRateButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int tripId)
            await ShowRatingDialogAsync(tripId);
    }

    private async Task ShowRatingDialogAsync(int tripId)
    {
        string? scoreStr = await DisplayPromptAsync(
            "Đánh giá tài xế",
            "Nhập điểm từ 1 đến 5:",
            initialValue: "5",
            keyboard: Keyboard.Numeric,
            maxLength: 1);

        if (string.IsNullOrWhiteSpace(scoreStr)) return;
        if (!int.TryParse(scoreStr, out int score) || score < 1 || score > 5)
        {
            await DisplayAlert("Lỗi", "Điểm phải từ 1 đến 5.", "OK");
            return;
        }

        string? comment = await DisplayPromptAsync(
            "Nhận xét (tùy chọn)",
            "Để trống nếu không muốn nhận xét:",
            placeholder: "Tài xế thân thiện...");

        var result = await _apiService.SubmitRatingAsync(tripId, score, comment);
        if (result.IsSuccess)
        {
            await DisplayAlert("Cảm ơn!", $"Bạn đã đánh giá {score} ⭐ cho chuyến đi này.", "OK");

            var item = _items.FirstOrDefault(x => x.TripId == tripId);
            if (item != null)
            {
                item.Rating  = $"{score} ⭐";
                item.CanRate = false;
            }
            TripList.ItemsSource = null;
            TripList.ItemsSource = _items;
        }
        else
        {
            await DisplayAlert("Không thể đánh giá",
                result.ErrorMessage ?? "Chuyến đi này có thể đã được đánh giá rồi.", "OK");
        }
    }

    private static string MapStatus(string status) => status?.ToLower() switch
    {
        "completed" or "done" => "Hoàn thành",
        "cancelled"           => "Đã hủy",
        "inprogress"          => "Đang đi",
        "pending"             => "Đang chờ",
        _                     => status ?? "—"
    };

    private static string MapStatusColor(string status) => status?.ToLower() switch
    {
        "completed" or "done" => "#00C853",
        "cancelled"           => "#FF5252",
        "inprogress"          => "#2196F3",
        _                     => "#FFC107"
    };
}

public class TripDisplayItem
{
    public int    TripId        { get; set; }
    public string StartAddress  { get; set; } = "";
    public string DestAddress   { get; set; } = "";
    public string VehicleType   { get; set; } = "";
    public string StatusText    { get; set; } = "";
    public string StatusColor   { get; set; } = "#FFC107";
    public string PriceText     { get; set; } = "";
    public string DateText      { get; set; } = "";
    public string DriverName    { get; set; } = "";
    public string DriverInitial { get; set; } = "?";
    public string Rating        { get; set; } = "—";
    public bool   CanRate       { get; set; }
}

public class HexToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try { return Color.FromArgb(hex); } catch { }
        }
        return Color.FromArgb("#FFC107");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
