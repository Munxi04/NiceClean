using NiceCleanApp.Services;

namespace NiceCleanApp.Pages;

public partial class AuthPage : ContentPage
{
    private readonly IClient _api;
    private bool _isLogin = true;
    private readonly IUserSession _userSession;
    private readonly ICredentialService _credentialService;

    public AuthPage(IClient api, IUserSession userSession, ICredentialService credentialService)
    {
        InitializeComponent();
        _api = api;
        _userSession = userSession;
        _credentialService = credentialService;

        // Follow the device theme by default unless the user has already
        // chosen a preference during this session.
        if (Application.Current!.UserAppTheme == AppTheme.Unspecified)
            Application.Current.UserAppTheme = AppTheme.Unspecified;
        
        UpdateTabIndicator();
        UpdateThemeHighlight();
    }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateThemeHighlight();
    }

    // ──────────────────────────────────────────────
    // Tab switching
    // ──────────────────────────────────────────────

    /// <summary>
    /// Switches between the login and registration forms, updating the UI elements
    /// </summary>
    /// <param name="login"></param>
    void SwitchTab(bool login)
    {
        _isLogin = login;
        LoginForm.IsVisible = login;
        RegisterForm.IsVisible = !login;
        HeadingLabel.Text = login ? "Welcome back" : "Join NiceClean";
        SubLabel.Text = login
            ? "Sign in to report and track pollution in your area."
            : "Create your account to start making a difference.";
        TabLogin.TextColor = login ? Color.FromArgb("#3B6D11") : Color.FromArgb("#aaa");
        TabLogin.FontAttributes = login ? FontAttributes.Bold : FontAttributes.None;
        TabRegister.TextColor = !login ? Color.FromArgb("#3B6D11") : Color.FromArgb("#aaa");
        TabRegister.FontAttributes = !login ? FontAttributes.Bold : FontAttributes.None;
        UpdateTabIndicator();
        ClearAlerts();
    }

    /// <summary>
    /// Updates the width and position of the tab indicator to align with the currently active tab.
    /// </summary>
    /// <remarks>This method should be called whenever the active tab changes to ensure the indicator
    /// accurately reflects the selected tab. The indicator's width is adjusted based on the active tab's text length,
    /// and its position is updated to match the active tab.</remarks>
    void UpdateTabIndicator()
    {
        var activeButton = _isLogin ? TabLogin : TabRegister;
        var text = activeButton.Text;

        var width = text.Length * 8;
        TabIndicator.WidthRequest = width;
        

        var leftMargin = _isLogin ? 0 : TabLogin.Width + 24; 
        TabIndicator.Margin = new Thickness(leftMargin, 0, 0, 0);
    }

    void OnTabLoginClicked(object sender, EventArgs e) => SwitchTab(true);
    void OnTabRegisterClicked(object sender, EventArgs e) => SwitchTab(false);

    // ──────────────────────────────────────────────
    // Alert helpers
    // ──────────────────────────────────────────────
    void ShowError(string msg) { ErrorLabel.Text = msg; ErrorBox.IsVisible = true; SuccessBox.IsVisible = false; }
    void ShowSuccess(string msg) { SuccessLabel.Text = msg; SuccessBox.IsVisible = true; ErrorBox.IsVisible = false; }
    void ClearAlerts() { ErrorBox.IsVisible = false; SuccessBox.IsVisible = false; }

    /// <summary>
    /// Enables or disables the login and registration buttons and updates their text to indicate a loading state.
    /// </summary>
    /// <param name="loading">true to indicate that a loading operation is in progress and disable the buttons; false to re-enable the buttons
    /// and restore their default text.</param>
    void SetLoading(bool loading)
    {
        LoginButton.IsEnabled = !loading;
        RegisterButton.IsEnabled = !loading;
        LoginButton.Text = loading ? "Please wait…" : "Sign in";
        RegisterButton.Text = loading ? "Please wait…" : "Create account";
    }

    // ──────────────────────────────────────────────
    // Theme toggle
    // ──────────────────────────────────────────────
    private void OnLightThemeTapped(object? sender, TappedEventArgs e)
    {
        Application.Current!.UserAppTheme = AppTheme.Light;
        UpdateThemeHighlight();
    }

    private void OnDarkThemeTapped(object? sender, TappedEventArgs e)
    {
        Application.Current!.UserAppTheme = AppTheme.Dark;
        UpdateThemeHighlight();
    }

    /// <summary>
    /// Highlights the active theme card with the brand green background
    /// and resets the inactive one to a subtle tinted background.
    /// Mirrors the same logic used in MapPage.
    /// </summary>
    private void UpdateThemeHighlight()
    {
        var userTheme = Application.Current?.UserAppTheme;
        bool isLight = userTheme == AppTheme.Light
            || (userTheme != AppTheme.Dark && Application.Current?.RequestedTheme == AppTheme.Light);

        bool isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark
                       || Application.Current?.UserAppTheme == AppTheme.Dark;

        var activeColor = Microsoft.Maui.Graphics.Color.FromArgb("#3B6D11");
        var inactiveLightColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5");
        var inactiveDarkColor = Microsoft.Maui.Graphics.Color.FromArgb("#2C2C2C");
        var inactiveColor = isDarkMode ? inactiveDarkColor : inactiveLightColor;

        LightThemeCard.BackgroundColor = isLight ? activeColor : inactiveColor;
        DarkThemeCard.BackgroundColor = isLight ? inactiveColor : activeColor;
    }

    // ──────────────────────────────────────────────
    // Login
    // ──────────────────────────────────────────────
    /// <summary>
    /// Handles the login button click event by validating user input, attempting authentication, and navigating to the
    /// main page upon successful login.
    /// </summary>
    /// <remarks>Displays appropriate error messages if the login credentials are invalid or if an error
    /// occurs during the login process. On successful authentication, the user's credentials are saved and the
    /// application navigates to the main page.</remarks>
    /// <param name="sender">The source of the event, typically the login button.</param>
    /// <param name="e">An object that contains the event data.</param>
    async void OnLoginClicked(object sender, EventArgs e)
    {
        ClearAlerts();
        if (string.IsNullOrWhiteSpace(LoginEmail.Text) || string.IsNullOrWhiteSpace(LoginPassword.Text))
        {
            ShowError("Please enter your email and password."); return;
        }

        SetLoading(true);
        try
        {
            var user = await _api.LoginAsync(new LoginDto
            {
                Email = LoginEmail.Text.Trim(),
                Password = LoginPassword.Text
            });

            await _credentialService.SaveCredentialsAsync(LoginEmail.Text.Trim(), LoginPassword.Text);

            _userSession.CurrentUser = user;

            ShowSuccess($"Welcome back, {user.Nickname ?? user.Email}!");

            await Task.Delay(300);
            await Shell.Current.GoToAsync("//MapPage");
        }
        catch (ApiException ex) when (ex.StatusCode == 401)   // <-- added
        {
            ShowError("Incorrect email or password. Please try again.");
        }
        catch (Exception ex)
        {
            ShowError($"Login failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"LOGIN ERROR: {ex}");
        }
        finally { SetLoading(false); }
    }

    // ──────────────────────────────────────────────
    // Register
    // ──────────────────────────────────────────────
    /// <summary>
    /// Handles the registration process when the Register button is clicked, validating user input and attempting to
    /// create a new account.
    /// </summary>
    /// <remarks>Displays error messages for invalid input, such as missing fields, insufficient password
    /// length, or underage users. Shows a success message and switches to the login tab upon successful registration.
    /// Handles and displays errors from the API and other exceptions. This method is intended to be used as an event
    /// handler for a registration UI element.</remarks>
    /// <param name="sender">The source of the event, typically the Register button.</param>
    /// <param name="e">An EventArgs object that contains the event data.</param>
    async void OnRegisterClicked(object sender, EventArgs e)
    {
        if (RegPassword.Text.Length < 8)
        {
            ShowError("Password must be at least 8 characters."); return;
        }

        var dob = RegDob.Date ?? DateTime.Today;
        var age = DateTime.Today.Year - dob.Year;
        if (dob.Date > DateTime.Today.AddYears(-age)) age--; // adjust if birthday hasn't occurred yet this year
        if (age < 18)
        {
            ShowError("You must be at least 18 years old to create an account."); return;
        }

        ClearAlerts();
        if (string.IsNullOrWhiteSpace(RegNickname.Text) ||
            string.IsNullOrWhiteSpace(RegEmail.Text) ||
            string.IsNullOrWhiteSpace(RegPassword.Text))
        {
            ShowError("Please fill in all fields."); return;
        }
        if (RegPassword.Text.Length < 8)
        {
            ShowError("Password must be at least 8 characters."); return;
        }

        SetLoading(true);
        try
        {
            await _api.UserPOSTAsync(new User
            {
                Email = RegEmail.Text.Trim(),
                Password = RegPassword.Text,
                Nickname = RegNickname.Text.Trim(),
                Age = new DateTimeOffset(dob.Year, dob.Month, dob.Day, 0, 0, 0, TimeSpan.Zero).ToUniversalTime(),
                NumberOfWalks = 0,
                IsVerified = false
            });

            ShowSuccess("Account created! You can now sign in.");
            LoginEmail.Text = RegEmail.Text;
            SwitchTab(true);
        }
        catch (ApiException<ProblemDetails> ex) when (ex.StatusCode == 400)
        {
            ShowError(ex.Result?.Detail ?? "Invalid registration data.");
        }
        catch (ApiException<ProblemDetails> ex)
        {
            ShowError($"Server error {ex.StatusCode}: {ex.Result?.Title ?? ex.Response}");
        }
        catch (ApiException ex)
        {
            ShowError($"API error {ex.StatusCode}: {ex.Response}");
        }
        catch (Exception ex)
        {
            ShowError($"Client error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"REGISTRATION EXCEPTION: {ex}");
        }
        finally
        {
            SetLoading(false);
        }
    }
}