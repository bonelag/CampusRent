using System.Net;
using System.Net.Http.Json;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusRent.Tests;

public class AdminRetentionTests : SecurityTestBase
{
    [Fact]
    public async Task AdminProfileAndRoleCannotBeChangedThroughAdministrationApi()
    {
        var admin = await Account(Role.ADMIN);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/admin/users/{admin.Id}", new
        {
            fullName = "Demoted admin",
            role = "STUDENT",
            isEduVerified = false
        })).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
        Assert.Equal(Role.ADMIN, (await db.Users.AsNoTracking().SingleAsync(user => user.Id == admin.Id)).Role);
    }
}
