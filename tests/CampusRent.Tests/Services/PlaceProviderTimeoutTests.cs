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
public class PlaceProviderTimeoutTests
{
    [Fact]
    public async Task PlaceProviderTimeoutsAllowFallbackWithoutHidingClientCancellation()
    {
        using var http = new HttpClient(new TimeoutHandler())
        {
            BaseAddress = new Uri("https://places.example.com/")
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["GooglePlaces:ApiKey"] = "test-key" }).Build();
        var google = new GooglePlacesClient(http, config, NullLogger<GooglePlacesClient>.Instance);
        var osm = new OsmPlacesClient(http, NullLogger<OsmPlacesClient>.Instance);
        Assert.Empty(await google.SuggestAsync("Bách Khoa", CancellationToken.None));
        Assert.Null(await google.ResolveAsync("places/example", CancellationToken.None));
        Assert.Empty(await osm.SuggestAsync("Bách Khoa", CancellationToken.None));
        Assert.Null(await osm.ReverseGeocodeAsync(21, 105, CancellationToken.None));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => osm.SuggestAsync("Bách Khoa", cancelled.Token));
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromException<HttpResponseMessage>(new TaskCanceledException("Provider timeout"));
    }
}
