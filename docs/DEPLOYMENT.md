# Triển khai CampusRent

Ứng dụng production chạy React và API cùng origin trong một container .NET 8; PostgreSQL lưu dữ liệu trên volume riêng. HTTPS được kết thúc tại reverse proxy (Caddy, nginx hoặc ingress). Không có tài khoản/phòng mẫu trong production.

## Chạy với Docker Compose

Yêu cầu Docker Engine và Docker Compose v2.

1. Sao chép `.env.example` thành `.env`.
2. Thay toàn bộ giá trị `CHANGE_ME`. Sinh JWT secret bằng `openssl rand -base64 48` hoặc một bộ sinh mật khẩu an toàn. Mật khẩu admin tối thiểu 12 ký tự. Đặt `CLIENT_URL=https://ten-mien-cua-ban.vn`.
3. Chạy `docker compose config --quiet`, sau đó `docker compose up app --build -d`.
4. Kiểm tra `docker compose ps` và `curl --fail http://localhost:8080/health`.
5. Cấu hình reverse proxy cùng máy chuyển HTTPS đến `127.0.0.1:8080`. Port container chỉ bind loopback. Nếu proxy ở máy/container khác, điều chỉnh binding/network phù hợp.
6. Đăng nhập bằng `ADMIN_EMAIL` và `ADMIN_PASSWORD` đã cấu hình. Chủ trọ/sinh viên tự đăng ký; tài khoản admin không thể tự đăng ký qua API.

Service `migrate` áp dụng migrations một lần trước khi app khởi động. Không chạy migrations đồng thời từ nhiều máy. Khi cập nhật: sao lưu DB, kiểm tra SQL migration rồi chạy lại `docker compose up app --build -d`.

Cloudinary là tùy chọn. Điền ba biến `CLOUDINARY_*` để upload ảnh từ máy. Khi chưa cấu hình, form vẫn nhận đường dẫn ảnh HTTPS. Ảnh được kiểm tra định dạng và giới hạn 5 MB. Ảnh demo sử dụng Unsplash; tile bản đồ lấy từ OpenStreetMap, font Google có font dự phòng. Với lưu lượng lớn, cấu hình dịch vụ tile phù hợp và chuyển ảnh demo sang storage của bạn.

Để gợi ý mọi địa chỉ theo thời gian thực, đặt `GOOGLE_PLACES_API_KEY` trong `.env`, bật Places API (New) cho project Google Cloud và giới hạn khóa ở mức server. Khóa chỉ được dùng ở backend, được cache 10 phút và endpoint gợi ý giới hạn 30 yêu cầu/phút/IP. Khi chưa có khóa, ô tìm kiếm vẫn gợi ý trường và địa chỉ phòng đang có trong CampusRent.

## Migrations và database có sẵn

- Nguồn schema hiện hành: `server/Migrations/`.
- SQL có thể kiểm tra trước: `docs/schema.postgres.sql`.
- CLI: `dotnet tool restore`, sau đó `dotnet ef database update --project server` với biến `ConnectionStrings__DefaultConnection`.
- Hoặc chạy binary đã publish với `--migrate`; cần cấu hình Postgres và JWT secret giống app.
- Không import `db.sql` cũ: đây là schema lịch sử, không khớp mô hình EF hiện tại.
- Với DB đã có dữ liệu theo schema cũ, tạo bản sao, đối chiếu và viết migration chuyển đổi dữ liệu trước. Initial migration dành cho database/schema mới; không xóa dữ liệu để ép migration chạy.
- SQLite chỉ phục vụ Development/Testing. Khi model thay đổi, sao lưu file demo rồi tạo DB development mới; không dùng EnsureCreated để nâng cấp DB có dữ liệu thật.

## Sao lưu và vận hành

Sao lưu PostgreSQL bằng `pg_dump` định kỳ. Ví dụ xuất file backup từ container:

```sh
docker compose exec -T db pg_dump -U campusrent -d campusrent -Fc > campusrent-backup.dump
```

Kiểm tra phục hồi trên database riêng trước khi cần dùng backup. Volume `postgres_data` phải được giữ qua các lần deploy; không dùng `docker compose down -v` khi cần giữ dữ liệu.

Theo dõi `/health`, HTTP 5xx và `docker compose logs app`. Worker tự ẩn tin hết hạn và kết thúc lịch hẹn cũ mỗi 30 phút. Các truy vấn công khai loại bỏ tin hết hạn ngay cả trước lần quét kế tiếp.

Để xoay JWT secret, cập nhật secret và khởi động lại app; các phiên hiện tại phải đăng nhập lại. Session JWT hiện có hạn 12 giờ. Rate limit đăng nhập là 20 yêu cầu/phút/IP trên mỗi instance; khi chạy nhiều instance cần giới hạn bổ sung ở reverse proxy. Cấu hình trusted proxy phù hợp nếu muốn dùng địa chỉ IP thật cho rate limit.

## CI và phạm vi xác minh

Workflow `.github/workflows/ci.yml` chạy unit/integration, PostgreSQL thật, E2E desktop/mobile, kiểm tra dependency và build/chạy Docker Compose. Test PostgreSQL dùng schema tạm riêng, kiểm tra migrations chạy lại và đọc/ghi API.

Máy phát triển hiện tại chưa có Docker/PostgreSQL. Các test SQLite, trình duyệt Chrome, build frontend, publish .NET và tính nhất quán migration được chạy tại đây. Job container và PostgreSQL cần chạy trên CI/máy có dịch vụ tương ứng trước khi đưa lên production.

Email trường không tự cấp huy hiệu xác thực. Production chưa có dịch vụ gửi email/khôi phục mật khẩu; cần tích hợp nhà cung cấp email nếu muốn mở các luồng đó. Đặt lịch và xác nhận hoạt động ngay trong app.

Tham chiếu kỹ thuật: [EF Core migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying), [Docker Compose startup order](https://docs.docker.com/compose/how-tos/startup-order/).
