import { test, expect } from '@playwright/test';

const api = 'http://127.0.0.1:5065/api';
const png = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jB5kAAAAASUVORK5CYII=',
  'base64'
);

test('menu responsive và nhãn chế độ hiển thị rõ sau khi tải font', async ({ page }) => {
  test.setTimeout(60000);
  await page.goto('/');
  await expect(page.locator('.room-card').first()).toBeVisible();
  await page.evaluate(() => document.fonts.ready);
  for (const width of [320, 360, 390, 768, 1024]) {
    await page.setViewportSize({ width, height: 844 });
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)
    ).toBeTruthy();
  }
  await page.setViewportSize({ width: 320, height: 844 });
  const contrast = await page.locator('.search-view-tabs button.active').evaluate((element) => {
    const style = getComputedStyle(element);
    return { foreground: style.color, background: style.backgroundColor };
  });
  expect(contrast.foreground).not.toBe(contrast.background);
  await page.getByRole('button', { name: 'Mở menu' }).click();
  await page
    .getByRole('navigation', { name: 'Điều hướng di động' })
    .getByRole('link', { name: 'Đăng nhập', exact: true })
    .click();
  await page.getByLabel('Email', { exact: true }).fill('landlord1@gmail.com');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('Password@123');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page).toHaveURL('/');
  await page.getByRole('button', { name: 'Mở menu' }).click();
  await page
    .getByRole('navigation', { name: 'Điều hướng di động' })
    .getByRole('link', { name: 'Đăng tin', exact: true })
    .click();
  await expect(page).toHaveURL('/post');
  await expect(page.getByRole('heading', { name: 'Chia sẻ một không gian tốt.' })).toBeVisible();
  expect(
    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)
  ).toBeTruthy();
});

async function register(page, prefix) {
  await page.goto('/register');
  await page.getByLabel('Họ và tên', { exact: true }).fill('Người dùng kiểm thử');
  await page.getByLabel('Email', { exact: true }).fill(`${prefix}-${Date.now()}@example.com`);
  await page.getByLabel('Mật khẩu', { exact: true }).fill('TestPassword123!');
  await page.getByRole('button', { name: 'Tạo tài khoản', exact: true }).click();
  await expect(page).toHaveURL('/');
  return page.evaluate(() => sessionStorage.getItem('campusrent-token'));
}

test('phân trang quay lại đúng trang trước và giữ bộ lọc', async ({ page }) => {
  await page.route('**/api/rooms?*', (route) => {
    const number = Number(new URL(route.request().url()).searchParams.get('page') || 1);
    const items = Array.from({ length: 12 }, (_, index) => ({
      id: `00000000-0000-0000-0000-${String(number * 12 + index).padStart(12, '0')}`,
      title: `Phòng trang ${number} số ${index + 1}`,
      address: 'Hà Nội',
      latitude: 21.005,
      longitude: 105.843,
      basePrice: 3000000,
      area: 25,
      maxPeople: 2,
      amenities: [],
      reviewCount: 0,
      averageRating: 0,
    }));
    return route.fulfill({
      json: { success: true, data: { items, total: 36, page: number, pageSize: 12 } },
    });
  });
  await page.goto('/?page=3&keyword=pagination');
  await expect(page.getByText('Trang 3 / 3', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Trang trước' }).click();
  await expect(page.getByText('Trang 2 / 3', { exact: true })).toBeVisible();
  await expect(page).toHaveURL(/keyword=pagination/);
  await page.getByRole('button', { name: 'Trang trước' }).click();
  await expect(page.getByText('Trang 1 / 3', { exact: true })).toBeVisible();
});

test('đổi mật khẩu giữ phiên hiện tại và vô hiệu hóa phiên cũ', async ({ page, request }) => {
  const token = await register(page, 'password');
  await page.goto('/account');
  await page.getByRole('button', { name: 'Đổi mật khẩu', exact: true }).click();
  await page.getByLabel('Mật khẩu hiện tại', { exact: true }).fill('TestPassword123!');
  await page.getByLabel('Mật khẩu mới', { exact: true }).fill('ChangedPassword123!');
  await page.getByLabel('Nhập lại mật khẩu mới').fill('ChangedPassword123!');
  await page.getByRole('button', { name: 'Lưu mật khẩu mới' }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  expect(
    (
      await request.get(`${api}/auth/me`, { headers: { Authorization: `Bearer ${token}` } })
    ).status()
  ).toBe(401);
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Thông tin cá nhân' })).toBeVisible();
});

test('tệp xác minh riêng tư, admin duyệt và chủ trọ tải ảnh đăng tin', async ({
  page,
  request,
}) => {
  test.setTimeout(60000);
  const token = await register(page, 'landlord');
  await page.goto('/account');
  await page.getByLabel(/Tiêu đề yêu cầu/).fill('Xác minh chủ trọ kiểm thử');
  await page
    .getByLabel(/Lý do & Giới thiệu/)
    .fill('Tôi có phòng cho sinh viên thuê và gửi tài liệu xác minh.');
  await page
    .locator('input[type=file][accept*=image]')
    .setInputFiles({ name: 'ownership.png', mimeType: 'image/png', buffer: png });
  await expect
    .poll(() => page.getByAltText('Ảnh 1', { exact: true }).getAttribute('src'))
    .toMatch(/^blob:/);
  await page.locator('input[type=file][accept*=pdf]').setInputFiles({
    name: 'ownership.pdf',
    mimeType: 'application/pdf',
    buffer: Buffer.from('%PDF-1.4\n%%EOF'),
  });
  await page.getByRole('button', { name: 'Gửi yêu cầu lên Quản trị viên' }).click();
  await expect(page.getByText('Chờ quản trị viên xét duyệt', { exact: true })).toBeVisible();
  const list = await request.get(`${api}/landlord-requests/my`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  const pending = (await list.json()).data[0];
  expect((await request.get(`http://127.0.0.1:5065${pending.pdfUrl}`)).status()).toBe(401);
  const download = page.waitForEvent('download');
  await page.getByRole('button', { name: /ownership.pdf/ }).click();
  expect((await download).suggestedFilename()).toBe('ownership.pdf');
  const login = await request.post(`${api}/auth/login`, {
    data: { email: 'admin@campusrent.vn', password: 'Password@123' },
  });
  const adminToken = (await login.json()).data.token;
  expect(
    (
      await request.patch(`${api}/admin/landlord-requests/${pending.id}/approve`, {
        headers: { Authorization: `Bearer ${adminToken}` },
      })
    ).ok()
  ).toBeTruthy();
  await page.evaluate(() => window.dispatchEvent(new Event('focus')));
  await expect(page.locator('.profile-card .status-pill')).toHaveText('Chủ trọ');
  await page.goto('/post');
  await page
    .locator('input[type=file]')
    .setInputFiles({ name: 'room.png', mimeType: 'image/png', buffer: png });
  await expect(page.locator('.post-page img[src*="/uploads/images/"]')).toBeVisible();
});

test('chọn gợi ý Google không có tọa độ và đặt ghim tại tọa độ bằng không', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Email', { exact: true }).fill('landlord1@gmail.com');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('Password@123');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page).toHaveURL('/');
  await page.route('**/api/places/suggest?*', (route) =>
    route.fulfill({
      json: {
        success: true,
        data: [
          {
            id: 'google:places/test-point',
            title: 'Địa điểm thử nghiệm',
            subtitle: 'Tọa độ 0',
            source: 'google',
            latitude: null,
            longitude: null,
          },
        ],
      },
    })
  );
  await page.route('**/api/places/resolve?*', (route) =>
    route.fulfill({
      json: {
        success: true,
        data: { title: 'Địa điểm thử nghiệm', subtitle: 'Tọa độ 0', latitude: 0, longitude: 0 },
      },
    })
  );
  await page.route('**/api/places/reverse?*', (route) =>
    route.fulfill({
      json: { success: true, data: { address: 'Địa điểm thử nghiệm', latitude: 0, longitude: 0 } },
    })
  );
  await page.goto('/post');
  await page.getByLabel('Tìm địa chỉ đặt ghim phòng trọ').fill('Địa điểm');
  await page.getByRole('button', { name: /Địa điểm thử nghiệm Tọa độ 0/ }).click();
  await expect(page.locator('.coords-display')).toHaveText(/0\.000000,\s*0\.000000/);
  await expect(page.locator('.picker-leaflet-map .leaflet-marker-icon')).toHaveCount(1);
});
