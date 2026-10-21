using CampusRent.Server.Data;
using CampusRent.Server.Models;
using Microsoft.EntityFrameworkCore;
namespace CampusRent.Server.BackgroundServices;
public class AutoExpireRoomsWorker(IServiceScopeFactory scopes, ILogger<AutoExpireRoomsWorker> logger) : BackgroundService
{
    public static async Task<int> ExpireAsync(CampusRentDbContext db, CancellationToken token = default)
    {
        var now = DateTime.UtcNow;
        var rooms = await db.Rooms.Where(r => r.Status == RoomStatus.AVAILABLE && r.ExpiresAt <= now).ToListAsync(token);
        foreach (var room in rooms) { room.Status = RoomStatus.HIDDEN; room.UpdatedAt = now; }
        var bookings = await db.Bookings.Where(b => (b.Status == "PENDING" || b.Status == "CONFIRMED") && b.VisitAt <= now).ToListAsync(token);
        foreach (var booking in bookings) booking.Status = "COMPLETED";
        await db.SaveChangesAsync(token);
        return rooms.Count;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                try { using var scope = scopes.CreateScope(); await ExpireAsync(scope.ServiceProvider.GetRequiredService<CampusRentDbContext>(), stoppingToken); }
                catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "Could not expire rooms"); }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
