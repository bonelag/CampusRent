# ProjectWeb
Dự án ứng dụng web và công nghệ phầm mềm
**MỤC ĐÍCH FILE NÀY:** Đây là tài liệu quy chuẩn kỹ thuật (System Specification & Prompt Map) cấp độ Production. Mọi AI Agent (Cursor, Claude Code, Windsurf, v.v.) đọc file này phải tuân thủ nghiêm ngặt 100% kiến trúc, schema CSDL, chuẩn API và luồng nghiệp vụ được đặc tả dưới đây để sinh code hoàn chỉnh từ A đến Z mà không cần hỏi lại.

---

## 1. TỔNG QUAN HỆ THỐNG & TECH STACK

CampusRent là nền tảng Web tìm kiếm và đánh giá phòng trọ lấy các trường đại học làm tọa độ trung tâm, giải quyết 3 vấn đề: (1) Lọc trọ theo bán kính thực tế (1km, 3km, 5km); (2) Đánh giá chủ trọ minh bạch với cơ chế chống review ảo (@*.edu.vn); (3) Tự động dự toán sinh hoạt phí/tháng.

### Tech Stack Chuẩn
* **Frontend:** React (Vite) + Tailwind CSS + Lucide Icons + Leaflet.js / React-Leaflet + Axios + React Router v6.
* **Backend:** Node.js (Express.js, ES Modules hoặc TypeScript) + Prisma ORM (hoặc Sequelize).
* **Database:** PostgreSQL (khuyên dùng trên Supabase/Neon) hoặc MySQL.
* **Storage & Utilities:** Cloudinary SDK (Upload/Resize ảnh), JWT (Xác thực), Bcryptjs, Node-cron (Tự ẩn bài), Bad-words / Regex (Kiểm duyệt).

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
├── server/                     # BACKEND (Node.js Express)
│   ├── prisma/
│   │   ├── schema.prisma       # Định nghĩa Schema Database
│   │   └── seed.js             # Dữ liệu mẫu (ĐH lớn & 30+ phòng trọ thực tế)
│   ├── src/
│   │   ├── config/             # DB connection, Cloudinary config, JWT secret
│   │   ├── controllers/        # authController, roomController, reviewController, adminController
│   │   ├── middlewares/        # authMiddleware (verifyToken, requireRole), uploadMiddleware, errorHandler
│   │   ├── routes/             # authRoutes, roomRoutes, reviewRoutes, adminRoutes, uniRoutes
│   │   ├── services/           # haversineService, livingCostService, filterProfanityService
│   │   ├── cron/               # autoExpireRooms.js (Node-cron quét tin quá hạn 30 ngày)
│   │   └── server.js           # Express App setup & Start
│   ├── .env.example
│   └── package.json
└── README.md

```

---

## 3. CƠ SỞ DỮ LIỆU: PRISMA SCHEMA HOÀN CHỈNH

Agent cần khởi tạo chính xác file `server/prisma/schema.prisma` như sau:

```prisma
datasource db {
  provider = "postgresql" // Hoặc "mysql"
  url      = env("DATABASE_URL")
}

generator client {
  provider = "prisma-client-js"
}

enum Role {
  STUDENT
  LANDLORD
  ADMIN
}

enum RoomStatus {
  AVAILABLE
  RENTED
  HIDDEN
}

enum ReportStatus {
  PENDING
  RESOLVED
  DISMISSED
}

model User {
  id            String    @id @default(uuid())
  email         String    @unique
  password      String
  fullName      String
  phoneNumber   String?
  avatar        String?
  role          Role      @default(STUDENT)
  isEduVerified Boolean   @default(false) // True nếu email kết thúc bằng .edu.vn
  createdAt     DateTime  @default(now())
  updatedAt     DateTime  @updatedAt

  rooms         Room[]    @relation("LandlordRooms")
  reviews       Review[]
  reports       Report[]
}

model University {
  id          String   @id @default(uuid())
  name        String
  shortName   String   // Ví dụ: BKHN, NEU, UET, UIT, v.v.
  latitude    Float
  longitude   Float
  address     String
  createdAt   DateTime @default(now())

  rooms       Room[]
}

model Room {
  id            String      @id @default(uuid())
  landlordId    String
  landlord      User        @relation("LandlordRooms", fields: [landlordId], references: [id], onDelete: Cascade)
  universityId  String?
  university    University? @relation(fields: [universityId], references: [id])
  
  title         String
  description   String      @db.Text
  address       String
  latitude      Float
  longitude     Float
  
  // Các khoản chi phí cố định & định mức (VND)
  basePrice     Float       // Tiền thuê phòng / tháng
  electricPrice Float       // Tiền điện / kWh
  waterPrice    Float       // Tiền nước / m3 hoặc / người
  internetPrice Float       @default(0) // Tiền mạng / phòng
  otherFee      Float       @default(0) // Phí dịch vụ (vệ sinh, rác, gửi xe)

  area          Float       // Diện tích (m2)
  maxPeople     Int         @default(2)
  amenities     String[]    // ['wifi', 'ac', 'parking', 'heater', 'balcony']
  status        RoomStatus  @default(AVAILABLE)
  
  expiresAt     DateTime    // Tự động gia hạn 30 ngày từ ngày đăng/update
  createdAt     DateTime    @default(now())
  updatedAt     DateTime    @updatedAt

  images        RoomImage[]
  reviews       Review[]
  reports       Report[]
}

model RoomImage {
  id        String   @id @default(uuid())
  roomId    String
  room      Room     @relation(fields: [roomId], references: [id], onDelete: Cascade)
  url       String
  publicId  String?
  createdAt DateTime @default(now())
}

model Review {
  id        String   @id @default(uuid())
  roomId    String
  room      Room     @relation(fields: [roomId], references: [id], onDelete: Cascade)
  userId    String
  user      User     @relation(fields: [userId], references: [id], onDelete: Cascade)
  
  rating    Int      // 1 đến 5 sao
  comment   String   @db.Text
  isFlagged Boolean  @default(false)
  createdAt DateTime @default(now())

  reports   Report[]
}

model Report {
  id          String       @id @default(uuid())
  reporterId  String
  reporter    User         @relation(fields: [reporterId], references: [id], onDelete: Cascade)
  roomId      String?
  room        Room?        @relation(fields: [roomId], references: [id], onDelete: Cascade)
  reviewId    String?
  review      Review?      @relation(fields: [reviewId], references: [id], onDelete: Cascade)
  
  reason      String
  status      ReportStatus @default(PENDING)
  createdAt   DateTime     @default(now())
}

```

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
"""

with open("README.md", "w", encoding="utf-8") as f:
f.write(readme_content)

print("Saved README.md successfully! File length:", len(readme_content))

```

```text?code_stdout&code_event_index=1
Saved README.md successfully! File length: 13886


```

Bản thiết kế `README.md` đã được biên soạn. Bạn có thể tải trực tiếp file hoặc sao chép toàn bộ nội dung bên dưới:

Tài liệu này được cấu trúc như một **System Prompt / Execution Map hoàn chỉnh** (chuẩn chỉ cho Cursor, Claude Code, Windsurf, hay bất kỳ AI coding agent nào đọc và tự động sinh code 100% từ Database đến UI):

```markdown
# CAMPUSRENT - SYSTEM ARCHITECTURE & IMPLEMENTATION MASTER BLUEPRINT (AGENT PROMPT MAP)

> **MỤC ĐÍCH FILE NÀY:** Đây là tài liệu quy chuẩn kỹ thuật (System Specification & Prompt Map) cấp độ Production. Mọi AI Agent (Cursor, Claude Code, Windsurf, v.v.) đọc file này phải tuân thủ nghiêm ngặt 100% kiến trúc, schema CSDL, chuẩn API và luồng nghiệp vụ được đặc tả dưới đây để sinh code hoàn chỉnh từ A đến Z mà không cần hỏi lại.

---

## 1. TỔNG QUAN HỆ THỐNG & TECH STACK

CampusRent là nền tảng Web tìm kiếm và đánh giá phòng trọ lấy các trường đại học làm tọa độ trung tâm, giải quyết 3 vấn đề: (1) Lọc trọ theo bán kính thực tế (1km, 3km, 5km); (2) Đánh giá chủ trọ minh bạch với cơ chế chống review ảo (@*.edu.vn); (3) Tự động dự toán sinh hoạt phí/tháng.

### Tech Stack Chuẩn
* **Frontend:** React (Vite) + Tailwind CSS + Lucide Icons + Leaflet.js / React-Leaflet + Axios + React Router v6.
* **Backend:** Node.js (Express.js, ES Modules hoặc TypeScript) + Prisma ORM (hoặc Sequelize).
* **Database:** PostgreSQL (khuyên dùng trên Supabase/Neon) hoặc MySQL.
* **Storage & Utilities:** Cloudinary SDK (Upload/Resize ảnh), JWT (Xác thực), Bcryptjs, Node-cron (Tự ẩn bài), Bad-words / Regex (Kiểm duyệt).

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
├── server/                     # BACKEND (Node.js Express)
│   ├── prisma/
│   │   ├── schema.prisma       # Định nghĩa Schema Database
│   │   └── seed.js             # Dữ liệu mẫu (ĐH lớn & 30+ phòng trọ thực tế)
│   ├── src/
│   │   ├── config/             # DB connection, Cloudinary config, JWT secret
│   │   ├── controllers/        # authController, roomController, reviewController, adminController
│   │   ├── middlewares/        # authMiddleware (verifyToken, requireRole), uploadMiddleware, errorHandler
│   │   ├── routes/             # authRoutes, roomRoutes, reviewRoutes, adminRoutes, uniRoutes
│   │   ├── services/           # haversineService, livingCostService, filterProfanityService
│   │   ├── cron/               # autoExpireRooms.js (Node-cron quét tin quá hạn 30 ngày)
│   │   └── server.js           # Express App setup & Start
│   ├── .env.example
│   └── package.json
└── README.md

```

---

## 3. CƠ SỞ DỮ LIỆU: PRISMA SCHEMA HOÀN CHỈNH

Agent cần khởi tạo chính xác file `server/prisma/schema.prisma` như sau:

```prisma
datasource db {
  provider = "postgresql" // Hoặc "mysql"
  url      = env("DATABASE_URL")
}

generator client {
  provider = "prisma-client-js"
}

enum Role {
  STUDENT
  LANDLORD
  ADMIN
}

enum RoomStatus {
  AVAILABLE
  RENTED
  HIDDEN
}

enum ReportStatus {
  PENDING
  RESOLVED
  DISMISSED
}

model User {
  id            String    @id @default(uuid())
  email         String    @unique
  password      String
  fullName      String
  phoneNumber   String?
  avatar        String?
  role          Role      @default(STUDENT)
  isEduVerified Boolean   @default(false) // True nếu email kết thúc bằng .edu.vn
  createdAt     DateTime  @default(now())
  updatedAt     DateTime  @updatedAt

  rooms         Room[]    @relation("LandlordRooms")
  reviews       Review[]
  reports       Report[]
}

model University {
  id          String   @id @default(uuid())
  name        String
  shortName   String   // Ví dụ: BKHN, NEU, UET, UIT, v.v.
  latitude    Float
  longitude   Float
  address     String
  createdAt   DateTime @default(now())

  rooms       Room[]
}

model Room {
  id            String      @id @default(uuid())
  landlordId    String
  landlord      User        @relation("LandlordRooms", fields: [landlordId], references: [id], onDelete: Cascade)
  universityId  String?
  university    University? @relation(fields: [universityId], references: [id])
  
  title         String
  description   String      @db.Text
  address       String
  latitude      Float
  longitude     Float
  
  // Các khoản chi phí cố định & định mức (VND)
  basePrice     Float       // Tiền thuê phòng / tháng
  electricPrice Float       // Tiền điện / kWh
  waterPrice    Float       // Tiền nước / m3 hoặc / người
  internetPrice Float       @default(0) // Tiền mạng / phòng
  otherFee      Float       @default(0) // Phí dịch vụ (vệ sinh, rác, gửi xe)

  area          Float       // Diện tích (m2)
  maxPeople     Int         @default(2)
  amenities     String[]    // ['wifi', 'ac', 'parking', 'heater', 'balcony']
  status        RoomStatus  @default(AVAILABLE)
  
  expiresAt     DateTime    // Tự động gia hạn 30 ngày từ ngày đăng/update
  createdAt     DateTime    @default(now())
  updatedAt     DateTime    @updatedAt

  images        RoomImage[]
  reviews       Review[]
  reports       Report[]
}

model RoomImage {
  id        String   @id @default(uuid())
  roomId    String
  room      Room     @relation(fields: [roomId], references: [id], onDelete: Cascade)
  url       String
  publicId  String?
  createdAt DateTime @default(now())
}

model Review {
  id        String   @id @default(uuid())
  roomId    String
  room      Room     @relation(fields: [roomId], references: [id], onDelete: Cascade)
  userId    String
  user      User     @relation(fields: [userId], references: [id], onDelete: Cascade)
  
  rating    Int      // 1 đến 5 sao
  comment   String   @db.Text
  isFlagged Boolean  @default(false)
  createdAt DateTime @default(now())

  reports   Report[]
}

model Report {
  id          String       @id @default(uuid())
  reporterId  String
  reporter    User         @relation(fields: [reporterId], references: [id], onDelete: Cascade)
  roomId      String?
  room        Room?        @relation(fields: [roomId], references: [id], onDelete: Cascade)
  reviewId    String?
  review      Review?      @relation(fields: [reviewId], references: [id], onDelete: Cascade)
  
  reason      String
  status      ReportStatus @default(PENDING)
  createdAt   DateTime     @default(now())
}

```

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