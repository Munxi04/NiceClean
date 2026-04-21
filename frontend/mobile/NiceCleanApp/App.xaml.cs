using CommunityToolkit.Maui.Core;
using NiceCleanApp.Pages;
using NiceCleanApp.Services;

namespace NiceCleanApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    protected override async void OnStart()
    {
        base.OnStart();

        // Set status bar color and style on supported platforms
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst())
        {
            try
            {
                CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(Color.FromArgb("#333B6D11"));
                CommunityToolkit.Maui.Core.Platform.StatusBar.SetStyle(StatusBarStyle.LightContent);
            }
            catch (NotSupportedException)
            {
                // Platform doesn't support changing the status bar — ignore
            }
        }

        // Get services
        var services = Windows[0].Page?.Handler?.MauiContext?.Services;
        if (services == null) return;

        var credentialService = services.GetRequiredService<ICredentialService>();
        var apiClient = services.GetRequiredService<IClient>();
        var userSession = services.GetRequiredService<IUserSession>();

        // Try to auto-login
        var (email, password) = await credentialService.GetCredentialsAsync();
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
        {
            try
            {
                var user = await apiClient.LoginAsync(new LoginDto
                {
                    Email = email,
                    Password = password
                });

                userSession.CurrentUser = user;

                // Navigate to MapPage
                await Shell.Current.GoToAsync("//MapPage");
                return;
            }
            catch
            {
                // Auto-login failed – clear bad credentials and show AuthPage
                credentialService.ClearCredentials();
            }
        }

        // No valid credentials – go to AuthPage
        await Shell.Current.GoToAsync("//AuthPage");
    }
}