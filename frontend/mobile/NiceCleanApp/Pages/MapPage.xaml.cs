using CommunityToolkit.Maui.Extensions;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Mapsui.Widgets;
using Mapsui.Widgets.ButtonWidgets;
using Mapsui.Widgets.ScaleBar;
using NiceCleanApp.Pages.Controls;
using NiceCleanApp.Services;
using HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment;
using Map = Mapsui.Map;
using VerticalAlignment = Mapsui.Widgets.VerticalAlignment;
using Pin = NiceCleanApp.Services.Pin;
using Mapsui.Nts;

namespace NiceCleanApp.Pages;

public partial class MapPage : ContentPage
{
    private readonly IClient _apiClient;

    // Manage tabs
    private enum Tab { Map, PlacePin, Events, Menu }
    private Tab _selectedTab = Tab.Map;

    // Single shared layer — all pins live here
    private readonly GenericCollectionLayer<List<IFeature>> _pinLayer = new();

    private bool _isPlacingPin;
    private MPoint? _pendingPinLocation;

    // Drawer width must match WidthRequest in XAML
    private const double DrawerWidth = 280;

    // For API calls that require user info (e.g. creating a pin), we can access the current user session
    private readonly IUserSession _userSession;

    // For sign-out flow, we need to clear stored credentials
    private readonly ICredentialService _credentialService;

    private bool _isAccountMenuVisible = false;

    public MapPage(IClient apiClient, IUserSession userSession, ICredentialService credentialService)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _userSession = userSession;
        _credentialService = credentialService;
        InitializeMap();
    }

    // ──────────────────────────────────────────────
    // Map setup
    // ──────────────────────────────────────────────

    private void InitializeMap()
    {
        MainMap.Map = new Map
        {
            CRS = "EPSG:3857",
            Layers = { OpenStreetMap.CreateTileLayer(), _pinLayer }
        };

        MainMap.Map.Widgets.Add(new ZoomInOutWidget { Margin = new MRect(0, 0, 20, 40) });
        MainMap.Map.Widgets.Add(new ScaleBarWidget(MainMap.Map)
        {
            TextAlignment = Alignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top
        });

        // Centre on Nice, France
        var nice = SphericalMercator.FromLonLat(7.2620, 43.7102);
        MainMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(nice), 12);
    }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RequestLocationPermissionAsync();
        await LoadExistingPinsAsync();
        UpdateThemeHighlight();
        SetSelectedTab(Tab.Map);
    }

    private static async Task RequestLocationPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
    }

    // ──────────────────────────────────────────────
    // Load existing pins from API
    // ──────────────────────────────────────────────
    private async Task LoadExistingPinsAsync()
    {
        try
        {
            var pins = await _apiClient.PinAllAsync();
            _pinLayer.Features.Clear();

            foreach (var pin in pins)
                AddPinFeatureToLayer(pin);

            MainMap.Map.Refresh();
        }
        catch (Exception ex)
        {
            await AppShell.DisplaySnackbarAsync($"Could not load pins: {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────
    // Hit-testing: find a pin near the clicked point (within 20 screen pixels)
    // ──────────────────────────────────────────────

    private Pin? FindPinAtPoint(MPoint worldPoint)
    {
        // Convert 20 screen pixels → world units using current zoom level
        double tolerance = MainMap.Map.Navigator.Viewport.Resolution * 20;

        foreach (var feature in _pinLayer.Features)
        {
            if (feature is GeometryFeature gf
                && gf.Geometry is NetTopologySuite.Geometries.Point pt
                && feature["Pin"] is Pin pin)
            {
                double dx = pt.X - worldPoint.X;
                double dy = pt.Y - worldPoint.Y;
                if (Math.Sqrt(dx * dx + dy * dy) < tolerance)
                    return pin;
            }
        }

        return null;
    }

    // ──────────────────────────────────────────────
    // Place-pin flow
    // ──────────────────────────────────────────────

    private void OnPlacePinTabClicked(object? sender, EventArgs e)
    {
        if (_isPlacingPin == false)
        {
            SetSelectedTab(Tab.PlacePin);
            _isPlacingPin = true;
            PlacingPinBanner.IsVisible = true;
        }
        else
        {
            OnMapTabClicked(sender, e);
        }
            
    }

    private void OnMapClicked(object? sender, MapClickedEventArgs e)
    {
        var clickPoint = e.Point.ToMapsui();

        // If we're in "placing pin" mode, the click sets the pending pin location and shows the confirmation popup
        if (_isPlacingPin)
        {
            _pendingPinLocation = clickPoint;
            _isPlacingPin = false;
            PlacingPinBanner.IsVisible = false;
            ShowPinConfirmationPopup(sender, e);
            return;
        }

        // Otherwise, check if the click hit an existing pin and show its info popup
        var hit = FindPinAtPoint(clickPoint);
        if (hit != null)
        {
            var popup = new PinInfoPopup(hit);
            this.ShowPopup(popup);
        }
    }

    private async void ShowPinConfirmationPopup(object? sender, MapClickedEventArgs e)
    {
        if (_pendingPinLocation == null) return;

        var popup = new PinConfirmationPopup(_pendingPinLocation);
        this.ShowPopup(popup);
        var result = await popup.Result;

        if (result != null)
        {
            await CreatePollutionPinAsync(result);
            OnMapTabClicked(sender, e);
        }
        else
        {
            _pendingPinLocation = null;
            _isPlacingPin = true;
            PlacingPinBanner.IsVisible = true;
        }
    }

    // ──────────────────────────────────────────────
    // Create pin via API
    // ──────────────────────────────────────────────

    private async Task CreatePollutionPinAsync(PinConfirmationPopup.PinConfirmationResult result)
    {
        if (_pendingPinLocation == null) return;

        var currentUserId = _userSession.CurrentUser?.Id ?? 0;
        if (currentUserId == 0)
        {
            await AppShell.DisplaySnackbarAsync("Error: User not logged in.");
            return;
        }

            var (lon, lat) = SphericalMercator.ToLonLat(_pendingPinLocation.X, _pendingPinLocation.Y);

        var dto = new PinCreateDto
        {
            UserId = currentUserId,
            Severity = result.Severity,
            Radius = 100.0,
            PollutionType = result.Type,
            Latitude = lat,
            Longitude = lon,
            LocationName = result.LocationName
        };

        try
        {
            var createdPin = await _apiClient.PinPOSTAsync(dto);
            AddPinFeatureToLayer(createdPin);
            MainMap.Map.Refresh();
            await AppShell.DisplaySnackbarAsync("Pin reported successfully!");
        }
        catch (Exception ex)
        {
            await AppShell.DisplaySnackbarAsync($"Error: {ex.Message}");
        }
        finally
        {
            _pendingPinLocation = null;
        }
    }

    // ──────────────────────────────────────────────
    // Map rendering helpers
    // ──────────────────────────────────────────────

    private void AddPinFeatureToLayer(Pin pin)
    {
        var point = SphericalMercator.FromLonLat(pin.Longitude, pin.Latitude);
        var feature = new GeometryFeature
        {
            Geometry = new NetTopologySuite.Geometries.Point(point),
            ["Label"] = $"{pin.PollutionType} – {pin.Severity}",
            ["Pin"] = pin
        };

        feature.Styles.Add(new SymbolStyle
        {
            Fill = new Mapsui.Styles.Brush(GetPinMapsuiColor(pin.Status)),
            Line = new Pen(Mapsui.Styles.Color.White, 2),
            SymbolScale = 0.8
        });

        _pinLayer.Features.Add(feature);
    }

    /// <summary>
    /// Color by pin status:
    ///   Unverified → orange
    ///   Verified   → red
    ///   Cleaned    → green
    ///   Deleted    → grey
    /// </summary>
    private static Mapsui.Styles.Color GetPinMapsuiColor(PinStatus status) => status switch
    {
        PinStatus.Verified => new Mapsui.Styles.Color(255, 51, 0),    // #FF3300
        PinStatus.Cleaned  => new Mapsui.Styles.Color(0, 170, 68),    // #00AA44
        PinStatus.Deleted  => new Mapsui.Styles.Color(136, 136, 136), // #888888
        _                  => new Mapsui.Styles.Color(255, 153, 0)    // #FF9900 Unverified
    };

    // ──────────────────────────────────────────────
    // Bottom tab bar handlers
    // ──────────────────────────────────────────────
    private void OnMapTabClicked(object? sender, EventArgs e)
    {
        _isPlacingPin = false;
        PlacingPinBanner.IsVisible = false;
        SetSelectedTab(Tab.Map);
    }
    private void OnEventsTabClicked(object? sender, EventArgs e)
    {
        _isPlacingPin = false;
        PlacingPinBanner.IsVisible = false;
        SetSelectedTab(Tab.Events);
        /* TODO: navigate to cleanwalks */
    }
    private async void OnMenuTabClicked(object? sender, EventArgs e)
    {
        _isPlacingPin = false;
        PlacingPinBanner.IsVisible = false;
        SetSelectedTab(Tab.Menu);
        UpdateThemeHighlight();

        // Position the panel fully off-screen to the right before making it visible
        MenuPanel.TranslationX = DrawerWidth;
        MenuOverlay.IsVisible = true;

        // Slide in from right → left (TranslationX: DrawerWidth → 0)
        await MenuPanel.TranslateToAsync(0, 0, 260, Easing.CubicOut);
    }
    private void SetSelectedTab(Tab selected)
    {
        _selectedTab = selected;

        // Helper to style a button
        void StyleButton(Button btn, bool isSelected)
        {
            if (isSelected)
            {
                btn.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#3B6D11");
                btn.TextColor = Colors.White;
            }
            else
            {
                btn.BackgroundColor = Colors.Transparent;
                // Restore theme-aware text color
                btn.SetAppThemeColor(Button.TextColorProperty,
                    light: Microsoft.Maui.Graphics.Color.FromArgb("#333333"),
                    dark: Microsoft.Maui.Graphics.Color.FromArgb("#EEEEEE"));
            }
        }

        // Find buttons by name (give them x:Name in XAML first!)
        StyleButton(MapTabButton, _selectedTab == Tab.Map);
        StyleButton(PlacePinTabButton, _selectedTab == Tab.PlacePin);
        StyleButton(EventsTabButton, _selectedTab == Tab.Events);
        StyleButton(MenuTabButton, _selectedTab == Tab.Menu);
    }

    // ──────────────────────────────────────────────
    // Right-side drawer
    // ──────────────────────────────────────────────
    private async void OnMenuBackdropTapped(object? sender, TappedEventArgs e)
    {
        await CloseMenuAsync();
        OnMapTabClicked(sender, e);
    }
    private async void OnMenuCloseClicked(object? sender, EventArgs e)
    {
        await CloseMenuAsync();
        OnMapTabClicked(sender, e);
    }

    private async Task CloseMenuAsync()
    {
        // Slide out right (TranslationX: 0 → DrawerWidth)
        await MenuPanel.TranslateToAsync(DrawerWidth, 0, 200, Easing.CubicIn);
        MenuOverlay.IsVisible = false;

        // Reset drawer state for next open
        if (_isAccountMenuVisible)
        {
            MainMenuContent.TranslationX = 0;
            AccountView.TranslationX = 280;
            MenuHeaderLabel.Text = "Menu";
            MenuHeaderLabel.Margin = new Thickness(12, 0, 0, 0);
            MenuBackButton.IsVisible = false;
            _isAccountMenuVisible = false;
            MenuCloseButton.IsVisible = true;
        }
    }

    private async void OnAccountMenuItemClicked(object? sender, EventArgs e)
    {
        PopulateMenuUserInfo();
        await NavigateToAccountViewAsync();
    }

    private async Task NavigateToAccountViewAsync()
    {
        _isAccountMenuVisible = true;
        MenuHeaderLabel.Text = "Account";
        MenuHeaderLabel.Margin = new Thickness(0);
        MenuBackButton.IsVisible = true;
        MenuCloseButton.IsVisible = false;

        // Slide main menu out to the left, account view in from the right
        var slideOut = MainMenuContent.TranslateToAsync(-280, 0, 220, Easing.CubicInOut);
        var slideIn = AccountView.TranslateToAsync(0, 0, 220, Easing.CubicInOut);
        await Task.WhenAll(slideOut, slideIn);
    }

    private async void OnDeleteAccountClicked(object? sender, EventArgs e)
    {
        bool confirmed = await DisplayAlertAsync(
            "⚠️ Delete Account",
            "This will permanently delete your account and all your data. This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        var userId = _userSession.CurrentUser?.Id;
        if (userId == null) return;

        try
        {
            await _apiClient.UserDELETEAsync(userId.Value);
            _userSession.Clear();
            _credentialService.ClearCredentials();

            await CloseMenuAsync();
            await Shell.Current.GoToAsync("//AuthPage");
        }
        catch (Exception ex)
        {
            await AppShell.DisplaySnackbarAsync($"Could not delete account: {ex.Message}");
        }
    }

    private async void OnMenuBackClicked(object? sender, EventArgs e)
    {
        await NavigateToMainMenuAsync();
    }

    private async Task NavigateToMainMenuAsync()
    {
        _isAccountMenuVisible = false;
        MenuHeaderLabel.Text = "Menu";
        MenuHeaderLabel.Margin = new Thickness(12, 0, 0, 0);
        MenuBackButton.IsVisible = false;
        MenuCloseButton.IsVisible = true;
        

        var slideIn = MainMenuContent.TranslateToAsync(0, 0, 220, Easing.CubicInOut);
        var slideOut = AccountView.TranslateToAsync(280, 0, 220, Easing.CubicInOut);
        await Task.WhenAll(slideIn, slideOut);
    }

    private void PopulateMenuUserInfo()
    {
        var user = _userSession.CurrentUser;
        if (user == null) return;

        MenuNicknameLabel.Text = user.Nickname ?? "No nickname";
        MenuEmailLabel.Text = user.Email ?? "";
        MenuWalksLabel.Text = $"{user.NumberOfWalks} walk{(user.NumberOfWalks == 1 ? "" : "s")}";
    }

    // ──────────────────────────────────────────────
    // Theme toggle (inside drawer)
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
    /// Highlights the active theme card (orange bg + white text)
    /// and resets the inactive one (subtle bg + themed text).
    /// </summary>
    private void UpdateThemeHighlight()
    {
        // Determine current effective theme
        var userTheme = Application.Current?.UserAppTheme;
        bool isLight = userTheme == AppTheme.Light
            || (userTheme != AppTheme.Dark && Application.Current?.RequestedTheme == AppTheme.Light);

        // Active card: brand green background, white text
        // Inactive card: subtle tinted background, default text
        var activeColor   = Microsoft.Maui.Graphics.Color.FromArgb("#3B6D11");
        var inactiveLightColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5");
        var inactiveDarkColor  = Microsoft.Maui.Graphics.Color.FromArgb("#2C2C2C");
        bool isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark
                       || Application.Current?.UserAppTheme == AppTheme.Dark;

        var inactiveColor = isDarkMode ? inactiveDarkColor : inactiveLightColor;
        var inactiveText  = isDarkMode ? Colors.White : Microsoft.Maui.Graphics.Color.FromArgb("#333333");
        var activeText    = Colors.White;

        LightThemeCard.BackgroundColor  = isLight ? activeColor   : inactiveColor;

        DarkThemeCard.BackgroundColor   = isLight ? inactiveColor : activeColor;
    }

    // ──────────────────────────────────────────────
    // Sign out flow
    // ──────────────────────────────────────────────
    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        // Clear session and stored credentials
        _userSession.Clear();
        _credentialService.ClearCredentials();

        // Close drawer
        await CloseMenuAsync();

        // Navigate back to login
        await Shell.Current.GoToAsync("//AuthPage");
    }
}
