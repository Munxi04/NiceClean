using CommunityToolkit.Maui.Views;
using Mapsui;
using Mapsui.Projections;
using NiceCleanApp.Services;

namespace NiceCleanApp.Pages.Controls;

public partial class PinConfirmationPopup : Popup
{
    private readonly double _latitude;
    private readonly double _longitude;
    private string _address;
    private readonly TaskCompletionSource<PinConfirmationResult> _tcs = new();
    public Task<PinConfirmationResult> Result => _tcs.Task;
    public PinConfirmationPopup(MPoint mapsuiPoint)
    {
        InitializeComponent();

        // Show approximate coordinates in the subtitle
        var (lon, lat) = SphericalMercator.ToLonLat(mapsuiPoint.X, mapsuiPoint.Y);

        _latitude = lat;
        _longitude = lon;

        LocationLabel.Text = $"Lat: {lat:F5}, Lon: {lon:F5}";
        _address = LocationLabel.Text;

        // Populate severity picker
        SeverityPicker.ItemsSource = new List<string>
        {
            "Low",
            "Moderate",
            "High",
            "Very High",
            "Extreme"
        };
        SeverityPicker.SelectedItem = PollutionSeverity.High; // default High

        // Populate type picker
        TypePicker.ItemsSource = new List<string>
        {
            "Plastic",
            "Glass",
            "Furniture"
        };
        TypePicker.SelectedItem = PollutionType.Plastic; // default: Plastic

        _ = FetchAddressAsync(); // Start fetching the address in the background
    }

    private void OnConfirmClicked(object? sender, EventArgs e)
    {
        // Extract severity number from the selected string (e.g., "3 – High" → 3)
        PollutionSeverity severity = (PollutionSeverity)(SeverityPicker.SelectedIndex + 1);
        // Get the selected type string
        PollutionType type = (PollutionType)TypePicker.SelectedIndex;

        var result = new PinConfirmationResult
        {
            Latitude = _latitude,
            Longitude = _longitude,
            Severity = severity,
            Type = type,
            LocationName = _address
        };

        // Close the popup and return the result to the caller
        _tcs.SetResult(result);
        _ = CloseAsync();
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        _tcs.SetResult(null);
        _ = CloseAsync(); // Close without returning a result
    }

    private async Task FetchAddressAsync()
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(_latitude, _longitude);
            var placemark = placemarks?.FirstOrDefault();
            if (placemark != null)
            {
                // Build a readable address string
                var parts = new[]
                {
                    placemark.Thoroughfare,        // Street name
                    placemark.SubThoroughfare,     // Street number
                    placemark.Locality,            // City
                    placemark.AdminArea,           // State/Province
                    placemark.CountryName
                }.Where(p => !string.IsNullOrWhiteSpace(p));

                _address = string.Join(", ", parts);
            }
        }
        catch (Exception ex)
        {
            // Handle geocoding failure (e.g., no network, service down)
            System.Diagnostics.Debug.WriteLine($"Geocoding error: {ex.Message}");
        }

        // Update the label on the UI thread
        if (!string.IsNullOrEmpty(_address))
        {
            LocationLabel.Text = _address;
        }
        // If geocoding failed, the label remains with coordinates
    }

    // Nested result class
    public class PinConfirmationResult
    {
        public required PollutionSeverity Severity { get; set; }
        public required PollutionType Type { get; set; }
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
        public required string LocationName { get; set; }
    }
}
