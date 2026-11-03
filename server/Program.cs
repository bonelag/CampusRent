
using System.Text;
using System.Threading.RateLimiting;
using CampusRent.Server.BackgroundServices;
using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Kiểm tra tình trạng hoạt động của ứng dụng.
if (args.Contains("--healthcheck"))
{
    try
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        var response = await client.GetAsync("http://127.0.0.1:8080/health");
        Environment.ExitCode = response.IsSuccessStatusCode ? 0 : 1;
    }
    catch
    {
        Environment.ExitCode = 1;
    }

    return;
}

// Tạo WebApplication Builder.
var builder = WebApplication.CreateBuilder(
    args.Where(a => a != "--migrate").ToArray()
);

var development =
    builder.Environment.IsDevelopment() ||
    builder.Environment.IsEnvironment("Testing");

// Kiểm tra JWT Secret.
var secret = builder.Configuration["Jwt:Secret"];

if (string.IsNullOrWhiteSpace(secret) ||
    Encoding.UTF8.GetByteCount(secret) < 32 ||
    secret.StartsWith("CHANGE_ME"))
{
    if (!development)
    {
        throw new InvalidOperationException(
            "Set Jwt__Secret to a random secret of at least 32 bytes."
        );
    }

    secret = "CampusRent-local-development-only-secret-2026";
    builder.Configuration["Jwt:Secret"] = secret;
}

// Đăng ký Controllers.
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        )
    );

// Cấu hình Database.
builder.Services.AddDbContext<CampusRentDbContext>(options =>
{
    if (builder.Configuration["Database:Provider"] == "Postgres")
    {
        var rawConn =
            builder.Configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "";

        options.UseNpgsql(
            NormalizePostgresConnectionString(rawConn)
        );
    }
    else
    {
        if (!development)
        {
            throw new InvalidOperationException(
                "Production requires Database__Provider=Postgres."
            );
        }

        options.UseSqlite(
            builder.Configuration.GetConnectionString("Sqlite")
            ?? "Data Source=campusrent.db"
        );
    }
});

// Cấu hình xác thực JWT.
builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme
).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secret)
        ),

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization();

// Cấu hình CORS.
builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
        p.WithOrigins(
            builder.Configuration["ClientUrl"] ?? "http://localhost:5173"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
    )
);

// Cấu hình giới hạn số lượng request.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;

    o.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }
        )
    );

    o.AddPolicy("places", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }
        )
    );
});

// Đăng ký các dịch vụ.
builder.Services.AddMemoryCache();

// Google Places.
builder.Services.AddHttpClient<IGooglePlacesClient, GooglePlacesClient>(
    client =>
    {
        client.BaseAddress = new Uri(
            "https://places.googleapis.com/v1/"
        );

        client.Timeout = TimeSpan.FromSeconds(5);
    }
);

// OpenStreetMap / Photon.
builder.Services.AddHttpClient<IOsmPlacesClient, OsmPlacesClient>(
    client =>
    {
        client.BaseAddress = new Uri("https://photon.komoot.io/");
        client.Timeout = TimeSpan.FromSeconds(5);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "CampusRent/1.0 (contact@campusrent.vn)"
        );
    }
);

builder.Services.AddSingleton<IHaversineService, HaversineService>();
builder.Services.AddSingleton<ILivingCostService, LivingCostService>();
builder.Services.AddSingleton<IProfanityFilterService, ProfanityFilterService>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IPlaceSearchService, PlaceSearchService>();

builder.Services.AddHostedService<AutoExpireRoomsWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Xây dựng ứng dụng.
var app = builder.Build();

// Middleware xử lý lỗi và bảo mật HTTP.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] =
        "strict-origin-when-cross-origin";

    if (!development)
    {
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' https: data: blob:; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
    }

    try
    {
        await next();
    }
    catch (Exception ex) when (!context.Response.HasStarted)
    {
        app.Logger.LogError(
            ex,
            "Request failed: {TraceId}",
            context.TraceIdentifier
        );

        context.Response.StatusCode = 500;

        await context.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(
                "Có lỗi hệ thống. Vui lòng thử lại."
            )
        );
    }
});

if (!development)
{
    app.UseHsts();
}

if (development)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Cấu hình Static Files.
app.UseDefaultFiles();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
        context.Context.Response.Headers.CacheControl =
            context.Context.Request.Path.StartsWithSegments("/assets")
                ? "public,max-age=31536000,immutable"
                : "no-cache"
});

// Cấu hình thư mục chứa ảnh tải lên.
var uploadsDir = Path.Combine(
    builder.Environment.ContentRootPath,
    "uploads"
);

if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider =
        new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
            uploadsDir
        ),
    RequestPath = "/uploads"
});

app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Đăng ký API endpoints.
app.MapControllers();

app.MapGet("/health", async (CampusRentDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "healthy" })
        : Results.StatusCode(503)
);

app.Map(
    "/api/{**path}",
    () => Results.NotFound(
        ApiResponse<object>.Fail("API không tồn tại.")
    )
);

app.MapFallbackToFile("index.html");

// Khởi tạo Database và dữ liệu.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<CampusRentDbContext>();

    // Development và Testing cần chuẩn bị Database.
    if (development)
    {
        if (db.Database.IsNpgsql())
        {
            // PostgreSQL:
            // Không tự chạy Migration trong Testing.
            // PostgresTests sẽ tự quản lý Migration.

            if (!builder.Environment.IsEnvironment("Testing"))
            {
                await db.Database.MigrateAsync();
            }
        }
        else
        {
            // SQLite vẫn cần tạo bảng cho ApiTests.
            await db.Database.EnsureCreatedAsync();

            // Tạo bảng landlord_requests nếu chưa tồn tại.
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""landlord_requests"" (
                    ""id"" TEXT NOT NULL CONSTRAINT ""PK_landlord_requests"" PRIMARY KEY,
                    ""user_id"" TEXT NOT NULL,
                    ""title"" TEXT NOT NULL,
                    ""reason"" TEXT NOT NULL,
                    ""image_urls"" TEXT NOT NULL,
                    ""pdf_url"" TEXT NULL,
                    ""pdf_file_name"" TEXT NULL,
                    ""status"" TEXT NOT NULL,
                    ""admin_note"" TEXT NULL,
                    ""created_at"" TEXT NOT NULL,
                    ""updated_at"" TEXT NOT NULL,
                    CONSTRAINT ""FK_landlord_requests_users_user_id""
                        FOREIGN KEY (""user_id"")
                        REFERENCES ""users"" (""id"")
                        ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS
                    ""IX_landlord_requests_status""
                    ON ""landlord_requests"" (""status"");

                CREATE INDEX IF NOT EXISTS
                    ""IX_landlord_requests_user_id""
                    ON ""landlord_requests"" (""user_id"");
            ");
        }
    }

    // Cho phép chạy Migration thủ công bằng --migrate.
    if (args.Contains("--migrate"))
    {
        return;
    }

    // Tạo dữ liệu mẫu trong Development.
    if (builder.Environment.IsDevelopment())
    {
        await DbSeeder.SeedAsync(db);
    }

    // Tạo dữ liệu tham chiếu trong Production.
    if (!development)
    {
        await DbSeeder.SeedReferenceAsync(db);
    }

    // Tạo tài khoản Admin nếu chưa tồn tại.
    var adminEmail = builder.Configuration["Admin:Email"];
    var adminPassword = builder.Configuration["Admin:Password"];

    if (!development &&
        !string.IsNullOrWhiteSpace(adminEmail) &&
        !string.IsNullOrWhiteSpace(adminPassword))
    {
        if (adminPassword.Length < 12 ||
            adminPassword.StartsWith("CHANGE_ME"))
        {
            throw new InvalidOperationException(
                "Set a unique admin password of at least 12 characters."
            );
        }

        adminEmail = adminEmail.Trim().ToLowerInvariant();

        if (!await db.Users.AnyAsync(u => u.Email == adminEmail))
        {
            db.Users.Add(new()
            {
                Email = adminEmail,
                FullName = "Quản trị viên",
                Role = CampusRent.Server.Models.Role.ADMIN,
                Password = BCrypt.Net.BCrypt.HashPassword(adminPassword)
            });

            await db.SaveChangesAsync();
        }
    }
}

// Chạy ứng dụng.
app.Run();

// Chuẩn hóa chuỗi kết nối PostgreSQL.
static string NormalizePostgresConnectionString(string connStr)
{
    if (string.IsNullOrWhiteSpace(connStr))
    {
        return connStr;
    }

    connStr = connStr.Trim();

    if (connStr.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        connStr.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var uri = new Uri(connStr);
            var userInfo = uri.UserInfo.Split(':');

            var username = Uri.UnescapeDataString(userInfo[0]);

            var password = userInfo.Length > 1
                ? Uri.UnescapeDataString(userInfo[1])
                : "";

            var database = uri.AbsolutePath.TrimStart('/');

            if (string.IsNullOrWhiteSpace(database))
            {
                database = "postgres";
            }

            var port = uri.Port > 0 ? uri.Port : 5432;
            var host = uri.Host;

            if (host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase) &&
                host.StartsWith("db.", StringComparison.OrdinalIgnoreCase))
            {
                var projectRef = host.Substring(
                    3,
                    host.IndexOf(
                        ".supabase.co",
                        StringComparison.OrdinalIgnoreCase
                    ) - 3
                );

                var isIpv4Resolved = false;

                try
                {
                    var entry = System.Net.Dns.GetHostEntry(host);

                    isIpv4Resolved = entry.AddressList.Any(
                        ip => ip.AddressFamily ==
                              System.Net.Sockets.AddressFamily.InterNetwork
                    );
                }
                catch
                {
                }

                if (!isIpv4Resolved)
                {
                    host = "aws-0-ap-south-1.pooler.supabase.com";

                    if (!username.Contains('.'))
                    {
                        username = $"{username}.{projectRef}";
                    }
                }
            }

            return
                $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
        }
        catch
        {
            return connStr;
        }
    }

    if (!connStr.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase))
    {
        connStr += ";SSL Mode=Require;Trust Server Certificate=true;";
    }

    return connStr;
}

// Giúp WebApplicationFactory truy cập Program trong kiểm thử.
public partial class Program { }
