# Tổ chức kiểm thử backend

Mỗi file kiểm tra một chức năng hoặc một nhóm tình huống liên quan. Class có tên trùng với file để dễ tìm và chạy riêng; các file không gom nhiều tính năng thành một bộ kiểm thử lớn.

| Thư mục | Trách nhiệm |
| --- | --- |
| `Authentication/` | Đăng ký, đăng nhập, JWT, thay đổi vai trò và mật khẩu |
| `Accounts/` | Cập nhật hồ sơ |
| `Rooms/` | Đăng/sửa tin, quyền sở hữu, hiển thị, tìm kiếm và validation |
| `Bookings/` | Tạo, xác nhận, hủy và validation lịch hẹn |
| `Favorites/` | Lưu/bỏ lưu và phòng không tồn tại |
| `Reviews/` | Tạo, validation và tính rating |
| `Reports/` | Validation báo cáo |
| `Administration/` | Kiểm duyệt, quyền quản trị và quản lý người dùng |
| `LandlordRequests/` | Gửi, duyệt, hủy và bảo vệ yêu cầu chủ trọ |
| `Uploads/` | Ảnh phòng, ảnh/PDF xác minh và quyền truy cập tệp |
| `Places/` | API gợi ý vị trí |
| `Services/` | Khoảng cách, chi phí, từ cấm, clients vị trí và connection string |
| `Persistence/` | Seed, ràng buộc dữ liệu và migrations PostgreSQL |
| `Workers/` | Tự ẩn tin hết hạn |
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
