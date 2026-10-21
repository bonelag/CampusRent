namespace CampusRent.Server.Models;
public class Favorite
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
}
