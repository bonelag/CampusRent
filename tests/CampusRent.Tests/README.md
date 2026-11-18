# Tổ chức kiểm thử backend

Mỗi file kiểm tra một chức năng hoặc một nhóm tình huống liên quan. Class có tên trùng với file để dễ tìm và chạy riêng; các file không gom nhiều tính năng thành một bộ kiểm thử lớn.

| Thư mục | Trách nhiệm |
| --- | --- |
| `Authentication/` | Đăng ký, đăng nhập, JWT, xác minh OTP Email, thay đổi vai trò và mật khẩu |
| `Accounts/` | Cập nhật hồ sơ |
| `Rooms/` | Đăng/sửa tin (cổng xác minh Email), quyền sở hữu, hiển thị, hạ hạng uy tín, tìm kiếm và validation |
| `Bookings/` | Tạo, xác nhận, hủy, đánh dấu khách chưa chốt và vòng đời lịch hẹn |
| `Favorites/` | Lưu/bỏ lưu và phòng không tồn tại |
| `Reviews/` | Tạo, validation, trọng số `2.0x` `.edu.vn` và xếp hạng Bayesian |
| `Reports/` | Validation báo cáo |
| `Administration/` | Kiểm duyệt, thống kê KPI, cấu hình Gmail SMTP AES-GCM, xác thực thủ công và quản lý người dùng |
| `LandlordRequests/` | Gửi, duyệt, hủy và bảo vệ yêu cầu chủ trọ |
| `Universities/` | Truy xuất và quản lý danh mục trường đại học |
| `Uploads/` | Ảnh phòng, ảnh/PDF xác minh và quyền truy cập tệp |
| `Places/` | API gợi ý vị trí |
| `Services/` | Khoảng cách Haversine, chi phí, từ cấm, xếp hạng đánh giá, clients vị trí và connection string |
| `Persistence/` | Seed, importer dữ liệu thị trường, ràng buộc dữ liệu và migrations PostgreSQL |
| `Workers/` | Nhắc lịch ngày hẹn, tự ẩn phòng sau `+3 ngày` và tự hết hạn tin 30 ngày |
| `Health/` | Health endpoint |
| `Infrastructure/` | Fixture, HTTP/database setup và helpers dùng chung |

`ApiFactory` tạo SQLite và storage riêng cho mỗi fixture. `ApiTestBase` chứa setup tài khoản/API; `RoomFeatureTestBase` thêm bộ dữ liệu phòng; `SecurityTestBase` thêm tài khoản/tệp cho các ca bảo mật. Các class infrastructure không chứa Fact/Theory.

Chạy toàn bộ:

```sh
dotnet test tests/CampusRent.Tests
```

Chạy một nhóm:

```sh
dotnet test tests/CampusRent.Tests --filter FullyQualifiedName~RoomSearchTests
dotnet test tests/CampusRent.Tests --filter FullyQualifiedName~RegistrationTests
```

Đặt `CAMPUSRENT_TEST_POSTGRES` để chạy ca migrations/persistence với PostgreSQL thật. Fixture dùng schema riêng và dọn sau khi test xong; không trỏ vào database production.
