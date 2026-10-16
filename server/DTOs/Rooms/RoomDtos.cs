using System.ComponentModel.DataAnnotations;
using CampusRent.Server.DTOs.Auth;
using CampusRent.Server.DTOs.Reviews;
using CampusRent.Server.DTOs.Universities;
using CampusRent.Server.Models;

namespace CampusRent.Server.DTOs.Rooms;

public class CreateRoomDto
{
    public Guid? UniversityId { get; set; }

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [Range(0, double.MaxValue)]
    public decimal BasePrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ElectricPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal WaterPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal InternetPrice { get; set; } = 0;

    [Range(0, double.MaxValue)]
    public decimal OtherFee { get; set; } = 0;

    [Range(1, double.MaxValue)]
    public double Area { get; set; }

    [Range(1, 100)]
    public int MaxPeople { get; set; } = 2;

    public List<string> Amenities { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
}

public class UpdateRoomDto
{
    public Guid? UniversityId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public decimal? BasePrice { get; set; }
    public decimal? ElectricPrice { get; set; }
    public decimal? WaterPrice { get; set; }
    public decimal? InternetPrice { get; set; }
    public decimal? OtherFee { get; set; }
    public double? Area { get; set; }
    public int? MaxPeople { get; set; }
    public List<string>? Amenities { get; set; }
    public RoomStatus? Status { get; set; }
    public bool RenewExpires { get; set; } = false;
}

public class RoomQueryParameters
{
    public Guid? UniversityId { get; set; }
    public double? Radius { get; set; } // 1, 3, 5 km
    public double? CenterLat { get; set; }
    public double? CenterLon { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public RoomStatus? Status { get; set; } = RoomStatus.AVAILABLE;
    public string? Keyword { get; set; }
}

public class RoomListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal BasePrice { get; set; }
    public decimal ElectricPrice { get; set; }
    public decimal WaterPrice { get; set; }
    public decimal InternetPrice { get; set; }
    public decimal OtherFee { get; set; }
    public double Area { get; set; }
    public int MaxPeople { get; set; }
    public List<string> Amenities { get; set; } = new();
    public string? CoverImage { get; set; }
    public double? DistanceKm { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public RoomStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RoomDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal BasePrice { get; set; }
    public decimal ElectricPrice { get; set; }
    public decimal WaterPrice { get; set; }
    public decimal InternetPrice { get; set; }
    public decimal OtherFee { get; set; }
    public double Area { get; set; }
    public int MaxPeople { get; set; }
    public List<string> Amenities { get; set; } = new();
    public List<string> Images { get; set; } = new();
    public RoomStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserProfileDto Landlord { get; set; } = null!;
    public UniversityDto? University { get; set; }
    public double? DistanceToUniversityKm { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<ReviewItemDto> Reviews { get; set; } = new();
}

public class CostEstimateDto
{
    public decimal BasePrice { get; set; }
    public decimal ExpectedKwh { get; set; } = 80;
    public decimal ElectricCost { get; set; }
    public decimal ExpectedM3 { get; set; } = 4;
    public decimal WaterCost { get; set; }
    public decimal InternetPrice { get; set; }
    public decimal OtherFee { get; set; }
    public decimal TotalEstimatedMonthlyCost { get; set; }
}
