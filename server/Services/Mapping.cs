using System.Security.Claims;
using CampusRent.Server.DTOs.Auth;
using CampusRent.Server.DTOs.LandlordRequests;
using CampusRent.Server.DTOs.Rooms;
using CampusRent.Server.Models;

namespace CampusRent.Server.Services;

public static class Mapping
{
    private static readonly IReviewRankingService DefaultRankingService = new ReviewRankingService();

    public static Guid UserId(this ClaimsPrincipal principal)
    {
        return Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    public static UserProfileDto Profile(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Avatar = user.Avatar,
            Role = user.Role,
            IsEduVerified = user.IsEduVerified,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt
        };
    }

    public static LandlordRequestResponseDto LandlordRequest(LandlordRequest request)
    {
        return new LandlordRequestResponseDto
        {
            Id = request.Id,
            UserId = request.UserId,
            UserName = request.User?.FullName ?? string.Empty,
            UserEmail = request.User?.Email ?? string.Empty,
            UserPhoneNumber = request.User?.PhoneNumber,
            Title = request.Title,
            Reason = request.Reason,
            ImageUrls = request.ImageUrls,
            PdfUrl = request.PdfUrl,
            PdfFileName = request.PdfFileName,
            Status = request.Status,
            AdminNote = request.AdminNote,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }

    public static RoomListItemDto Card(
        Room room,
        double? distance = null,
        IReviewRankingService? rankingService = null,
        IEnumerable<Review>? landlordReviews = null)
    {
        var ranking = rankingService ?? DefaultRankingService;
        var activeReviews = (room.Reviews ?? Enumerable.Empty<Review>())
            .Where(r => !r.IsFlagged)
            .ToList();
        var demotion = ranking.EvaluateRoomDemotion(room, landlordReviews);
        var averageRating = ranking.CalculateWeightedRating(activeReviews);
        var coverImage = room.Images
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.DisplayOrder)
            .FirstOrDefault()?.Url;

        return new RoomListItemDto
        {
            Id = room.Id,
            Title = room.Title,
            Address = room.Address,
            Latitude = room.Latitude,
            Longitude = room.Longitude,
            BasePrice = room.BasePrice,
            ElectricPrice = room.ElectricPrice,
            WaterPrice = room.WaterPrice,
            InternetPrice = room.InternetPrice,
            OtherFee = room.OtherFee,
            Area = room.Area,
            MaxPeople = room.MaxPeople,
            Amenities = room.Amenities,
            CoverImage = coverImage,
            Status = room.Status,
            ExpiresAt = room.ExpiresAt,
            CreatedAt = room.CreatedAt,
            DistanceKm = distance,
            AverageRating = averageRating,
            ReviewCount = activeReviews.Count,
            IsDemoted = demotion.IsDemoted,
            DemotionReason = demotion.Reason,
            RecommendationScore = ranking.CalculateRecommendationScore(activeReviews),
            IsLowRated = demotion.IsDemoted
        };
    }
}
