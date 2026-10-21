using System.Security.Claims;
using CampusRent.Server.DTOs.Auth;
using CampusRent.Server.DTOs.Rooms;
using CampusRent.Server.Models;
namespace CampusRent.Server.Services;
public static class Mapping
{
    public static Guid UserId(this ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static UserProfileDto Profile(User user) => new() { Id = user.Id, Email = user.Email, FullName = user.FullName, PhoneNumber = user.PhoneNumber, Avatar = user.Avatar, Role = user.Role, IsEduVerified = user.IsEduVerified, CreatedAt = user.CreatedAt };
    public static RoomListItemDto Card(Room r, double? distance = null)
    {
        var reviews = r.Reviews.Where(x => !x.IsFlagged).ToList();
        return new() { Id = r.Id, Title = r.Title, Address = r.Address, Latitude = r.Latitude, Longitude = r.Longitude, BasePrice = r.BasePrice, ElectricPrice = r.ElectricPrice, WaterPrice = r.WaterPrice, InternetPrice = r.InternetPrice, OtherFee = r.OtherFee, Area = r.Area, MaxPeople = r.MaxPeople, Amenities = r.Amenities, CoverImage = r.Images.OrderByDescending(i => i.IsCover).ThenBy(i => i.DisplayOrder).FirstOrDefault()?.Url, Status = r.Status, CreatedAt = r.CreatedAt, DistanceKm = distance, AverageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(x => x.Rating), 1), ReviewCount = reviews.Count };
    }
}
