# Triển khai CampusRent

## Cài nhanh trên máy chủ Linux

```sh
curl -fsSL https://raw.githubusercontent.com/bonelag/CampusRent/main/deploy.sh | sh
```

Có thể thay `sh` bằng `bash`. Installer đọc câu trả lời từ `/dev/tty`, nên các câu hỏi PostgreSQL vẫn hoạt động khi script được tải qua pipe. Lệnh trên dùng cho repository public. Kho `bonelag/CampusRent` hiện private; cần xác thực theo hướng dẫn dưới đây.

### Repository private

Tạo file cấu hình curl ngoài checkout, điền token GitHub có quyền đọc repository rồi đặt quyền `600`:

```sh
mkdir -p "$HOME/.config/campusrent"
cat > "$HOME/.config/campusrent/github-curl.conf" <<'CURL'
header = "Authorization: Bearer YOUR_GITHUB_TOKEN"
header = "Accept: application/vnd.github.raw+json"
CURL
chmod 600 "$HOME/.config/campusrent/github-curl.conf"
```

Thay `YOUR_GITHUB_TOKEN` trong file bằng token của bạn trước khi chạy:

```sh
curl -fsSL --config "$HOME/.config/campusrent/github-curl.conf" \
  'https://api.github.com/repos/bonelag/CampusRent/contents/deploy.sh?ref=main' \
  | CAMPUSRENT_GITHUB_CURL_CONFIG="$HOME/.config/campusrent/github-curl.conf" sh
```

Installer dùng lại file cấu hình cho bước tải source private; token không nằm trong đối số command hoặc cấu hình ứng dụng. File credential bị loại khỏi Git và Docker build. Với checkout đã được clone bằng quyền Git của bạn, có thể dùng lệnh cài từ checkout ngay bên dưới.

Để cài ngay từ checkout hiện tại trên Linux:

```sh
CAMPUSRENT_SOURCE_DIR="$PWD" sh deploy.sh
```

Máy chủ cần Ubuntu 22.04+ hoặc Debian 12+, Internet và quyền root/sudo. Nếu Docker Engine + Compose v2 đã có, script dùng lại. Khi thiếu Docker, script cài từ repository chính thức của Docker. Trên bản Linux khác, cài Docker/Compose, Python 3, curl và tar trước khi chạy.

## Thông tin installer hỏi

| Thông tin | Cách điền |
| --- | --- |
| PostgreSQL host | Host truy cập được từ container; dùng host pooler và username đầy đủ do Supabase/Neon cấp nếu cần |
| PostgreSQL port | Thường là `5432`; pooler có thể dùng port khác |
| Database name, username, password | Tài khoản có quyền kết nối và tạo/cập nhật schema ứng dụng |
| SSL | `VerifyFull` mặc định xác minh chứng chỉ bằng CA hệ thống; `Require` mã hóa mà không xác minh hostname; `Disable` chỉ cho mạng nội bộ tin cậy |
| Tên miền | Ví dụ `campusrent.example.com`, không gồm scheme, port hoặc đường dẫn |
| Port HTTP | Mặc định `8080`; khi có HTTPS, port này chỉ bind `127.0.0.1` |
| Admin email, password | Mật khẩu tối thiểu 12 ký tự và tối đa 72 byte UTF-8; input được ẩn |

PostgreSQL phải có sẵn; installer không tạo hoặc xóa database bên ngoài. `localhost` được đổi thành `host.docker.internal`, nhưng PostgreSQL trên host vẫn phải cho phép kết nối từ mạng Docker. Script kiểm tra kết nối từ một container PostgreSQL client trước khi khởi động web, và không truyền mật khẩu qua đối số shell hoặc ghi mật khẩu vào log.

Trỏ DNS A/AAAA của tên miền vào máy chủ và cho phép TCP 80/443 trước khi bật HTTPS. Caddy tự cấp và gia hạn chứng chỉ. Nếu máy đã có reverse proxy chiếm các port này, dùng cấu hình proxy hiện có và đọc mục chạy thủ công. Để trống tên miền chỉ dành cho kiểm tra HTTP; cấu hình HTTPS trước khi nhận tài khoản thật.

Một chuỗi kết nối có mật khẩu từng xuất hiện trong mẫu cấu hình cũ. Chủ database cần đổi credential đó trước khi dùng production; mẫu hiện tại đã được thay bằng placeholder.

## Installer tạo và giữ những gì

Thư mục mặc định là `/opt/campusrent`:

```text
runtime/
  appsettings.Production.json     Cấu hình gốc, gồm JWT secret và PostgreSQL
  appsettings.<timestamp>.json    Snapshot cấu hình cho từng lần triển khai
  pgpass                         Credential chỉ dùng cho kiểm tra PostgreSQL
  deploy.env                     Port, network, đường dẫn; không chứa mật khẩu/JWT
releases/<timestamp>/             Source và compose của từng bản
current -> releases/<timestamp>/  Bản triển khai đã qua healthcheck
```

Thư mục `runtime` có quyền `0700`; JSON cấu hình có quyền `0640` cho UID ứng dụng `1654`. Password và JWT không được đưa vào Git hoặc Docker image. Container web chạy bằng user ứng dụng, filesystem chỉ đọc và một vùng `/tmp` riêng. Volume `uploads_data` giữ ảnh phòng và tệp xác minh qua các lần cập nhật.

`compose.production.yaml` chạy `web` với PostgreSQL bên ngoài. Profile `https` thêm Caddy. Installer tạo network riêng, cấu hình danh sách network reverse proxy được tin cậy, build React/API, áp dụng EF migrations trong quá trình khởi động và chờ `/health`. Mỗi container dùng snapshot cấu hình riêng để việc cập nhật không đổi secret của container đang chạy.

Production chỉ seed trường đại học và tạo admin nếu chưa có; không seed phòng hoặc tài khoản demo. Admin đã tồn tại giữ nguyên mật khẩu. Tài khoản mới luôn là người tìm trọ; chủ trọ gửi yêu cầu trên `/account` để admin duyệt tại `/admin`. Quyền mới có hiệu lực với phiên hiện tại. Đổi mật khẩu tại trang tài khoản sẽ vô hiệu hóa các phiên cũ. Phiên tạo bởi phiên bản cũ cần đăng nhập lại một lần.

## Cập nhật và vận hành

Chạy lại lệnh cài nhanh để lấy source mới và build lại. Khi có cấu hình, mặc định installer dùng lại credential, JWT, project/network và volume tải lên. Không tự xóa database hoặc volume. Sao lưu PostgreSQL trước khi cập nhật schema.

```sh
sudo docker compose --project-name campusrent \
  --env-file /opt/campusrent/runtime/deploy.env \
  -f /opt/campusrent/current/compose.production.yaml \
  --profile https ps

sudo docker compose --project-name campusrent \
  --env-file /opt/campusrent/runtime/deploy.env \
  -f /opt/campusrent/current/compose.production.yaml \
  --profile https logs --tail 100 -f
```

Bỏ `--profile https` khi dùng HTTP để kiểm tra. Theo dõi `/health`, log `web`/`caddy`, HTTP 5xx và hạn sử dụng chứng chỉ. Log container được xoay với giới hạn dung lượng. Nếu khởi động thất bại, installer in lệnh chẩn đoán của release đó và giữ cấu hình/dữ liệu để xử lý. Migrations không được tự động rollback.

Để đổi tên miền/database/credential bằng câu hỏi tương tác:

```sh
curl -fsSL https://raw.githubusercontent.com/bonelag/CampusRent/main/deploy.sh | CAMPUSRENT_RECONFIGURE=1 sh
```

Để đổi tích hợp hoặc JWT, sửa cấu hình gốc bằng `sudoedit /opt/campusrent/runtime/appsettings.Production.json`, rồi chạy lại installer và chọn dùng lại cấu hình. Đổi JWT secret khiến các phiên hiện tại phải đăng nhập lại. Không sửa snapshot của container đang chạy.

Các tùy chọn: `CAMPUSRENT_INSTALL_DIR` đổi thư mục, `CAMPUSRENT_REPO`/`CAMPUSRENT_REF` chọn repository/ref, `CAMPUSRENT_PROJECT` đổi tên project Docker và `CAMPUSRENT_NETWORK_CIDR` chọn subnet riêng không trùng network hiện có. Giữ tên project khi cập nhật để dùng đúng volume dữ liệu.

## Cấu hình tự động và tích hợp tùy chọn

`sh deploy.sh --help` liệt kê các biến `CAMPUSRENT_DB_HOST`, `CAMPUSRENT_DB_PORT`, `CAMPUSRENT_DB_NAME`, `CAMPUSRENT_DB_USER`, `CAMPUSRENT_DB_PASSWORD`, `CAMPUSRENT_DB_SSL`, `CAMPUSRENT_ADMIN_EMAIL`, `CAMPUSRENT_ADMIN_PASSWORD`, `CAMPUSRENT_DOMAIN` và `CAMPUSRENT_PORT`. Dùng secret manager của môi trường khi gọi `sh deploy.sh --non-interactive`; không đặt mật khẩu trực tiếp trong shell history.

`sh deploy.sh --configure-only` chỉ tạo cấu hình, không cài Docker hay chạy app. Khi truyền tùy chọn qua pipe, cú pháp shell là `curl ... | sh -s -- --configure-only`.

Ảnh phòng được lưu local khi chưa có Cloudinary; URL HTTPS vẫn được hỗ trợ. Để bật Cloudinary, thêm `Cloudinary.CloudName`, `Cloudinary.ApiKey`, `Cloudinary.ApiSecret` trong JSON cấu hình gốc. Ảnh hồ sơ xác minh và PDF luôn được lưu riêng tư; chỉ người gửi và admin được tải bằng JWT. Sao lưu volume tải lên cùng database.

Gợi ý vị trí kết hợp trường/địa chỉ phòng với Photon/OpenStreetMap. Có thể thêm `GooglePlaces.ApiKey` để dùng Places API (New); khóa chỉ ở backend. Các gợi ý được cache 10 phút và giới hạn 30 yêu cầu/phút/IP. Font Google và tile OpenStreetMap cần Internet; chọn dịch vụ tile phù hợp khi tăng lưu lượng.

Rate limit đăng nhập là 20 yêu cầu/phút/IP trên mỗi instance. Cấu hình trusted proxy hiện tại dành cho network của installer. Khi đổi proxy/network hoặc chạy nhiều instance, cập nhật allowlist và giới hạn tại proxy tương ứng. Chưa tích hợp SMTP/khôi phục mật khẩu qua email; đổi mật khẩu trong phiên đăng nhập đã hoạt động.

## Stack local có PostgreSQL riêng

`compose.yaml` là stack `web`/`db` độc lập, giữ database trong `postgres_data`. Sao chép `.env.example` sang `.env`, thay mọi `CHANGE_ME`, rồi chạy:

```sh
docker compose config --quiet
docker compose up -d --build --wait --wait-timeout 180
curl --fail http://localhost:8080/health
```

Web bind loopback mặc định. `JWT_SECRET`, `ADMIN_EMAIL`, `ADMIN_PASSWORD` và `POSTGRES_PASSWORD` phải được điền; không có secret mặc định. Với volume database cũ, dùng đúng password hiện tại. Đổi biến `POSTGRES_PASSWORD` không tự đổi password bên trong PostgreSQL. PostgreSQL user `postgres` được giữ tương thích với stack cũ. Dùng installer/`compose.production.yaml` cho database bên ngoài.

## Schema và sao lưu

Nguồn schema là `server/Migrations/`; `docs/schema.postgres.sql` chứa SQL idempotent của cả ba migration. Không import `db.sql` lịch sử. Có thể chạy `dotnet tool restore` và `dotnet ef database update --project server`, hoặc binary đã publish với `--migrate`, cùng cấu hình PostgreSQL/JWT của app.

Migration mới yêu cầu mỗi tài khoản chỉ có một yêu cầu chủ trọ PENDING. Trước khi nâng cấp database cũ, kiểm tra:

```sql
SELECT user_id, COUNT(*)
FROM landlord_requests
WHERE status = 'PENDING'
GROUP BY user_id
HAVING COUNT(*) > 1;
```

Nếu có kết quả, admin xử lý các yêu cầu trùng trước khi áp dụng migration; giữ lịch sử thay vì xóa database. Với schema lịch sử khác EF hiện tại, đối chiếu trên bản sao và viết migration chuyển dữ liệu.

Sao lưu PostgreSQL qua công cụ của nhà cung cấp hoặc `pg_dump`. Với stack local:

```sh
docker compose exec -T db pg_dump -U postgres -d campusrent -Fc > campusrent-backup.dump
```

Sao lưu volume tải lên của installer:

```sh
sudo docker run --rm -v campusrent_uploads_data:/data:ro \
  -v "$PWD":/backup alpine tar -czf /backup/campusrent-uploads.tar.gz -C /data .
```

Kiểm tra phục hồi trên database/volume riêng. Giữ cấu hình gốc trong nơi sao lưu có kiểm soát quyền truy cập. Không dùng `docker compose down -v` hoặc xóa volume trên hệ thống cần giữ dữ liệu.

## Kiểm chứng

Xem [TESTING.md](TESTING.md) cho kết quả hiện tại. Installer đã được kiểm tra với shell POSIX/Bash, input mật khẩu qua pipe, PostgreSQL thật và hai lần triển khai Docker liên tiếp giữ dữ liệu/phiên. DNS và chứng chỉ của tên miền thực tế phải được kiểm tra khi cài trên máy chủ của bạn.
