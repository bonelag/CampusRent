using System.Net;
using System.Text;
using CampusRent.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusRent.Tests;
public class GooglePlacesClientTests
{
    [Fact]
    public async Task GooglePlacesClientMapsSuggestionAndResolvedCoordinates()
    {
        var handler = new StubHttpHandler();
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://places.googleapis.com/v1/")
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["GooglePlaces:ApiKey"] = "test-key" }).Build();
        var sut = new GooglePlacesClient(client, configuration, NullLogger<GooglePlacesClient>.Instance);
        var suggestions = await sut.SuggestAsync("103 Tạ Quang Bửu", CancellationToken.None);
        var place = Assert.Single(suggestions);
        Assert.Equal("google:places/ChIJ-test", place.Id);
        Assert.Equal("Tòa nhà 103", place.Title);
        Assert.Null(place.Latitude);
        var location = await sut.ResolveAsync("places/ChIJ-test", CancellationToken.None);
        Assert.NotNull(location);
        Assert.Equal(21.005, location.Latitude);
        Assert.Equal(105.843, location.Longitude);
        Assert.All(handler.Requests, request => Assert.Equal("test-key", request.Headers.GetValues("X-Goog-Api-Key").Single()));
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Method == HttpMethod.Post)
            {
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                Assert.Contains("\"input\"", body);
                return Json("""{"suggestions":[{"placePrediction":{"place":"places/ChIJ-test","text":{"text":"Tòa nhà 103, Tạ Quang Bửu"},"structuredFormat":{"mainText":{"text":"Tòa nhà 103"},"secondaryText":{"text":"Hai Bà Trưng, Hà Nội"}}}}]}""");
            }

            return Json("""{"displayName":{"text":"Tòa nhà 103"},"formattedAddress":"Tạ Quang Bửu, Hà Nội","location":{"latitude":21.005,"longitude":105.843}}""");
        }

        private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}
