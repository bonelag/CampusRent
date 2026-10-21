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


## Mốc 3
- Giao diện tìm phòng, bộ lọc URL, Leaflet, chi tiết, dự toán chi phí và modal đặt lịch/đánh giá/báo cáo.
- 10 test frontend đạt; build production đạt; kiểm tra ảnh desktop 1440px và mobile 390px, không tràn ngang hay lỗi JavaScript.
- Dùng CSS riêng để thống nhất nhận diện; bỏ Tailwind không còn dùng. Cập nhật React Router và công cụ test, npm audit hiện không có lỗ hổng.


## Mốc 4
- Hoàn thiện đăng ký/đăng nhập, hồ sơ, yêu thích, lịch hẹn, đăng/sửa/quản lý tin và trang quản trị.
- 15 test frontend, 34 test backend và 12 E2E desktop/mobile đạt.
- E2E tìm được lỗi cập nhật ảnh của EF; đã sửa và bổ sung test hồi quy.


## Mốc 5
- Cập nhật thư viện .NET có cảnh báo; NuGet và npm hiện không có advisory được phát hiện.
- Bổ sung kiểm tra JWT, rate limit, upload giả, Unicode, quyền quản trị, lịch trùng và UTC; 46 backend + 15 frontend + 16 E2E đạt.
- Test PostgreSQL được cấu hình chạy ở CI; local bỏ qua 1 ca vì chưa có PostgreSQL.
- Chuẩn hóa định dạng frontend, thêm error boundary và giới hạn input; tạo migration PostgreSQL ban đầu cùng SQL để review.

