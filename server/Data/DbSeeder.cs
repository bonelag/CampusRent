using CampusRent.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusRent.Server.Data;

public static class DbSeeder
{
    public static async Task SeedReferenceAsync(CampusRentDbContext dbContext)
    {
        if (await dbContext.Universities.AnyAsync())
        {
            return; // Đã có dữ liệu
        }

        // 1. Seed Universities
        var hust = new University
        {
            Name = "Đại học Bách Khoa Hà Nội",
            ShortName = "BKHN",
            Latitude = 21.0055,
            Longitude = 105.8433,
            Address = "Số 1 Đại Cồ Việt, Hai Bà Trưng, Hà Nội"
        };
        var neu = new University
        {
            Name = "Đại học Kinh Tế Quốc Dân",
            ShortName = "NEU",
            Latitude = 21.0007,
            Longitude = 105.8427,
            Address = "207 Giải Phóng, Đồng Tâm, Hai Bà Trưng, Hà Nội"
        };
        var uet = new University
        {
            Name = "Đại học Công Nghệ - ĐHQGHN",
            ShortName = "UET",
            Latitude = 21.0381,
            Longitude = 105.7828,
            Address = "144 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội"
        };
        var hnue = new University
        {
            Name = "Đại học Sư Phạm Hà Nội",
            ShortName = "HNUE",
            Latitude = 21.0366,
            Longitude = 105.7836,
            Address = "136 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội"
        };
        var utc = new University
        {
            Name = "Đại học Giao Thông Vận Tải",
            ShortName = "UTC",
            Latitude = 21.0289,
            Longitude = 105.8018,
            Address = "Số 3 Cầu Giấy, Láng Thượng, Đống Đa, Hà Nội"
        };

        dbContext.Universities.AddRange(hust, neu, uet, hnue, utc);
        await dbContext.SaveChangesAsync();

    }

    public static async Task SeedAsync(CampusRentDbContext dbContext)
    {
        await SeedReferenceAsync(dbContext);
        if (await dbContext.Users.AnyAsync()) return;
        var hust = await dbContext.Universities.SingleAsync(u => u.ShortName == "BKHN");
        var neu = await dbContext.Universities.SingleAsync(u => u.ShortName == "NEU");
        var uet = await dbContext.Universities.SingleAsync(u => u.ShortName == "UET");
        var hnue = await dbContext.Universities.SingleAsync(u => u.ShortName == "HNUE");
        var utc = await dbContext.Universities.SingleAsync(u => u.ShortName == "UTC");
        // Development demo accounts only.
        var hash = BCrypt.Net.BCrypt.HashPassword("Password@123");

        var admin = new User
        {
            Email = "admin@campusrent.vn",
            Password = hash,
            FullName = "Quản Trị Viên",
            PhoneNumber = "0901234567",
            Role = Role.ADMIN,
            IsEduVerified = false
        };

        var landlord1 = new User
        {
            Email = "landlord1@gmail.com",
            Password = hash,
            FullName = "Bác Ba Chủ Trọ",
            PhoneNumber = "0912345678",
            Role = Role.LANDLORD,
            IsEduVerified = false
        };

        var landlord2 = new User
        {
            Email = "landlord2@gmail.com",
            Password = hash,
            FullName = "Cô Mai Cầu Giấy",
            PhoneNumber = "0987654321",
            Role = Role.LANDLORD,
            IsEduVerified = false
        };

        var studentEdu = new User
        {
            Email = "nguyenvanan@sis.hust.edu.vn",
            Password = hash,
            FullName = "Nguyễn Văn An (BKHN)",
            PhoneNumber = "0933112233",
            Role = Role.STUDENT,
            IsEduVerified = true
        };

        var studentNormal = new User
        {
            Email = "tranthib@gmail.com",
            Password = hash,
            FullName = "Trần Thị Bích",
            PhoneNumber = "0944223344",
            Role = Role.STUDENT,
            IsEduVerified = false
        };

        dbContext.Users.AddRange(admin, landlord1, landlord2, studentEdu, studentNormal);
        await dbContext.SaveChangesAsync();

        // 3. Seed Sample Rooms
        var sampleRooms = new List<Room>
        {
            new()
            {
                LandlordId = landlord1.Id,
                UniversityId = hust.Id,
                Title = "Phòng khép kín ban công thoáng gần Bách Khoa",
                Description = "Phòng 25m2 full đồ: điều hòa, nóng lạnh, giường tủ, máy giặt chung. Ngõ ô tô đỗ cửa, cách cổng Parabol Bách Khoa chỉ 600m.",
                Address = "Số 15 ngõ 30 Tạ Quang Bửu, Hai Bà Trưng, Hà Nội",
                Latitude = 21.0042,
                Longitude = 105.8458,
                BasePrice = 3200000,
                ElectricPrice = 3800,
                WaterPrice = 30000,
                InternetPrice = 100000,
                OtherFee = 50000,
                Area = 25,
                MaxPeople = 2,
                Amenities = new() { "wifi", "ac", "heater", "balcony", "parking", "no_curfew" },
                Status = RoomStatus.AVAILABLE,
                ExpiresAt = DateTime.UtcNow.AddDays(25),
                Images = new List<RoomImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800", IsCover = true, DisplayOrder = 0 },
                    new() { Url = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800", DisplayOrder = 1 }
                }
            },
            new()
            {
                LandlordId = landlord1.Id,
                UniversityId = neu.Id,
                Title = "Chung cư mini cao cấp ngõ 205 Giải Phóng đối diện NEU",
                Description = "Căn hộ 35m2 có gác xép lửng, thang máy, khóa vân tay an ninh tuyệt đối. Giờ giấc tự do không chung chủ, cách ĐH Kinh Tế Quốc Dân 400m.",
                Address = "Số 8 ngách 205/14 Giải Phóng, Hai Bà Trưng, Hà Nội",
                Latitude = 21.0015,
                Longitude = 105.8415,
                BasePrice = 4500000,
                ElectricPrice = 3500,
                WaterPrice = 28000,
                InternetPrice = 80000,
                OtherFee = 80000,
                Area = 35,
                MaxPeople = 3,
                Amenities = new() { "wifi", "ac", "heater", "elevator", "parking", "cctv", "kitchen" },
                Status = RoomStatus.AVAILABLE,
                ExpiresAt = DateTime.UtcNow.AddDays(28),
                Images = new List<RoomImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800", IsCover = true, DisplayOrder = 0 }
                }
            },
            new()
            {
                LandlordId = landlord2.Id,
                UniversityId = uet.Id,
                Title = "Studio khép kín ngõ 175 Xuân Thủy gần ĐH Công Nghệ",
                Description = "Phòng xinh xắn tiện nghi đầy đủ, có bếp riêng, tủ lạnh, điều hòa inverter. Đi bộ 5 phút sang ĐH Ngoại Ngữ và ĐH Công Nghệ.",
                Address = "Số 22 ngõ 175 Xuân Thủy, Cầu Giấy, Hà Nội",
                Latitude = 21.0375,
                Longitude = 105.7845,
                BasePrice = 3800000,
                ElectricPrice = 4000,
                WaterPrice = 100000,
                InternetPrice = 100000,
                OtherFee = 70000,
                Area = 28,
                MaxPeople = 2,
                Amenities = new() { "wifi", "ac", "heater", "refrigerator", "parking", "security" },
                Status = RoomStatus.AVAILABLE,
                ExpiresAt = DateTime.UtcNow.AddDays(20),
                Images = new List<RoomImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1493809842364-78817add7ffb?w=800", IsCover = true, DisplayOrder = 0 }
                }
            },
            new()
            {
                LandlordId = landlord2.Id,
                UniversityId = hnue.Id,
                Title = "Phòng trọ giá rẻ cho sinh viên Sư Phạm ngõ Phan Văn Trường",
                Description = "Phòng sạch sẽ thoáng mát, vệ sinh khép kín, gần chợ sinh viên Phan Văn Trường cực kỳ tiện mua sắm ăn uống.",
                Address = "Ngõ 48 Phan Văn Trường, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
                Latitude = 21.0392,
                Longitude = 105.7861,
                BasePrice = 2400000,
                ElectricPrice = 4000,
                WaterPrice = 30000,
                InternetPrice = 80000,
                OtherFee = 30000,
                Area = 18,
                MaxPeople = 2,
                Amenities = new() { "wifi", "heater", "parking" },
                Status = RoomStatus.AVAILABLE,
                ExpiresAt = DateTime.UtcNow.AddDays(15),
                Images = new List<RoomImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1598928506311-c55ded91a20c?w=800", IsCover = true, DisplayOrder = 0 }
                }
            },
            new()
            {
                LandlordId = landlord1.Id,
                UniversityId = utc.Id,
                Title = "Căn hộ 1 ngủ 1 khách phố Chùa Láng gần ĐH Giao Thông",
                Description = "Gần hồ Quan Hệ Chùa Láng, ngõ thông thoáng, trang bị sẵn salon, giường nệm, bếp từ, máy hút mùi. Phù hợp 2-3 bạn ở.",
                Address = "Số 112 ngõ 84 Chùa Láng, Láng Thượng, Đống Đa, Hà Nội",
                Latitude = 21.0268,
                Longitude = 105.8035,
                BasePrice = 5200000,
                ElectricPrice = 3800,
                WaterPrice = 35000,
                InternetPrice = 100000,
                OtherFee = 100000,
                Area = 40,
                MaxPeople = 3,
                Amenities = new() { "wifi", "ac", "heater", "kitchen", "washing_machine", "parking", "cctv" },
                Status = RoomStatus.AVAILABLE,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Images = new List<RoomImage>
                {
                    new() { Url = "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=800", IsCover = true, DisplayOrder = 0 }
                }
            }
        };

        dbContext.Rooms.AddRange(sampleRooms);
        await dbContext.SaveChangesAsync();

        // 4. Seed Sample Reviews
        var room1 = sampleRooms[0];
        var review1 = new Review
        {
            RoomId = room1.Id,
            UserId = studentEdu.Id,
            Rating = 5,
            Comment = "Phòng bác Ba rất sạch sẽ, đúng như ảnh. Bác chủ dễ tính hỗ trợ sinh viên nhiệt tình. Đi bộ ra Bách Khoa mất đúng 7 phút!",
            IsFlagged = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var review2 = new Review
        {
            RoomId = room1.Id,
            UserId = studentNormal.Id,
            Rating = 4,
            Comment = "Khu vực yên tĩnh học bài tốt, an ninh đảm bảo. Điện nước tính chuẩn theo công tơ.",
            IsFlagged = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        dbContext.Reviews.AddRange(review1, review2);
        await dbContext.SaveChangesAsync();
    }
}
