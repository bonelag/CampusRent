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
public class LandlordRequestSubmissionTests : RoomFeatureTestBase
{
    [Fact]
    public async Task ExistingLandlordCannotSubmitUpgradeRequest()
    {
        await Register("LANDLORD");
        var res = await client.PostAsJsonAsync("/api/landlord-requests", new { title = "Yêu cầu", reason = "Muốn nâng cấp", imageUrls = new string[0] });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
