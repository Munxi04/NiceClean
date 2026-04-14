using CommunityToolkit.Maui.Core;
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

    // Single shared layer — all pins live here
    private readonly GenericCollectionLayer<List<IFeature>> _pinLayer = new();

    private bool _isPlacingPin;
    private MPoint? _pendingPinLocation;

    // TODO: Replace with real user from auth session
    private const int CurrentUserId = 1;

    public MapPage(IClient apiClient)
    {
        InitializeComponent();
        _apiClient = apiClient;
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

        MainMap.Map.Widgets.Add(new ZoomInOutWidget { Margin = new MRect(20, 40, 0, 0) });
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
    // Place-pin flow
    // ──────────────────────────────────────────────

    private void OnPlacePinTabClicked(object? sender, EventArgs e)
    {
        _isPlacingPin = true;
        PlacingPinBanner.IsVisible = true;
    }

    private void OnMapClicked(object? sender, MapClickedEventArgs e)
    {
        if (!_isPlacingPin) return;
        _pendingPinLocation = e.Point.ToMapsui();
        _isPlacingPin = false;
        PlacingPinBanner.IsVisible = false;

        ShowPinConfirmationPopup();
    }

    private async void ShowPinConfirmationPopup()
    {
        if (_pendingPinLocation == null) return;

        var popup = new PinConfirmationPopup(_pendingPinLocation);
        this.ShowPopup(popup);
        var result = await popup.Result;

        if (result != null)
            await CreatePollutionPinAsync(result);
        else
            _pendingPinLocation = null;
    }

    // ──────────────────────────────────────────────
    // Create pin via API
    // ──────────────────────────────────────────────

    private async Task CreatePollutionPinAsync(PinConfirmationPopup.PinConfirmationResult result)
    {
        if (_pendingPinLocation == null) return;

        var (lon, lat) = SphericalMercator.ToLonLat(_pendingPinLocation.X, _pendingPinLocation.Y);

        var dto = new PinCreateDto // TODO: Map PinConfirmationResult to PinCreateDTO from API layer
        {
            UserId = CurrentUserId,
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
            ["Label"] = $"{pin.PollutionType} – {pin.Severity}"
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
        PinStatus.Verified => new Mapsui.Styles.Color(255, 51, 0),   // #FF3300
        PinStatus.Cleaned => new Mapsui.Styles.Color(0, 170, 68),  // #00AA44
        PinStatus.Deleted => new Mapsui.Styles.Color(136, 136, 136), // #888888
        _ => new Mapsui.Styles.Color(255, 153, 0)    // #FF9900 Unverified
    };

    // ──────────────────────────────────────────────
    // Bottom tab bar handlers
    // ──────────────────────────────────────────────

    private void OnMapTabClicked(object? sender, EventArgs e) { /* already on map */ }
    private void OnEventsTabClicked(object? sender, EventArgs e) { /* TODO: navigate to cleanwalks */ }
    private void OnMenuTabClicked(object? sender, EventArgs e) => Shell.Current.FlyoutIsPresented = true;
}
