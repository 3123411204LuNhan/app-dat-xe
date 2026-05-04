using RideHailingApp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
namespace RideHailingApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Cấu hình WebView cho Android để Google Maps JS API hoạt động
        ConfigureWebView();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Add simple file logging for diagnostics (writes to LocalApplicationData/app.log)
        var logFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logPath = Path.Combine(logFolder, "app.log");
        builder.Logging.AddProvider(new RideHailingApp.Logging.FileLoggerProvider(logPath));
        builder.Services.AddSingleton<GeoLocatorService>();
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<OfflineQueueService>();
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<TripHubService>();
        builder.Services.AddSingleton<GoogleMapsService>();
        builder.Services.AddSingleton<FcmService>();
        var app = builder.Build();
        Services = app.Services;
        return app;
    }

    static void ConfigureWebView()
    {
#if ANDROID
        WebViewHandler.Mapper.AppendToMapping(
            "WebViewCustomization",
            (handler, webview) =>
            {
                // Bật DOM storage, JS, Geolocation (Google Maps cần)
                handler.PlatformView.Settings.JavaScriptEnabled = true;
                handler.PlatformView.Settings.DomStorageEnabled = true;
                handler.PlatformView.Settings.DatabaseEnabled   = true;
                handler.PlatformView.Settings.SetGeolocationEnabled(true);
                handler.PlatformView.Settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
                
                // Cho phép chạy JS ngoài main thread
                Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
            });
#endif
    }

    public static IServiceProvider Services { get; private set; } = null!;
}