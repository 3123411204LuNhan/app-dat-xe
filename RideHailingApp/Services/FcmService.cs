namespace RideHailingApp.Services
{
    /// <summary>
    /// Quản lý Firebase Cloud Messaging: lưu token, đăng ký backend, xử lý notification khi app ở background.
    /// Phần platform-specific (nhận token từ Firebase SDK) nằm trong Platforms/Android/Services/FcmPlatformService.cs.
    /// </summary>
    public class FcmService
    {
        private readonly ApiService _apiService;
        private const string TokenPrefKey = "fcmDeviceToken";

        // Sự kiện để các page lắng nghe khi có notification foreground
        public event Action<string, Dictionary<string, string>>? NotificationReceived;

        public FcmService(ApiService apiService)
        {
            _apiService = apiService;
        }

        // ─── Khởi tạo ───

        public async Task InitializeAsync()
        {
            // Xin quyền thông báo (Android 13+)
#if ANDROID
            await RequestNotificationPermissionAsync();
#endif
            // Đăng ký token đã lưu (nếu có) với backend ngay sau login
            var token = GetStoredToken();
            if (!string.IsNullOrEmpty(token))
                await RegisterTokenWithBackendAsync(token);
        }

#if ANDROID
        private static async Task RequestNotificationPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
                await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
#endif

        // ─── Token Management ───

        public string? GetStoredToken() => Preferences.Get(TokenPrefKey, (string?)null);

        public void StoreToken(string token) => Preferences.Set(TokenPrefKey, token);

        // Gọi từ FcmPlatformService khi Firebase SDK trả về token mới
        public async Task OnTokenRefreshedAsync(string newToken)
        {
            StoreToken(newToken);
            await RegisterTokenWithBackendAsync(newToken);
        }

        private async Task RegisterTokenWithBackendAsync(string token)
        {
            int userId = Preferences.Get("userId", 0);
            if (userId == 0) return;

            string platform = DeviceInfo.Platform == DevicePlatform.iOS ? "ios" : "android";
            var req = new DeviceTokenRequest
            {
                UserId      = userId,
                DeviceToken = token,
                Platform    = platform
            };
            await _apiService.RegisterDeviceTokenAsync(req);
        }

        // ─── Xử lý Notification ───

        // Gọi từ FcmPlatformService khi nhận được data notification
        public void OnNotificationReceived(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("type", out var type)) return;

            NotificationReceived?.Invoke(type, data);

            // Nếu app đang foreground, xử lý deep link trực tiếp
            if (Application.Current?.MainPage != null)
            {
                data.TryGetValue("tripId", out var tripId);
                data.TryGetValue("screen", out var screen);
                HandleDeepLink(screen ?? type, data);
            }
        }

        public void HandleDeepLink(string screen, Dictionary<string, string> data)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var nav = Application.Current?.MainPage?.Navigation;
                if (nav == null) return;

                switch (screen)
                {
                    case "trip_tracking":
                    case "DriverAccepted":
                    case "DriverOnTheWay":
                        // User đang ở background → điều hướng về MainPage (đang theo dõi chuyến)
                        if (Application.Current?.MainPage is AppShell shell)
                        {
                            await shell.GoToAsync("//MainPage");
                        }
                        break;

                    case "trip_completed":
                        if (Application.Current?.MainPage is AppShell appShell)
                            await appShell.GoToAsync("//TripHistoryPage");
                        break;

                    case "new_trip_request":
                        // Driver nhận cuốc mới qua FCM khi app ở background
                        if (Application.Current?.MainPage is DriverShell driverShell)
                            await driverShell.GoToAsync("//DriverHomePage");
                        break;
                }
            });
        }
    }
}
