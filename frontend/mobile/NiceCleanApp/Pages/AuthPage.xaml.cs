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
        UpdateTabIndicator();
    }

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

    void UpdateTabIndicator()
    {
        // Set indicator width and position based on active tab text
        var activeButton = _isLogin ? TabLogin : TabRegister;
        var text = activeButton.Text;
        // Approximate width: font size 14, average char width ~8px
        var width = text.Length * 8;
        TabIndicator.WidthRequest = width;
        // Position will be handled by HorizontalOptions="Start" and margin
        // We'll use TranslationX for precise placement
        var leftMargin = _isLogin ? 0 : TabLogin.Width + 24; // 24 is spacing
        TabIndicator.Margin = new Thickness(leftMargin, 0, 0, 0);
    }

    void OnTabLoginClicked(object sender, EventArgs e) => SwitchTab(true);
    void OnTabRegisterClicked(object sender, EventArgs e) => SwitchTab(false);

    void ShowError(string msg) { ErrorLabel.Text = msg; ErrorBox.IsVisible = true; SuccessBox.IsVisible = false; }
    void ShowSuccess(string msg) { SuccessLabel.Text = msg; SuccessBox.IsVisible = true; ErrorBox.IsVisible = false; }
    void ClearAlerts() { ErrorBox.IsVisible = false; SuccessBox.IsVisible = false; }

    void SetLoading(bool loading)
    {
        LoginButton.IsEnabled = !loading;
        RegisterButton.IsEnabled = !loading;
        LoginButton.Text = loading ? "Please wait…" : "Sign in";
        RegisterButton.Text = loading ? "Please wait…" : "Create account";
    }

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

            await Task.Delay(800);
            await Shell.Current.GoToAsync("//MapPage");
        }
        catch (ApiException<ProblemDetails> ex) when (ex.StatusCode == 401)
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