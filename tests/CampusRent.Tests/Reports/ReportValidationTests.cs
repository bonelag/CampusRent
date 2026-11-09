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
public class ReportValidationTests : ApiTestBase
{
    [Fact]
    public async Task ReportsRequireExactlyOneExistingTarget()
    {
        var id = await Room();
        await Register();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/reports", new { reason = "Tin giả" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/reports", new { roomId = Guid.NewGuid(), reason = "Tin giả" })).StatusCode);
        await Data(await client.PostAsJsonAsync("/api/reports", new { roomId = id, reason = "Thông tin sai" }));
    }
}
