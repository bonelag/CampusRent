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
public class UniversityAccessTests : RoomFeatureTestBase
{
    [Fact]
    public async Task UniversitiesGetAllAndAccessControl()
    {
        // Seed university via Admin first
        await Admin();
        await client.PostAsJsonAsync("/api/universities", new { name = "Đại học Bách Khoa Hà Nội", shortName = "HUST", address = "1 Đại Cồ Việt, Hai Bà Trưng, Hà Nội", latitude = 21.004, longitude = 105.843 });
        client.DefaultRequestHeaders.Authorization = null;
        var list = await Data(await client.GetAsync("/api/universities"));
        Assert.NotEmpty(list.EnumerateArray());
        // Student cannot create university
        await Register("STUDENT");
        var res = await client.PostAsJsonAsync("/api/universities", new { name = "Đại học mới", shortName = "DHMOI", address = "Hà Nội", latitude = 21.0, longitude = 105.8 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
