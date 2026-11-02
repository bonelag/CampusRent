
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
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection")
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

builder.Services.AddHttpClient<IGooglePlacesClient, GooglePlacesClient>(
    client =>
    {
        client.BaseAddress = new Uri(
            "https://places.googleapis.com/v1/"
        );

        client.Timeout = TimeSpan.FromSeconds(5);
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

// Cấu hình Static Files và HTTP Pipeline.
app.UseDefaultFiles();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
        context.Context.Response.Headers.CacheControl =
            context.Context.Request.Path.StartsWithSegments("/assets")
                ? "public,max-age=31536000,immutable"
                : "no-cache"
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
        }
    }

    // Cho phép chạy Migration thủ công bằng --migrate.
    if (args.Contains("--migrate"))
    {
        await db.Database.MigrateAsync();
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

public partial class Program { }
