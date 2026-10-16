# CampusRent

**MỤC ĐÍCH FILE NÀY:** Đây là tài liệu quy chuẩn kỹ thuật (System Specification & Prompt Map) cấp độ Production. Mọi AI Agent (Cursor, Claude Code, Windsurf, v.v.) đọc file này phải tuân thủ nghiêm ngặt 100% kiến trúc, schema CSDL, chuẩn API và luồng nghiệp vụ được đặc tả dưới đây để sinh code hoàn chỉnh từ A đến Z mà không cần hỏi lại.

---

## 1. TỔNG QUAN HỆ THỐNG & TECH STACK

CampusRent là nền tảng Web tìm kiếm và đánh giá phòng trọ lấy các trường đại học làm tọa độ trung tâm, giải quyết 3 vấn đề: (1) Lọc trọ theo bán kính thực tế (1km, 3km, 5km); (2) Đánh giá chủ trọ minh bạch với cơ chế chống review ảo (@*.edu.vn); (3) Tự động dự toán sinh hoạt phí/tháng.

### Tech Stack Chuẩn
* **Frontend:** React (Vite) + Tailwind CSS + Lucide Icons + Leaflet.js / React-Leaflet + Axios + React Router v6.
* **Backend:** ASP.NET Core 8 (.NET 8 Web API) + Entity Framework Core (EF Core 8) + Npgsql.
* **Database:** PostgreSQL (khuyên dùng trên Supabase/Neon) hoặc PostgreSQL Local.
* **Storage & Utilities:** CloudinaryDotNet (Upload/Resize ảnh), JWT Bearer (Xác thực), BCrypt.Net, BackgroundService (Tự ẩn bài sau 30 ngày), Regex (Kiểm duyệt từ cấm).

---

## 2. CẤU TRÚC THƯ MỤC CHUẨN (MONOREPO GỌN GÀNG)

```text
campusrent/
├── client/                     # FRONTEND (React + Vite)
│   ├── public/
│   ├── src/
│   │   ├── api/                # Cấu hình Axios & API calls (auth, room, review, admin)
│   │   ├── components/         # Reusable UI (Navbar, Footer, Modal, Button, RoomCard)
│   │   │   ├── map/            # Leaflet Map components (MapContainer, RadiusCircle, Marker)
│   │   │   ├── room/           # CostCalculator, ImageGallery, RoomFilter
│   │   │   └── review/         # ReviewList, ReviewForm, StarRating
│   │   ├── context/            # AuthContext (quản lý login state, role, edu verification)
│   │   ├── hooks/              # Custom hooks (useGeolocation, useDebounce, etc.)
│   │   ├── pages/              # HomePage, SearchPage, RoomDetailPage, PostRoomPage, AdminPage, ProfilePage
│   │   ├── routes/             # AppRoutes & ProtectedRoute (theo Role)
│   │   ├── App.jsx
│   │   └── main.jsx
│   ├── package.json
│   ├── tailwind.config.js
│   └── vite.config.js
│
├── server/                     # BACKEND (ASP.NET Core 8 Web API)
│   ├── BackgroundServices/     # AutoExpireRoomsWorker.cs (Quét tin quá hạn 30 ngày)
│   ├── Controllers/            # AuthController, RoomsController, UniversitiesController, ReviewsController, ReportsController, AdminController
│   ├── Data/                   # CampusRentDbContext.cs, DbSeeder.cs (ĐH lớn & 30+ phòng trọ thực tế)
│   ├── DTOs/                   # Request/Response models (Auth, Rooms, Reviews, Reports, Universities, Common)
│   ├── Models/                 # Entities (User, University, Room, RoomImage, Review, Report) & Enums
│   ├── Services/               # HaversineService, LivingCostService, ProfanityFilterService, TokenService, CloudinaryService
│   ├── appsettings.json        # DB Connection, JWT Secret, Cloudinary Config
│   ├── Program.cs              # Dependency Injection, Middleware, Authentication, CORS, Swagger
│   └── CampusRent.Server.csproj
└── README.md

```

---

## 3. CƠ SỞ DỮ LIỆU: PRISMA SCHEMA HOÀN CHỈNH

Agent cần khởi tạo chính xác file `server/prisma/schema.prisma` như sau:

## 3. CƠ SỞ DỮ LIỆU: ENTITY FRAMEWORK CORE (C# MODELS & DBCONTEXT)

Agent triển khai chính xác các Entity Models tại `server/Models/` và DbContext tại `server/Data/CampusRentDbContext.cs`:

### 3.1. Các Enums
* `Role`: `STUDENT`, `LANDLORD`, `ADMIN`
* `RoomStatus`: `AVAILABLE`, `RENTED`, `HIDDEN`
* `ReportStatus`: `PENDING`, `RESOLVED`, `DISMISSED`

### 3.2. Thực thể CSDL chính (Database Entities)
1. **`User` (`users`):** `Id` (Guid PK), `Email` (unique, lowercase), `Password` (BCrypt hash), `FullName`, `PhoneNumber`, `Avatar`, `Role`, `IsEduVerified` (true nếu email đuôi `@*.edu.vn`), `CreatedAt`, `UpdatedAt`.
2. **`University` (`universities`):** `Id` (Guid PK), `Name`, `ShortName` (BKHN, NEU, UET, etc.), `Latitude`, `Longitude`, `Address`, `CreatedAt`.
3. **`Room` (`rooms`):** `Id` (Guid PK), `LandlordId` (FK -> `User`), `UniversityId` (FK nullable -> `University`), `Title`, `Description`, `Address`, `Latitude`, `Longitude`, `BasePrice`, `ElectricPrice`, `WaterPrice`, `InternetPrice`, `OtherFee`, `Area`, `MaxPeople`, `Amenities` (List<string>), `Status`, `ExpiresAt`, `CreatedAt`, `UpdatedAt`.
4. **`RoomImage` (`room_images`):** `Id` (Guid PK), `RoomId` (FK -> `Room`), `Url`, `PublicId`, `DisplayOrder`, `IsCover`, `CreatedAt`.
5. **`Review` (`reviews`):** `Id` (Guid PK), `RoomId` (FK -> `Room`), `UserId` (FK -> `User`), `Rating` (1-5), `Comment`, `IsFlagged`, `FlagReason`, `CreatedAt`. Ràng buộc: Mỗi User chỉ đánh giá 1 lần trên 1 Room.
6. **`Report` (`reports`):** `Id` (Guid PK), `ReporterId` (FK -> `User`), `RoomId` (FK nullable -> `Room`), `ReviewId` (FK nullable -> `Review`), `Reason`, `Description`, `Status`, `CreatedAt`.

---

## 4. CÔNG THỨC & LOGIC NGHIỆP VỤ CỐT LÕI

### 4.1. Thuật toán Haversine tính cự ly bán kính (C# Service Implementation)

Cài đặt trong `server/Services/HaversineService.cs`:

```csharp
public class HaversineService : IHaversineService
{
    private const double EarthRadiusKm = 6371.0;

    public double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = (lat2 - lat1) * (Math.PI / 180.0);
        double dLon = (lon2 - lon1) * (Math.PI / 180.0);

        double a =
            Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
            Math.Cos(lat1 * (Math.PI / 180.0)) *
            Math.Cos(lat2 * (Math.PI / 180.0)) *
            Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return Math.Round(EarthRadiusKm * c, 2);
    }
}
```

### 4.2. Công thức Dự toán Chi phí Sinh hoạt Thực tế (Living Cost Formula)

Tại trang Chi tiết phòng và Component `CostCalculator.jsx`:

$$\text{Tổng chi phí/tháng} = \text{basePrice} + (kWh_{\text{dự kiến}} \times \text{electricPrice}) + (m^3_{\text{dự kiến}} \times \text{waterPrice}) + \text{internetPrice} + \text{otherFee}$$

* Input mặc định gợi ý cho sinh viên: $kWh = 80$, $m^3 \text{ nước} = 4$.
* Khi sinh viên kéo Slider hoặc gõ số, bảng tự động re-calculate tổng tiền ngay lập tức.
* Triển khai Backend trong `server/Services/LivingCostService.cs`.

### 4.3. Kiểm duyệt từ ngữ thô tục & Tự ẩn tin trọ (Scheduled BackgroundService)

* **Profanity Filter (`server/Services/ProfanityFilterService.cs`):** Regex kiểm tra danh sách Blacklist từ cấm tiếng Việt trong bình luận review trước khi lưu CSDL. Nếu dính từ cấm -> tự động đánh dấu `IsFlagged = true`.
* **BackgroundService (`server/BackgroundServices/AutoExpireRoomsWorker.cs`):**
Worker kế thừa `BackgroundService` chạy quét định kỳ:
```csharp
var now = DateTime.UtcNow;
var expiredRooms = await dbContext.Rooms
    .Where(r => r.ExpiresAt < now && r.Status == RoomStatus.AVAILABLE)
    .ToListAsync(stoppingToken);

foreach (var room in expiredRooms)
{
    room.Status = RoomStatus.HIDDEN;
    room.UpdatedAt = now;
}
await dbContext.SaveChangesAsync(stoppingToken);
```

---

## 5. ĐẶC TẢ RESTFUL API (STANDARDIZED API SPEC)

Chuẩn hóa toàn bộ Response trả về định dạng JSON:

```json
{
  "success": true,
  "data": { ... },
  "message": "Mô tả trạng thái"
}

```

### 5.1. Authentication (`/api/auth`)

* `POST /api/auth/register`: Đăng ký tài khoản. Tự động kiểm tra `email.endsWith('.edu.vn')` -> set `isEduVerified = true`.
* `POST /api/auth/login`: Trả về JWT Token chứa `{ userId, role, isEduVerified }`.
* `GET /api/auth/me`: Lấy thông tin cá nhân hiện tại (Yêu cầu JWT).

### 5.2. University (`/api/universities`)

* `GET /api/universities`: Lấy danh sách trường ĐH (Tên, tọa độ Lat/Lng).
* `POST /api/universities`: Admin thêm trường ĐH mới.

### 5.3. Rooms (`/api/rooms`)

* `GET /api/rooms`: Tìm kiếm & Lọc trọ:
* Query params: `universityId`, `radius` (1, 3, 5 km), `minPrice`, `maxPrice`, `roomType`.
* Logic: Lấy tọa độ trường ĐH làm tâm, dùng `calculateDistance` quét tất cả phòng có trạng thái `AVAILABLE` nằm trong khoảng cách `<= radius`.


* `GET /api/rooms/:id`: Chi tiết phòng (kèm danh sách ảnh, thông tin chủ trọ, rating trung bình, danh sách reviews).
* `POST /api/rooms`: Đăng tin mới (Role: `LANDLORD`, `ADMIN`). Upload ảnh qua Cloudinary. `expiresAt = now + 30 days`.
* `PUT /api/rooms/:id`: Cập nhật phòng / Gia hạn thêm 30 ngày.
* `PATCH /api/rooms/:id/report-full`: Sinh viên bấm nút "Báo hết phòng" (tăng count hoặc gắn cờ cảnh báo).

### 5.4. Reviews & Reports (`/api/reviews`, `/api/reports`)

* `POST /api/rooms/:roomId/reviews`: Tạo review (Role: `STUDENT`). Chỉ cho phép đánh giá khi đã đăng nhập. Nếu user có `isEduVerified = true` -> gắn badge "Sinh viên uy tín".
* `POST /api/reports`: Báo cáo vi phạm tin đăng hoặc review bôi nhọ.

### 5.5. Admin Dashboard (`/api/admin`) - Role: `ADMIN`

* `GET /api/admin/stats`: Tổng user, tổng phòng trọ (Available, Hidden), tổng report chờ duyệt.
* `GET /api/admin/reports`: Danh sách báo cáo vi phạm.
* `PATCH /api/admin/reports/:id`: Xử lý báo cáo (`RESOLVED` -> Xóa phòng/review, hoặc `DISMISSED` -> Giữ nguyên).

---

## 6. HƯỚNG DẪN XÂY DỰNG GIAO DIỆN CLIENT (REACT + VITE)

### Các trang chính cần sinh code:

1. **HomePage (`/`):**
* Header thanh điều hướng, nút Đăng nhập / Đăng ký, hiển thị User Profile.
* Thanh tìm kiếm trung tâm: Dropdown chọn Trường Đại Học + Chọn cự ly (1km, 3km, 5km) + Bộ lọc khoảng giá.
* Khu vực Bản đồ chia đôi (Split View): Bên trái là danh sách Card phòng trọ, bên phải là Leaflet Map tương tác (hiển thị Marker và vòng tròn bán kính Radius Circle). Click marker -> Tự scroll và highlight card tương ứng.


2. **RoomDetailPage (`/rooms/:id`):**
* Carousel/Gallery ảnh phòng đẹp mắt.
* Thông tin chi tiết: Giá gốc, địa chỉ, khoảng cách đến trường ĐH gần nhất (ví dụ: "Cách ĐH Bách Khoa 1.2 km").
* **Component Bảng tính sinh hoạt phí:** Cho phép trượt số điện, số nước để tính tổng chi phí ước tính / tháng.
* **Khu vực Đánh giá (Review Section):** Form đánh giá số sao + bình luận; Danh sách đánh giá đã có (hiển thị tag "Sinh viên đã xác thực email trường" cho tài khoản .edu.vn). Nút Report tin ảo / review bẩn.


3. **PostRoomPage (`/post-room`):**
* Form đăng trọ dành cho chủ nhà: Chọn vị trí trên bản đồ để lấy tọa độ tự động, nhập các định mức điện nước, kéo thả ảnh (Drag & Drop) preview và đẩy lên Cloudinary.


4. **AdminDashboardPage (`/admin`):**
* Thống kê KPI hệ thống, bảng quản lý và duyệt báo cáo vi phạm.



---

## 7. BIẾN MÔI TRƯỜNG (`.env.example`)

### `server/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=campusrent;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "campusrent_super_secret_jwt_key_2026_at_least_32_characters_long",
    "Issuer": "CampusRent",
    "Audience": "CampusRentApp"
  },
  "Cloudinary": {
    "CloudName": "your_cloud_name",
    "ApiKey": "your_api_key",
    "ApiSecret": "your_api_secret"
  },
  "ClientUrl": "http://localhost:5173"
}
```

### `client/.env`

```env
VITE_API_BASE_URL="http://localhost:5000/api"
```

---

## 8. STEP-BY-STEP PROMPT CHO AGENT THỰC HIỆN

Khi giao việc cho AI Agent, hãy yêu cầu làm tuần tự theo 5 bước sau:

* **Bước 1: Khởi tạo Project & Database:** Setup cấu trúc folder `client` (React + Vite) và `server` (ASP.NET Core 8 Web API). Cài dependencies (`dotnet restore`, `npm install`). Tạo Models EF Core, cấu hình `CampusRentDbContext` và chạy `DbSeeder.cs` tự nạp 5 trường ĐH lớn và phòng trọ mẫu quanh Hà Nội.
* **Bước 2: Triển khai Backend Core:** Triển khai Authentication (JWT Bearer, kiểm tra `@*.edu.vn`), CRUD Rooms, Cloudinary upload, service Haversine và RESTful API chuẩn Response `ApiResponse<T>`. Kích hoạt BackgroundService `AutoExpireRoomsWorker` tự ẩn bài quá hạn 30 ngày.
* **Bước 3: Xây dựng Giao diện & Bản đồ Leaflet:** Dựng React App với Tailwind CSS. Cài `leaflet` và `react-leaflet`. Xây dựng Split View: Map hiển thị vị trí các phòng trọ theo bán kính trường ĐH và List Card đồng bộ trạng thái khi click.
* **Bước 4: Tính năng Bảng tính tiền & Đánh giá Review:** Dựng Component `CostCalculator`, Form Review kèm lọc từ ngữ cấm và gắn badge Sinh viên uy tín.
* **Bước 5: Admin Panel & Hoàn thiện:** Dựng trang Admin duyệt report vi phạm. Test toàn bộ luồng hoạt động từ Đăng ký -> Tìm trọ -> Tính tiền -> Review -> Quản trị.


---

## 4. CÔNG THỨC & LOGIC NGHIỆP VỤ CỐT LÕI

### 4.1. Thuật toán Haversine tính cự ly bán kính (Backend Implementation)

Agent cài đặt chính xác function này trong `server/src/services/haversineService.js`:

```javascript
/**
 * Tính khoảng cách giữa 2 tọa độ GPS (KM) theo công thức Haversine
 */
export function calculateDistance(lat1, lon1, lat2, lon2) {
  const R = 6371; // Bán kính Trái Đất (km)
  const dLat = (lat2 - lat1) * (Math.PI / 180);
  const dLon = (lon2 - lon1) * (Math.PI / 180);
  
  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(lat1 * (Math.PI / 180)) *
    Math.cos(lat2 * (Math.PI / 180)) *
    Math.sin(dLon / 2) * Math.sin(dLon / 2);
    
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return parseFloat((R * c).toFixed(2)); // Trả về khoảng cách theo km, làm tròn 2 chữ số
}

```

### 4.2. Công thức Dự toán Chi phí Sinh hoạt Thực tế (Living Cost Formula)

Tại trang Chi tiết phòng và Component `CostCalculator.jsx`:


$$\text{Tổng chi phí/tháng} = \text{basePrice} + (kWh_{\text{dự kiến}} \times \text{electricPrice}) + (m^3_{\text{dự kiến}} \times \text{waterPrice}) + \text{internetPrice} + \text{otherFee}$$

* Input mặc định gợi ý cho sinh viên: $kWh = 80$, $m^3 \text{ nước} = 4$.
* Khi sinh viên kéo Slider hoặc gõ số, bảng tự động re-calculate tổng tiền ngay lập tức.

### 4.3. Kiểm duyệt từ ngữ thô tục & Tự ẩn tin trọ (Scheduled Job)

* **Profanity Filter:** Regex kiểm tra danh sách Blacklist từ cấm tiếng Việt trong bình luận review trước khi lưu CSDL. Nếu dính từ cấm -> tự động đánh dấu `isFlagged = true` hoặc từ chối lưu.
* **Node-cron Job (`server/src/cron/autoExpireRooms.js`):**
Chạy mỗi 00:00 hàng đêm:
```javascript
import cron from 'node-cron';
import prisma from '../config/prisma.js';

cron.schedule('0 0 * * *', async () => {
  const now = new Date();
  await prisma.room.updateMany({
    where: {
      expiresAt: { lt: now },
      status: 'AVAILABLE'
    },
    data: { status: 'HIDDEN' }
  });
  console.log('[CRON] Đã tự động ẩn các bài đăng phòng trọ hết hạn.');
});

```



---

## 5. ĐẶC TẢ RESTFUL API (STANDARDIZED API SPEC)

Chuẩn hóa toàn bộ Response trả về định dạng JSON:

```json
{
  "success": true,
  "data": { ... },
  "message": "Mô tả trạng thái"
}

```

### 5.1. Authentication (`/api/auth`)

* `POST /api/auth/register`: Đăng ký tài khoản. Tự động kiểm tra `email.endsWith('.edu.vn')` -> set `isEduVerified = true`.
* `POST /api/auth/login`: Trả về JWT Token chứa `{ userId, role, isEduVerified }`.
* `GET /api/auth/me`: Lấy thông tin cá nhân hiện tại (Yêu cầu JWT).

### 5.2. University (`/api/universities`)

* `GET /api/universities`: Lấy danh sách trường ĐH (Tên, tọa độ Lat/Lng).
* `POST /api/universities`: Admin thêm trường ĐH mới.

### 5.3. Rooms (`/api/rooms`)

* `GET /api/rooms`: Tìm kiếm & Lọc trọ:
* Query params: `universityId`, `radius` (1, 3, 5 km), `minPrice`, `maxPrice`, `roomType`.
* Logic: Lấy tọa độ trường ĐH làm tâm, dùng `calculateDistance` quét tất cả phòng có trạng thái `AVAILABLE` nằm trong khoảng cách `<= radius`.


* `GET /api/rooms/:id`: Chi tiết phòng (kèm danh sách ảnh, thông tin chủ trọ, rating trung bình, danh sách reviews).
* `POST /api/rooms`: Đăng tin mới (Role: `LANDLORD`, `ADMIN`). Upload ảnh qua Cloudinary. `expiresAt = now + 30 days`.
* `PUT /api/rooms/:id`: Cập nhật phòng / Gia hạn thêm 30 ngày.
* `PATCH /api/rooms/:id/report-full`: Sinh viên bấm nút "Báo hết phòng" (tăng count hoặc gắn cờ cảnh báo).

### 5.4. Reviews & Reports (`/api/reviews`, `/api/reports`)

* `POST /api/rooms/:roomId/reviews`: Tạo review (Role: `STUDENT`). Chỉ cho phép đánh giá khi đã đăng nhập. Nếu user có `isEduVerified = true` -> gắn badge "Sinh viên uy tín".
* `POST /api/reports`: Báo cáo vi phạm tin đăng hoặc review bôi nhọ.

### 5.5. Admin Dashboard (`/api/admin`) - Role: `ADMIN`

* `GET /api/admin/stats`: Tổng user, tổng phòng trọ (Available, Hidden), tổng report chờ duyệt.
* `GET /api/admin/reports`: Danh sách báo cáo vi phạm.
* `PATCH /api/admin/reports/:id`: Xử lý báo cáo (`RESOLVED` -> Xóa phòng/review, hoặc `DISMISSED` -> Giữ nguyên).

---

## 6. HƯỚNG DẪN XÂY DỰNG GIAO DIỆN CLIENT (REACT + VITE)

### Các trang chính cần sinh code:

1. **HomePage (`/`):**
* Header thanh điều hướng, nút Đăng nhập / Đăng ký, hiển thị User Profile.
* Thanh tìm kiếm trung tâm: Dropdown chọn Trường Đại Học + Chọn cự ly (1km, 3km, 5km) + Bộ lọc khoảng giá.
* Khu vực Bản đồ chia đôi (Split View): Bên trái là danh sách Card phòng trọ, bên phải là Leaflet Map tương tác (hiển thị Marker và vòng tròn bán kính Radius Circle). Click marker -> Tự scroll và highlight card tương ứng.


2. **RoomDetailPage (`/rooms/:id`):**
* Carousel/Gallery ảnh phòng đẹp mắt.
* Thông tin chi tiết: Giá gốc, địa chỉ, khoảng cách đến trường ĐH gần nhất (ví dụ: "Cách ĐH Bách Khoa 1.2 km").
* **Component Bảng tính sinh hoạt phí:** Cho phép trượt số điện, số nước để tính tổng chi phí ước tính / tháng.
* **Khu vực Đánh giá (Review Section):** Form đánh giá số sao + bình luận; Danh sách đánh giá đã có (hiển thị tag "Sinh viên đã xác thực email trường" cho tài khoản .edu.vn). Nút Report tin ảo / review bẩn.


3. **PostRoomPage (`/post-room`):**
* Form đăng trọ dành cho chủ nhà: Chọn vị trí trên bản đồ để lấy tọa độ tự động, nhập các định mức điện nước, kéo thả ảnh (Drag & Drop) preview và đẩy lên Cloudinary.


4. **AdminDashboardPage (`/admin`):**
* Thống kê KPI hệ thống, bảng quản lý và duyệt báo cáo vi phạm.



---

## 7. BIẾN MÔI TRƯỜNG (`.env.example`)

### `server/.env`

```env
PORT=5000
DATABASE_URL="postgresql://user:password@localhost:5432/campusrent?schema=public"
JWT_SECRET="campusrent_super_secret_jwt_key_2026"
JWT_EXPIRES_IN="7d"

CLOUDINARY_CLOUD_NAME="your_cloud_name"
CLOUDINARY_API_KEY="your_api_key"
CLOUDINARY_API_SECRET="your_api_secret"

CLIENT_URL="http://localhost:5173"

```

### `client/.env`

```env
VITE_API_BASE_URL="http://localhost:5000/api"

```

---

## 8. STEP-BY-STEP PROMPT CHO AGENT THỰC HIỆN

Khi giao việc cho AI Agent, hãy yêu cầu làm tuần tự theo 5 bước sau:

* **Bước 1: Khởi tạo Project & Database:** Setup cấu trúc folder `client` và `server`. Cài dependencies. Viết `schema.prisma`, migrate database và chạy file `seed.js` tạo sẵn 5 trường ĐH và 30 phòng trọ mẫu quanh Hà Nội / TP.HCM có tọa độ thật.
* **Bước 2: Triển khai Backend Core:** Viết Auth (JWT, kiểm tra `@*.edu.vn`), CRUD Rooms, Cloudinary upload, service Haversine và RESTful API chuẩn Response. Cài đặt cron job tự ẩn bài quá hạn 30 ngày.
* **Bước 3: Xây dựng Giao diện & Bản đồ Leaflet:** Dựng React App với Tailwind CSS. Cài `leaflet` và `react-leaflet`. Xây dựng Split View: Map hiển thị vị trí các phòng trọ theo bán kính trường ĐH và List Card đồng bộ trạng thái khi click.
* **Bước 4: Tính năng Bảng tính tiền & Đánh giá Review:** Dựng Component `CostCalculator`, Form Review kèm lọc từ ngữ cấm và gắn badge Sinh viên uy tín.
* **Bước 5: Admin Panel & Hoàn thiện:** Dựng trang Admin duyệt report vi phạm. Test toàn bộ luồng hoạt động từ Đăng ký -> Tìm trọ -> Tính tiền -> Review -> Quản trị.

```

---

### Cách dùng file này để giao việc cho AI Agent:
Tạo file `README.md` ở root folder của repo rồi ném câu prompt sau vào Agent (Claude Code / Cursor / Windsurf):

> *"Đọc kỹ file `README.md` tại root project. Hãy làm theo đúng lộ trình ở mục 8: Khởi tạo project, dựng Database Prisma với seed data, viết Backend API Node.js Express, sau đó dựng Frontend React Vite với Leaflet Map và Cost Calculator. Tuân thủ 100% Schema CSDL và quy chuẩn Response API."*

```