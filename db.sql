-- ============================================================================
-- CAMPUSRENT - DATABASE SCHEMA (PostgreSQL 14+)
-- Chuẩn hóa dữ liệu 3NF (Third Normal Form)
-- ============================================================================

-- Bật extension tạo UUID ngẫu nhiên
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================================
-- 1. ENUM TYPES
-- ============================================================================

CREATE TYPE user_role AS ENUM (
    'STUDENT',      -- Sinh viên tìm trọ
    'LANDLORD',     -- Chủ trọ / Người cho thuê
    'ADMIN'         -- Quản trị viên hệ thống
);

CREATE TYPE room_type AS ENUM (
    'ROOM',             -- Phòng đơn / khép kín
    'SHARED_ROOM',      -- Ở ghép
    'MINI_APARTMENT',   -- Chung cư mini
    'STUDIO',           -- Căn hộ studio
    'HOUSE',            -- Nhà nguyên căn
    'DORMITORY'         -- Ký túc xá tư nhân
);

CREATE TYPE post_status AS ENUM (
    'DRAFT',            -- Bản nháp
    'PENDING_APPROVAL', -- Chờ duyệt (nếu bật kiểm duyệt)
    'AVAILABLE',        -- Đang hiển thị còn phòng
    'RENTED',           -- Đã cho thuê
    'HIDDEN',           -- Tạm ẩn bởi chủ nhà
    'EXPIRED'           -- Quá hạn 30 ngày (cron job cập nhật)
);

CREATE TYPE utility_billing_type AS ENUM (
    'PER_UNIT',     -- Tính theo công tơ (kWh / m3)
    'PER_PERSON',   -- Tính theo đầu người / tháng
    'FIXED',        -- Trọn gói cố định / phòng / tháng
    'FREE'          -- Miễn phí hoàn toàn
);

CREATE TYPE report_target_type AS ENUM (
    'POST',         -- Báo cáo bài đăng
    'REVIEW',       -- Báo cáo bình luận
    'USER'          -- Báo cáo người dùng
);

CREATE TYPE report_status AS ENUM (
    'PENDING',      -- Chờ xử lý
    'IN_REVIEW',    -- Đang kiểm tra
    'RESOLVED',     -- Đã xử lý vi phạm
    'DISMISSED'     -- Bác bỏ (báo cáo sai)
);

CREATE TYPE appointment_status AS ENUM (
    'PENDING',      -- Chờ chủ trọ xác nhận
    'ACCEPTED',     -- Đồng ý lịch hẹn
    'REJECTED',     -- Từ chối
    'CANCELLED',    -- Sinh viên hủy
    'COMPLETED'     -- Đã xem phòng
);

-- ============================================================================
-- 2. CORE TABLES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 2.1 Bảng Người dùng (users)
-- ----------------------------------------------------------------------------
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    phone_number VARCHAR(20),
    avatar_url TEXT,
    role user_role NOT NULL DEFAULT 'STUDENT',
    
    -- Xác thực sinh viên qua email trường (.edu.vn)
    is_edu_verified BOOLEAN NOT NULL DEFAULT FALSE,
    edu_email VARCHAR(255) UNIQUE,
    verified_at TIMESTAMPTZ,
    
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_users_phone CHECK (phone_number IS NULL OR phone_number ~ '^[0-9+\s-]{8,20}$')
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role ON users(role);
CREATE INDEX idx_users_edu_verified ON users(is_edu_verified);


-- ----------------------------------------------------------------------------
-- 2.2 Bảng Danh mục Tiện nghi (amenities)
-- Chuẩn 3NF: Tách danh mục tiện nghi ra khỏi Post để tránh đa trị / trùng lặp
-- ----------------------------------------------------------------------------
CREATE TABLE amenities (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,       -- air_conditioner, wifi, private_bathroom...
    name VARCHAR(100) NOT NULL,             -- Điều hòa, Wifi, Vệ sinh khép kín...
    icon VARCHAR(100),                      -- Lucide icon slug (wind, wifi, bath...)
    category VARCHAR(50) NOT NULL DEFAULT 'GENERAL', -- GENERAL, SAFETY, FURNITURE
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ----------------------------------------------------------------------------
-- 2.3 Bảng Bài đăng Phòng trọ (posts)
-- Chuẩn 3NF: Phụ thuộc khóa chính id, không chứa mảng đa trị
-- ----------------------------------------------------------------------------
CREATE TABLE posts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    landlord_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    -- Thông tin cơ bản
    title VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    room_type room_type NOT NULL DEFAULT 'ROOM',
    
    -- Địa chỉ & Tọa độ thực tế
    street_address VARCHAR(255) NOT NULL,
    ward VARCHAR(100) NOT NULL,
    district VARCHAR(100) NOT NULL,
    city VARCHAR(100) NOT NULL DEFAULT 'Hà Nội',
    full_address VARCHAR(500) NOT NULL,
    latitude DECIMAL(10, 7) NOT NULL,
    longitude DECIMAL(10, 7) NOT NULL,
    
    -- Thông số phòng
    area_sqm DECIMAL(6, 2) NOT NULL,        -- Diện tích (m²)
    max_tenants INT NOT NULL DEFAULT 2,     -- Số người tối đa
    floor_number INT,                       -- Tầng số
    total_floors INT,                       -- Tổng số tầng tòa nhà
    
    -- Chi phí thuê & Tiện ích (Phục vụ công cụ tính dự toán chi phí)
    base_price DECIMAL(12, 2) NOT NULL,     -- Giá thuê phòng/tháng (VNĐ)
    deposit DECIMAL(12, 2) NOT NULL DEFAULT 0, -- Tiền cọc (VNĐ)
    
    electricity_price DECIMAL(10, 2) NOT NULL DEFAULT 0,
    electricity_billing utility_billing_type NOT NULL DEFAULT 'PER_UNIT',
    
    water_price DECIMAL(10, 2) NOT NULL DEFAULT 0,
    water_billing utility_billing_type NOT NULL DEFAULT 'PER_UNIT',
    
    internet_price DECIMAL(10, 2) NOT NULL DEFAULT 0,
    service_fee DECIMAL(10, 2) NOT NULL DEFAULT 0, -- Phí dịch vụ chung (vệ sinh, thang máy, gửi xe)
    
    -- Trạng thái & Vòng đời bài viết
    status post_status NOT NULL DEFAULT 'AVAILABLE',
    views_count INT NOT NULL DEFAULT 0,
    expires_at TIMESTAMPTZ NOT NULL,        -- Ngày hết hạn tin (thường 30 ngày từ khi đăng)
    published_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_posts_price CHECK (base_price >= 0),
    CONSTRAINT chk_posts_area CHECK (area_sqm > 0),
    CONSTRAINT chk_posts_max_tenants CHECK (max_tenants >= 1),
    CONSTRAINT chk_posts_lat CHECK (latitude BETWEEN -90.0 AND 90.0),
    CONSTRAINT chk_posts_lng CHECK (longitude BETWEEN -180.0 AND 180.0)
);

CREATE INDEX idx_posts_landlord ON posts(landlord_id);
CREATE INDEX idx_posts_status ON posts(status);
CREATE INDEX idx_posts_coords ON posts(latitude, longitude);
CREATE INDEX idx_posts_price ON posts(base_price);
CREATE INDEX idx_posts_status_expires ON posts(status, expires_at);
CREATE INDEX idx_posts_published ON posts(published_at DESC);

-- ----------------------------------------------------------------------------
-- 2.4 Bảng Tiện nghi của phòng (post_amenities) - Chuẩn 3NF
-- ----------------------------------------------------------------------------
CREATE TABLE post_amenities (
    post_id UUID NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    amenity_id UUID NOT NULL REFERENCES amenities(id) ON DELETE RESTRICT,
    note VARCHAR(255),                      -- Ghi chú bổ sung (vd: 2 điều hòa)
    PRIMARY KEY (post_id, amenity_id)
);

CREATE INDEX idx_post_amenities_amenity ON post_amenities(amenity_id);

-- ----------------------------------------------------------------------------
-- 2.5 Bảng Hình ảnh phòng trọ (post_images) - Chuẩn 3NF
-- ----------------------------------------------------------------------------
CREATE TABLE post_images (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    post_id UUID NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    image_url TEXT NOT NULL,
    cloudinary_public_id VARCHAR(255),
    display_order INT NOT NULL DEFAULT 0,
    is_cover BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_post_images_post ON post_images(post_id, display_order ASC);

-- ----------------------------------------------------------------------------
-- 2.6 Bảng Đánh giá & Bình luận (reviews)
-- Chuẩn 3NF: 1 người dùng đánh giá 1 bài đăng tối đa 1 lần (hoặc cập nhật lại)
-- ----------------------------------------------------------------------------
CREATE TABLE reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    post_id UUID NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    rating SMALLINT NOT NULL,
    comment TEXT NOT NULL,
    
    -- Chống ngôn từ thô tục / Kiểm duyệt
    is_flagged BOOLEAN NOT NULL DEFAULT FALSE,
    flag_reason VARCHAR(255),
    
    -- Phản hồi từ chủ nhà
    landlord_reply TEXT,
    replied_at TIMESTAMPTZ,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_reviews_rating CHECK (rating BETWEEN 1 AND 5),
    CONSTRAINT uq_user_post_review UNIQUE (user_id, post_id)
);

CREATE INDEX idx_reviews_post ON reviews(post_id);
CREATE INDEX idx_reviews_user ON reviews(user_id);
CREATE INDEX idx_reviews_flagged ON reviews(is_flagged);

-- ----------------------------------------------------------------------------
-- 2.7 Bảng Lưu tin yêu thích (saved_posts) - Chuẩn 3NF
-- ----------------------------------------------------------------------------
CREATE TABLE saved_posts (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    post_id UUID NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, post_id)
);

CREATE INDEX idx_saved_posts_post ON saved_posts(post_id);

-- ----------------------------------------------------------------------------
-- 2.8 Bảng Lịch hẹn xem phòng (viewing_appointments)
-- ----------------------------------------------------------------------------
CREATE TABLE viewing_appointments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    post_id UUID NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    student_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    appointment_time TIMESTAMPTZ NOT NULL,
    contact_phone VARCHAR(20) NOT NULL,
    note TEXT,
    status appointment_status NOT NULL DEFAULT 'PENDING',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_appointments_post ON viewing_appointments(post_id);
CREATE INDEX idx_appointments_student ON viewing_appointments(student_id);
CREATE INDEX idx_appointments_status ON viewing_appointments(status);

-- ----------------------------------------------------------------------------
-- 2.9 Bảng Báo cáo vi phạm (reports)
-- Chuẩn 3NF với Khóa ngoại toàn vẹn tham chiếu
-- ----------------------------------------------------------------------------
CREATE TABLE reports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reporter_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    target_type report_target_type NOT NULL,
    
    -- Tham chiếu trực tiếp đến thực thể bị báo cáo (Đảm bảo tính toàn vẹn khóa ngoại)
    post_id UUID REFERENCES posts(id) ON DELETE CASCADE,
    review_id UUID REFERENCES reviews(id) ON DELETE CASCADE,
    reported_user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    
    reason VARCHAR(255) NOT NULL,
    description TEXT,
    status report_status NOT NULL DEFAULT 'PENDING',
    
    -- Nhật ký xử lý của Quản trị viên
    resolved_by UUID REFERENCES users(id) ON DELETE SET NULL,
    resolution_notes TEXT,
    resolved_at TIMESTAMPTZ,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Đảm bảo loại báo cáo khớp đúng 1 đối tượng duy nhất
    CONSTRAINT chk_report_target_consistency CHECK (
        (target_type = 'POST' AND post_id IS NOT NULL AND review_id IS NULL AND reported_user_id IS NULL) OR
        (target_type = 'REVIEW' AND review_id IS NOT NULL AND post_id IS NULL AND reported_user_id IS NULL) OR
        (target_type = 'USER' AND reported_user_id IS NOT NULL AND post_id IS NULL AND review_id IS NULL)
    )
);

CREATE INDEX idx_reports_status ON reports(status);
CREATE INDEX idx_reports_target_post ON reports(post_id);
CREATE INDEX idx_reports_target_review ON reports(review_id);
CREATE INDEX idx_reports_target_user ON reports(reported_user_id);
CREATE INDEX idx_reports_reporter ON reports(reporter_id);

-- ============================================================================
-- 3. HÀM TỰ ĐỘNG CẬP NHẬT updated_at (TRIGGER)
-- ============================================================================

CREATE OR REPLACE FUNCTION set_updated_at_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_users_updated_at
BEFORE UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION set_updated_at_timestamp();

CREATE TRIGGER trg_posts_updated_at
BEFORE UPDATE ON posts
FOR EACH ROW EXECUTE FUNCTION set_updated_at_timestamp();

CREATE TRIGGER trg_reviews_updated_at
BEFORE UPDATE ON reviews
FOR EACH ROW EXECUTE FUNCTION set_updated_at_timestamp();

CREATE TRIGGER trg_viewing_appointments_updated_at
BEFORE UPDATE ON viewing_appointments
FOR EACH ROW EXECUTE FUNCTION set_updated_at_timestamp();

CREATE TRIGGER trg_reports_updated_at
BEFORE UPDATE ON reports
FOR EACH ROW EXECUTE FUNCTION set_updated_at_timestamp();

-- ============================================================================
-- 4. HÀM TÍNH KHOẢNG CÁCH ĐỊA LÝ HAVERSINE (ĐƠN VỊ: KM)
-- Dùng để tìm kiếm phòng trọ quanh trường đại học với hiệu năng cao trực tiếp trong SQL
-- ============================================================================

CREATE OR REPLACE FUNCTION calculate_haversine_distance_km(
    lat1 DECIMAL,
    lon1 DECIMAL,
    lat2 DECIMAL,
    lon2 DECIMAL
)
RETURNS DECIMAL AS $$
DECLARE
    r CONSTANT DECIMAL := 6371.0; -- Bán kính Trái Đất (km)
    dlat DECIMAL;
    dlon DECIMAL;
    a DECIMAL;
    c DECIMAL;
BEGIN
    dlat := radians(lat2 - lat1);
    dlon := radians(lon2 - lon1);
    
    a := sin(dlat / 2.0) * sin(dlat / 2.0) +
         cos(radians(lat1)) * cos(radians(lat2)) *
         sin(dlon / 2.0) * sin(dlon / 2.0);
         
    c := 2.0 * atan2(sqrt(a), sqrt(1.0 - a));
    
    RETURN ROUND((r * c)::NUMERIC, 2);
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- ============================================================================
-- 5. SEED TIỆN NGHI CƠ BẢN (INITIAL AMENITIES)
-- ============================================================================

INSERT INTO amenities (code, name, icon, category) VALUES
('air_conditioner', 'Điều hòa', 'wind', 'FURNITURE'),
('water_heater', 'Bình nóng lạnh', 'flame', 'FURNITURE'),
('washing_machine', 'Máy giặt', 'waves', 'FURNITURE'),
('refrigerator', 'Tủ lạnh', 'refrigerator', 'FURNITURE'),
('mezzanine', 'Gác lửng', 'layers', 'STRUCTURE'),
('balcony', 'Ban công thoáng', 'sun', 'STRUCTURE'),
('private_bathroom', 'Vệ sinh khép kín', 'bath', 'STRUCTURE'),
('kitchen_shelf', 'Kệ bếp nấu ăn', 'utensils', 'FURNITURE'),
('wifi', 'Wifi tốc độ cao', 'wifi', 'SERVICE'),
('free_time', 'Giờ giấc tự do', 'clock', 'POLICY'),
('no_curfew_shared', 'Không chung chủ', 'key', 'POLICY'),
('parking', 'Chỗ để xe máy an toàn', 'bike', 'FACILITY'),
('elevator', 'Thang máy', 'chevrons-up-down', 'FACILITY'),
('cctv', 'Camera an ninh 24/7', 'shield-check', 'SAFETY'),
('pet_allowed', 'Cho phép nuôi thú cưng', 'heart', 'POLICY')
ON CONFLICT (code) DO NOTHING;
