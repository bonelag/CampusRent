# Nhật ký hoàn thiện CampusRent

Nhánh thực hiện: `codex/complete-campusrent`. Mỗi mốc được kiểm tra và commit riêng.

1. Nền tảng API, cấu hình môi trường, JWT, rate limit và dịch vụ nghiệp vụ; unit test các công thức.
2. Dữ liệu và API: tài khoản, tìm phòng, quản lý tin, đánh giá, yêu thích, đặt lịch và kiểm duyệt; integration test quyền truy cập.
3. Giao diện tìm phòng responsive, bộ lọc, bản đồ, trang chi tiết và dự toán chi phí.
4. Giao diện đăng nhập, đăng tin, lịch hẹn, tin đã lưu và trang quản trị; kiểm thử tương tác.
5. Kiểm thử xuyên suốt, migrations PostgreSQL, container production, CI và hướng dẫn triển khai.

Các tài khoản mẫu chỉ được nạp ở Development. Production sử dụng PostgreSQL và secret do môi trường cung cấp.

## Mốc 2
- Hoàn thiện API tài khoản, tìm kiếm, tin đăng, ảnh Cloudinary, đánh giá, yêu thích, lịch hẹn và kiểm duyệt.
- 33 unit/integration test đạt, dùng SQLite thật trong mỗi test để kiểm tra cả ràng buộc dữ liệu.
- Bỏ các controller MVC mẫu; React là giao diện chính.
- Email đuôi edu không tự coi là đã xác thực: cần xác minh quyền sở hữu email trước khi cấp huy hiệu trong production.

