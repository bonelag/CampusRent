using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace CampusRent.Tests;
public class LandlordRequestConstraintTests : SecurityTestBase
{
    [Fact]
    public async Task PendingRequestUniquenessIsEnforcedByTheDatabase()
    {
        var account = await Account();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
        db.LandlordRequests.Add(new() { UserId = account.Id, Title = "First", Reason = "First" });
        await db.SaveChangesAsync();
        db.LandlordRequests.Add(new() { UserId = account.Id, Title = "Concurrent", Reason = "Concurrent" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
