using Microsoft.Extensions.Logging;
using MouseBD.Controls;
using MouseBD.Services;
using MouseBD.Views;

#if ANDROID
using MouseBD.Platforms.Android;
#endif

namespace MouseBD;

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
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler<MultiTouchSurface, MultiTouchSurfaceHandler>();
#endif
            });

        // Register services
        builder.Services.AddSingleton<TouchpadService>();

        // Register pages
        builder.Services.AddSingleton<TouchpadPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
