# CampusRent

Nền tảng tìm phòng trọ sinh viên theo trường đại học. React 18 + Vite, ASP.NET Core 8, Entity Framework Core; SQLite cho phát triển và PostgreSQL cho production.

## Chức năng

- Tìm kiếm theo từ khóa, trường, giá, tiện ích, bán kính; sắp xếp và phân trang.
- Danh sách và bản đồ Leaflet; ảnh, thông tin chủ trọ, chi phí minh bạch.
- Dự toán tiền thuê + điện + nước + Internet + dịch vụ theo mức sử dụng.
- Đăng ký sinh viên/chủ trọ, đăng nhập JWT, cập nhật hồ sơ.
- Lưu phòng, đặt lịch xem, chủ trọ xác nhận, hủy lịch.
- Chủ trọ đăng/sửa/ẩn tin, đánh dấu đã thuê, gia hạn 30 ngày; upload Cloudinary hoặc URL HTTPS.
- Đánh giá một lần/phòng, lọc từ cấm, báo cáo tin/đánh giá và quản trị kiểm duyệt.
- Tự ẩn tin hết hạn, kiểm soát quyền sở hữu và vai trò, validation, rate limit.

Email có đuôi trường học chưa đủ để chứng minh quyền sở hữu nên không tự cấp huy hiệu. Dữ liệu demo có tài khoản sinh viên đã xác thực để minh họa.

## Chạy nhanh tại máy

Yêu cầu .NET SDK 8 trở lên và Node.js 22.12 trở lên.

```sh
dotnet restore server
npm --prefix client ci
```

Terminal 1:

```sh
dotnet run --project server --urls http://localhost:5059
```

Terminal 2:

```sh
npm --prefix client run dev
```

Mở [localhost:5173](http://localhost:5173). API [localhost:5059](http://localhost:5059/api/universities), Swagger [localhost:5059/swagger](http://localhost:5059/swagger), health [localhost:5059/health](http://localhost:5059/health).

Development tự tạo `server/campusrent.db`, 5 trường, 5 phòng và tài khoản demo. Không cần PostgreSQL để trải nghiệm local. Vite proxy `/api` đến port 5059. Để đổi backend, dùng `client/.env.example`. Khi chạy binary trực tiếp, đặt `ASPNETCORE_ENVIRONMENT=Development` để dùng chế độ local.

| Vai trò | Email | Mật khẩu development |
| --- | --- | --- |
| Sinh viên | nguyenvanan@sis.hust.edu.vn | Password@123 |
| Chủ trọ | landlord1@gmail.com | Password@123 |
| Quản trị | admin@campusrent.vn | Password@123 |

Các tài khoản này chỉ được seed trong Development. Production dùng admin được cấu hình riêng bằng biến môi trường.

## Kiểm thử

```sh
dotnet test tests/CampusRent.Tests
npm --prefix client test
npm --prefix client run build
dotnet build server
cd client
npx playwright install chromium
npm run test:e2e
```

Máy có Chrome có thể đặt `PLAYWRIGHT_CHANNEL=chrome` để sử dụng Chrome đã cài. E2E tự mở API port 5065 và Vite port 5174, dùng DB `campusrent.e2e.db` riêng. Cần build server trước E2E; tránh build lại khi một server dùng cùng output đang chạy trên Windows.

Test backend dùng SQLite quan hệ thật và cơ sở dữ liệu riêng cho từng test. Test PostgreSQL chỉ chạy khi có biến `CAMPUSRENT_TEST_POSTGRES` chứa connection string tới DB kiểm thử; CI đã cấu hình sẵn. E2E bao phủ tìm/lọc phòng, bản đồ, chi phí, lưu phòng, đăng ký/đăng nhập, đặt/hủy lịch, đánh giá, báo cáo, hồ sơ, đăng/sửa/ẩn tin và quản trị trên desktop/mobile.

## Triển khai

Đọc [hướng dẫn triển khai](docs/DEPLOYMENT.md). Có Dockerfile nhiều giai đoạn, Compose với PostgreSQL, migration chạy trước ứng dụng, healthcheck và GitHub Actions CI. Cần điền secret, cấu hình domain/HTTPS và chạy kiểm tra PostgreSQL/container trên môi trường deploy trước khi công khai.

Migration chuẩn nằm trong `server/Migrations/`; `db.sql` và Razor Views là tài liệu/khung giao diện lịch sử, không được dùng bởi ứng dụng React hiện tại.

## Cấu trúc

```text
client/src/
  components/   Giao diện dùng chung, map, modal, route bảo vệ
  context/      Phiên tài khoản, phòng đã lưu, thông báo
  lib/          API client, định dạng và tính chi phí
  pages/        Tìm phòng, chi tiết, tài khoản, chủ trọ, quản trị
client/e2e/     Playwright desktop/mobile
server/
  Controllers/  REST API
  Data/         DbContext, seed và design-time factory
  DTOs/         Hợp đồng API và validation
  Migrations/   Schema PostgreSQL
  Models/       Thực thể
  Services/     JWT, khoảng cách, chi phí, lọc bình luận, Cloudinary
  BackgroundServices/
tests/CampusRent.Tests/
docs/           Đặc tả, nhật ký, SQL migration và hướng dẫn triển khai
```

Nhật ký từng mốc: [IMPLEMENTATION.md](docs/IMPLEMENTATION.md).
