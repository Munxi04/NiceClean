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
    private readonly IUserSession _userSession;
    private readonly ICredentialService _credentialService;
    private readonly IPinProximityService _proximityService;
    private readonly IPinNotificationService _pinNotificationService;

    // Manage tabs
    private enum Tab { Map, PlacePin, Events, Menu }
    private Tab _selectedTab = Tab.Map;

    // Single shared layer — all pins live here
    private readonly GenericCollectionLayer<List<IFeature>> _pinLayer = new();

    private bool _isPlacingPin;
    private MPoint? _pendingPinLocation;

    // Drawer width must match WidthRequest in XAML
    private const double DrawerWidth = 280;

    private bool _isAccountMenuVisible = false;
    private bool _validationPopupOpen = false;

    private CancellationTokenSource? _pollCts;

    public MapPage(IClient apiClient, 
                   IUserSession userSession, 
                   ICredentialService credentialService,
                   IPinProximityService proximityService,
                   IPinNotificationService pinNotificationService)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _userSession = userSession;
        _credentialService = credentialService;
        _proximityService = proximityService;
        _pinNotificationService = pinNotificationService;

        // Subscribe to proximity events once — never re-subscribe.
        proximityService.PinEntered += OnProximityPinEntered;

        InitializeMap();
    }

    // ──────────────────────────────────────────────
    // Map setup
    // ──────────────────────────────────────────────
    /// <summary>
    /// Initializes the map control with default layers, widgets, and a centered view.
    /// </summary>
    /// <remarks>Sets up the map to use the Web Mercator projection (EPSG:3857), adds an OpenStreetMap tile
    /// layer and a pin layer, and configures zoom and scale bar widgets. The map view is centered and zoomed to Nice,
    /// France. Call this method during application startup or when resetting the map to its default state.</remarks>
    private void InitializeMap()
    {
        MainMap.Map = new Map
        {
            CRS = "EPSG:3857",
            Layers = { OpenStreetMap.CreateTileLayer(), _pinLayer }
        };

        MainMap.MyLocationEnabled = true;

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
        try
        {
            await RequestLocationPermissionAsync();
            await LoadExistingPinsAsync();
            await _pinNotificationService.RequestPermissionAsync();
            UpdateThemeHighlight();
            SetSelectedTab(Tab.Map);

            var location = await Geolocation.GetLastKnownLocationAsync();

            if (location != null)
            {
                MainMap.MyLocationLayer.UpdateMyLocation(new Position(location.Latitude, location.Longitude), true);

                // Focus the map on this location if needed
                // MainMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(location.Latitude, location.Longitude), 12);
            }

            _pollCts = new CancellationTokenSource();
            _ = PollPinsAsync(_pollCts.Token);
            _ = PollUserLocationAsync(_pollCts.Token);
        }
        catch (Exception ex)
        {
            // This will tell you exactly what's throwing
            System.Diagnostics.Debug.WriteLine($"OnAppearing error: {ex}");
            await AppShell.DisplaySnackbarAsync($"Startup error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _pollCts?.Cancel();

        // Keep monitoring alive in background via the platform service;
        // only pause the in-app popup logic. The background service / iOS
        // delegate will send a notification instead.
        // If you want to completely stop when backgrounded, call:
        //   _proximityService.StopMonitoring();
    }

    private async Task PollPinsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct).ContinueWith(_ => { }); // swallow cancellation
            if (!ct.IsCancellationRequested)
                await LoadExistingPinsAsync();
        }
    }

    private async Task PollUserLocationAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();
                if (location != null)
                {
                    MainMap.MyLocationLayer.UpdateMyLocation(new Position(location.Latitude, location.Longitude), true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error polling location: {ex}");
            }
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>
    /// Requests location permission from the user if it has not already been granted.
    /// </summary>
    /// <remarks>This method checks the current location permission status and prompts the user to grant
    /// permission if necessary. It should be called before attempting to access location services that require user
    /// consent.</remarks>
    /// <returns>A task that represents the asynchronous operation of requesting location permission.</returns>
    private static async Task RequestLocationPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
    }

    // ──────────────────────────────────────────────
    // Load existing pins from API
    // ──────────────────────────────────────────────
    /// <summary>
    /// Asynchronously loads all existing pins from the API and updates the map layer with the retrieved pins.
    /// </summary>
    /// <remarks>If an error occurs while loading pins, a notification is displayed to the user. The method
    /// clears the current pin layer before adding the loaded pins.</remarks>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    private async Task LoadExistingPinsAsync()
    {
        try
        {
            var pins = await _apiClient.PinAllAsync();
            _pinLayer.Features.Clear();

            foreach (var pin in pins)
                AddPinFeatureToLayer(pin);

            MainMap.Map.Refresh();

            // (Re)start proximity monitoring with the fresh pin list.
            var userId = _userSession.CurrentUser?.Id ?? 0;
            _proximityService.StartMonitoring(pins, userId);
        }
        catch (Exception ex)
        {
            await AppShell.DisplaySnackbarAsync($"Could not load pins: {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────
    // Proximity event → show validation popup or notification
    // ──────────────────────────────────────────────

    private void OnProximityPinEntered(object? sender, Pin pin)
    {
        // This runs on the main thread (guaranteed by PinProximityService).

        bool appIsForegrounded = Application.Current?.Windows
            .Any(w => w.IsActivated) ?? false;

        if (appIsForegrounded)
        {
            ShowValidationPopup(pin);
        }
        else
        {
            // App is backgrounded — send a local notification instead.
            _pinNotificationService.NotifyNearbyPin(pin);
        }
    }

    private async void ShowValidationPopup(Pin pin)
    {
        // Avoid stacking popups if the user moves between two close pins.
        if (_validationPopupOpen) return;

        _validationPopupOpen = true;

        var currentUserId = _userSession.CurrentUser?.Id ?? 0;
        if (currentUserId == 0)
        {
            await AppShell.DisplaySnackbarAsync("Error: User not logged in.");
            return;
        }
        try
        {
            var popup = new PinValidationPopup(pin, _apiClient, currentUserId);
            this.ShowPopup(popup);
            var chosenStatus = await popup.Result;

            if (chosenStatus.HasValue)
            {
                // Refresh the pin's visual on the map immediately.
                await LoadExistingPinsAsync();

                string msg = chosenStatus.Value == PinStatus.Cleaned
                    ? "Great job — the area is marked as cleaned! 🌿"
                    : "Thanks for confirming the pollution is still there.";
                await AppShell.DisplaySnackbarAsync(msg);
            }
        }
        finally
        {
            _validationPopupOpen = false;
        }
    }

    // ──────────────────────────────────────────────
    // Map click handler
    // ──────────────────────────────────────────────
    private async void OnMapClicked(object? sender, MapClickedEventArgs e)
    {
        var clickPoint = e.Point.ToMapsui();
        var userLocation = await Geolocation.GetLastKnownLocationAsync();
        var (lon, lat) = SphericalMercator.ToLonLat(clickPoint.X, clickPoint.Y);
        bool isUserNearby = userLocation != null 
            && await _apiClient.IsUserNearAsync(userLocation.Latitude, userLocation.Longitude, lat, lon);
        Pin? nearPin = null;
        try
        {
            nearPin = await _apiClient.AtLocationAsync(lat, lon);
        }
        catch (ApiException ex) when (ex.StatusCode == 204)
        {
            // No pin near the clicked location
        }

            // If we're in "placing pin" mode, the click sets the pending pin location and shows the confirmation popup
        if (_isPlacingPin && isUserNearby && nearPin == null)
        {
            _pendingPinLocation = clickPoint;
            _isPlacingPin = false;
            PlacingPinBanner.IsVisible = false;
            TooNearBanner.IsVisible = false;
            TooFarBanner.IsVisible = false;
            ShowPinReportPopup(sender, e);
            return;
        }
        else if (_isPlacingPin && !isUserNearby)
        {
            PlacingPinBanner.IsVisible = false;
            TooFarBanner.IsVisible = true;
            TooNearBanner.IsVisible = false;
            return;
        }
        else if (_isPlacingPin && nearPin != null)
        {
            PlacingPinBanner.IsVisible = false;
            TooFarBanner.IsVisible = false;
            TooNearBanner.IsVisible = true;
            return;
        }

        // Otherwise, check if the click hit an existing pin and show its info popup
        if (nearPin != null)
        {
            var userId = _userSession.CurrentUser?.Id ?? -1;

            // If the tapped pin belongs to another user and is actionable,
            // show the validation popup directly (user tapped intentionally).
            if (nearPin.UserId != userId
                && nearPin.Status != PinStatus.Cleaned
                && nearPin.Status != PinStatus.Deleted
                && isUserNearby)
            {
                ShowValidationPopup(nearPin);
            }
            else if (nearPin.UserId != userId && !isUserNearby)
            {
                var popup = new PinInfoPopup(nearPin, !isUserNearby);
                this.ShowPopup(popup);
            }
            else
            {
                var popup = new PinInfoPopup(nearPin, false);
                this.ShowPopup(popup);
            }
        }
    }

    // ──────────────────────────────────────────────
    // Place-pin flow
    // ──────────────────────────────────────────────

    /// <summary>
    /// Handles the click event for the Place Pin tab, toggling the pin placement mode.
    /// </summary>
    /// <remarks>If pin placement mode is not active, this method activates it and displays the relevant UI.
    /// If pin placement mode is already active, it switches back to the map tab.</remarks>
    /// <param name="sender">The source of the event, typically the UI element that was clicked.</param>
    /// <param name="e">An EventArgs object that contains the event data.</param>
    private void OnPlacePinTabClicked(object? sender, EventArgs e)
    {
        if (!_isPlacingPin)
        {
            SetSelectedTab(Tab.PlacePin);
            _isPlacingPin = true;
            PlacingPinBanner.IsVisible = true;
            TooFarBanner.IsVisible = false;
            TooNearBanner.IsVisible = false;
        }
        else
        {
            OnMapTabClicked(sender, e);
        }
            
    }

    /// <summary>
    /// Displays a popup dialog to confirm the placement of a pollution pin at the pending location when the map is
    /// clicked.
    /// </summary>
    /// <remarks>If the user confirms the pin placement, a new pollution pin is created and the map tab is
    /// refreshed. If the user cancels, the pin placement process is reset and the placement banner is shown. This
    /// method should be called in response to a map click event when a pin placement is pending.</remarks>
    /// <param name="sender">The source of the event, typically the map control that was clicked.</param>
    /// <param name="e">The event data containing information about the map click location.</param>
    private async void ShowPinReportPopup(object? sender, MapClickedEventArgs e)
    {
        if (_pendingPinLocation == null) return;

        var popup = new PinReportPopup(_pendingPinLocation);
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
    /// <summary>
    /// Creates a new pollution pin based on the specified confirmation result and submits it to the server
    /// asynchronously.
    /// </summary>
    /// <remarks>Displays a notification to the user indicating success or failure. If the user is not logged
    /// in or the pin location is not set, the operation is not performed.</remarks>
    /// <param name="result">The result of the pin report dialog containing pollution details to be reported.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task CreatePollutionPinAsync(PinReportPopup.PinReportResult result)
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

            // Refresh monitoring so the new pin is included but filtered correctly.
            var allPins = _pinLayer.Features
                .OfType<GeometryFeature>()
                .Select(f => f["Pin"] as Pin)
                .Where(p => p != null)
                .Cast<Pin>();
            _proximityService.StartMonitoring(allPins, currentUserId);

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
    /// <summary>
    /// Adds a pin as a feature to the map layer, with styling based on its status and info for popups.
    /// </summary>
    /// <param name="pin"></param>
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
        static void StyleButton(Button btn, bool isSelected)
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
        => await NavigateToMainMenuAsync();

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
        // Determine active theme considering both system and user preference
        var userTheme = Application.Current?.UserAppTheme;
        bool isLight = userTheme == AppTheme.Light
            || (userTheme != AppTheme.Dark && Application.Current?.RequestedTheme == AppTheme.Light);

        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark
                     || Application.Current?.UserAppTheme == AppTheme.Dark;

        // Colors (same as in XAML)
        var active = Microsoft.Maui.Graphics.Color.FromArgb("#3B6D11");
        var inactiveLight = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5");
        var inactiveDark = Microsoft.Maui.Graphics.Color.FromArgb("#2C2C2C");
        var inactive = isDark ? inactiveDark : inactiveLight;

        LightThemeCard.BackgroundColor = isLight ? active : inactive;
        DarkThemeCard.BackgroundColor = isLight ? inactive : active;
    }

    // ──────────────────────────────────────────────
    // Sign out flow
    // ──────────────────────────────────────────────
    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        // Clear session and stored credentials
        _proximityService.StopMonitoring();
        _userSession.Clear();
        _credentialService.ClearCredentials();

        // Close drawer
        await CloseMenuAsync();

        // Navigate back to login
        await Shell.Current.GoToAsync("//AuthPage");
    }
}
