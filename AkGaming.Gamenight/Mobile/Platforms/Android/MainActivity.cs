using Android.App;
using Android.Content;
using Android.Content.PM;
namespace AkGaming.Gamenight.Mobile;
[Activity(Theme = "@style/Maui.MainTheme.NoActionBar", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity { }
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "de.akgaming.gamenight", DataHost = "callback")]
public class AuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity { }

