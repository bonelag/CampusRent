# Nhật ký hoàn thiện CampusRent

Nhánh thực hiện: `codex/complete-campusrent`. Mỗi mốc được kiểm tra và commit riêng.

1. Nền tảng API, cấu hình môi trường, JWT, rate limit và dịch vụ nghiệp vụ; unit test các công thức.
2. Dữ liệu và API: tài khoản, tìm phòng, quản lý tin, đánh giá, yêu thích, đặt lịch và kiểm duyệt; integration test quyền truy cập.
3. Giao diện tìm phòng responsive, bộ lọc, bản đồ, trang chi tiết và dự toán chi phí.
4. Giao diện đăng nhập, đăng tin, lịch hẹn, tin đã lưu và trang quản trị; kiểm thử tương tác.
5. Kiểm thử xuyên suốt, migrations PostgreSQL, container production, CI và hướng dẫn triển khai.

Các tài khoản mẫu chỉ được nạp ở Development. Production sử dụng PostgreSQL và secret do môi trường cung cấp.
