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

public class FeaturesCoverageTests : IDisposable
{
    private readonly ApiFactory factory = new();
    private readonly HttpClient client;

    public FeaturesCoverageTests()
    {
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    private async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.GetProperty("data").Clone();
    }

    private async Task<Guid> Register(string role = "STUDENT", string? email = null)
    {
        var data = await Data(await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = email ?? $"{Guid.NewGuid()}@example.com",
            password = "TestPass123!",
            fullName = "Test User",
            role
        }));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.GetProperty("token").GetString());
        return data.GetProperty("user").GetProperty("id").GetGuid();
    }

    private async Task Admin()
    {
        var id = await Register("LANDLORD");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
        var user = (await db.Users.FindAsync(id))!;
        user.Role = Role.ADMIN;
        await db.SaveChangesAsync();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().GenerateToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private object RoomPayload(object? overrides = null) => new
    {
        title = "Phòng trọ cao cấp Hai Bà Trưng",
        description = "Phòng khép kín đầy đủ đồ",
        address = "15 Tạ Quang Bửu, Hà Nội",
        latitude = 21.005,
        longitude = 105.843,
        basePrice = 3000000,
        electricPrice = 4000,
        waterPrice = 30000,
        internetPrice = 100000,
        otherFee = 50000,
        area = 28,
        maxPeople = 2,
        amenities = new[] { "wifi", "ac", "kitchen" },
        imageUrls = new[] { "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800" }
    };

    private async Task<Guid> CreateRoom(decimal basePrice = 3000000, string[]? amenities = null, string? title = null)
    {
        await Register("LANDLORD");
        var payload = new
        {
            title = title ?? "Phòng trọ cao cấp Hai Bà Trưng",
            description = "Phòng khép kín đầy đủ đồ",
            address = "15 Tạ Quang Bửu, Hà Nội",
            latitude = 21.005,
            longitude = 105.843,
            basePrice,
            electricPrice = 4000,
            waterPrice = 30000,
            internetPrice = 100000,
            otherFee = 50000,
            area = 28,
            maxPeople = 2,
            amenities = amenities ?? new[] { "wifi", "ac" },
            imageUrls = new[] { "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800" }
        };
        var data = await Data(await client.PostAsJsonAsync("/api/rooms", payload));
        return data.GetProperty("id").GetGuid();
    }

    // ==========================================
    // 1. LANDLORD REQUESTS WORKFLOW
    // ==========================================

    [Fact]
    public async Task StudentCanCancelPendingLandlordUpgradeRequest()
    {
        await Register("STUDENT");
        var reqPayload = new
        {
            title = "Yêu cầu nâng cấp tài khoản",
            reason = "Có nhà trọ cho sinh viên thuê",
            imageUrls = new[] { "https://example.com/landlord1.jpg" }
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
        var created = await Data(await client.PostAsJsonAsync("/api/landlord-requests", new
        {
            title = "Yêu cầu của A",
            reason = "Lý do của A",
            imageUrls = new string[0]
        }));
        var reqId = created.GetProperty("id").GetGuid();

        // Switch to Student B
        await Register("STUDENT", "student_b@example.com");
        var cancelRes = await client.DeleteAsync($"/api/landlord-requests/{reqId}");
        Assert.Equal(HttpStatusCode.NotFound, cancelRes.StatusCode);
    }

    [Fact]
    public async Task ExistingLandlordCannotSubmitUpgradeRequest()
    {
        await Register("LANDLORD");
        var res = await client.PostAsJsonAsync("/api/landlord-requests", new
        {
            title = "Yêu cầu",
            reason = "Muốn nâng cấp",
            imageUrls = new string[0]
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ==========================================
    // 2. ROOM MANAGEMENT & ADVANCED SEARCH
    // ==========================================

    [Fact]
    public async Task LandlordCanViewOwnRoomsViaMineEndpoint()
    {
        var roomId = await CreateRoom();
        var myRooms = await Data(await client.GetAsync("/api/rooms/mine"));
        Assert.Contains(myRooms.EnumerateArray(), r => r.GetProperty("id").GetGuid() == roomId);

        // Another landlord has zero rooms
        await Register("LANDLORD", "landlord_two@example.com");
        var otherRooms = await Data(await client.GetAsync("/api/rooms/mine"));
        Assert.Empty(otherRooms.EnumerateArray());
    }

    [Fact]
    public async Task RoomSearchSortingByPriceAscendingAndDescending()
    {
        await CreateRoom(basePrice: 1500000, title: "Phòng giá rẻ");
        await CreateRoom(basePrice: 4500000, title: "Phòng cao cấp");

        client.DefaultRequestHeaders.Authorization = null;

        var ascRes = await Data(await client.GetAsync("/api/rooms?sort=price_asc"));
        var ascItems = ascRes.GetProperty("items").EnumerateArray().ToList();
        Assert.True(ascItems.Count >= 2);
        Assert.True(ascItems[0].GetProperty("basePrice").GetDecimal() <= ascItems[1].GetProperty("basePrice").GetDecimal());

        var descRes = await Data(await client.GetAsync("/api/rooms?sort=price_desc"));
        var descItems = descRes.GetProperty("items").EnumerateArray().ToList();
        Assert.True(descItems.Count >= 2);
        Assert.True(descItems[0].GetProperty("basePrice").GetDecimal() >= descItems[1].GetProperty("basePrice").GetDecimal());
    }

    [Fact]
    public async Task RoomSearchFilterByAmenity()
    {
        var roomId = await CreateRoom(amenities: new[] { "wifi", "ac", "kitchen" }, title: "Phòng có bếp riêng");
        client.DefaultRequestHeaders.Authorization = null;

        var withKitchen = await Data(await client.GetAsync("/api/rooms?amenity=kitchen"));
        Assert.Contains(withKitchen.GetProperty("items").EnumerateArray(), r => r.GetProperty("id").GetGuid() == roomId);

        var withPool = await Data(await client.GetAsync("/api/rooms?amenity=swimming_pool"));
        Assert.DoesNotContain(withPool.GetProperty("items").EnumerateArray(), r => r.GetProperty("id").GetGuid() == roomId);
    }

    [Fact]
    public async Task RoomSearchFilterByKeywordInTitleOrAddress()
    {
        var roomId = await CreateRoom(title: "Chung cư mini vip Hoàng Mai");
        client.DefaultRequestHeaders.Authorization = null;

        var found = await Data(await client.GetAsync("/api/rooms?keyword=Ho%C3%A0ng%20Mai"));
        Assert.Contains(found.GetProperty("items").EnumerateArray(), r => r.GetProperty("id").GetGuid() == roomId);

        var notFound = await Data(await client.GetAsync("/api/rooms?keyword=KhongCoPhongNayDau999"));
        Assert.Equal(0, notFound.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task LandlordCanChangeRoomStatusToRentedAndRenew()
    {
        var roomId = await CreateRoom();
        var landlordToken = client.DefaultRequestHeaders.Authorization;

        // Change to RENTED
        var rentedRes = await Data(await client.PatchAsJsonAsync($"/api/rooms/{roomId}/status", new { status = "RENTED" }));
        Assert.Equal("RENTED", rentedRes.GetProperty("status").GetString());

        // Disappears from public search
        client.DefaultRequestHeaders.Authorization = null;
        var search = await Data(await client.GetAsync("/api/rooms"));
        Assert.DoesNotContain(search.GetProperty("items").EnumerateArray(), r => r.GetProperty("id").GetGuid() == roomId);

        // Landlord re-authenticates and renews
        client.DefaultRequestHeaders.Authorization = landlordToken;
        var renewedRes = await Data(await client.PatchAsJsonAsync($"/api/rooms/{roomId}/status", new { status = "AVAILABLE", renew = true }));
        Assert.Equal("AVAILABLE", renewedRes.GetProperty("status").GetString());
        var expiresAt = renewedRes.GetProperty("expiresAt").GetDateTime();
        Assert.True(expiresAt > DateTime.UtcNow.AddDays(25));
    }

    [Fact]
    public async Task OwnerCanViewTheirOwnHiddenRoomDetailWhilePublicCannot()
    {
        var roomId = await CreateRoom();
        var landlordToken = client.DefaultRequestHeaders.Authorization;

        // Hide room
        await client.DeleteAsync($"/api/rooms/{roomId}");

        // Public/Student gets 404
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/rooms/{roomId}")).StatusCode);

        // Owner gets 200 OK
        client.DefaultRequestHeaders.Authorization = landlordToken;
        var detailRes = await client.GetAsync($"/api/rooms/{roomId}");
        Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);
    }

    [Fact]
    public async Task CreateRoomValidationRejectsInvalidInputs()
    {
        await Register("LANDLORD");

        // Non-existent university
        var p1 = JsonSerializer.SerializeToNode(RoomPayload())!;
        p1["universityId"] = Guid.NewGuid().ToString();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rooms", p1)).StatusCode);

        // Empty images
        var p2 = JsonSerializer.SerializeToNode(RoomPayload())!;
        p2["imageUrls"] = new System.Text.Json.Nodes.JsonArray();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rooms", p2)).StatusCode);

        // HTTP image (not HTTPS)
        var p3 = JsonSerializer.SerializeToNode(RoomPayload())!;
        p3["imageUrls"] = new System.Text.Json.Nodes.JsonArray("http://insecure.example.com/img.jpg");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rooms", p3)).StatusCode);

        // More than 10 images
        var p4 = JsonSerializer.SerializeToNode(RoomPayload())!;
        var arr = new System.Text.Json.Nodes.JsonArray();
        for (int i = 0; i < 11; i++) arr.Add($"https://example.com/img{i}.jpg");
        p4["imageUrls"] = arr;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rooms", p4)).StatusCode);

        // Too many amenities (> 20)
        var p5 = JsonSerializer.SerializeToNode(RoomPayload())!;
        var amArr = new System.Text.Json.Nodes.JsonArray();
        for (int i = 0; i < 21; i++) amArr.Add($"amenity_{i}");
        p5["amenities"] = amArr;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rooms", p5)).StatusCode);
    }

    // ==========================================
    // 3. BOOKINGS & ACTIVITIES
    // ==========================================

    [Fact]
    public async Task StudentCannotBookOwnRoom()
    {
        var roomId = await CreateRoom();
        var res = await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new
        {
            visitAt = DateTimeOffset.UtcNow.AddDays(2),
            note = "Tự đặt phòng của mình"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task BookingValidationRejectsInvalidDates()
    {
        var roomId = await CreateRoom();
        await Register("STUDENT");

        // Past or less than 1 hour in future
        var pastRes = await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new
        {
            visitAt = DateTimeOffset.UtcNow.AddMinutes(30)
        });
        Assert.Equal(HttpStatusCode.BadRequest, pastRes.StatusCode);

        // More than 30 days in future
        var futureRes = await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new
        {
            visitAt = DateTimeOffset.UtcNow.AddDays(35)
        });
        Assert.Equal(HttpStatusCode.BadRequest, futureRes.StatusCode);

        // Booking non-existent room
        var notFoundRes = await client.PostAsJsonAsync($"/api/rooms/{Guid.NewGuid()}/bookings", new
        {
            visitAt = DateTimeOffset.UtcNow.AddDays(2)
        });
        Assert.Equal(HttpStatusCode.NotFound, notFoundRes.StatusCode);
    }

    [Fact]
    public async Task StudentCanCancelTheirOwnBooking()
    {
        var roomId = await CreateRoom();
        await Register("STUDENT");

        var booking = await Data(await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new
        {
            visitAt = DateTimeOffset.UtcNow.AddDays(3),
            note = "Hẹn xem phòng"
        }));
        var bookingId = booking.GetProperty("id").GetGuid();

        var cancelRes = await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "CANCELLED" });
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);

        // Cannot update cancelled booking
        var reUpdate = await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "CONFIRMED" });
        Assert.Equal(HttpStatusCode.BadRequest, reUpdate.StatusCode);
    }

    [Fact]
    public async Task InvalidBookingStatusIsRejected()
    {
        var roomId = await CreateRoom();
        await Register("STUDENT");

        var booking = await Data(await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new
        {
            visitAt = DateTimeOffset.UtcNow.AddDays(3)
        }));
        var bookingId = booking.GetProperty("id").GetGuid();

        var res = await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "INVALID_STATUS" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task FavoriteNonExistentRoomReturnsNotFound()
    {
        await Register("STUDENT");
        var res = await client.PutAsync($"/api/favorites/{Guid.NewGuid()}", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ==========================================
    // 4. REVIEWS & RATINGS
    // ==========================================

    [Fact]
    public async Task ReviewingNonExistentOrHiddenRoomReturnsNotFound()
    {
        await Register("STUDENT");
        var notFound = await client.PostAsJsonAsync($"/api/rooms/{Guid.NewGuid()}/reviews", new
        {
            rating = 5,
            comment = "Phòng ảo"
        });
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);

        var roomId = await CreateRoom();
        await client.DeleteAsync($"/api/rooms/{roomId}"); // Hidden

        await Register("STUDENT", "student_reviewer@example.com");
        var hiddenReview = await client.PostAsJsonAsync($"/api/rooms/{roomId}/reviews", new
        {
            rating = 5,
            comment = "Phòng đã ẩn"
        });
        Assert.Equal(HttpStatusCode.NotFound, hiddenReview.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public async Task ReviewRatingOutOfRangeIsRejected(int rating)
    {
        var roomId = await CreateRoom();
        await Register("STUDENT");

        var res = await client.PostAsJsonAsync($"/api/rooms/{roomId}/reviews", new
        {
            rating,
            comment = "Đánh giá thử nghiệm"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CleanReviewsComputeAverageRatingAndReviewCount()
    {
        var roomId = await CreateRoom();

        // Student 1 leaves 4 stars
        await Register("STUDENT", "student_one@example.com");
        await client.PostAsJsonAsync($"/api/rooms/{roomId}/reviews", new { rating = 4, comment = "Phòng khá đẹp và sạch sẽ" });

        // Student 2 leaves 5 stars
        await Register("STUDENT", "student_two@example.com");
        await client.PostAsJsonAsync($"/api/rooms/{roomId}/reviews", new { rating = 5, comment = "Chủ trọ nhiệt tình chu đáo" });

        client.DefaultRequestHeaders.Authorization = null;
        var detail = await Data(await client.GetAsync($"/api/rooms/{roomId}"));
        Assert.Equal(2, detail.GetProperty("reviewCount").GetInt32());
        Assert.Equal(4.5, detail.GetProperty("averageRating").GetDouble());
    }

    // ==========================================
    // 5. REPORTS & ADMIN MODERATION
    // ==========================================

    [Fact]
    public async Task ReportRejectsBothOrNeitherTargets()
    {
        var roomId = await CreateRoom();
        await Register("STUDENT");

        // Neither
        var neitherRes = await client.PostAsJsonAsync("/api/reports", new { reason = "Lý do chung chung" });
        Assert.Equal(HttpStatusCode.BadRequest, neitherRes.StatusCode);

        // Both
        var bothRes = await client.PostAsJsonAsync("/api/reports", new
        {
            roomId,
            reviewId = Guid.NewGuid(),
            reason = "Báo cáo cả hai"
        });
        Assert.Equal(HttpStatusCode.BadRequest, bothRes.StatusCode);
    }

    [Fact]
    public async Task AdminDismissesReportWithoutModifyingTarget()
    {
        var roomId = await CreateRoom();
        await Register("STUDENT");
        var report = await Data(await client.PostAsJsonAsync("/api/reports", new { roomId, reason = "Báo cáo nhầm" }));
        var reportId = report.GetProperty("id").GetGuid();

        await Admin();
        var resolveRes = await client.PatchAsJsonAsync($"/api/admin/reports/{reportId}", new
        {
            status = "DISMISSED",
            deleteTarget = false
        });
        Assert.Equal(HttpStatusCode.OK, resolveRes.StatusCode);

        // Room is still available
        client.DefaultRequestHeaders.Authorization = null;
        var roomRes = await client.GetAsync($"/api/rooms/{roomId}");
        Assert.Equal(HttpStatusCode.OK, roomRes.StatusCode);
    }

    [Fact]
    public async Task AdminSearchUsersByQueryAndValidation()
    {
        await Register("STUDENT", "nguyen_van_a@example.com");
        await Admin();

        var searchFound = await Data(await client.GetAsync("/api/admin/users?q=nguyen_van_a"));
        Assert.Single(searchFound.EnumerateArray());

        var searchEmpty = await Data(await client.GetAsync("/api/admin/users?q=khong_co_ai_dau_nhe"));
        Assert.Empty(searchEmpty.EnumerateArray());

        // Update non-existent user
        var notFoundUpdate = await client.PutAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}", new
        {
            fullName = "Tên",
            role = "STUDENT",
            isEduVerified = false
        });
        Assert.Equal(HttpStatusCode.NotFound, notFoundUpdate.StatusCode);

        // Update with empty name
        var invalidUpdate = await client.PutAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}", new
        {
            fullName = "",
            role = "STUDENT",
            isEduVerified = false
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidUpdate.StatusCode);
    }

    [Fact]
    public async Task NonAdminCannotAccessAdminEndpoints()
    {
        await Register("STUDENT");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/reports")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/landlord-requests")).StatusCode);

        await Register("LANDLORD");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/reviews")).StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/stats")).StatusCode);
    }

    // ==========================================
    // 6. UNIVERSITIES API
    // ==========================================

    [Fact]
    public async Task UniversitiesGetAllAndAccessControl()
    {
        // Seed university via Admin first
        await Admin();
        await client.PostAsJsonAsync("/api/universities", new
        {
            name = "Đại học Bách Khoa Hà Nội",
            shortName = "HUST",
            address = "1 Đại Cồ Việt, Hai Bà Trưng, Hà Nội",
            latitude = 21.004,
            longitude = 105.843
        });

        client.DefaultRequestHeaders.Authorization = null;
        var list = await Data(await client.GetAsync("/api/universities"));
        Assert.NotEmpty(list.EnumerateArray());

        // Student cannot create university
        await Register("STUDENT");
        var res = await client.PostAsJsonAsync("/api/universities", new
        {
            name = "Đại học mới",
            shortName = "DHMOI",
            address = "Hà Nội",
            latitude = 21.0,
            longitude = 105.8
        });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ==========================================
    // 7. FILE UPLOADS
    // ==========================================

    [Fact]
    public async Task UploadPdfSucceedsWithValidPdfHeader()
    {
        await Register("STUDENT");
        using var content = new MultipartFormDataContent();
        var pdfBytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF");
        content.Add(new ByteArrayContent(pdfBytes), "file", "giay_to_nha_dat.pdf");

        var response = await client.PostAsync("/api/uploads/pdf", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await Data(response);
        Assert.EndsWith(".pdf", data.GetProperty("url").GetString());
        Assert.Equal("giay_to_nha_dat.pdf", data.GetProperty("fileName").GetString());
    }

    [Fact]
    public async Task UploadPdfRejectsNonPdfExtensions()
    {
        await Register("STUDENT");
        using var content = new MultipartFormDataContent();
        var pdfBytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 fake header");
        content.Add(new ByteArrayContent(pdfBytes), "file", "malicious.exe");

        var response = await client.PostAsync("/api/uploads/pdf", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadImageFallbackToLocalOnValidPng()
    {
        await Register("LANDLORD");
        using var content = new MultipartFormDataContent();
        var pngHeader = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0 };
        content.Add(new ByteArrayContent(pngHeader), "file", "room_test.png");

        var response = await client.PostAsync("/api/uploads/image", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await Data(response);
        var url = data.GetProperty("url").GetString();
        Assert.NotNull(url);
        Assert.StartsWith("/uploads/images/", url);
        Assert.EndsWith(".png", url);
    }

    [Fact]
    public async Task UploadImageRejectsCorruptBytes()
    {
        await Register("LANDLORD");
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 }), "file", "corrupt.jpg");

        var response = await client.PostAsync("/api/uploads/image", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUserCannotUpload()
    {
        client.DefaultRequestHeaders.Authorization = null;
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 1, 2 }), "file", "test.pdf");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/uploads/pdf", content)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/uploads/image", content)).StatusCode);
    }

    // ==========================================
    // 8. PLACES SERVICE TESTS
    // ==========================================

    [Fact]
    public async Task OsmPlacesClientHandlesEmptyQueryAndErrorResponses()
    {
        var handler = new StubOsmHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://photon.komoot.io/") };
        var sut = new OsmPlacesClient(httpClient, NullLogger<OsmPlacesClient>.Instance);

        // Empty query
        var empty = await sut.SuggestAsync("", CancellationToken.None);
        Assert.Empty(empty);

        // Short query
        var shortQ = await sut.SuggestAsync("a", CancellationToken.None);
        Assert.Empty(shortQ);

        // Normal query
        var suggestions = await sut.SuggestAsync("Hai Bà Trưng", CancellationToken.None);
        Assert.Single(suggestions);
        Assert.Equal("Hai Bà Trưng", suggestions[0].Title);
        Assert.Equal(21.01, suggestions[0].Latitude);
        Assert.Equal(105.85, suggestions[0].Longitude);

        // Reverse geocoding
        var reverse = await sut.ReverseGeocodeAsync(21.01, 105.85, CancellationToken.None);
        Assert.NotNull(reverse);
        Assert.Contains("Hai Bà Trưng", reverse);
    }

    private sealed class StubOsmHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("reverse"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"features":[{"properties":{"name":"Hai Bà Trưng","city":"Hà Nội"}}]}""", System.Text.Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"features":[{"properties":{"name":"Hai Bà Trưng","city":"Hà Nội","district":"Hai Bà Trưng"},"geometry":{"coordinates":[105.85,21.01]}}]}""", System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
