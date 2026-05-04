using RideHailingApp.Services;

namespace RideHailingApp;

public partial class RegisterPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly GeoLocatorService _geo;

    public RegisterPage()
    {
        InitializeComponent();
        // Lấy ApiService từ trung tâm
        _apiService = MauiProgram.Services.GetRequiredService<ApiService>();
        _geo = MauiProgram.Services.GetRequiredService<GeoLocatorService>();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var fullName = FullNameEntry.Text?.Trim();
        var userName = UserNameEntry.Text?.Trim();
        var phone = PhoneEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var confirm = ConfirmPasswordEntry.Text;

        // Validate
        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng điền đầy đủ thông tin.");
            return;
        }
        if (password != confirm)
        {
            ShowError("Mật khẩu xác nhận không khớp.");
            return;
        }
        if (password.Length < 6)
        {
            ShowError("Mật khẩu phải có ít nhất 6 ký tự.");
            return;
        }
        if (phone.Length < 8)
        {
            ShowError("Số điện thoại không hợp lệ.");
            return;
        }

        ErrorLabel.IsVisible = false;
        ((Button)sender).IsEnabled = false;

        // Xác định vùng từ Picker (0 = South, 1 = North)
        string region;
        if (RegionPicker.SelectedIndex == 1) region = "North";
        else if (RegionPicker.SelectedIndex == 0) region = "South";
        else
            region = _geo.GetCachedRegion();

        _geo.SetRegionManually(region);
        System.Diagnostics.Debug.WriteLine($"[RegisterPage] Using region='{region}' for register");

        // GÓI DỮ LIỆU ĐỂ GỬI LÊN API
        var request = new RegisterRequest
        {
            UserName = userName,
            FullName = fullName,
            Phone = phone,
            Password = password,
            RegisteredRegion = region
        };

        // Kiểm tra trạng thái read-only của server trước khi gửi
        var canWrite = await _apiService.CheckAndSetReadOnlyAsync();
        if (!canWrite)
        {
            await DisplayAlert("Bảo trì", "Server hiện đang ở chế độ Read-Only. Không thể tạo tài khoản mới.", "OK");
            ((Button)sender).IsEnabled = true;
            return;
        }

        // GỌI API ĐĂNG KÝ
        var result = await _apiService.RegisterAsync(request);

        if (result.IsSuccess)
        {
            await DisplayAlert("Thành công!", $"Tài khoản đã được tạo tại Server {region}.\nVui lòng đăng nhập.", "OK");
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }
        else if (result.IsReadOnlyMode)
        {
            await DisplayAlert("Bảo trì", "Server chính đang sập, không thể tạo tài khoản mới lúc này. Vui lòng thử lại sau.", "OK");
        }
        else
        {
            // Parse lỗi chuẩn hóa từ API
            var msg = result.ErrorMessage ?? "Đăng ký thất bại. Tên đăng nhập hoặc SĐT có thể đã tồn tại.";
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(msg);
                if (doc.RootElement.TryGetProperty("message", out var m))
                    msg = m.GetString() ?? msg;
                else if (doc.RootElement.TryGetProperty("error", out var e2))
                    msg = e2.GetString() ?? msg;
            }
            catch { }
            ShowError(msg);
        }

        ((Button)sender).IsEnabled = true;
    }

    private void OnGoToLoginClicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new NavigationPage(new LoginPage());
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}