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
public class ReportDismissalTests : RoomFeatureTestBase
{
    [Fact]
    public async Task AdminDismissesReportWithoutModifyingTarget()
    {
        var roomId = await CreateRoom();
        await Register("STUDENT");
        var report = await Data(await client.PostAsJsonAsync("/api/reports", new { roomId, reason = "Báo cáo nhầm" }));
        var reportId = report.GetProperty("id").GetGuid();
        await Admin();
        var resolveRes = await client.PatchAsJsonAsync($"/api/admin/reports/{reportId}", new { status = "DISMISSED", deleteTarget = false });
        Assert.Equal(HttpStatusCode.OK, resolveRes.StatusCode);
        // Room is still available
        client.DefaultRequestHeaders.Authorization = null;
        var roomRes = await client.GetAsync($"/api/rooms/{roomId}");
        Assert.Equal(HttpStatusCode.OK, roomRes.StatusCode);
    }
}
