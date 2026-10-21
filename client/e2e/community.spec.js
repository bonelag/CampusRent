import { test, expect } from '@playwright/test';
test('sinh viên đăng ký, đặt lịch, đánh giá, báo cáo và cập nhật hồ sơ', async ({ page }) => {
  const comment = 'Không gian thoáng, tiện đi học, chi phí rõ ràng. ' + Date.now();
  await page.goto('/register');
  await page.getByLabel('Họ và tên', { exact: true }).fill('Sinh viên kiểm thử');
  await page.getByLabel('Email', { exact: true }).fill('student-' + Date.now() + '@example.com');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('StudentPass123!');
  await page.getByRole('button', { name: 'Tạo tài khoản', exact: true }).click();
  await expect(page).toHaveURL('/');
  await page.getByLabel('Từ khóa tìm phòng').fill('Bách Khoa');
  await page.getByRole('button', { name: 'Tìm phòng ngay' }).click();
  await expect(page.locator('.room-card')).toHaveCount(1);
  await page.locator('.room-card h3 a').click();
  await page.getByRole('button', { name: 'Đặt lịch xem phòng', exact: true }).click();
  const date = new Date(Date.now() + 48 * 3600000);
  await page.getByLabel('Ngày và giờ xem phòng').fill(date.toISOString().slice(0, 16));
  await page.getByLabel('Lời nhắn cho chủ trọ').fill('Mình muốn xem phòng cùng bạn.');
  await page.getByRole('button', { name: 'Gửi yêu cầu xem phòng' }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await page.getByRole('button', { name: 'Viết đánh giá' }).click();
  await page.getByRole('button', { name: '4 sao', exact: true }).click();
  await page.getByLabel('Đánh giá của bạn').fill(comment);
  await page.getByRole('button', { name: 'Gửi đánh giá', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.locator('.review').filter({ hasText: comment })).toBeVisible();
  await page.getByRole('button', { name: 'Báo cáo tin không chính xác' }).click();
  await page.getByLabel('Thông tin bổ sung').fill('Đây là báo cáo kiểm thử chức năng.');
  await page.getByRole('button', { name: 'Gửi báo cáo', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await page.goto('/bookings');
  const row = page.locator('.booking-item').filter({ hasText: 'Mình muốn xem phòng cùng bạn.' });
  await expect(row.getByText('Chờ xác nhận', { exact: true })).toBeVisible();
  await row.getByRole('button', { name: 'Hủy lịch', exact: true }).click();
  await page.getByRole('button', { name: 'Xác nhận hủy', exact: true }).click();
  await expect(row.getByText('Đã hủy', { exact: true })).toBeVisible();
  await page.goto('/account');
  await page.getByLabel('Họ và tên', { exact: true }).fill('Tên sau cập nhật');
  await page.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Tên sau cập nhật', exact: true })).toBeVisible();
});
test('lọc trường và khoảng cách, có trạng thái rỗng dễ phục hồi', async ({ page, isMobile }) => {
  await page.goto('/');
  await page
    .getByLabel('Trường đại học', { exact: true })
    .selectOption({ label: 'Đại học Bách Khoa Hà Nội' });
  if (isMobile) await page.getByRole('button', { name: 'Bộ lọc', exact: true }).click();
  await page.getByRole('button', { name: '1 km', exact: true }).click();
  if (isMobile) await page.getByRole('button', { name: 'Đóng bộ lọc', exact: true }).click();
  await expect(page.locator('.room-card').first()).toBeVisible();
  await expect
    .poll(async () => {
      const distances = await page.locator('.room-specs .distance').allTextContents();
      return (
        distances.length > 0 && distances.every((d) => parseFloat(d.replace(/[^0-9.]/g, '')) <= 1)
      );
    })
    .toBeTruthy();
  await page.getByLabel('Từ khóa tìm phòng').fill('Từ khóa chắc chắn không tồn tại xyz');
  await page.getByRole('button', { name: 'Tìm phòng ngay' }).click();
  await expect(page.getByRole('heading', { name: 'Chưa tìm thấy phòng phù hợp' })).toBeVisible();
  await page.getByRole('button', { name: 'Xóa bộ lọc', exact: true }).click();
  await expect(page.locator('.room-card').first()).toBeVisible();
});
