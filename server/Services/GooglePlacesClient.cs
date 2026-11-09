using System.Net.Http.Json;
using System.Text.Json;
using CampusRent.Server.DTOs.Places;

namespace CampusRent.Server.Services;

public interface IGooglePlacesClient
{
    Task<IReadOnlyList<PlaceSuggestionDto>> SuggestAsync(string query, CancellationToken cancellationToken);
    Task<PlaceLocationDto?> ResolveAsync(string placeId, CancellationToken cancellationToken);
}

public sealed class GooglePlacesClient(HttpClient client, IConfiguration configuration, ILogger<GooglePlacesClient> logger) : IGooglePlacesClient
{
    private string? ApiKey => configuration["GooglePlaces:ApiKey"];

    public async Task<IReadOnlyList<PlaceSuggestionDto>> SuggestAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ApiKey)) return [];
        using var request = new HttpRequestMessage(HttpMethod.Post, "places:autocomplete")
        {
            Content = JsonContent.Create(new { input = query, includedRegionCodes = new[] { "vn" }, languageCode = "vi" })
        };
        request.Headers.Add("X-Goog-Api-Key", ApiKey);
        request.Headers.Add("X-Goog-FieldMask", "suggestions.placePrediction.place,suggestions.placePrediction.text.text,suggestions.placePrediction.structuredFormat.mainText.text,suggestions.placePrediction.structuredFormat.secondaryText.text");
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Google Places autocomplete returned {StatusCode}", response.StatusCode);
                return [];
            }
            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            var results = new List<PlaceSuggestionDto>();
            var suggestions = document.RootElement.GetPropertyOrDefault("suggestions");
            if (suggestions.ValueKind != JsonValueKind.Array) return results;
            foreach (var item in suggestions.EnumerateArray())
            {
                if (!item.TryGetProperty("placePrediction", out var prediction)) continue;
                var place = prediction.GetStringOrDefault("place");
                if (string.IsNullOrWhiteSpace(place)) continue;
                var text = prediction.GetPropertyOrDefault("text").GetStringOrDefault("text") ?? "Địa điểm";
                var format = prediction.GetPropertyOrDefault("structuredFormat");
                var title = format.GetPropertyOrDefault("mainText").GetStringOrDefault("text") ?? text;
                var subtitle = format.GetPropertyOrDefault("secondaryText").GetStringOrDefault("text") ?? text;
                results.Add(new PlaceSuggestionDto($"google:{place}", title, subtitle, null, null, "google"));
            }
            return results;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Google Places autocomplete timed out");
            return [];
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogWarning(ex, "Google Places autocomplete request failed");
            return [];
        }
    }

    public async Task<PlaceLocationDto?> ResolveAsync(string placeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ApiKey) || !placeId.StartsWith("places/", StringComparison.Ordinal)) return null;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{placeId}?languageCode=vi");
        request.Headers.Add("X-Goog-Api-Key", ApiKey);
        request.Headers.Add("X-Goog-FieldMask", "displayName,formattedAddress,location");
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            var location = document.RootElement.GetPropertyOrDefault("location");
            if (!location.TryGetProperty("latitude", out var latitude) || !location.TryGetProperty("longitude", out var longitude)) return null;
            return new PlaceLocationDto(
                document.RootElement.GetPropertyOrDefault("displayName").GetStringOrDefault("text") ?? "Địa điểm",
                document.RootElement.GetStringOrDefault("formattedAddress") ?? "",
                latitude.GetDouble(),
                longitude.GetDouble());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Google Places resolution timed out");
            return null;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogWarning(ex, "Google Places detail request failed");
            return null;
        }
    }
}

internal static class JsonElementExtensions
{
    public static JsonElement GetPropertyOrDefault(this JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var property) ? property : default;

    public static string? GetStringOrDefault(this JsonElement element, string name) =>
        element.GetPropertyOrDefault(name).ValueKind == JsonValueKind.String ? element.GetPropertyOrDefault(name).GetString() : null;
}
