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
public class RoomEditingTests : ApiTestBase
{
    [Fact]
    public async Task OwnerCanUpdateRoomAndReplaceImages()
    {
        var id = await Room();
        await Data(await client.PutAsJsonAsync($"/api/rooms/{id}", RoomPayload()));
        var detail = await Data(await client.GetAsync($"/api/rooms/{id}"));
        Assert.Single(detail.GetProperty("images").EnumerateArray());
    }
}
