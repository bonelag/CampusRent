namespace CampusRent.Server.DTOs.Places;

public record PlaceSuggestionDto(
    string Id,
    string Title,
    string Subtitle,
    double? Latitude,
    double? Longitude,
    string Source);

public record PlaceLocationDto(string Title, string Subtitle, double Latitude, double Longitude);
