using System.Security.Cryptography;
using CampusRent.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusRent.Server.Data;

public sealed record MarketSampleImportResult(
    int Users,
    int Universities,
    int Rooms,
    int RoomImages,
    int Reviews,
    int Reports,
    int Bookings,
    int Favorites,
    int LandlordRequests)
{
    public const int ExpectedPerTable = 20;

    public bool IsComplete =>
        Users == ExpectedPerTable && Universities == ExpectedPerTable && Rooms == ExpectedPerTable &&
        RoomImages == ExpectedPerTable && Reviews == ExpectedPerTable && Reports == ExpectedPerTable &&
        Bookings == ExpectedPerTable && Favorites == ExpectedPerTable && LandlordRequests == ExpectedPerTable;
}

public static class MarketSampleDataImporter
{
    private const int SampleCount = MarketSampleImportResult.ExpectedPerTable;

    private sealed record UniversitySeed(string Name, string ShortName, string Address, double Latitude, double Longitude);
    private sealed record RoomSeed(
        string Source,
        string Title,
        string Description,
        string Address,
        double Latitude,
        double Longitude,
        decimal Price,
        double Area,
        string ImageUrl,
        string SourceUrl,
        int UniversityIndex);

    private static readonly UniversitySeed[] UniversitySeeds =
    [
        new("Đại học Ngoại thương", "FTU-SAMPLE", "91 Chùa Láng, Đống Đa, Hà Nội", 21.0225029, 105.8051480),
        new("Học viện Ngân hàng", "HVNH-SAMPLE", "12 Chùa Bộc, Đống Đa, Hà Nội", 21.0088850, 105.8278922),
        new("Đại học Thủy lợi", "TLU-SAMPLE", "175 Tây Sơn, Đống Đa, Hà Nội", 21.0069895, 105.8251010),
        new("Đại học Xây dựng Hà Nội", "HUCE-SAMPLE", "55 Giải Phóng, Hai Bà Trưng, Hà Nội", 21.0039686, 105.8418706),
        new("Đại học Y Hà Nội", "HMU-SAMPLE", "1 Tôn Thất Tùng, Đống Đa, Hà Nội", 21.0022278, 105.8313173),
        new("Học viện Công nghệ Bưu chính Viễn thông", "PTIT-SAMPLE", "122 Hoàng Quốc Việt, Cầu Giấy, Hà Nội", 21.0477865, 105.8009131),
        new("Học viện Báo chí và Tuyên truyền", "AJC-SAMPLE", "36 Xuân Thủy, Cầu Giấy, Hà Nội", 21.0375674, 105.7888489),
        new("Đại học Ngoại ngữ - ĐHQGHN", "ULIS-SAMPLE", "2 Phạm Văn Đồng, Cầu Giấy, Hà Nội", 21.0411769, 105.7812256),
        new("Đại học Kinh tế - ĐHQGHN", "VNUUEB-SAMPLE", "144 Xuân Thủy, Cầu Giấy, Hà Nội", 21.0388103, 105.7818897),
        new("Đại học Kiến trúc Hà Nội", "HAU-SAMPLE", "Km 10 Nguyễn Trãi, Hà Đông, Hà Nội", 20.9809427, 105.7870861),
        new("Đại học Hà Nội", "HANU-SAMPLE", "Km 9 Nguyễn Trãi, Nam Từ Liêm, Hà Nội", 20.9881912, 105.7960202),
        new("Đại học Mỏ - Địa chất", "HUMG-SAMPLE", "18 Phố Viên, Bắc Từ Liêm, Hà Nội", 21.0725140, 105.7738703),
        new("Đại học Điện lực", "EPU-SAMPLE", "235 Hoàng Quốc Việt, Bắc Từ Liêm, Hà Nội", 21.0483792, 105.7856678),
        new("Học viện Tài chính", "AOF-SAMPLE", "58 Lê Văn Hiến, Bắc Từ Liêm, Hà Nội", 21.0741525, 105.7734917),
        new("Đại học Luật Hà Nội", "HLU-SAMPLE", "87 Nguyễn Chí Thanh, Đống Đa, Hà Nội", 21.0212424, 105.8100297),
        new("Học viện Ngoại giao", "DAV-SAMPLE", "69 Chùa Láng, Đống Đa, Hà Nội", 21.0225728, 105.8062739),
        new("Đại học Phenikaa", "PKA-SAMPLE", "Nguyễn Trác, Hà Đông, Hà Nội", 20.9621370, 105.7474770),
        new("Đại học Thương mại", "TMU-SAMPLE", "79 Hồ Tùng Mậu, Cầu Giấy, Hà Nội", 21.0364694, 105.7751597),
        new("Đại học Công nghiệp Hà Nội", "HAUI-SAMPLE", "298 Cầu Diễn, Bắc Từ Liêm, Hà Nội", 21.0542531, 105.7350390),
        new("Học viện Chính sách và Phát triển", "APD-SAMPLE", "Khu đô thị Nam An Khánh, Hoài Đức, Hà Nội", 21.0020179, 105.7190127)
    ];

    private static readonly RoomSeed[] RoomSeeds =
    [
        new("Phongtro123", "Phòng 35 m² đầy đủ nội thất tại An Khánh", "Phòng 35 m² có tủ bếp, tủ áo, tủ giày, điều hòa, bình nóng lạnh, tủ lạnh và máy giặt chung.", "Số 15, ngõ 90 đường La Tinh 2, An Khánh, Hoài Đức, Hà Nội", 21.0683830, 105.7081026, 2_000_000m, 35, "https://pt123.cdn.static123.com/images/thumbs/450x300/fit/2026/07/13/7_1783913395.jpg", "https://phongtro123.com/35m-du-dh-tu-quat-giuong-4nguoi-chi-tu-2tr-pr709553.html", 20),
        new("Phongtro123", "Phòng 20–40 m² tại Nguyễn Lân và Lê Trọng Tấn", "Phòng trọ rộng rãi trong chung cư mini tại phố Lê Trọng Tấn và khu Nguyễn Lân, Thanh Xuân.", "Phố Lê Trọng Tấn, Phương Liệt, Thanh Xuân, Hà Nội", 20.9919008, 105.8334299, 3_500_000m, 25, "https://pt123.cdn.static123.com/images/thumbs/450x300/fit/2026/11/07/1794056910989-181272254604868005-5368816303887889568-aba2fb4cbd38215b355c66f01fe2df2a_1794062453.jpg", "https://phongtro123.com/cho-thue-phong-dt-20m2-40m2-pho-nguyen-lan-va-pho-le-trong-tan-thanh-xuan-0912966199-pr715773.html", 3),
        new("Phongtro123", "Phòng khép kín gần AEON Mall Long Biên", "Phòng tầng một tại Tư Đình, phù hợp người đi làm hoặc sinh viên, khu vực gần AEON Mall Long Biên.", "Số 6 ngõ 188/35/14 Tư Đình, Long Biên, Hà Nội", 21.0240231, 105.8857306, 2_500_000m, 25, "https://pt123.cdn.static123.com/images/thumbs/450x300/fit/2026/11/02/1791968344081-2279144030379059256-7519835379620783554-6d46e2b03f9ab091f79678aaeaba6ff7_1793606226.jpg", "https://phongtro123.com/trong-phong-4tr-co-linh-gan-aeon-mall-long-bien-pr713562.html", 4),
        new("Phongtro123", "Phòng tại Nguyễn An Ninh gần Kinh tế, Xây dựng, Bách khoa", "Chung cư mini khép kín tại Nguyễn An Ninh, Tương Mai, thuận tiện di chuyển tới các trường khu Giải Phóng.", "57/35 Nguyễn An Ninh, Tương Mai, Hoàng Mai, Hà Nội", 20.9910548, 105.8462116, 3_500_000m, 25, "https://pt123.cdn.static123.com/images/thumbs/450x300/fit/2020/12/01/mt001_1606798356.jpg", "https://phongtro123.com/cho-thue-phong-tro-tai-nguyen-an-ninh-truong-dinh-gan-dhkt-xd-bk-pr315520.html", 4),
        new("Phongtro123", "Chung cư mini khép kín tại ngõ 192 Kim Giang", "Phòng khép kín có giường, tủ, tủ lạnh, máy giặt và máy sấy dùng chung.", "Ngõ 192 Kim Giang, Khương Đình, Thanh Xuân, Hà Nội", 20.9849390, 105.8117827, 3_300_000m, 30, "https://pt123.cdn.static123.com/images/thumbs/450x300/fit/2023/03/13/z3890356541633-84d2be345b9c751261718a7a1497e7dd_1678688835.jpg", "https://phongtro123.com/cho-thue-phong-tro-homestay-khep-kin-o-ngo-192-kim-giang-pr602927.html", 10),
        new("Phongtro123", "Phòng đủ nội thất tại 395 Thụy Khuê", "Phòng trong nhà năm tầng, có điều hòa, máy giặt và Wi-Fi, nằm trên phố Thụy Khuê.", "395 Thụy Khuê, Tây Hồ, Hà Nội", 21.0434008, 105.8169172, 2_600_000m, 15, "https://pt123.cdn.static123.com/images/thumbs/450x300/fit/2026/08/18/z7555259973327-d4a3586d3ec6cb077343a1eeb6711546_1787020132.jpg", "https://phongtro123.com/cho-thue-phong-tai-nha-mat-pho-395-thuy-khue-du-noi-that-dieu-hoa-may-giat-wifi-2-pr711699.html", 15),
        new("Phongtro123", "Phòng khép kín có gác tại Mỹ Đình 1", "Phòng có gác xép, đầy đủ đồ cơ bản, khu nhà sạch sẽ và có trang bị phòng cháy chữa cháy.", "37 đường Mỹ Đình, Nam Từ Liêm, Hà Nội", 21.0218931, 105.7744154, 3_500_000m, 25, "https://pt123.cdn.static123.com/images/thumbs/450x300/fit/2026/11/10/phong-202_1794274446.jpg", "https://phongtro123.com/cho-thue-phong-khep-kin-tai-my-dinh-1-pr715861.html", 18),
        new("Phongtro123", "Phòng gác lửng tại ngõ 322 Mỹ Đình", "Phòng gác lửng khép kín, rộng rãi, sạch sẽ và nằm trong khu dân cư an ninh.", "322 đường Mỹ Đình, Nam Từ Liêm, Hà Nội", 21.0218901, 105.7698540, 1_700_000m, 25, "https://pt123.cdn.static123.com/images/thumbs/450x300/fit/2026/11/10/phong-gac-lung_1794274194.jpg", "https://phongtro123.com/phong-lung-khep-kin-gia-re-tai-ngo-322-duong-my-dinh-my-dinh-1-pr715860.html", 18),
        new("Phongtro123", "Phòng khép kín 22 m² tại Võ Thị Sáu", "Phòng trong nhà chính chủ xây theo kiểu chung cư mini, có công trình phụ khép kín và khu vực sinh hoạt thuận tiện.", "28E ngõ 84 Võ Thị Sáu, Hai Bà Trưng, Hà Nội", 21.0055661, 105.8569535, 3_000_000m, 22, "https://pt123.cdn.static123.com/images/thumbs/450x300/fit/2026/08/06/28engo84vothisau-4_1785994339.jpg", "https://phongtro123.com/chinh-chu-vo-thi-sau-phong-tro-22m2-moi-trong-an-ninh-tot-pccc-sinh-hoat-thuan-tien-pr710950.html", 4),
        new("Phongtro123", "Studio khép kín đủ nội thất tại Xuân Đỉnh", "Studio có cổng khóa vân tay, camera khu để xe và nội thất cơ bản tại ngõ 435 Xuân Đỉnh.", "Ngõ 435 đường Xuân Đỉnh, Bắc Từ Liêm, Hà Nội", 21.0711467, 105.7896390, 3_200_000m, 22, "https://pt123.cdn.static123.com/images/thumbs/450x300/fit/2026/06/01/1_1780286334.jpg", "https://phongtro123.com/hop-ly-cho-thue-can-ho-studio-khep-kin-day-du-noi-that-ngo-435-duong-xuan-dinh-pr685937.html", 12),
        new("Thuephongtro", "Phòng đầy đủ nội thất tại Trần Khát Chân", "Phòng có nóng lạnh, điều hòa, máy giặt, giường tủ, gác xép và vệ sinh khép kín; không chung chủ.", "Trần Khát Chân, Hai Bà Trưng, Hà Nội", 21.0089365, 105.8591684, 4_500_000m, 30, "https://img.thuephongtro.com/images/thumb/2026/10/29/20261029091435-1faon.jpg", "https://thuephongtro.com/cho-thue-phong-full-do-tai-tran-khat-chan-gan-dai-hoc-bach-khoa-171511.html", 4),
        new("Thuephongtro", "Phòng mới xây khép kín tại Láng Hạ", "Nhà khép kín trong ngõ 90 Láng Hạ, có gác xép, điện nước riêng và khu vực yên tĩnh.", "Ngõ 90 Láng Hạ, Đống Đa, Hà Nội", 21.0125238, 105.8135061, 5_000_000m, 22, "https://img.thuephongtro.com/images/thumb/2022/10/07/20221007153048-zvst3.jpg", "https://thuephongtro.com/chinh-chu-cho-thue-phong-tro-moi-xay-khep-kin-thiet-bi-moi-100--169038.html", 1),
        new("Thuephongtro", "Phòng mới gần ga Cầu Diễn", "Phòng khép kín có gác xép, điều hòa, bình nóng lạnh; gần tàu điện, xe buýt và chợ Cầu Diễn.", "Số 11 ngõ 99 Cầu Diễn, Phú Diễn, Bắc Từ Liêm, Hà Nội", 21.0453469, 105.7846153, 2_500_000m, 18.5, "https://img.thuephongtro.com/images/thumb/2026/08/18/20260818200526-tpcah.jpg", "https://thuephongtro.com/phong-moi-chi-2-5-trieu-thang-cau-dien-gan-tau-dien-buyt-cho--171497.html", 19),
        new("Thuephongtro", "Phòng cho nữ tại ngõ 254D Minh Khai", "Phòng trong nhà riêng bốn tầng, gần nhiều trường đại học và khu Times City, thuận tiện sinh hoạt.", "Ngõ 254D Minh Khai, Hai Bà Trưng, Hà Nội", 20.9981814, 105.8706579, 2_400_000m, 15, "https://img.thuephongtro.com/images/thumb/2026/08/17/20260817094426-xcvyv.jpg", "https://thuephongtro.com/cho-nu-thue-phong-trong-nha-rieng-4-tang-ngo-254d-minh-khai-gia-2-4-tr-171496.html", 4),
        new("Thuephongtro", "Phòng khép kín mới xây tại ngõ Quỳnh", "Phòng mới xây có gác xép, khu vệ sinh khép kín và sân chung, phù hợp một đến hai người.", "Ngõ Quỳnh, Thanh Nhàn, Hai Bà Trưng, Hà Nội", 21.0095490, 105.8555293, 3_300_000m, 16, "https://img.thuephongtro.com/images/thumb/2026/07/21/20260721120013-lvryi.jpg", "https://thuephongtro.com/cho-thue-phong-tro-khep-kin-moi-xay-ngo-quynh-thanh-nhan-2026-171488.html", 4),
        new("Thuephongtro", "Phòng khép kín có thang máy tại Lĩnh Nam", "Phòng có vệ sinh riêng, điều hòa, bệ bếp, thang máy, bình nóng lạnh và cửa sổ thoáng.", "Đường Lĩnh Nam, Hoàng Mai, Hà Nội", 20.9809460, 105.8819592, 2_500_000m, 21, "https://img.thuephongtro.com/images/thumb/2024/05/23/20240523062030-2sr5q.jpg", "https://thuephongtro.com/phong-co-wc-khep-kin-rong-thoang-linh-nam-thang-may-nong-lanh-dieu-hoa-169776.html", 4),
        new("Thuephongtro", "Phòng khép kín tại phố Trần Nhân Tông", "Phòng có giường, tủ, điều hòa, quạt, tủ lạnh và nóng lạnh; ô tô có thể đỗ gần cửa.", "Phố Trần Nhân Tông, Hai Bà Trưng, Hà Nội", 21.0174757, 105.8430667, 3_500_000m, 20, "https://img.thuephongtro.com/images/thumb/2026/07/13/20260713231928-wi3ut.jpg", "https://thuephongtro.com/pho-tran-nhan-tong-hbt-gia-phong-3-5tr-vskk-o-to-do-cua-view-thanh-pho-171485.html", 4),
        new("Thuephongtro", "Studio tại ngõ 281 Trần Khát Chân", "Studio đầy đủ nội thất, có máy giặt riêng và thang máy; không chung chủ.", "Ngõ 281 Trần Khát Chân, Hai Bà Trưng, Hà Nội", 21.0098461, 105.8564463, 4_500_000m, 25, "https://img.thuephongtro.com/images/thumb/2026/07/12/20260712100725-r12by.jpg", "https://thuephongtro.com/-cho-thue-phong-studio-ngo-281-tran-khat-chan-hai-ba-trung-ha-noi-171482.html", 4),
        new("Thuephongtro", "Studio tại ngõ 195 Trần Cung", "Studio có nóng lạnh, điều hòa, tủ bếp, giường tủ, bàn và cửa sổ lớn nhiều ánh sáng.", "Ngõ 195 Trần Cung, Bắc Từ Liêm, Hà Nội", 21.0527992, 105.7886240, 4_100_000m, 25, "https://img.thuephongtro.com/images/thumb/2026/07/11/20260711174436-4mb32.jpg", "https://thuephongtro.com/cho-thue-phong-studio-ngo-195-tran-cung-cau-giay-ha-noi-171477.html", 6),
        new("Thuephongtro", "Studio tại ngõ 30 Lê Trọng Tấn, Hà Đông", "Studio có thang máy, điều hòa, nóng lạnh, tủ lạnh, giường tủ, bàn ăn, máy giặt, máy sấy và hút mùi.", "Ngõ 30 Lê Trọng Tấn, Hà Đông, Hà Nội", 20.9666867, 105.7613200, 4_100_000m, 25, "https://img.thuephongtro.com/images/thumb/2026/07/11/20260711181543-m4zy1.jpg", "https://thuephongtro.com/-cho-thue-phong-studio-ngo-30-le-trong-tan-ha-dong-ha-noi-171478.html", 10)
    ];

    public static async Task<MarketSampleImportResult> ImportAsync(CampusRentDbContext db, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await UpsertUsers(db, now, cancellationToken);
        await UpsertUniversities(db, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await UpsertRooms(db, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await UpsertRoomImages(db, now, cancellationToken);
        await UpsertReviews(db, now, cancellationToken);
        await UpsertReports(db, now, cancellationToken);
        await UpsertBookings(db, now, cancellationToken);
        await UpsertFavorites(db, cancellationToken);
        await UpsertLandlordRequests(db, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var result = await CountImportedRows(db, cancellationToken);
        if (!result.IsComplete)
            throw new InvalidOperationException($"Sample import verification failed: {result}");

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static async Task UpsertUsers(CampusRentDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var ids = Ids(1);
        var emails = Enumerable.Range(1, SampleCount).Select(UserEmail).ToArray();
        var conflicts = await db.Users.AsNoTracking()
            .Where(user => emails.Contains(user.Email) && !ids.Contains(user.Id))
            .Select(user => user.Email)
            .ToListAsync(cancellationToken);
        if (conflicts.Count != 0)
            throw new InvalidOperationException($"Sample user email is already owned by another record: {string.Join(", ", conflicts)}");

        var existing = await db.Users.Where(user => ids.Contains(user.Id)).ToDictionaryAsync(user => user.Id, cancellationToken);
        for (var index = 1; index <= SampleCount; index++)
        {
            var id = SampleId(1, index);
            if (!existing.TryGetValue(id, out var user))
            {
                user = new User
                {
                    Id = id,
                    Password = BCrypt.Net.BCrypt.HashPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(32))),
                    CreatedAt = now.AddDays(-60 - index)
                };
                db.Users.Add(user);
            }

            var landlord = index <= 5;
            user.Email = UserEmail(index);
            user.FullName = landlord ? $"Chủ trọ mẫu {index:00}" : $"Sinh viên mẫu {index - 5:00}";
            user.PhoneNumber = null;
            user.Avatar = null;
            user.Role = landlord ? Role.LANDLORD : Role.STUDENT;
            user.IsEduVerified = !landlord && index % 2 == 0;
            user.UpdatedAt = now;
        }
    }

    private static async Task UpsertUniversities(CampusRentDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var ids = Ids(2);
        var shortNames = UniversitySeeds.Select(seed => seed.ShortName).ToArray();
        var conflicts = await db.Universities.AsNoTracking()
            .Where(university => shortNames.Contains(university.ShortName) && !ids.Contains(university.Id))
            .Select(university => university.ShortName)
            .ToListAsync(cancellationToken);
        if (conflicts.Count != 0)
            throw new InvalidOperationException($"Sample university short name is already owned by another record: {string.Join(", ", conflicts)}");

        var existing = await db.Universities.Where(university => ids.Contains(university.Id))
            .ToDictionaryAsync(university => university.Id, cancellationToken);
        for (var index = 1; index <= SampleCount; index++)
        {
            var id = SampleId(2, index);
            var seed = UniversitySeeds[index - 1];
            if (!existing.TryGetValue(id, out var university))
            {
                university = new University { Id = id, CreatedAt = now.AddDays(-90 - index) };
                db.Universities.Add(university);
            }

            university.Name = seed.Name;
            university.ShortName = seed.ShortName;
            university.Address = seed.Address;
            university.Latitude = seed.Latitude;
            university.Longitude = seed.Longitude;
        }
    }

    private static async Task UpsertRooms(CampusRentDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var ids = Ids(3);
        var existing = await db.Rooms.Where(room => ids.Contains(room.Id)).ToDictionaryAsync(room => room.Id, cancellationToken);
        for (var index = 1; index <= SampleCount; index++)
        {
            var id = SampleId(3, index);
            var seed = RoomSeeds[index - 1];
            if (!existing.TryGetValue(id, out var room))
            {
                room = new Room { Id = id, CreatedAt = now.AddDays(-45 - index) };
                db.Rooms.Add(room);
            }

            room.LandlordId = SampleId(1, ((index - 1) % 5) + 1);
            room.UniversityId = SampleId(2, seed.UniversityIndex);
            room.Title = seed.Title;
            room.Description = $"{seed.Description}\n\n[Dữ liệu mẫu thị trường] Giá, diện tích, địa chỉ và ảnh được tham khảo từ {seed.Source} ngày 11/11/2026. Phí sinh hoạt là dữ liệu minh họa của CampusRent; hãy kiểm tra lại với nguồn trước khi giao dịch. Nguồn: {seed.SourceUrl}";
            room.Address = seed.Address;
            room.Latitude = seed.Latitude;
            room.Longitude = seed.Longitude;
            room.BasePrice = seed.Price;
            room.ElectricPrice = 4_000m;
            room.WaterPrice = 30_000m;
            room.InternetPrice = 100_000m;
            room.OtherFee = 50_000m;
            room.Area = seed.Area;
            room.MaxPeople = seed.Area >= 30 ? 3 : 2;
            room.Amenities = InferAmenities(seed.Title + " " + seed.Description);
            room.Status = RoomStatus.AVAILABLE;
            room.ExpiresAt = now.AddDays(120 + index);
            room.UpdatedAt = now;
        }
    }

    private static async Task UpsertRoomImages(CampusRentDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var ids = Ids(4);
        var existing = await db.RoomImages.Where(image => ids.Contains(image.Id)).ToDictionaryAsync(image => image.Id, cancellationToken);
        for (var index = 1; index <= SampleCount; index++)
        {
            var id = SampleId(4, index);
            if (!existing.TryGetValue(id, out var image))
            {
                image = new RoomImage { Id = id, CreatedAt = now.AddDays(-45 - index) };
                db.RoomImages.Add(image);
            }

            image.RoomId = SampleId(3, index);
            image.Url = RoomSeeds[index - 1].ImageUrl;
            image.PublicId = null;
            image.DisplayOrder = 0;
            image.IsCover = true;
        }
    }

    private static async Task UpsertReviews(CampusRentDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var ids = Ids(5);
        var expectedPairs = Enumerable.Range(1, SampleCount).ToDictionary(
            index => (UserId: ReviewUserId(index), RoomId: SampleId(3, index)),
            index => SampleId(5, index));
        var userIds = Ids(1);
        var roomIds = Ids(3);
        var potentialConflicts = await db.Reviews.AsNoTracking()
            .Where(review => userIds.Contains(review.UserId) && roomIds.Contains(review.RoomId))
            .Select(review => new { review.Id, review.UserId, review.RoomId })
            .ToListAsync(cancellationToken);
        var conflict = potentialConflicts.FirstOrDefault(review =>
            expectedPairs.TryGetValue((review.UserId, review.RoomId), out var expectedId) && review.Id != expectedId);
        if (conflict is not null)
            throw new InvalidOperationException("A sample review pair is already owned by another record.");

        var existing = await db.Reviews.Where(review => ids.Contains(review.Id)).ToDictionaryAsync(review => review.Id, cancellationToken);
        for (var index = 1; index <= SampleCount; index++)
        {
            var id = SampleId(5, index);
            if (!existing.TryGetValue(id, out var review))
            {
                review = new Review { Id = id };
                db.Reviews.Add(review);
            }

            review.RoomId = SampleId(3, index);
            review.UserId = ReviewUserId(index);
            review.Rating = index % 4 == 0 ? 4 : 5;
            review.Comment = $"[Dữ liệu mẫu] Người dùng tham khảo đánh giá tích cực về vị trí và thông tin của phòng mẫu {index:00}.";
            review.IsFlagged = false;
            review.FlagReason = null;
            review.CreatedAt = now.AddDays(-25 - index);
        }
    }

    private static async Task UpsertReports(CampusRentDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var ids = Ids(6);
        var existing = await db.Reports.Where(report => ids.Contains(report.Id)).ToDictionaryAsync(report => report.Id, cancellationToken);
        for (var index = 1; index <= SampleCount; index++)
        {
            var id = SampleId(6, index);
            if (!existing.TryGetValue(id, out var report))
            {
                report = new Report { Id = id };
                db.Reports.Add(report);
            }

            var targetsRoom = index % 2 == 0;
            report.ReporterId = ReviewUserId(((index + 4) % SampleCount) + 1);
            report.RoomId = targetsRoom ? SampleId(3, index) : null;
            report.ReviewId = targetsRoom ? null : SampleId(5, index);
            report.Reason = targetsRoom ? "[Dữ liệu mẫu] Kiểm tra thông tin tin đăng" : "[Dữ liệu mẫu] Kiểm tra nội dung đánh giá";
            report.Description = "Báo cáo này chỉ dùng để minh họa luồng quản trị và không phản ánh vi phạm thực tế.";
            report.Status = index % 3 == 0 ? ReportStatus.DISMISSED : ReportStatus.PENDING;
            report.CreatedAt = now.AddDays(-15 - index);
        }
    }

    private static async Task UpsertBookings(CampusRentDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var ids = Ids(7);
        var existing = await db.Bookings.Where(booking => ids.Contains(booking.Id)).ToDictionaryAsync(booking => booking.Id, cancellationToken);
        for (var index = 1; index <= SampleCount; index++)
        {
            var id = SampleId(7, index);
            if (!existing.TryGetValue(id, out var booking))
            {
                booking = new Booking { Id = id };
                db.Bookings.Add(booking);
            }

            booking.RoomId = SampleId(3, index);
            booking.StudentId = ReviewUserId(index);
            booking.VisitAt = now.AddDays(-35 - index).AddHours(9 + index % 8);
            booking.Note = $"[Dữ liệu mẫu] Lịch xem phòng mẫu {index:00}.";
            booking.Status = index % 2 == 0 ? "COMPLETED" : "CANCELLED";
            booking.CreatedAt = now.AddDays(-40 - index);
        }
    }

    private static async Task UpsertFavorites(CampusRentDbContext db, CancellationToken cancellationToken)
    {
        var intended = Enumerable.Range(1, SampleCount)
            .Select(index => (UserId: ReviewUserId(index), RoomId: SampleId(3, index)))
            .ToHashSet();
        var userIds = intended.Select(pair => pair.UserId).Distinct().ToArray();
        var roomIds = intended.Select(pair => pair.RoomId).ToArray();
        var existing = (await db.Favorites
                .Where(favorite => userIds.Contains(favorite.UserId) && roomIds.Contains(favorite.RoomId))
                .Select(favorite => new { favorite.UserId, favorite.RoomId })
                .ToListAsync(cancellationToken))
            .Select(favorite => (favorite.UserId, favorite.RoomId))
            .ToHashSet();

        foreach (var pair in intended.Where(pair => !existing.Contains(pair)))
            db.Favorites.Add(new Favorite { UserId = pair.UserId, RoomId = pair.RoomId });
    }

    private static async Task UpsertLandlordRequests(CampusRentDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var ids = Ids(8);
        var pendingUsers = Enumerable.Range(16, 5).Select(index => SampleId(1, index)).ToArray();
        var pendingConflict = await db.LandlordRequests.AsNoTracking()
            .AnyAsync(request => pendingUsers.Contains(request.UserId) && request.Status == LandlordRequestStatus.PENDING && !ids.Contains(request.Id), cancellationToken);
        if (pendingConflict)
            throw new InvalidOperationException("A sample user already has a pending landlord request outside the managed sample set.");

        var existing = await db.LandlordRequests.Where(request => ids.Contains(request.Id))
            .ToDictionaryAsync(request => request.Id, cancellationToken);
        for (var index = 1; index <= SampleCount; index++)
        {
            var id = SampleId(8, index);
            if (!existing.TryGetValue(id, out var request))
            {
                request = new LandlordRequest { Id = id };
                db.LandlordRequests.Add(request);
            }

            var status = index <= 5
                ? LandlordRequestStatus.APPROVED
                : index <= 15 ? LandlordRequestStatus.REJECTED : LandlordRequestStatus.PENDING;
            request.UserId = SampleId(1, index);
            request.Title = $"[Dữ liệu mẫu] Hồ sơ nâng cấp chủ trọ {index:00}";
            request.Reason = "Hồ sơ tổng hợp phục vụ minh họa quy trình duyệt quyền đăng tin trong CampusRent.";
            request.ImageUrls = [];
            request.PdfUrl = null;
            request.PdfFileName = null;
            request.Status = status;
            request.AdminNote = status switch
            {
                LandlordRequestStatus.APPROVED => "[Dữ liệu mẫu] Hồ sơ đã được duyệt.",
                LandlordRequestStatus.REJECTED => "[Dữ liệu mẫu] Hồ sơ minh họa đã bị từ chối.",
                _ => null
            };
            request.CreatedAt = now.AddDays(-50 - index);
            request.UpdatedAt = status == LandlordRequestStatus.PENDING ? request.CreatedAt : request.CreatedAt.AddDays(2);
        }
    }

    private static async Task<MarketSampleImportResult> CountImportedRows(CampusRentDbContext db, CancellationToken cancellationToken)
    {
        var userIds = Ids(1);
        var universityIds = Ids(2);
        var roomIds = Ids(3);
        var imageIds = Ids(4);
        var reviewIds = Ids(5);
        var reportIds = Ids(6);
        var bookingIds = Ids(7);
        var requestIds = Ids(8);
        var favoritePairs = Enumerable.Range(1, SampleCount)
            .Select(index => (UserId: ReviewUserId(index), RoomId: SampleId(3, index)))
            .ToHashSet();
        var favoriteUsers = favoritePairs.Select(pair => pair.UserId).Distinct().ToArray();
        var favorites = await db.Favorites.AsNoTracking()
            .Where(favorite => favoriteUsers.Contains(favorite.UserId) && roomIds.Contains(favorite.RoomId))
            .Select(favorite => new { favorite.UserId, favorite.RoomId })
            .ToListAsync(cancellationToken);

        return new MarketSampleImportResult(
            await db.Users.CountAsync(user => userIds.Contains(user.Id), cancellationToken),
            await db.Universities.CountAsync(university => universityIds.Contains(university.Id), cancellationToken),
            await db.Rooms.CountAsync(room => roomIds.Contains(room.Id), cancellationToken),
            await db.RoomImages.CountAsync(image => imageIds.Contains(image.Id), cancellationToken),
            await db.Reviews.CountAsync(review => reviewIds.Contains(review.Id), cancellationToken),
            await db.Reports.CountAsync(report => reportIds.Contains(report.Id), cancellationToken),
            await db.Bookings.CountAsync(booking => bookingIds.Contains(booking.Id), cancellationToken),
            favorites.Count(favorite => favoritePairs.Contains((favorite.UserId, favorite.RoomId))),
            await db.LandlordRequests.CountAsync(request => requestIds.Contains(request.Id), cancellationToken));
    }

    private static string UserEmail(int index) => $"market-sample-{index:00}@campusrent.invalid";

    private static Guid ReviewUserId(int index) => SampleId(1, 6 + ((index - 1) % 15));

    private static Guid[] Ids(int group) => Enumerable.Range(1, SampleCount).Select(index => SampleId(group, index)).ToArray();

    private static Guid SampleId(int group, int index) => Guid.Parse($"ca{group:x2}0000-{index:x4}-4000-8000-{index:x12}");

    private static List<string> InferAmenities(string value)
    {
        var text = value.ToLowerInvariant();
        var amenities = new List<string>();
        AddIf("wifi", "wifi", "wi-fi", "internet");
        AddIf("ac", "điều hòa", "điều hoà");
        AddIf("heater", "nóng lạnh", "bình nóng");
        AddIf("parking", "để xe", "nhà xe", "ô tô");
        AddIf("no_curfew", "không chung chủ");
        AddIf("elevator", "thang máy");
        AddIf("cctv", "camera");
        AddIf("kitchen", "bếp");
        AddIf("refrigerator", "tủ lạnh");
        AddIf("washing_machine", "máy giặt", "máy sấy");
        AddIf("security", "an ninh", "vân tay");
        AddIf("balcony", "ban công");
        return amenities;

        void AddIf(string amenity, params string[] terms)
        {
            if (terms.Any(text.Contains)) amenities.Add(amenity);
        }
    }
}
