using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace PokajanRandomizer.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HideSystemBars();
    }

    protected override void OnResume()
    {
        base.OnResume();
        HideSystemBars();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
        {
            HideSystemBars();
        }
    }

    private void HideSystemBars()
    {
        if (Window is null)
        {
            return;
        }

        Window.AddFlags(WindowManagerFlags.KeepScreenOn);
        Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
        if (OperatingSystem.IsAndroidVersionAtLeast(28) && Window.Attributes is not null)
        {
            Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
        }
        WindowCompat.SetDecorFitsSystemWindows(Window, false);

        var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
        if (controller is null)
        {
            return;
        }

        controller.Hide(WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars());
        controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
    }
}
