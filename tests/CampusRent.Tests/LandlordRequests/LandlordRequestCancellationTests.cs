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
public class LandlordRequestCancellationTests : RoomFeatureTestBase
{
    [Fact]
    public async Task StudentCanCancelPendingLandlordUpgradeRequest()
    {
        await Register("STUDENT");
        var reqPayload = new
        {
            title = "Yêu cầu nâng cấp tài khoản",
            reason = "Có nhà trọ cho sinh viên thuê",
            imageUrls = Array.Empty<string>()
        };
        var created = await Data(await client.PostAsJsonAsync("/api/landlord-requests", reqPayload));
        var reqId = created.GetProperty("id").GetGuid();
        var cancelRes = await client.DeleteAsync($"/api/landlord-requests/{reqId}");
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
        var myRequests = await Data(await client.GetAsync("/api/landlord-requests/my"));
        Assert.Empty(myRequests.EnumerateArray());
    }

    [Fact]
    public async Task CannotCancelProcessedLandlordUpgradeRequest()
    {
        await Register("STUDENT");
        var studentToken = client.DefaultRequestHeaders.Authorization;
        var reqPayload = new
        {
            title = "Yêu cầu nâng cấp",
            reason = "Cho thuê phòng",
            imageUrls = new string[0]
        };
        var created = await Data(await client.PostAsJsonAsync("/api/landlord-requests", reqPayload));
        var reqId = created.GetProperty("id").GetGuid();
        // Admin approves it
        await Admin();
        await client.PatchAsync($"/api/admin/landlord-requests/{reqId}/approve", null);
        // Student tries to cancel after approval
        client.DefaultRequestHeaders.Authorization = studentToken;
        var cancelRes = await client.DeleteAsync($"/api/landlord-requests/{reqId}");
        Assert.Equal(HttpStatusCode.BadRequest, cancelRes.StatusCode);
    }

    [Fact]
    public async Task CannotCancelOtherStudentsLandlordRequest()
    {
        await Register("STUDENT", "student_a@example.com");
        var created = await Data(await client.PostAsJsonAsync("/api/landlord-requests", new { title = "Yêu cầu của A", reason = "Lý do của A", imageUrls = new string[0] }));
        var reqId = created.GetProperty("id").GetGuid();
        // Switch to Student B
        await Register("STUDENT", "student_b@example.com");
        var cancelRes = await client.DeleteAsync($"/api/landlord-requests/{reqId}");
        Assert.Equal(HttpStatusCode.NotFound, cancelRes.StatusCode);
    }
}
