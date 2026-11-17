using CampusRent.Server.Data;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.EntityFrameworkCore;
namespace CampusRent.Server.BackgroundServices;
public class AutoExpireRoomsWorker(IServiceScopeFactory scopes, ILogger<AutoExpireRoomsWorker> logger) : BackgroundService
{
    public static async Task<int> ExpireAsync(CampusRentDbContext db, IEmailService? emailService = null, CancellationToken token = default)
    {
        var now = DateTime.UtcNow;

        // 1. Auto-hide listings after their 30-day active window
        var expiredRooms = await db.Rooms.Where(r => r.Status == RoomStatus.AVAILABLE && r.ExpiresAt <= now).ToListAsync(token);
        foreach (var room in expiredRooms)
        {
            room.Status = RoomStatus.HIDDEN;
            room.UpdatedAt = now;
        }

        // 2. Send appointment-day reminder emails to landlords for confirmed bookings
        var vnTodayEndUtc = now.AddHours(7).Date.AddDays(1).AddHours(-7);
        var reminderCandidates = await db.Bookings
            .Include(b => b.Room).ThenInclude(r => r.Landlord)
            .Include(b => b.Student)
            .Where(b => (b.Status == "CONFIRMED" || (b.ConfirmedAt != null && b.Status != "CANCELLED"))
                && b.ReminderSentAt == null
                && b.VisitAt < vnTodayEndUtc
                && b.VisitAt.AddDays(3) > now)
            .ToListAsync(token);

        foreach (var booking in reminderCandidates)
        {
            if (emailService == null) continue;
            var delivery = await emailService.SendBookingReminderAsync(
                booking.Room.Landlord.Email,
                booking.Room.Landlord.FullName,
                booking.Student.FullName,
                booking.Room.Title,
                booking.VisitAt,
                token);
            if (delivery.Sent || delivery.IsSimulated)
                booking.ReminderSentAt = now;
        }

        // 3. Auto-hide room 3 days after confirmed visit unless landlord marked "Khách chưa chốt"
        var unconfirmedPostVisitBookings = await db.Bookings
            .Include(b => b.Room)
            .Where(b => (b.Status == "CONFIRMED" || (b.ConfirmedAt != null && b.Status != "CANCELLED"))
                && b.VisitAt.AddDays(3) <= now
                && b.CustomerNotClosedAt == null
                && b.AutoClosedAt == null)
            .ToListAsync(token);

        var hiddenCount = expiredRooms.Count;
        foreach (var booking in unconfirmedPostVisitBookings)
        {
            booking.AutoClosedAt = now;
            booking.Status = "COMPLETED";
            if (booking.Room.Status == RoomStatus.AVAILABLE)
            {
                booking.Room.Status = RoomStatus.HIDDEN;
                booking.Room.UpdatedAt = now;
                hiddenCount++;
            }
        }

        // 4. Mark stale pending bookings or resolved post-visit bookings as COMPLETED
        var stalePending = await db.Bookings
            .Where(b => b.Status == "PENDING" && b.VisitAt <= now)
            .ToListAsync(token);
        foreach (var booking in stalePending) booking.Status = "COMPLETED";

        var settledNotClosed = await db.Bookings
            .Where(b => b.Status == "CONFIRMED" && b.VisitAt.AddDays(3) <= now && b.CustomerNotClosedAt != null)
            .ToListAsync(token);
        foreach (var booking in settledNotClosed) booking.Status = "COMPLETED";

        await db.SaveChangesAsync(token);
        return hiddenCount;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunSafelyAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunSafelyAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task RunSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
            var emailService = scope.ServiceProvider.GetService<IEmailService>();
            await ExpireAsync(db, emailService, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not process room expiry or booking reminders");
        }
    }
}
