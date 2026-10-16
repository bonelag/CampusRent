using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusRent.Server.Models;

[Table("rooms")]
public class Room
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("landlord_id")]
    public Guid LandlordId { get; set; }

    [ForeignKey(nameof(LandlordId))]
    public User Landlord { get; set; } = null!;

    [Column("university_id")]
    public Guid? UniversityId { get; set; }

    [ForeignKey(nameof(UniversityId))]
    public University? University { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    [Column("address")]
    public string Address { get; set; } = string.Empty;

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("base_price", TypeName = "decimal(12,2)")]
    public decimal BasePrice { get; set; }

    [Column("electric_price", TypeName = "decimal(10,2)")]
    public decimal ElectricPrice { get; set; }

    [Column("water_price", TypeName = "decimal(10,2)")]
    public decimal WaterPrice { get; set; }

    [Column("internet_price", TypeName = "decimal(10,2)")]
    public decimal InternetPrice { get; set; } = 0;

    [Column("other_fee", TypeName = "decimal(10,2)")]
    public decimal OtherFee { get; set; } = 0;

    [Column("area")]
    public double Area { get; set; }

    [Column("max_people")]
    public int MaxPeople { get; set; } = 2;

    [Column("amenities")]
    public List<string> Amenities { get; set; } = new();

    [Column("status")]
    public RoomStatus Status { get; set; } = RoomStatus.AVAILABLE;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RoomImage> Images { get; set; } = new List<RoomImage>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
