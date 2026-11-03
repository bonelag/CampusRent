
using System.Collections.Generic;
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
using Xunit.Abstractions;

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
    private readonly ITestOutputHelper _output;

    public PostgresTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [PostgresFact]
    public async Task MigrationsAreRepeatableAndRentalWorkflowPersistsInPostgres()
    {
        // 1. Lấy chuỗi kết nối PostgreSQL.
        var connection =
            Environment.GetEnvironmentVariable("CAMPUSRENT_TEST_POSTGRES")!;

        // 2. Cấu hình kết nối.
        var testConnection = new NpgsqlConnectionStringBuilder(connection)
        {
            SslMode = SslMode.Disable
        };

        // 3. Tạo schema riêng cho bài kiểm thử.
        var schema = "campusrent_test_" + Guid.NewGuid().ToString("N");

        // 4. Kết nối database quản trị.
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
            // 5. Thiết lập SearchPath cho schema kiểm thử.
            var options = new NpgsqlConnectionStringBuilder(
                testConnection.ConnectionString)
            {
                SearchPath = schema
            };

            _output.WriteLine("========== POSTGRES TEST ==========");
            _output.WriteLine($"TEST SCHEMA: {schema}");
            _output.WriteLine($"CONNECTION DATABASE: {options.Database}");
            _output.WriteLine($"CONNECTION HOST: {options.Host}");
            _output.WriteLine($"CONNECTION SEARCH PATH: {options.SearchPath}");

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

            // 7. Kiểm tra môi trường ứng dụng.
            var environment = factory.Services
                .GetRequiredService<IWebHostEnvironment>();

            _output.WriteLine(
                $"APPLICATION ENVIRONMENT: {environment.EnvironmentName}");

            Assert.Equal("Testing", environment.EnvironmentName);

            using var client = factory.CreateClient();

            // 8. Kiểm tra và chạy migration.
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<CampusRentDbContext>();

                var dbConnection = db.Database.GetDbConnection();

                _output.WriteLine($"EF DATABASE: {dbConnection.Database}");
                _output.WriteLine($"EF DATA SOURCE: {dbConnection.DataSource}");

                // Kiểm tra migration trước khi chạy.
                var pendingBefore =
                    (await db.Database.GetPendingMigrationsAsync()).ToArray();

                _output.WriteLine(
                    "PENDING BEFORE: " + string.Join(", ", pendingBefore));

                Assert.NotEmpty(pendingBefore);

                // Chạy migration.
                await db.Database.MigrateAsync();

                // 9. Kiểm tra schema PostgreSQL thực tế.
                await db.Database.OpenConnectionAsync();

                try
                {
                    await using var command = dbConnection.CreateCommand();

                    command.CommandText = """
                        SELECT
                            current_database(),
                            current_schema(),
                            current_setting('search_path'),
                            to_regclass('"__EFMigrationsHistory"')::text;
                        """;

                    await using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        _output.WriteLine(
                            $"CURRENT DATABASE: {reader.GetString(0)}");

                        _output.WriteLine(
                            $"CURRENT SCHEMA: " +
                            (reader.IsDBNull(1)
                                ? "NULL"
                                : reader.GetString(1)));

                        _output.WriteLine(
                            $"SEARCH PATH: {reader.GetString(2)}");

                        _output.WriteLine(
                            $"MIGRATION HISTORY TABLE: " +
                            (reader.IsDBNull(3)
                                ? "NOT FOUND"
                                : reader.GetString(3)));
                    }
                }
                finally
                {
                    await db.Database.CloseConnectionAsync();
                }

                // 10. Kiểm tra migration đã áp dụng.
                var applied =
                    (await db.Database.GetAppliedMigrationsAsync()).ToArray();

                _output.WriteLine(
                    "APPLIED MIGRATIONS: " + string.Join(", ", applied));

                // 11. Kiểm tra migration còn chờ.
                var pendingAfter =
                    (await db.Database.GetPendingMigrationsAsync()).ToArray();

                _output.WriteLine(
                    "PENDING AFTER: " + string.Join(", ", pendingAfter));

                _output.WriteLine("========== END MIGRATION DEBUG ==========");

                Assert.Empty(pendingAfter);

                // 12. Chạy migration lần thứ hai.
                await db.Database.MigrateAsync();

                // 13. Tạo dữ liệu tham chiếu.
                await DbSeeder.SeedReferenceAsync(db);

                Assert.Equal(5, await db.Universities.CountAsync());
            }

            // 14. Đăng ký tài khoản chủ trọ.
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

            // 15. Lấy JWT token.
            using var authDocument = JsonDocument.Parse(
                await registered.Content.ReadAsStringAsync());

            var auth = authDocument.RootElement.GetProperty("data");

            var token = auth.GetProperty("token").GetString();

            Assert.False(string.IsNullOrWhiteSpace(token));

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 16. Tạo phòng trọ.
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

            // 17. Lấy danh sách phòng và kiểm tra.
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
            // 18. Xóa connection pool và schema kiểm thử.
            NpgsqlConnection.ClearAllPools();

            await using var drop = new NpgsqlCommand(
                $"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE",
                admin);

            await drop.ExecuteNonQueryAsync();
        }
    }
}
