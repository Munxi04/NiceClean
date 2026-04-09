using System.Text;
using System.Text.Json;
using NiceCleanApp.Models;

namespace NiceCleanApp.Services;

public static class ApiService
{
    private static readonly HttpClient _httpClient = new()
    {
        //BaseAddress = new Uri("http://localhost:5249")

        // For Android emulator, use this
        BaseAddress = new Uri("http://10.0.2.2:5249") 
    };

    public static async Task<Pin> PostPinAsync(object pinDto)
    {
        var json = JsonSerializer.Serialize(pinDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/Pin", content);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Pin>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}