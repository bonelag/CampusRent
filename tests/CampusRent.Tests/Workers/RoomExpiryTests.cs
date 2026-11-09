using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CampusRent.Server.BackgroundServices;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusRent.Tests;
public class RoomExpiryTests : ApiTestBase
{
    [Fact]
    public async Task ExpiryWorkerHidesOnlyExpiredAvailableRooms()
    {
        var id = await Room();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
        var room = (await db.Rooms.FindAsync(id))!;
        room.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        Assert.Equal(1, await AutoExpireRoomsWorker.ExpireAsync(db));
        Assert.Equal(RoomStatus.HIDDEN, room.Status);
    }
}
