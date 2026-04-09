// Pages/MapPage.xaml.cs
using Mapsui;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.UI.Maui;
using Mapsui.Utilities;
using Mapsui.Layers;
using Mapsui.Extensions;
using Mapsui.Widgets;
using Mapsui.Widgets.ScaleBar;
using Mapsui.Widgets.Zoom;
using NiceCleanApp.Services; // For your ApiService
using CommunityToolkit.Maui.Views;
using Mapsui.UI;
using Color = Microsoft.Maui.Graphics.Color;
using Map = Mapsui.Map;

namespace NiceCleanApp.Pages;

public partial class MapPage : ContentPage
{
    private bool _isPlacingPin;
    private MPoint? _pendingPinLocation;

    public MapPage()
    {
        InitializeComponent();
        InitializeMap();
    }

    private void InitializeMap()
    {
        MainMap.Map = new Map
        {
            CRS = "EPSG:3857",
            // You can change the tile provider here if you prefer a different style
            Layers = { OpenStreetMap.CreateTileLayer() }
        };

        // Add zoom and scale bar widgets for better user experience
        MainMap.Map.Widgets.Add(new ZoomInOutWidget { MarginX = 20, MarginY = 40 });
        MainMap.Map.Widgets.Add(new ScaleBarWidget(MainMap.Map)
        {
            TextAlignment = Alignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top
        });

        // Set initial view to Nice, France
        var niceCoordinates = SphericalMercator.FromLonLat(7.2620, 43.7102);
        MainMap.Map.Navigator.CenterOnAndZoomTo(niceCoordinates, 12);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RequestLocationPermission();
    }

    private async Task RequestLocationPermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }
    }

    private void OnMapClicked(object? sender, MapClickedEventArgs e)
    {
        if (!_isPlacingPin) return;

        _pendingPinLocation = e.Point;
        _isPlacingPin = false;
        ShowPinConfirmationSheet();
    }

    private void OnPlacePinTabClicked(object? sender, EventArgs e)
    {
        _isPlacingPin = true;
        AppShell.DisplayToastAsync("Tap on the map to place a pollution pin");
    }

    private async void ShowPinConfirmationSheet()
    {
        var popup = new PinConfirmationPopup(_pendingPinLocation);
        var result = await this.ShowPopupAsync(popup);

        if (result is bool confirmed && confirmed)
        {
            await CreatePollutionPin();
        }
        else
        {
            _pendingPinLocation = null;
        }
    }

    private async Task CreatePollutionPin()
    {
        if (_pendingPinLocation == null) return;

        int currentUserId = 1; // TODO: Replace with actual user ID from auth

        var pinDto = new
        {
            UserId = currentUserId,
            Latitude = SphericalMercator.ToLonLat(_pendingPinLocation.X, _pendingPinLocation.Y).lat,
            Longitude = SphericalMercator.ToLonLat(_pendingPinLocation.X, _pendingPinLocation.Y).lon,
            Severity = 3,
            PollutionType = 0,
            Radius = 100.0,
            LocationName = "Reported via app"
        };

        try
        {
            var createdPin = await ApiService.PostPinAsync(pinDto);
            AddPinToMap(createdPin);
            AppShell.DisplayToastAsync("Pin reported successfully!");
        }
        catch (Exception ex)
        {
            AppShell.DisplaySnackbarAsync($"Error: {ex.Message}");
        }
        finally
        {
            _pendingPinLocation = null;
        }
    }

    private void AddPinToMap(NiceCleanApp.Models.Pin pinData)
    {
        var pinLayer = new GenericCollectionLayer<List<IFeature>>
        {
            Style = SymbolStyles.CreatePinStyle(Color.FromArgb("#FF3300"))
        };

        var point = SphericalMercator.FromLonLat(pinData.Longitude, pinData.Latitude);
        var feature = new GeometryFeature
        {
            Geometry = point,
            ["Label"] = $"Severity: {pinData.Severity}"
        };

        pinLayer?.Features.Add(feature);
        MainMap.Map.Layers.Add(pinLayer);
        MainMap.Map.Refresh();
    }

    private void OnMapTabClicked(object? sender, EventArgs e) { /* Already on map */ }
    private void OnEventsTabClicked(object? sender, EventArgs e) { /* TODO */ }
    private void OnMenuTabClicked(object? sender, EventArgs e) { Shell.Current.FlyoutIsPresented = true; }
}