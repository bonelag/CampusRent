# CampusRent

[![CampusRent CI](https://github.com/bonelag/CampusRent/actions/workflows/ci.yml/badge.svg)](https://github.com/bonelag/CampusRent/actions/workflows/ci.yml)

Nền tảng tìm kiếm, dự toán chi phí, đặt lịch xem phòng và đánh giá phòng trọ sinh viên theo bán kính trường đại học. Xây dựng trên **React 18 + Vite 6**, **ASP.NET Core 8 Web API** và **Entity Framework Core 8** (dùng **SQLite** khi phát triển cục bộ và **PostgreSQL 16** trên môi trường Production).

---

## Tính năng chính

- **Tìm kiếm theo bán kính trường học & Bản đồ tương tác (`React-Leaflet` + Haversine):**
  - Lọc theo từ khóa, trường đại học, khoảng giá, tiện nghi và bán kính địa lý thực tế; hỗ trợ sắp xếp và phân trang giữ nguyên bộ lọc.
  - Gợi ý vị trí tức thời kết hợp danh mục trường nội bộ, Google Places và OpenStreetMap/Photon.
  - Lăn chuột phóng to/thu nhỏ trực tiếp khi con trỏ nằm trong vùng bản đồ (không làm cuộn trượt trang) và hiển thị chấm mốc đỏ tại đúng tâm vùng chọn hoặc địa điểm tìm kiếm.
- **Minh bạch chi phí & Dự toán sinh hoạt hàng tháng:**
  - Công khai rõ giá thuê cơ bản, đơn giá điện, nước, Internet, phí dịch vụ và tính tổng chi phí ước tính theo mức tiêu thụ thực tế.
- **Xác thực JWT, Xác minh Email OTP (`Gmail SMTP`) & Phân quyền chặt chẽ:**
  - Mọi tài khoản đăng ký mới khởi tạo với vai trò `STUDENT`. Để trở thành `LANDLORD`, người dùng gửi hồ sơ nâng cấp kèm ảnh/PDF xác minh riêng tư (chỉ chủ hồ sơ và `ADMIN` truy cập được) để quản trị viên xét duyệt.
  - Cấu hình máy chủ **Gmail SMTP động** trực tiếp trên giao diện Quản trị (`smtp_settings`, mật khẩu ứng dụng mã hóa **AES-GCM 256-bit**), hỗ trợ gửi email kiểm tra ngay lập tức.
  - Xác minh email bằng mã **OTP 6 chữ số** (băm SHA-256, hiệu lực 15 phút, giới hạn gửi lại 60 giây và tối đa 5 lần nhập sai); bắt buộc xác minh email trước khi đăng bài phòng trọ.
  - Quản trị viên có thể cấp/thu hồi thủ công trạng thái **Email đã xác minh** và **Sinh viên `.edu`**; mọi thay đổi vai trò hoặc mật khẩu có hiệu lực tức thời trên các phiên JWT đã cấp.
- **Chống review bẩn & Xếp hạng uy tín có trọng số:**
  - Mỗi người dùng chỉ đánh giá tối đa 1 lần trên mỗi phòng, chặn chủ phòng tự đánh giá và tự động gắn cờ ngôn từ vi phạm qua `ProfanityFilterService`.
  - Đánh giá từ tài khoản sinh viên xác thực (`.edu.vn` / `IsEduVerified`) được tính trọng số **`2.0x`**; kết hợp trung bình Bayesian (`RecommendationScore`) để tự động cảnh báo và đẩy phòng/chủ trọ có điểm uy tín thấp xuống cuối danh sách tìm kiếm.
- **Đặt lịch xem phòng, Nhắc lịch tự động & Vòng đời tin 30 ngày:**
  - Sinh viên đặt/hủy lịch xem phòng; chủ trọ xác nhận lịch hẹn.
  - Tiến trình nền `AutoExpireRoomsWorker` tự động gửi email nhắc lịch trong ngày hẹn, tự động chuyển phòng sang `HIDDEN` sau **`+3 ngày`** kể từ lịch hẹn đã xác nhận (trừ khi chủ trọ đánh dấu **"Khách chưa chốt"**), và tự hết hạn tin đăng sau **30 ngày** kèm nút kích hoạt lại chu kỳ 30 ngày.
- **Giao diện Sáng / Tối (`Light / Dark Mode`) & Bảng điều khiển `Adminator` (10 phân hệ):**
  - Hỗ trợ đầy đủ chế độ Sáng / Tối cho cả giao diện Người dùng và trang Quản trị viên, tối ưu responsive từ `320px` đến `1440px` (chuyển bảng thành Record Cards trên màn hình nhỏ, chia nhỏ gói tải bằng `React.lazy`).
  - 10 phân hệ quản trị: Tổng quan KPI, Người dùng, Phòng trọ, Duyệt chủ trọ, Đánh giá, Báo cáo vi phạm, Lịch hẹn, Trường đại học, Dữ liệu hệ thống và Cài đặt SMTP.

---

## Công nghệ sử dụng

| Tầng | Công nghệ |
| --- | --- |
| **Frontend** | React 18.3, Vite 6, React Router 7, Leaflet & React-Leaflet, Lucide Icons, Custom CSS Tokens (Light/Dark) |
| **Backend** | ASP.NET Core 8 Web API, Entity Framework Core 8, Swagger / OpenAPI, BackgroundService |
| **Cơ sở dữ liệu** | PostgreSQL 16 (Production) · SQLite (Development & Integration Test) |
| **Bảo mật & Tích hợp** | JWT Bearer (DB Role/Fingerprint Validation), BCrypt, AES-GCM 256-bit (SMTP Secret), Rate Limiting, Gmail SMTP, Cloudinary, Google Places & Photon |
| **Kiểm thử** | xUnit (.NET 8), Vitest 5 + Testing Library, Playwright 1.58 (Desktop & Mobile iPhone 13), Python `unittest` + ShellCheck |
| **Đóng gói & CI/CD** | Multi-stage Docker (non-root), Docker Compose, Caddy / Nginx Reverse Proxy, GitHub Actions (`ubuntu-24.04`) |

---

## Chạy nhanh tại máy (Local Development)

Yêu cầu **.NET SDK 8.0+** và **Node.js 22.12+**.

### 1. Chế độ SQLite Development (Mặc định, không cần cài PostgreSQL)

Khởi chạy nhanh bằng script PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File run.ps1
```

Hoặc chạy thủ công trên 2 cửa sổ terminal:

```sh
dotnet restore server
npm --prefix client ci

# Terminal 1 – Backend API (http://localhost:5059)
dotnet run --project server --urls http://localhost:5059

# Terminal 2 – Frontend Vite (http://localhost:5173, tự proxy /api -> 5059)
npm --prefix client run dev
```

- Giao diện Web: [http://localhost:5173](http://localhost:5173)
- Swagger UI: [http://localhost:5059/swagger](http://localhost:5059/swagger)
- Healthcheck: [http://localhost:5059/health](http://localhost:5059/health)

Trong môi trường `Development`, hệ thống tự khởi tạo `server/campusrent.db` cùng 5 trường đại học, 5 phòng mẫu và 3 tài khoản thử nghiệm (không bao giờ được seed trên `Production`):

| Vai trò | Email | Mật khẩu Development |
| --- | --- | --- |
| Sinh viên (`.edu` xác thực) | `nguyenvanan@sis.hust.edu.vn` | `Password@123` |
| Chủ trọ | `landlord1@gmail.com` | `Password@123` |
| Quản trị viên | `admin@campusrent.vn` | `Password@123` |

### 2. Chế độ Local kết nối trực tiếp PostgreSQL Production (`run.bat`)

`run.bat` (hoặc `run.ps1 -UseProductionDatabase`) nạp `DATABASE_URL` và các khóa dịch vụ từ tệp `.env`, dừng tiến trình backend cũ trên cổng `5059` và khởi chạy runtime `Production` ngay tại máy. Do có toàn quyền đọc/ghi vào cơ sở dữ liệu thật, script yêu cầu gõ xác nhận `PRODUCTION` trước khi mở kết nối.

### 3. Chạy trọn gói bằng Docker Compose tại máy

Sao chép `.env.example` sang `.env`, điền các mật khẩu/khóa bí mật (`JWT_SECRET` tối thiểu 32 byte), sau đó khởi chạy:

```sh
cp .env.example .env
docker compose up -d --build
```

Ứng dụng web và API phục vụ chung trên cổng `8080` ([http://localhost:8080](http://localhost:8080)), đi kèm container `postgres:16-alpine` và tự động chạy EF Core Migrations khi khởi động.

### 4. Nhập dữ liệu mẫu thị trường (Idempotent CLI)

Dữ liệu mẫu không tự chạy khi khởi động Production. Để nạp an toàn 20 bản ghi mẫu thực tế vào từng bảng nghiệp vụ trên PostgreSQL (có thể chạy lại nhiều lần không trùng lặp):

```sh
dotnet run --project server -- --import-sample-data
```

---

## Biến môi trường chính (`.env`)

| Biến | Bắt buộc (Prod) | Mô tả |
| --- | :---: | --- |
| `DATABASE_URL` / `POSTGRES_PASSWORD` | Có | Chuỗi kết nối PostgreSQL ngoài (`Host=...` hoặc `postgresql://...`) hoặc mật khẩu DB nội bộ Compose |
| `JWT_SECRET` | Có | Khóa bí mật ký JWT và dẫn xuất khóa mã hóa AES-GCM cho mật khẩu SMTP (`>= 32` byte, không dùng giá trị mẫu) |
| `ADMIN_EMAIL` | Có | Email tài khoản Quản trị viên khởi tạo lần đầu |
| `ADMIN_PASSWORD` | Có | Mật khẩu Quản trị viên khởi tạo lần đầu |
| `CLIENT_URL` | Tùy chọn | Origin giao diện cho phép CORS (mặc định `http://localhost:8080` khi chạy Docker) |
| `CLOUDINARY_CLOUD_NAME` / `API_KEY` / `API_SECRET` | Tùy chọn | Lưu trữ ảnh đám mây Cloudinary (nếu để trống sẽ lưu vào volume `/app/uploads` cục bộ) |
| `GOOGLE_PLACES_API_KEY` | Tùy chọn | Bổ sung gợi ý địa điểm Google Places bên cạnh OpenStreetMap/Photon |

> **Cấu hình Gmail SMTP:** Không cần khai báo cứng trong `.env`. Đăng nhập quyền `ADMIN` -> vào phân hệ **Cài đặt SMTP** để lưu tài khoản Gmail & App Password (tự động mã hóa AES-GCM xuống bảng `smtp_settings`) và gửi thư kiểm tra trực tiếp.

---

## Kiểm thử tự động

| Bộ kiểm thử | Quy mô hiện tại | Công cụ |
| --- | --- | --- |
| **Backend Unit & Integration** | **124 bài kiểm thử** (70 file C# / 16 thư mục) | xUnit (.NET 8) + SQLite cô lập & PostgreSQL 16 thật |
| **Frontend Component** | **29 bài kiểm thử** (8 test files) | Vitest 5 + React Testing Library |
| **End-to-End Đa thiết bị** | **28 kịch bản** (Desktop Chromium + Mobile iPhone 13) | Playwright 1.58 |
| **Linux Installer & Rollout** | **10 bài kiểm thử** (Caddy, External Nginx, Rollout lặp lại) | Python `test_deploy.py` + ShellCheck + Docker |

Lệnh chạy kiểm thử đầy đủ:

```sh
# 1. Backend xUnit (thêm biến CAMPUSRENT_TEST_POSTGRES để chạy cả PostgresMigrationTests)
dotnet test tests/CampusRent.Tests
dotnet ef migrations has-pending-model-changes --project server --no-build

# 2. Frontend: kiểm toán bảo mật, định dạng, unit test & build
npm --prefix client audit --omit=dev --audit-level=moderate
npm --prefix client run format:check
npm --prefix client test
npm --prefix client run build

# 3. End-to-End Playwright (cần build server trước; không khóa file DLL trên cổng 5059)
dotnet build server
npx --prefix client playwright install chromium
npm --prefix client run test:e2e

# 4. Kiểm thử bộ cài Linux & Docker (chạy trên Linux / WSL)
sudo python3 tests/deployment/test_deploy.py --docker
```

Chi tiết phạm vi kiểm thử xem thêm tại [docs/TESTING.md](docs/TESTING.md) và [tests/CampusRent.Tests/README.md](tests/CampusRent.Tests/README.md).

---

## Triển khai Production (VPS Ubuntu / Debian)

Xem hướng dẫn chi tiết tại [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md). Bộ cài một dòng lệnh `deploy.sh` tự động cài Docker, hỏi cấu hình PostgreSQL & Admin qua `/dev/tty`, sinh `JWT_SECRET` an toàn, áp dụng migrations và xác minh `/health`:

```sh
curl -fsSL https://raw.githubusercontent.com/bonelag/CampusRent/main/deploy.sh | sh
```

- **Chế độ mặc định (`CAMPUSRENT_PROXY=caddy`):** Tự động chạy Caddy cấp chứng chỉ HTTPS Let's Encrypt cho tên miền.
- **Máy chủ đã có sẵn Nginx / Apache (`CAMPUSRENT_PROXY=external`):**
  ```sh
  curl -fsSL https://raw.githubusercontent.com/bonelag/CampusRent/main/deploy.sh | CAMPUSRENT_PROXY=external sh
  ```
  Ứng dụng chỉ lắng nghe nội bộ tại `127.0.0.1:8080`; cấu hình reverse proxy ngoài theo mẫu [docs/nginx/campusrent.conf](docs/nginx/campusrent.conf). Cấu hình và tệp tải lên được giữ nguyên an toàn qua các lần chạy lại để cập nhật phiên bản mới.

---

## Cấu trúc thư mục

```text
CampusRent/
├── client/
│   ├── src/
│   │   ├── components/         # Layout, RoomMap, LocationPickerMap, RoomCard, EmailVerificationCard, Modal...
│   │   ├── context/            # AuthContext, SavedContext, ToastContext
│   │   ├── lib/                # Fetch wrapper, chuẩn hóa mediaUrl, dự toán chi phí
│   │   └── pages/              # Search, RoomDetail, PostRoom, MyRooms, Bookings, Account, Saved, Auth, Admin
│   └── e2e/                    # Kịch bản Playwright E2E (Desktop & Mobile iPhone 13)
├── server/
│   ├── BackgroundServices/     # AutoExpireRoomsWorker (nhắc lịch hẹn, tự ẩn +3 ngày, hết hạn 30 ngày)
│   ├── Controllers/            # Auth, Rooms, Reviews, Bookings/Activity, LandlordRequests, Reports, Places, Uploads, Admin
│   ├── Data/                   # CampusRentDbContext, DbSeeder, MarketSampleDataImporter
│   ├── DTOs/                   # Hợp đồng dữ liệu API & DataAnnotations validation
│   ├── Migrations/             # Lịch sử EF Core Migrations chuẩn PostgreSQL
│   ├── Models/                 # 10 thực thể CSDL (User, Room, RoomImage, Review, Booking, SmtpSetting...)
│   └── Services/               # EmailService (AES-GCM), ReviewRankingService, Haversine, LivingCost, Token, Places...
├── tests/
│   ├── CampusRent.Tests/       # 70 file C# kiểm thử xUnit phân tách theo 16 thư mục nghiệp vụ
│   └── deployment/             # Kiểm thử tự động bộ cài Linux (test_deploy.py)
├── docs/                       # Hướng dẫn triển khai (DEPLOYMENT.md), kiểm thử (TESTING.md), mẫu Nginx, Postman
├── Dockerfile                  # Đóng gói Multi-stage (Node 22 -> .NET 8 SDK -> ASP.NET 8 non-root)
├── compose.yaml                # Docker Compose đầy đủ (Web + PostgreSQL 16)
├── compose.production.yaml     # Docker Compose Production ghép với deploy.sh & Caddy
└── deploy.sh                   # Bộ cài đặt & cập nhật tự động trên Linux VPS
```

---

## Nhóm phát triển

- **Trần Hoàng Lân** – Trưởng nhóm / Backend Lead & DevOps
- **Nguyễn Trung Hiếu** – Frontend Lead / Map & Admin Dashboard
- **Bùi Lê Thanh Đức** – BA & QA Automation Lead
- **Trần Đức Nhân** – Backend Developer & Database / Ranking
- **Mai Tuấn Kiệt** – UI/UX Designer & Frontend Mobile

Nhật ký triển khai chi tiết từng giai đoạn: [docs/IMPLEMENTATION.md](docs/IMPLEMENTATION.md).
