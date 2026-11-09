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
public class UniversityCreationTests : ApiTestBase
{
    [Fact]
    public async Task UniversityCreationValidatesCoordinatesAndDuplicates()
    {
        await Admin();
        var payload = new
        {
            name = "Đại học kiểm thử",
            shortName = "TEST",
            address = "Hà Nội",
            latitude = 21,
            longitude = 105
        };
        await Data(await client.PostAsJsonAsync("/api/universities", payload));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/universities", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/universities", new { name = "Wrong", shortName = "WRONG", address = "test", latitude = 91, longitude = 0 })).StatusCode);
    }
}
