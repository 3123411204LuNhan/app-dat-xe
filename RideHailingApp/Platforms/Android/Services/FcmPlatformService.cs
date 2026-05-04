// ═══════════════════════════════════════════════════════════════════════════
//  FcmPlatformService.cs  —  Android-specific Firebase Cloud Messaging stub
//
//  Để kích hoạt FCM thật sự:
//  1. Thêm NuGet: Plugin.Firebase.CloudMessaging (>= 3.0.0)
//  2. Đặt google-services.json vào Platforms/Android/ (Build Action: GoogleServicesJson)
//  3. Bỏ comment toàn bộ code bên dưới
//  4. Đăng ký service trong AndroidManifest.xml (xem comment ở cuối file)
// ═══════════════════════════════════════════════════════════════════════════

// Uncomment when Plugin.Firebase.CloudMessaging is added:
//
// using Android.App;
// using Android.Content;
// using Firebase.Messaging;
// using RideHailingApp.Services;
//
// namespace RideHailingApp.Platforms.Android.Services
// {
//     [Service(Exported = false)]
//     [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
//     public class FcmPlatformService : FirebaseMessagingService
//     {
//         public override void OnNewToken(string token)
//         {
//             base.OnNewToken(token);
//             var fcmService = MauiProgram.Services.GetRequiredService<FcmService>();
//             _ = fcmService.OnTokenRefreshedAsync(token);
//         }
//
//         public override void OnMessageReceived(RemoteMessage message)
//         {
//             base.OnMessageReceived(message);
//
//             var data = message.Data.ToDictionary(kv => kv.Key, kv => kv.Value);
//
//             var fcmService = MauiProgram.Services.GetRequiredService<FcmService>();
//             fcmService.OnNotificationReceived(data);
//
//             // Hiển thị local notification nếu app đang foreground
//             if (IsAppInForeground())
//                 return; // FcmService đã xử lý foreground
//
//             // Build notification channel + show notification khi app ở background
//             ShowLocalNotification(message.GetNotification()?.Title ?? "RideHailing",
//                                   message.GetNotification()?.Body  ?? "",
//                                   data);
//         }
//
//         private static bool IsAppInForeground()
//         {
//             var activityManager = (ActivityManager?)global::Android.App.Application.Context
//                 .GetSystemService(ActivityService);
//             var runningApps = activityManager?.GetRunningAppProcesses();
//             if (runningApps == null) return false;
//             var packageName = global::Android.App.Application.Context.PackageName;
//             return runningApps.Any(p =>
//                 p.ProcessName == packageName &&
//                 p.Importance == ActivityManager.RunningAppProcessInfo.ImportanceForeground);
//         }
//
//         private void ShowLocalNotification(string title, string body, Dictionary<string, string> data)
//         {
//             const string channelId = "ridehailing_trips";
//             var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
//
//             if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
//             {
//                 var channel = new NotificationChannel(channelId, "Chuyến xe", NotificationImportance.High);
//                 notificationManager?.CreateNotificationChannel(channel);
//             }
//
//             var intent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? "") ?? new Intent();
//             foreach (var kv in data) intent.PutExtra(kv.Key, kv.Value);
//             var pendingIntent = PendingIntent.GetActivity(this, 0, intent,
//                 PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
//
//             var notification = new global::Android.App.Notification.Builder(this, channelId)
//                 .SetContentTitle(title)
//                 .SetContentText(body)
//                 .SetSmallIcon(Resource.Drawable.ic_stat_notification)
//                 .SetContentIntent(pendingIntent)
//                 .SetAutoCancel(true)
//                 .Build();
//
//             notificationManager?.Notify(data.GetValueOrDefault("tripId", "0").GetHashCode(), notification);
//         }
//     }
// }

// ─── AndroidManifest.xml additions (inside <application> tag) ───────────────
// <service android:name="com.ridehailing.app.platforms.android.services.FcmPlatformService"
//          android:exported="false">
//     <intent-filter>
//         <action android:name="com.google.firebase.MESSAGING_EVENT" />
//     </intent-filter>
// </service>

namespace RideHailingApp.Platforms.Android.Services
{
    // Placeholder — xem hướng dẫn uncomment ở trên để kích hoạt FCM thật sự
    public class FcmPlatformService { }
}
