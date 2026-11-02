using System.Text.Json;
using CampusRent.Server.DTOs.Places;

namespace CampusRent.Server.Services;

public interface IOsmPlacesClient
{
    Task<IReadOnlyList<PlaceSuggestionDto>> SuggestAsync(string query, CancellationToken cancellationToken);
    Task<string?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken);
}

public sealed class OsmPlacesClient(HttpClient client, ILogger<OsmPlacesClient> logger) : IOsmPlacesClient
{
    public async Task<IReadOnlyList<PlaceSuggestionDto>> SuggestAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) return [];
        var url = $"api/?q={Uri.EscapeDataString(query.Trim())}&limit=6";
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Photon geocode returned status {StatusCode}", response.StatusCode);
                return [];
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            var results = new List<PlaceSuggestionDto>();
            if (!document.RootElement.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
                return results;

            foreach (var feature in features.EnumerateArray())
            {
                if (!feature.TryGetProperty("properties", out var props) || !feature.TryGetProperty("geometry", out var geom))
                    continue;

                var name = props.GetStringOrDefault("name") ?? props.GetStringOrDefault("street") ?? "Địa điểm";
                var city = props.GetStringOrDefault("city") ?? props.GetStringOrDefault("state");
                var district = props.GetStringOrDefault("district") ?? props.GetStringOrDefault("county");
                var locality = props.GetStringOrDefault("locality");
                var country = props.GetStringOrDefault("country");

                var subParts = new[] { locality, district, city, country }
                    .Where(s => !string.IsNullOrWhiteSpace(s) && !s.Equals(name, StringComparison.OrdinalIgnoreCase))
                    .Distinct();
                var subtitle = string.Join(", ", subParts);
                if (string.IsNullOrWhiteSpace(subtitle)) subtitle = "Địa điểm tại Việt Nam";

                if (!geom.TryGetProperty("coordinates", out var coords) || coords.ValueKind != JsonValueKind.Array || coords.GetArrayLength() < 2)
                    continue;

                var lon = coords[0].GetDouble();
                var lat = coords[1].GetDouble();
                var osmId = props.GetPropertyOrDefault("osm_id").ToString();

                results.Add(new PlaceSuggestionDto($"osm:{osmId}", name, subtitle, lat, lon, "osm"));
            }
            return results;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Photon geocode request failed");
            return [];
        }
    }

    public async Task<string?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var url = $"reverse?lat={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            if (!document.RootElement.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
                return null;

            var first = features.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Object || !first.TryGetProperty("properties", out var props))
                return null;

            var name = props.GetStringOrDefault("name");
            var street = props.GetStringOrDefault("street");
            var locality = props.GetStringOrDefault("locality");
            var district = props.GetStringOrDefault("district") ?? props.GetStringOrDefault("county");
            var city = props.GetStringOrDefault("city") ?? props.GetStringOrDefault("state");

            var parts = new[] { name, street, locality, district, city }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct();

            var full = string.Join(", ", parts);
            return string.IsNullOrWhiteSpace(full) ? null : full;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Photon reverse geocode failed");
            return null;
        }
    }
}
