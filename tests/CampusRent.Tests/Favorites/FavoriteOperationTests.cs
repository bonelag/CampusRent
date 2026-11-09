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
public class FavoriteOperationTests : ApiTestBase
{
    [Fact]
    public async Task FavoriteOperationsAreIdempotent()
    {
        var id = await Room();
        await Register();
        await Data(await client.PutAsync($"/api/favorites/{id}", null));
        await Data(await client.PutAsync($"/api/favorites/{id}", null));
        Assert.Equal(1, (await Data(await client.GetAsync("/api/favorites"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/favorites/{id}")).StatusCode);
        Assert.Equal(0, (await Data(await client.GetAsync("/api/favorites"))).GetArrayLength());
    }
}
