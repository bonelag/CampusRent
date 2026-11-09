using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusRent.Tests;
public class FavoriteValidationTests : RoomFeatureTestBase
{
    [Fact]
    public async Task FavoriteNonExistentRoomReturnsNotFound()
    {
        await Register("STUDENT");
        var res = await client.PutAsync($"/api/favorites/{Guid.NewGuid()}", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
