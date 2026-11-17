using System.Security.Claims;
using CampusRent.Server.DTOs.Auth;
using CampusRent.Server.DTOs.LandlordRequests;
using CampusRent.Server.DTOs.Rooms;
using CampusRent.Server.Models;
namespace CampusRent.Server.Services;
public static class Mapping
{
    public static Guid UserId(this ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static UserProfileDto Profile(User user) => new() { Id = user.Id, Email = user.Email, FullName = user.FullName, PhoneNumber = user.PhoneNumber, Avatar = user.Avatar, Role = user.Role, IsEduVerified = user.IsEduVerified, IsEmailVerified = user.IsEmailVerified, CreatedAt = user.CreatedAt };
    public static LandlordRequestResponseDto LandlordRequest(LandlordRequest r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        UserName = r.User?.FullName ?? string.Empty,
        UserEmail = r.User?.Email ?? string.Empty,
        UserPhoneNumber = r.User?.PhoneNumber,
        Title = r.Title,
        Reason = r.Reason,
        ImageUrls = r.ImageUrls,
        PdfUrl = r.PdfUrl,
        PdfFileName = r.PdfFileName,
        Status = r.Status,
        AdminNote = r.AdminNote,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
    private static readonly IReviewRankingService DefaultRankingService = new ReviewRankingService();

    public static RoomListItemDto Card(Room r, double? distance = null, IReviewRankingService? rankingService = null, IEnumerable<Review>? landlordReviews = null)
    {
        var svc = rankingService ?? DefaultRankingService;
        var reviews = (r.Reviews ?? Enumerable.Empty<Review>()).Where(x => !x.IsFlagged).ToList();
        var demotion = svc.EvaluateRoomDemotion(r, landlordReviews);
        var stats = ReviewWeighting.Calculate(reviews);
        return new()
        {
            Id = r.Id,
            Title = r.Title,
            Address = r.Address,
            Latitude = r.Latitude,
            Longitude = r.Longitude,
            BasePrice = r.BasePrice,
            ElectricPrice = r.ElectricPrice,
            WaterPrice = r.WaterPrice,
            InternetPrice = r.InternetPrice,
            OtherFee = r.OtherFee,
            Area = r.Area,
            MaxPeople = r.MaxPeople,
            Amenities = r.Amenities,
            CoverImage = r.Images.OrderByDescending(i => i.IsCover).ThenBy(i => i.DisplayOrder).FirstOrDefault()?.Url,
            Status = r.Status,
            ExpiresAt = r.ExpiresAt,
            CreatedAt = r.CreatedAt,
            DistanceKm = distance,
            AverageRating = svc.CalculateWeightedRating(reviews),
            ReviewCount = reviews.Count,
            IsDemoted = demotion.IsDemoted,
            DemotionReason = demotion.Reason,
            RecommendationScore = stats.RecommendationScore,
            IsLowRated = demotion.IsDemoted || stats.IsLowRated
        };
    }
}
