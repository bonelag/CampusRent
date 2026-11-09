using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace CampusRent.Tests;
public class LandlordRegistrationTests : SecurityTestBase
{
    [Fact]
    public async Task PublicRegistrationCannotBypassLandlordApproval()
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "unapproved@example.com", password = "TestPassword123!", fullName = "Unapproved owner", role = "LANDLORD" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
