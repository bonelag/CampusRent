# Kết quả kiểm thử

Kiểm tra tại workspace ngày 10/11/2026 (Asia/Saigon), với .NET SDK 8.0.426, Node.js 22.23.4, Chrome và Docker/Compose trong Ubuntu 22.04 WSL.

GitHub Actions dùng runner cố định `ubuntu-24.04`, tương thích Playwright 1.58.2. Không dùng `ubuntu-latest` vì nhãn này có thể chuyển sang Ubuntu 26.04, khiến bước cài Chromium/dependencies thất bại trước khi E2E chạy.

| Nhóm | Kết quả |
| --- | --- |
| Backend xUnit + HTTP integration | 96 đạt, không bỏ qua |
| PostgreSQL migrations/persistence | Đạt với PostgreSQL 16 thật, schema riêng cho test |
| Frontend Vitest / Testing Library | 22 đạt, gồm kiểm tra menu đăng tin của ADMIN |
| Playwright desktop + mobile | 28 đạt |
| Rà giao diện ở 320/360/390/768/1024/1440 px | 48 màn hình, không tràn ngang hoặc lỗi JavaScript sau khi tải font |
| Vite production build | Đạt |
| .NET build / Docker Release publish | Đạt; build .NET không có warning |
| EF migration/model consistency | Không có model chưa migrate; SQL export gồm ba migration |
| Installer Linux | 7 kiểm tra đạt, gồm pipeline curl/sh/Bash và triển khai Docker thực |
| Cập nhật installer | Hai lần rollout liên tiếp giữ dữ liệu và JWT của phiên đã có |
| Stack local / Caddy | Compose web/db, database health, SPA deep link và Caddy config validation đạt |
| GitHub Actions | Actionlint và ShellCheck workflow đạt |
| npm audit / NuGet advisory scan | Không phát hiện advisory tại thời điểm kiểm tra |

## Các phần đã được kiểm tra

Bộ xUnit được chia theo trách nhiệm thành 63 file C#; file lớn nhất là 96 dòng. Fixture/helper nằm trong `Infrastructure/`, các ca kiểm thử nằm trong thư mục chức năng và class có tên trùng file. Xem [hướng dẫn tổ chức backend tests](../tests/CampusRent.Tests/README.md). Toàn bộ 96 ca, gồm dữ liệu Theory, đã chạy với SQLite/PostgreSQL.

Backend bao phủ đăng ký người tìm trọ, chặn tự cấp chủ trọ/admin, đăng nhập/JWT, chuẩn hóa email, quyền sở hữu và vai trò hiện tại, tìm kiếm/bán kính/giá/tiện ích, dự toán chi phí, đăng/sửa/ẩn/gia hạn tin, thay ảnh, đánh giá duy nhất và kiểm duyệt, báo cáo, yêu thích, đặt/xác nhận/hủy/đặt lại lịch, UTC và hết hạn tin.

Các ca mới kiểm tra duyệt/hạ quyền với JWT đã cấp, đổi mật khẩu và vô hiệu hóa phiên cũ, giữ ít nhất một admin, ràng buộc một yêu cầu chủ trọ PENDING trong database, tệp xác minh chỉ dành cho owner/admin, từ chối đính kèm tệp của tài khoản khác, upload ảnh local khi chưa có Cloudinary, chuẩn hóa mật khẩu đặc biệt trong connection string và xử lý timeout/cancellation của dịch vụ vị trí.

Frontend/E2E kiểm tra đăng nhập và giữ phiên, đăng ký, lưu/bỏ lưu, tìm/lọc/bản đồ/chi phí, lịch hẹn, đánh giá/báo cáo, hồ sơ, đăng/sửa/ẩn tin, quản trị, tệp ảnh/PDF riêng tư, nâng cấp chủ trọ, đổi mật khẩu và Google suggestion chưa có tọa độ. Có kiểm tra font-loaded responsive, tương phản nhãn active và điều hướng mobile. Giao diện được rà với guest, sinh viên, chủ trọ và admin.

Installer được kiểm tra dưới shell POSIX và Bash với input từ terminal khi stdin chứa script; mật khẩu không xuất hiện trong output, ký tự `;`, dấu nháy, dấu `$`, `#`, `:` và backslash được giữ nguyên. Cấu hình có quyền truy cập giới hạn, các input sai được từ chối và installer không ghi secret vào checkout. Rollout thật dùng PostgreSQL/container mới riêng cho test, xác minh migrations, health và dữ liệu/phiên sau cập nhật.

## Tối ưu đã đo được

Các trang phụ được tải khi cần. Gói JavaScript chính giảm từ **113,77 KB xuống 44,06 KB**; gzip giảm từ **31,22 KB xuống 14,21 KB**. API/static assets có response compression. Ảnh local được giải quyết theo origin API đã cấu hình, kể cả khi frontend/backend khác port.

Rà ảnh màn hình phát hiện CSS desktop ghi đè menu mobile và nhãn active có màu trùng nền. Đã sửa thứ tự CSS, bố cục header ở tablet/điện thoại nhỏ, tương phản và chiều cao card. Các kiểm tra responsive mới đã đạt ở cả hai project Playwright.

## Chạy lại

```sh
dotnet test tests/CampusRent.Tests
npm --prefix client test
npm --prefix client run build
npm --prefix client run format:check
dotnet build server
npm --prefix client run test:e2e
```

Test PostgreSQL cần biến `CAMPUSRENT_TEST_POSTGRES` trỏ tới database kiểm thử có quyền tạo schema; fixture tự tạo và xóa schema riêng. Tránh dùng database production. E2E có thể đặt `CAMPUSRENT_E2E_DB` và `CAMPUSRENT_E2E_UPLOADS` để cô lập dữ liệu mỗi lần chạy; Chrome đã cài được chọn bằng `PLAYWRIGHT_CHANNEL=chrome`.

Kiểm tra installer trên Linux với quyền root/sudo:

```sh
sudo python3 tests/deployment/test_deploy.py
sudo python3 tests/deployment/test_deploy.py --docker
sh -n deploy.sh
bash -n deploy.sh
shellcheck deploy.sh
```

Tùy chọn `--docker` tạo database/container test riêng, build và chạy installer hai lần, rồi dọn các tài nguyên của test. Cần Docker Engine, Compose v2 và kết nối Internet. ShellCheck cần được cài để chạy kiểm tra tĩnh. CI chạy cả bộ kiểm tra installer/container trước khi xuất bản image.

Kiểm tra dependency và schema:

```sh
npm --prefix client audit --audit-level=moderate
dotnet list server package --vulnerable --include-transitive
dotnet ef migrations has-pending-model-changes --project server
```

## Bổ sung ngày 11/11/2026: proxy Nginx đã có

- Bộ kiểm tra installer Linux đạt **10/10**, gồm curl/sh/Bash, ShellCheck, chọn `caddy`/`external`, giữ lựa chọn proxy/JWT/credential khi chạy lại, tương thích cấu hình cũ và từ chối proxy/input không hợp lệ.
- Rollout Docker/PostgreSQL thật được chạy hai lần cho mỗi chế độ HTTP và proxy `external`, giữ dữ liệu/tài khoản/JWT. Chế độ `external` chỉ xác nhận backend, không công bố HTTPS đã hoạt động khi Nginx chưa được cấu hình.
- Ca external được chạy thêm với container Caddy ở trạng thái Created và đang chạy thuộc cùng project; cả hai được dừng trong khi backend và phiên đã có vẫn hoạt động.
- Mẫu `docs/nginx/campusrent.conf` đạt `nginx -t` trong container Nginx Alpine. Mẫu chuyển tiếp về loopback, giữ Host/forwarded headers và cho phép request 20 MB để upload ảnh/PDF.
- Các kiểm tra backend/frontend/E2E phía trên là kết quả ngày 10/11; lần sửa installer này không chạy lại các bộ đó. Nginx/DNS/chứng chỉ trên VPS thực tế cần kiểm tra bằng lệnh trong hướng dẫn triển khai.

## Bổ sung ngày 11/11/2026: ADMIN, dữ liệu thị trường và launcher production

- ADMIN hiển thị và truy cập chức năng đăng/quản lý phòng giống LANDLORD; ca frontend mới kiểm tra cả menu desktop và mobile.
- CLI `--import-sample-data` tạo idempotent 20 bản ghi liên kết hợp lệ trong từng bảng nghiệp vụ. Hai mươi phòng giữ giá, diện tích, địa chỉ, ảnh và URL nguồn từ Phongtro123/Thuephongtro; tài khoản và hoạt động được đánh dấu là dữ liệu mẫu.
- Importer đã chạy hai lần trên PostgreSQL 16 cô lập, sau đó chạy và chạy lại thành công trên PostgreSQL production: mỗi bảng được xác nhận đúng 20 bản ghi do importer quản lý.
- `run.bat` tải `.env`, yêu cầu xác nhận `PRODUCTION`, thay backend CampusRent cũ trên cổng 5059 và kết nối database production với đầy đủ quyền. `run.ps1` không tham số vẫn dùng SQLite development.
- Kết quả cuối: 96/96 backend với PostgreSQL thật, 22/22 frontend, build .NET không warning, Vite build/Prettier đạt, EF không có model chưa migrate và 28/28 Playwright desktop/mobile đạt.

## Báo cáo và giới hạn

Playwright report nằm ở `client/playwright-report/`. Ảnh và kết quả rà thủ công nằm trong `artifacts/ui-review/`; các output này được Git bỏ qua. Coverage dùng `tests/coverage.runsettings`, loại migration sinh tự động và Razor Views lịch sử. E2E không cộng vào coverage .NET; không khẳng định coverage 100%.

Chưa triển khai lên domain công khai hoặc gửi email. Chưa gọi Cloudinary/Google Places bằng credential production; luồng local, fallback và client mock đã được kiểm tra. DNS, chứng chỉ HTTPS của domain thật, credential đã được xoay và chính sách backup phải được xác minh trên máy chủ sẽ vận hành.
