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
public class LandlordRequestDecisionTests : ApiTestBase
{
    [Fact]
    public async Task StudentCanSubmitLandlordUpgradeRequestAndAdminCanApprove()
    {
        var studentId = await Register("STUDENT");
        var reqPayload = new
        {
            title = "Đăng ký làm chủ trọ khu vực Bách Khoa",
            reason = "Tôi có dãy 5 phòng trọ cho sinh viên thuê tại Tạ Quang Bửu, cần đăng tin.",
            imageUrls = Array.Empty<string>()
        };
        var created = await Data(await client.PostAsJsonAsync("/api/landlord-requests", reqPayload));
        var reqId = created.GetProperty("id").GetGuid();
        Assert.Equal("PENDING", created.GetProperty("status").GetString());
        Assert.Equal("Đăng ký làm chủ trọ khu vực Bách Khoa", created.GetProperty("title").GetString());
        // Duplicate pending request is rejected
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/landlord-requests", reqPayload)).StatusCode);
        // Student can view their requests
        var myRequests = await Data(await client.GetAsync("/api/landlord-requests/my"));
        Assert.Single(myRequests.EnumerateArray());
        // Admin views and approves
        await Admin();
        var adminList = await Data(await client.GetAsync("/api/admin/landlord-requests"));
        Assert.Contains(adminList.EnumerateArray(), r => r.GetProperty("id").GetGuid() == reqId);
        var approveRes = await client.PatchAsync($"/api/admin/landlord-requests/{reqId}/approve", null);
        Assert.True(approveRes.IsSuccessStatusCode);
        // Verify user role is now LANDLORD
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
        var user = await db.Users.FindAsync(studentId);
        Assert.Equal(Role.LANDLORD, user!.Role);
    }

    [Fact]
    public async Task AdminCanRejectLandlordUpgradeRequest()
    {
        await Register("STUDENT");
        var reqPayload = new
        {
            title = "Yêu cầu nâng cấp",
            reason = "Muốn cho thuê phòng",
            imageUrls = new string[0]
        };
        var created = await Data(await client.PostAsJsonAsync("/api/landlord-requests", reqPayload));
        var reqId = created.GetProperty("id").GetGuid();
        await Admin();
        var rejectRes = await client.PatchAsJsonAsync($"/api/admin/landlord-requests/{reqId}/reject", new { adminNote = "Chưa đủ giấy tờ chứng minh" });
        Assert.True(rejectRes.IsSuccessStatusCode);
        var adminList = await Data(await client.GetAsync("/api/admin/landlord-requests"));
        var req = adminList.EnumerateArray().First(r => r.GetProperty("id").GetGuid() == reqId);
        Assert.Equal("REJECTED", req.GetProperty("status").GetString());
    }
}
