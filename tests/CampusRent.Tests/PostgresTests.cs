using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using CampusRent.Server.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;
namespace CampusRent.Tests;
public class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CAMPUSRENT_TEST_POSTGRES")))
            Skip = "Set CAMPUSRENT_TEST_POSTGRES to run PostgreSQL migrations and persistence checks (enabled in CI).";
    }
}
public class PostgresTests
{
    [PostgresFact]
    public async Task MigrationsAreRepeatableAndRentalWorkflowPersistsInPostgres()
    {
        var connection = Environment.GetEnvironmentVariable("CAMPUSRENT_TEST_POSTGRES")!;
        var schema = "campusrent_test_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var options = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema };
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Testing").ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string,string?> { ["Database:Provider"]="Postgres",["ConnectionStrings:DefaultConnection"]=options.ToString() })));
            using var client = factory.CreateClient();
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
                Assert.Empty(await db.Database.GetPendingMigrationsAsync());
                await db.Database.MigrateAsync();
                await DbSeeder.SeedReferenceAsync(db);
                Assert.Equal(5, await db.Universities.CountAsync());
            }
            var registered = await client.PostAsJsonAsync("/api/auth/register", new {email="owner@example.com",password="TestPassword123!",fullName="Owner",role="LANDLORD"});
            registered.EnsureSuccessStatusCode();
            var auth = JsonDocument.Parse(await registered.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",auth.GetProperty("token").GetString());
            var created = await client.PostAsJsonAsync("/api/rooms",new {title="Postgres test",description="Verified on PostgreSQL",address="Hà Nội",latitude=21.005,longitude=105.843,basePrice=3200000,area=25,amenities=new[]{"wifi"},imageUrls=new[]{"https://example.com/room.jpg"}});
            created.EnsureSuccessStatusCode();
            var response = await client.GetStringAsync("/api/rooms?maxPrice=4000000&sort=price_asc");
            var result = JsonDocument.Parse(response).RootElement.GetProperty("data");
            Assert.Equal(1,result.GetProperty("total").GetInt32());
            Assert.Equal("wifi",result.GetProperty("items")[0].GetProperty("amenities")[0].GetString());
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}

