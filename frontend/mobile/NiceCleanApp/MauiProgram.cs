using CommunityToolkit.Maui;
using Fonts;
using Microsoft.Extensions.Logging;
using NiceCleanApp.Pages;
using NiceCleanApp.Services;
using Syncfusion.Maui.Toolkit.Hosting;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp.Views.Maui.Handlers;

namespace NiceCleanApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit(options => options.SetShouldEnableSnackbarOnWindows(true))
            .ConfigureSyncfusionToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);

                // Custom fonts for authentication page
                fonts.AddFont("InstrumentSerif-Regular.ttf", "InstrumentSerif");
                fonts.AddFont("InstrumentSerif-Italic.ttf", "InstrumentSerifItalic");
                fonts.AddFont("DMSans-Regular.ttf", "DMSans");
                fonts.AddFont("DMSans-Medium.ttf", "DMSansMedium");
                fonts.AddFont("DMSans-Light.ttf", "DMSansLight");
            })
            .ConfigureMauiHandlers(handlers =>
            {
                // Register SkiaSharp handler for SKGLView so MAUI can map the control to a platform view
                handlers.AddHandler<SKGLView, SKGLViewHandler>();
            });

#if DEBUG
        builder.Logging.AddDebug();
        builder.Services.AddLogging(c => c.AddDebug());
#endif

        // Services
        builder.Services.AddHttpClient<IClient, Client>(client =>
        {
            client.BaseAddress = new Uri("https://nicecleanrest.azurewebsites.net");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // User session is a singleton since we want to maintain the logged-in user state across the app
        builder.Services.AddSingleton<ICredentialService, CredentialService>();
        builder.Services.AddSingleton<IUserSession, UserSession>();

        // Pages
        builder.Services.AddTransient<AuthPage>();
        builder.Services.AddSingleton<MapPage>();

        return builder.Build();
    }
}