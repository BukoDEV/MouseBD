using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace MouseBD.Platforms.Android;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density,
    ScreenOrientation = ScreenOrientation.Portrait)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Keep screen on while app is running
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);

        // Full screen immersive mode
        Window?.SetDecorFitsSystemWindows(false);
    }
}
