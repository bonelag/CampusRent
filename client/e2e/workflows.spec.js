import { test, expect } from '@playwright/test';
async function login(page, email = 'nguyenvanan@sis.hust.edu.vn') {
  await page.goto('/login');
  await page.getByLabel('Email', { exact: true }).fill(email);
  await page.getByLabel('Mật khẩu', { exact: true }).fill('Password@123');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.getByRole('link', { name: 'Đăng nhập', exact: true })).toHaveCount(0);
}
test('tìm phòng, lọc và xem chi phí thực tế', async ({ page }) => {
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/');
  await expect(page.locator('.room-card').first()).toBeVisible();
  await page.getByLabel('Từ khóa tìm phòng').fill('Bách Khoa');
  await page.getByRole('button', { name: 'Tìm phòng ngay' }).click();
  await expect(page.locator('.room-card')).toHaveCount(1);
  await page.locator('.room-card h3 a').click();
  await expect(
    page.getByRole('heading', { name: 'Mỗi tháng, bạn sẽ cần bao nhiêu?' })
  ).toBeVisible();
  const total = page.getByTestId('estimated-total');
  await expect(total).toContainText('3.774.000');
  await page.getByLabel('Điện dự kiến', { exact: true }).fill('0');
  await expect(total).toContainText('3.470.000');
  expect(errors).toEqual([]);
  expect(
    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)
  ).toBeTruthy();
});
test('gợi ý vị trí thời gian thực đặt tâm tìm kiếm trên bản đồ', async ({ page }) => {
  await page.route('**/api/places/suggest?*', async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        data: [
          {
            id: 'listing:103',
            title: 'Tòa nhà 103 Tạ Quang Bửu',
            subtitle: 'Hai Bà Trưng, Hà Nội',
            latitude: 21.005,
            longitude: 105.843,
            source: 'listing',
          },
        ],
      }),
    });
  });
  await page.goto('/');
  const location = page.getByLabel('Tìm vị trí hoặc trường đại học');
  await location.fill('103 Tạ Quang Bửu');
  await expect(page.getByRole('option', { name: /Tòa nhà 103 Tạ Quang Bửu/ })).toBeVisible();
  await page.getByRole('option', { name: /Tòa nhà 103 Tạ Quang Bửu/ }).click();
  await expect(location).toHaveValue('Tòa nhà 103 Tạ Quang Bửu');
  await expect(page).toHaveURL(/centerLat=21\.005000/);
  await expect(page.locator('.map-panel-heading')).toContainText('Tòa nhà 103 Tạ Quang Bửu');
});
test('đăng nhập, lưu phòng và giữ phiên sau khi tải lại', async ({ page }) => {
  await login(page);
  await page.goto('/');
  await page.locator('.room-card .save-button').first().click();
  await page.goto('/saved');
  await expect(page.locator('.room-card').first()).toBeVisible();
  await page.reload();
  await expect(page.locator('.room-card').first()).toBeVisible();
  await page.locator('.room-card .save-button').first().click();
  await expect(page.getByRole('status')).toContainText('Đã bỏ lưu');
});
test('bảo vệ trang chủ trọ khi chưa đăng nhập', async ({ page }) => {
  await page.goto('/post');
  await expect(page).toHaveURL(/login/);
  await expect(page.getByRole('heading', { name: 'Chào mừng trở lại.' })).toBeVisible();
});
test('chủ trọ tạo và chỉnh sửa tin đăng', async ({ page }) => {
  await login(page, 'landlord1@gmail.com');
  await page.goto('/post');
  const title = 'Phòng kiểm thử ' + Date.now();
  await page.getByLabel('Tiêu đề tin đăng').fill(title);
  await page.getByLabel('Địa chỉ đầy đủ').fill('12 Tạ Quang Bửu, Hà Nội');
  await page.getByLabel('Mô tả chi tiết').fill('Phòng thử nghiệm sạch sẽ, ánh sáng tự nhiên.');
  await page
    .getByLabel('Đường dẫn ảnh HTTPS')
    .fill('https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800');
  await page.getByRole('button', { name: 'Thêm ảnh', exact: true }).click();
  await page.getByRole('button', { name: 'Đăng tin phòng trọ' }).click({ force: true });
  await expect(page.getByRole('heading', { name: title, exact: true })).toBeVisible();
  const path = new URL(page.url()).pathname;
  await page.goto(path + '/edit');
  await page.getByLabel('Tiêu đề tin đăng').fill(title + ' đã sửa');
  await page.getByRole('button', { name: 'Lưu thay đổi' }).click({ force: true });
  await expect(page.getByRole('heading', { name: title + ' đã sửa', exact: true })).toBeVisible();
  await page.goto('/my-rooms');
  const row = page.locator('.managed-room').filter({ hasText: title + ' đã sửa' });
  await row.getByRole('button', { name: 'Ẩn tin', exact: true }).click();
  await page
    .getByRole('dialog')
    .getByRole('button', { name: 'Ẩn tin', exact: true })
    .click({ force: true });
  await expect(row.getByText('Đã ẩn', { exact: true })).toBeVisible();
});
test('quản trị xem tổng quan và danh sách kiểm duyệt', async ({ page }) => {
  await login(page, 'admin@campusrent.vn');
  await page.goto('/admin');
  await expect(page.getByRole('heading', { name: 'Tổng quan quản trị.' })).toBeVisible();
  await expect(page.locator('.stat-card')).toHaveCount(6);
  await page.getByRole('button', { name: /Đánh giá chờ duyệt/ }).click();
  await expect(page.getByRole('heading', { name: 'Đã kiểm duyệt xong' })).toBeVisible();
});
test('bản đồ hiển thị và trang không tràn ngang', async ({ page }) => {
  await page.goto('/');
  await expect(page.locator('.room-card').first()).toBeVisible();
  await page.getByRole('button', { name: 'Xem bản đồ', exact: true }).click();
  await expect(page.locator('.leaflet-container')).toBeVisible();
  await expect(page.locator('.price-marker').first()).toBeVisible();
  expect(
    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)
  ).toBeTruthy();
});
