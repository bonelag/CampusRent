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
public class PlaceSuggestionApiTests : ApiTestBase
{
    [Fact]
    public async Task PlaceSuggestionsReturnKnownListingCoordinatesAndValidateInput()
    {
        await Room();
        client.DefaultRequestHeaders.Authorization = null;
        var suggestions = await Data(await client.GetAsync("/api/places/suggest?q=T%E1%BA%A1%20Quang"));
        var suggestion = suggestions.EnumerateArray().First(s => s.GetProperty("source").GetString() == "listing");
        Assert.Equal("listing", suggestion.GetProperty("source").GetString());
        Assert.Equal(21.005, suggestion.GetProperty("latitude").GetDouble());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/places/suggest?q=x")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/places/suggest?q=" + new string ('x', 121))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/places/reverse?lat=21.023&lon=105.801")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/places/reverse?lat=999&lon=105")).StatusCode);
    }
}
