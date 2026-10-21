# Kết quả kiểm thử

Kiểm tra tại workspace ngày 21/10/2026 (Asia/Saigon).

| Nhóm | Kết quả |
| --- | --- |
| Backend xUnit + HTTP integration / SQLite quan hệ | 48 đạt |
| PostgreSQL migrations + persistence | 1 bỏ qua local; CI bật bằng CAMPUSRENT_TEST_POSTGRES |
| Frontend Vitest / Testing Library | 15 đạt |
| Playwright desktop + mobile | 16 đạt |
| Vite production build | Đạt |
| .NET 8 build / Release publish | Đạt, 0 warning build |
| EF migration/model consistency | Không có thay đổi model chưa migrate |
| npm audit + NuGet advisory scan | Không phát hiện advisory tại thời điểm chạy |
| Docker Compose / CI YAML | Parse thành công; Docker chưa chạy local |
| Published app smoke | Trang chính, deep link, API, health và cache assets đạt |
| Published app browser smoke | Không lỗi JavaScript, không tràn ngang mobile |

## Phạm vi

Backend: đăng ký/đăng nhập, chuẩn hóa email, chặn tự cấp admin, JWT sai, rate limit, validation và giới hạn BCrypt Unicode; quyền sở hữu tin; tìm kiếm, giá, vị trí, dự toán; thay ảnh; đánh giá duy nhất, lọc/duyệt nội dung; báo cáo, xử lý báo cáo; yêu thích idempotent; đặt, xác nhận, hủy và đặt lại lịch; UTC; hết hạn tin; dữ liệu demo idempotent; ảnh giả và storage chưa cấu hình.

Frontend: API client, tiền/chi phí, card phòng, lưu phòng, form đăng nhập, chọn vai trò và modal bàn phím. E2E kiểm tra tìm/lọc phòng, trạng thái rỗng, bản đồ marker, tính chi phí, đăng nhập và giữ phiên sau reload, lưu/bỏ lưu, bảo vệ route, đăng/sửa/ẩn tin, trang quản trị, đăng ký sinh viên, đặt/hủy lịch, đánh giá/báo cáo và cập nhật hồ sơ trên desktop và mobile.

Coverage dùng tests/coverage.runsettings để loại mã migration sinh tự động và Razor Views lịch sử; không coi các file này là nghiệp vụ cần unit test. E2E không được cộng vào coverage .NET. Không khẳng định độ bao phủ 100%.

## Chạy lại

- Backend: dotnet test tests/CampusRent.Tests --settings tests/coverage.runsettings --collect:"XPlat Code Coverage"
- Frontend: npm --prefix client test
- Browser: dotnet build server, sau đó npm --prefix client run test:e2e
- Format: npm --prefix client run format:check
- Dependency: npm --prefix client audit; dotnet list server package --vulnerable --include-transitive

Test report Playwright nằm ở client/playwright-report/. Coverage nằm ở tests/CampusRent.Tests/TestResults/. Đây là output sinh tự động và được bỏ qua bởi Git.

## Giới hạn xác minh

Chưa chạy Docker/PostgreSQL trên máy này vì chưa cài hai dịch vụ. CI có service PostgreSQL và job build/chạy container. Chưa gọi Cloudinary thật vì không có credentials; đã kiểm tra lỗi rõ ràng khi chưa cấu hình. Ứng dụng không gửi email và không tự xác thực email trường. Chưa deploy ra máy chủ/domain công khai.
