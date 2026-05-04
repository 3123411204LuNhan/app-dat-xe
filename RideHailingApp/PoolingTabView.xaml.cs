using System.Collections.ObjectModel;
using RideHailingApp.Services;

namespace RideHailingApp;

public partial class PoolingTabView : ContentView
{
    private readonly ApiService _apiService;
    private int _currentTripId = 0;
    private double _mainPickupLat, _mainPickupLon, _mainDropoffLat, _mainDropoffLon;

    public ObservableCollection<PoolingCandidateItem> PoolingCandidates { get; } = new();

    public PoolingTabView()
    {
        InitializeComponent();
        BindingContext = this;
        _apiService = MauiProgram.Services.GetRequiredService<ApiService>();
    }

    // ContentView has no DisplayAlert — delegate to the active Shell page
    private Task ShowAlert(string title, string message, string accept)
        => Shell.Current.DisplayAlert(title, message, accept);

    private Task<bool> ShowAlert(string title, string message, string accept, string cancel)
        => Shell.Current.DisplayAlert(title, message, accept, cancel);

    /// <summary>
    /// Khởi tạo tab ghép cuốc với thông tin cuốc hiện tại (MainTripId)
    /// </summary>
    public void Initialize(int mainTripId, double pickupLat, double pickupLon, double dropoffLat, double dropoffLon)
    {
        _currentTripId = mainTripId;
        _mainPickupLat = pickupLat;
        _mainPickupLon = pickupLon;
        _mainDropoffLat = dropoffLat;
        _mainDropoffLon = dropoffLon;

        // Cập nhật UI
        SearchCandidatesSection.IsVisible = true;
        ActivePoolingCard.IsVisible = false;
    }

    // ───────────────── Search for candidates ─────────────────

    private async void OnFindCandidatesClicked(object sender, EventArgs e)
    {
        if (_currentTripId == 0)
        {
            await ShowAlert("⚠️ Lỗi", "Chưa có cuốc nào được chọn để ghép.", "OK");
            return;
        }

        try
        {
            SearchLoadingIndicator.IsRunning = true;
            SearchLoadingIndicator.IsVisible = true;
            FindCandidatesButton.IsEnabled = false;

            var result = await _apiService.GetPoolCandidatesAsync(
                _currentTripId,
                _mainPickupLat,
                _mainPickupLon,
                _mainDropoffLat,
                _mainDropoffLon);

            PoolingCandidates.Clear();

            if (result.IsSuccess && result.Data != null)
            {
                foreach (var candidate in result.Data)
                    PoolingCandidates.Add(candidate);

                SearchingCountLabel.Text = PoolingCandidates.Count.ToString();

                if (PoolingCandidates.Count == 0)
                    await ShowAlert("ℹ️ Kết quả", "Không tìm thấy cuốc phù hợp để ghép. Hãy thử lại sau.", "OK");
                else
                    await ShowAlert("✅ Thành công", $"Tìm thấy {PoolingCandidates.Count} cuốc phù hợp!", "OK");
            }
            else
            {
                await ShowAlert("❌ Lỗi", result.ErrorMessage ?? "Không thể tìm kiếm cuốc.", "OK");
            }
        }
        catch (Exception ex)
        {
            await ShowAlert("❌ Lỗi", $"Lỗi: {ex.Message}", "OK");
        }
        finally
        {
            SearchLoadingIndicator.IsRunning = false;
            SearchLoadingIndicator.IsVisible = false;
            FindCandidatesButton.IsEnabled = true;
        }
    }

    // ───────────────── Pool a trip ─────────────────

    private async void OnPoolButtonClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not int secondaryTripId)
            return;

        // Xác nhận trước khi ghép
        bool confirm = await ShowAlert("Xác nhận ghép cuốc", 
            $"Bạn có chắc muốn ghép cuốc #{_currentTripId} với cuốc #{secondaryTripId}?",
            "Ghép", "Hủy");

        if (!confirm) return;

        try
        {
            btn.IsEnabled = false;
            btn.Text = "Đang ghép...";

            var result = await _apiService.PoolTripsAsync(_currentTripId, secondaryTripId);

            if (result.IsSuccess)
            {
                await ShowAlert("✅ Thành công", "Ghép cuốc thành công! Cả hai hành khách sẽ nhận thông báo.", "OK");

                // Cập nhật UI
                PooledCountLabel.Text = (int.Parse(PooledCountLabel.Text ?? "0") + 1).ToString();
                PoolingCandidates.Remove(PoolingCandidates.FirstOrDefault(c => c.TripID == secondaryTripId));
                SearchingCountLabel.Text = PoolingCandidates.Count.ToString();

                // Hiển thị info cuốc ghép
                ShowPooledTripInfo(_currentTripId, secondaryTripId);
            }
            else
            {
                await ShowAlert("❌ Lỗi", result.ErrorMessage ?? "Không thể ghép cuốc.", "OK");
            }
        }
        catch (Exception ex)
        {
            await ShowAlert("❌ Lỗi", $"Lỗi: {ex.Message}", "OK");
        }
        finally
        {
            btn.IsEnabled = true;
            btn.Text = "Ghép 🚗";
        }
    }

    // ───────────────── Display pooled trip info ─────────────────

    private async void ShowPooledTripInfo(int mainTripId, int secondaryTripId)
    {
        try
        {
            var result = await _apiService.GetPooledTripInfoAsync(mainTripId);

            if (result.IsSuccess && result.Data != null)
            {
                var poolInfo = result.Data;

                MainTripLabel.Text = $"#{poolInfo.MainTripID}: {poolInfo.MainPickup} → {poolInfo.MainDropoff}";
                SecondaryTripLabel.Text = $"#{poolInfo.SecondaryTripID}: {poolInfo.SecondaryPickup} → {poolInfo.SecondaryDropoff}";
                PassengersCountLabel.Text = $"👥 {poolInfo.CurrentPassengers}/2";

                SearchCandidatesSection.IsVisible = false;
                ActivePoolingCard.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            // Lỗi nhỏ — không show dialog, chỉ log
            System.Diagnostics.Debug.WriteLine($"Error showing pooled trip info: {ex.Message}");
        }
    }

    // ───────────────── Unpool ─────────────────

    private async void OnUnpoolClicked(object sender, EventArgs e)
    {
        bool confirm = await ShowAlert("Hủy ghép cuốc", 
            "Bạn có chắc muốn hủy ghép cuốc?",
            "Hủy", "Giữ lại");

        if (!confirm) return;

        try
        {
            // In real app: call API to unpool
            // Tạm thời chỉ cập nhật UI
            SearchCandidatesSection.IsVisible = true;
            ActivePoolingCard.IsVisible = false;
            PoolingCandidates.Clear();

            await ShowAlert("✅ Hoàn tất", "Cuốc đã được hủy ghép.", "OK");
        }
        catch (Exception ex)
        {
            await ShowAlert("❌ Lỗi", $"Lỗi: {ex.Message}", "OK");
        }
    }
}
