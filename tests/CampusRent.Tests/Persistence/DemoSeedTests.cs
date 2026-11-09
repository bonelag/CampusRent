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
public class DemoSeedTests : ApiTestBase
{
    [Fact]
    public async Task DemoSeedIsIdempotentAndAdminStatisticsMatch()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
            await DbSeeder.SeedAsync(db);
            await DbSeeder.SeedAsync(db);
            Assert.Equal(5, await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Rooms));
            Assert.Equal(5, await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Users));
            Assert.Equal(5, await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Universities));
        }

        await Admin();
        var stats = await Data(await client.GetAsync("/api/admin/stats"));
        Assert.Equal(5, stats.GetProperty("rooms").GetInt32());
        Assert.Equal(5, stats.GetProperty("availableRooms").GetInt32());
        Assert.Equal(0, (await Data(await client.GetAsync("/api/admin/reports"))).GetArrayLength());
    }
}
