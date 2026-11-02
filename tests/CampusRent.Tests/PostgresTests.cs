
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
        if (string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("CAMPUSRENT_TEST_POSTGRES")))
        {
            Skip = "Set CAMPUSRENT_TEST_POSTGRES to run PostgreSQL tests.";
        }
    }
}

public class PostgresTests
{
    [PostgresFact]
    public async Task MigrationsAreRepeatableAndRentalWorkflowPersistsInPostgres()
    {
        // 1. Lấy chuỗi kết nối PostgreSQL từ biến môi trường.
        var connection =
            Environment.GetEnvironmentVariable("CAMPUSRENT_TEST_POSTGRES")!;

        // 2. Tắt SSL cho PostgreSQL CI.
        var testConnection = new NpgsqlConnectionStringBuilder(connection)
        {
            SslMode = SslMode.Disable
        };

        // 3. Tạo schema riêng để tránh ảnh hưởng database khác.
        var schema = "campusrent_test_" + Guid.NewGuid().ToString("N");

        // 4. Kết nối quản trị database.
        await using var admin =
            new NpgsqlConnection(testConnection.ConnectionString);

        await admin.OpenAsync();

        await using (var create = new NpgsqlCommand(
            $"CREATE SCHEMA \"{schema}\"",
            admin))
        {
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            // 5. Cấu hình connection string sử dụng schema riêng.
            var options = new NpgsqlConnectionStringBuilder(
                testConnection.ConnectionString)
            {
                SearchPath = schema
            };

            // 6. Khởi tạo ứng dụng trong môi trường Testing.
            using var factory =
                new WebApplicationFactory<Program>()
                    .WithWebHostBuilder(builder =>
                    {
                        builder.UseEnvironment("Testing");

                        builder.ConfigureAppConfiguration((_, config) =>
                        {
                            config.AddInMemoryCollection(
                                new Dictionary<string, string?>
                                {
                                    ["Database:Provider"] = "Postgres",
                                    ["ConnectionStrings:DefaultConnection"] =
                                        options.ToString()
                                });
                        });
                    });

            // 7. Xác nhận ứng dụng thực sự chạy trong Testing.
            var environment = factory.Services
                .GetRequiredService<IWebHostEnvironment>();

            Assert.Equal("Testing", environment.EnvironmentName);

            using var client = factory.CreateClient();

            // 8. Kiểm tra và chạy migration.
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<CampusRentDbContext>();

                // Đảm bảo migration chưa được áp dụng.
                var pendingBefore =
                    (await db.Database.GetPendingMigrationsAsync()).ToArray();

                Assert.NotEmpty(pendingBefore);

                // Chỉ chạy migration tại đây.
                await db.Database.MigrateAsync();

                // Kiểm tra migration đã hoàn thành.
                var pendingAfter =
                    await db.Database.GetPendingMigrationsAsync();

                Assert.Empty(pendingAfter);

                // Chạy migration lần thứ hai để kiểm tra tính lặp lại.
                await db.Database.MigrateAsync();

                // Thêm dữ liệu tham chiếu.
                await DbSeeder.SeedReferenceAsync(db);

                // Kiểm tra dữ liệu trường đại học.
                Assert.Equal(5, await db.Universities.CountAsync());
            }

            // 9. Đăng ký tài khoản chủ trọ.
            var registered = await client.PostAsJsonAsync(
                "/api/auth/register",
                new
                {
                    email = "owner@example.com",
                    password = "TestPassword123!",
                    fullName = "Owner",
                    role = "LANDLORD"
                });

            registered.EnsureSuccessStatusCode();

            // 10. Lấy JWT token.
            using var authDocument = JsonDocument.Parse(
                await registered.Content.ReadAsStringAsync());

            var auth = authDocument.RootElement.GetProperty("data");

            var token = auth.GetProperty("token").GetString();

            Assert.False(string.IsNullOrWhiteSpace(token));

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 11. Tạo phòng trọ.
            var created = await client.PostAsJsonAsync(
                "/api/rooms",
                new
                {
                    title = "Postgres test",
                    description = "Verified on PostgreSQL",
                    address = "Hà Nội",
                    latitude = 21.005,
                    longitude = 105.843,
                    basePrice = 3200000,
                    area = 25,
                    amenities = new[] { "wifi" },
                    imageUrls = new[]
                    {
                        "https://example.com/room.jpg"
                    }
                });

            created.EnsureSuccessStatusCode();

            // 12. Lấy danh sách phòng và kiểm tra dữ liệu.
            var response = await client.GetStringAsync(
                "/api/rooms?maxPrice=4000000&sort=price_asc");

            using var resultDocument = JsonDocument.Parse(response);

            var result = resultDocument.RootElement
                .GetProperty("data");

            Assert.Equal(
                1,
                result.GetProperty("total").GetInt32());

            Assert.Equal(
                "wifi",
                result.GetProperty("items")[0]
                    .GetProperty("amenities")[0]
                    .GetString());
        }
        finally
        {
            // 13. Đóng các connection pool trước khi xóa schema.
            NpgsqlConnection.ClearAllPools();

            // 14. Xóa schema kiểm thử.
            await using var drop = new NpgsqlCommand(
                $"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE",
                admin);

            await drop.ExecuteNonQueryAsync();
        }
    }
}
