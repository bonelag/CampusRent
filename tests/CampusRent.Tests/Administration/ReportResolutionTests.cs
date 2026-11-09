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
public class ReportResolutionTests : ApiTestBase
{
    [Fact]
    public async Task AdminResolvesReportAndHidesTarget()
    {
        var id = await Room();
        await Register();
        var report = await Data(await client.PostAsJsonAsync("/api/reports", new { roomId = id, reason = "Tin không đúng" }));
        await Admin();
        var rid = report.GetProperty("id").GetGuid();
        await Data(await client.PatchAsJsonAsync($"/api/admin/reports/{rid}", new { status = "RESOLVED", deleteTarget = true }));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync($"/api/admin/reports/{rid}", new { status = "RESOLVED" })).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/rooms/{id}")).StatusCode);
    }
}
