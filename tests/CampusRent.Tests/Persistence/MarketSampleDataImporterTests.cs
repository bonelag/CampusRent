using CampusRent.Server.Data;
using CampusRent.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusRent.Tests;

public class MarketSampleDataImporterTests : ApiTestBase
{
    [Fact]
    public async Task ImportIsIdempotentAndCreatesTwentyManagedRowsPerTable()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();

        var first = await MarketSampleDataImporter.ImportAsync(db);
        var second = await MarketSampleDataImporter.ImportAsync(db);

        Assert.True(first.IsComplete);
        Assert.Equal(first, second);
        Assert.Equal(20, await db.Users.CountAsync());
        Assert.Equal(20, await db.Universities.CountAsync());
        Assert.Equal(20, await db.Rooms.CountAsync());
        Assert.Equal(20, await db.RoomImages.CountAsync());
        Assert.Equal(20, await db.Reviews.CountAsync());
        Assert.Equal(20, await db.Reports.CountAsync());
        Assert.Equal(20, await db.Bookings.CountAsync());
        Assert.Equal(20, await db.Favorites.CountAsync());
        Assert.Equal(20, await db.LandlordRequests.CountAsync());

        var rooms = await db.Rooms.Include(room => room.Images).ToListAsync();
        Assert.All(rooms, room =>
        {
            Assert.Contains("[Dữ liệu mẫu thị trường]", room.Description);
            Assert.Contains("Nguồn: https://", room.Description);
            Assert.Single(room.Images);
            Assert.StartsWith("https://", room.Images.Single().Url);
        });
        Assert.Equal(5, await db.Users.CountAsync(user => user.Role == Role.LANDLORD));
        Assert.Equal(15, await db.Users.CountAsync(user => user.Role == Role.STUDENT));
    }
}
