using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Places;
using CampusRent.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CampusRent.Server.Services;

public interface IPlaceSearchService
{
    Task<IReadOnlyList<PlaceSuggestionDto>> SuggestAsync(string query, CancellationToken cancellationToken);
    Task<PlaceLocationDto?> ResolveAsync(string id, CancellationToken cancellationToken);
}

public sealed class PlaceSearchService(
    CampusRentDbContext db,
    IGooglePlacesClient googlePlaces,
    IMemoryCache cache) : IPlaceSearchService
{
    public async Task<IReadOnlyList<PlaceSuggestionDto>> SuggestAsync(string query, CancellationToken cancellationToken)
    {
        var normalized = query.Trim();
        if (normalized.Length < 2) return [];
        var cacheKey = $"place-suggestions:{normalized.ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<PlaceSuggestionDto>? cached)) return cached!;

        var internalPlaces = await InternalSuggestionsAsync(normalized, cancellationToken);
        var googleSuggestions = await googlePlaces.SuggestAsync(normalized, cancellationToken);
        var places = internalPlaces.Concat(googleSuggestions)
            .GroupBy(place => $"{place.Title}|{place.Subtitle}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(6)
            .ToList();
        cache.Set(cacheKey, places, TimeSpan.FromMinutes(10));
        return places;
    }

    public Task<PlaceLocationDto?> ResolveAsync(string id, CancellationToken cancellationToken) =>
        id.StartsWith("google:", StringComparison.Ordinal)
            ? googlePlaces.ResolveAsync(id["google:".Length..], cancellationToken)
            : Task.FromResult<PlaceLocationDto?>(null);

    private async Task<List<PlaceSuggestionDto>> InternalSuggestionsAsync(string query, CancellationToken cancellationToken)
    {
        var pattern = $"%{query}%";
        var universities = await db.Universities.AsNoTracking()
            .Where(u => EF.Functions.Like(u.Name, pattern) || EF.Functions.Like(u.ShortName, pattern) || EF.Functions.Like(u.Address, pattern))
            .OrderBy(u => u.Name)
            .Take(3)
            .Select(u => new PlaceSuggestionDto($"university:{u.Id}", u.Name, u.Address, u.Latitude, u.Longitude, "university"))
            .ToListAsync(cancellationToken);
        var addresses = await db.Rooms.AsNoTracking()
            .Where(r => r.Status == RoomStatus.AVAILABLE && r.ExpiresAt > DateTime.UtcNow && EF.Functions.Like(r.Address, pattern))
            .OrderByDescending(r => r.CreatedAt)
            .Take(6)
            .Select(r => new { r.Id, r.Address, r.Latitude, r.Longitude })
            .ToListAsync(cancellationToken);
        return universities.Concat(addresses.Select(r => new PlaceSuggestionDto($"room:{r.Id}", r.Address, "Địa chỉ phòng trọ trên CampusRent", r.Latitude, r.Longitude, "listing"))).ToList();
    }
}
