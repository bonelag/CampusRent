using System.ComponentModel.DataAnnotations;

namespace CampusRent.Server.DTOs.Universities;

public class CreateUniversityDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string ShortName { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [Required, MaxLength(300)]
    public string Address { get; set; } = string.Empty;
}

public class UniversityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
}
