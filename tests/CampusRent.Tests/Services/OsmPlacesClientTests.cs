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
public class OsmPlacesClientTests
{
    [Fact]
    public async Task OsmPlacesClientHandlesEmptyQueryAndErrorResponses()
    {
        var handler = new StubOsmHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://photon.komoot.io/")
        };
        var sut = new OsmPlacesClient(httpClient, NullLogger<OsmPlacesClient>.Instance);
        // Empty query
        var empty = await sut.SuggestAsync("", CancellationToken.None);
        Assert.Empty(empty);
        // Short query
        var shortQ = await sut.SuggestAsync("a", CancellationToken.None);
        Assert.Empty(shortQ);
        // Normal query
        var suggestions = await sut.SuggestAsync("Hai Bà Trưng", CancellationToken.None);
        Assert.Single(suggestions);
        Assert.Equal("Hai Bà Trưng", suggestions[0].Title);
        Assert.Equal(21.01, suggestions[0].Latitude);
        Assert.Equal(105.85, suggestions[0].Longitude);
        // Reverse geocoding
        var reverse = await sut.ReverseGeocodeAsync(21.01, 105.85, CancellationToken.None);
        Assert.NotNull(reverse);
        Assert.Contains("Hai Bà Trưng", reverse);
    }

    private sealed class StubOsmHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("reverse"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"features":[{"properties":{"name":"Hai Bà Trưng","city":"Hà Nội"}}]}""", System.Text.Encoding.UTF8, "application/json") });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"features":[{"properties":{"name":"Hai Bà Trưng","city":"Hà Nội","district":"Hai Bà Trưng"},"geometry":{"coordinates":[105.85,21.01]}}]}""", System.Text.Encoding.UTF8, "application/json") });
        }
    }
}
