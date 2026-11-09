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
public class RoomSearchValidationTests : ApiTestBase
{
    [Theory]
    [InlineData("/api/rooms?page=0")]
    [InlineData("/api/rooms?pageSize=101")]
    [InlineData("/api/rooms?centerLat=100&centerLon=0")]
    [InlineData("/api/rooms?radius=3")]
    public async Task SearchRejectsInvalidFilters(string url) => Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(url)).StatusCode);
}
