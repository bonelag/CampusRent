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
public class ReportTargetValidationTests : RoomFeatureTestBase
{
    [Fact]
    public async Task ReportRejectsBothOrNeitherTargets()
    {
        var roomId = await CreateRoom();
        await Register("STUDENT");
        // Neither
        var neitherRes = await client.PostAsJsonAsync("/api/reports", new { reason = "Lý do chung chung" });
        Assert.Equal(HttpStatusCode.BadRequest, neitherRes.StatusCode);
        // Both
        var bothRes = await client.PostAsJsonAsync("/api/reports", new { roomId, reviewId = Guid.NewGuid(), reason = "Báo cáo cả hai" });
        Assert.Equal(HttpStatusCode.BadRequest, bothRes.StatusCode);
    }
}
