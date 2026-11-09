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
public class PublicRoomVisibilityTests : ApiTestBase
{
    [Fact]
    public async Task HiddenRoomsDisappearFromPublicSearchAndDetail()
    {
        var id = await Room();
        await Data(await client.DeleteAsync($"/api/rooms/{id}"));
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/rooms/{id}")).StatusCode);
        Assert.Equal(0, (await Data(await client.GetAsync("/api/rooms"))).GetProperty("total").GetInt32());
    }
}
