import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { vi, it, expect, beforeEach } from 'vitest';
import PostRoomPage from './PostRoomPage';

// Tạo các hàm giả lập để kiểm thử, không gọi máy chủ thật.
const { apiMock, notifyMock } = vi.hoisted(() => ({
  apiMock: vi.fn(),
  notifyMock: vi.fn(),
}));

// Giả lập API.
vi.mock('../lib/api', () => ({
  api: apiMock,
  amenities: {},
  estimateCost: () => 3500000,
  money: (value) => String(value),
}));

// Giả lập AppContext.
vi.mock('../context/AppContext', () => ({
  useApp: () => ({ notify: notifyMock }),
}));

// Chuẩn bị dữ liệu trước mỗi bài kiểm thử.
beforeEach(() => {
  apiMock.mockReset();
  notifyMock.mockReset();

  apiMock.mockImplementation((path) => {
    if (path === '/universities') {
      return Promise.resolve([]);
    }

    return Promise.resolve({ id: 42 });
  });
});

// Hiển thị trang trong môi trường Router.
function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/post-room']}>
      <Routes>
        <Route path="/post-room" element={<PostRoomPage />} />
        <Route path="/rooms/:id" element={<h1>Trang chi tiết phòng</h1>} />
      </Routes>
    </MemoryRouter>
  );
}

// Điền những trường bắt buộc.
async function fillRequiredFields(user) {
  await user.type(screen.getByLabelText('Tiêu đề tin đăng'), 'Phòng trọ gần trường đại học');

  await user.type(screen.getByLabelText('Địa chỉ đầy đủ'), 'Hà Nội');

  await user.type(screen.getByLabelText('Mô tả chi tiết'), 'Phòng sạch sẽ, đầy đủ tiện nghi.');
}

// Thêm một đường dẫn ảnh HTTPS.
async function addImage(user) {
  await user.type(screen.getByLabelText('Đường dẫn ảnh HTTPS'), 'https://example.com/room.jpg');

  await user.click(screen.getByRole('button', { name: 'Thêm ảnh' }));
}

// TEST 1: Kiểm tra trang hiển thị.
it('hiển thị các trường nhập thông tin phòng', async () => {
  renderPage();

  expect(await screen.findByLabelText('Tiêu đề tin đăng')).toBeInTheDocument();

  expect(screen.getByLabelText('Địa chỉ đầy đủ')).toBeInTheDocument();

  expect(screen.getByLabelText('Mô tả chi tiết')).toBeInTheDocument();

  expect(screen.getByRole('button', { name: /Đăng tin phòng trọ/ })).toBeInTheDocument();
});

// TEST 2: Không cho đăng tin khi chưa có ảnh.
it('hiển thị lỗi khi chưa thêm ảnh phòng', async () => {
  const user = userEvent.setup();

  renderPage();

  await screen.findByLabelText('Tiêu đề tin đăng');

  await fillRequiredFields(user);

  await user.click(screen.getByRole('button', { name: /Đăng tin phòng trọ/ }));

  expect(await screen.findByRole('alert')).toHaveTextContent(
    'Thêm ít nhất một ảnh phòng trước khi đăng.'
  );

  expect(apiMock).not.toHaveBeenCalledWith('/rooms', expect.anything());
});

// TEST 3: Đăng tin thành công.
it('gửi dữ liệu tạo phòng và chuyển đến trang chi tiết', async () => {
  const user = userEvent.setup();

  renderPage();

  await screen.findByLabelText('Tiêu đề tin đăng');

  await fillRequiredFields(user);
  await addImage(user);

  await user.click(screen.getByRole('button', { name: /Đăng tin phòng trọ/ }));

  expect(await screen.findByText('Trang chi tiết phòng')).toBeInTheDocument();

  expect(apiMock).toHaveBeenCalledWith(
    '/rooms',
    expect.objectContaining({
      method: 'POST',
      body: expect.objectContaining({
        title: 'Phòng trọ gần trường đại học',
        address: 'Hà Nội',
        universityId: null,
        imageUrls: ['https://example.com/room.jpg'],
      }),
    })
  );

  expect(notifyMock).toHaveBeenCalledWith('Tin đăng của bạn đã sẵn sàng!');
});

// TEST 4: Hiển thị lỗi khi API từ chối tạo phòng.
it('hiển thị lỗi khi máy chủ không tạo được phòng', async () => {
  const user = userEvent.setup();

  apiMock.mockImplementation((path) => {
    if (path === '/universities') {
      return Promise.resolve([]);
    }

    return Promise.reject(new Error('Không thể tạo phòng.'));
  });

  renderPage();

  await screen.findByLabelText('Tiêu đề tin đăng');

  await fillRequiredFields(user);
  await addImage(user);

  await user.click(screen.getByRole('button', { name: /Đăng tin phòng trọ/ }));

  expect(await screen.findByRole('alert')).toHaveTextContent('Không thể tạo phòng.');
});
