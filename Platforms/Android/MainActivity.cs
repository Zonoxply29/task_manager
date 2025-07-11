using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Microsoft.Maui.ApplicationModel; // Usamos las implementaciones de MAUI

namespace task_manager
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize |
                             ConfigChanges.Orientation |
                             ConfigChanges.UiMode |
                             ConfigChanges.ScreenLayout |
                             ConfigChanges.SmallestScreenSize |
                             ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            // Configuración de TLS para Android
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 |
                System.Net.SecurityProtocolType.Tls13;

            base.OnCreate(savedInstanceState);
        }
    }
}
