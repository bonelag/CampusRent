import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import AdminPage from './AdminPage';

const { api, notify, logout } = vi.hoisted(() => ({
  api: vi.fn(),
  notify: vi.fn(),
  logout: vi.fn(),
}));

vi.mock('../context/AppContext', () => ({
  useApp: () => ({
    user: { id: 'admin', fullName: 'Quản trị viên', role: 'ADMIN' },
    notify,
    logout,
  }),
}));

vi.mock('../lib/api', async (importOriginal) => {
  const actual = await importOriginal();
  return { ...actual, api };
});

const datasets = {
  '/admin/stats': {
    users: 1,
    students: 1,
    landlords: 0,
    administrators: 1,
    rooms: 2,
    availableRooms: 1,
    rentedRooms: 0,
    hiddenRooms: 1,
    bookings: 1,
    pendingBookings: 1,
    favorites: 1,
    reviews: 1,
    reports: 1,
    pendingReports: 1,
    landlordRequests: 0,
    pendingLandlordRequests: 0,
    universities: 1,
    flaggedReviews: 0,
    moderationBacklog: 2,
    activity: Array.from({ length: 7 }, (_, index) => ({
      date: `0${index + 1}/11`,
      users: index % 2,
      rooms: 1,
      bookings: index % 3,
      reports: 0,
    })),
  },
  '/admin/users': [
    {
      id: 'user-1',
      email: 'student@example.com',
      fullName: 'Nguyễn Văn A',
      role: 'STUDENT',
      isEduVerified: false,
      createdAt: '2026-11-01T00:00:00Z',
      roomsCount: 0,
      reviewsCount: 1,
      reportsCount: 0,
      landlordRequestsCount: 0,
    },
  ],
  '/admin/rooms': [
    {
      id: 'room-1',
      title: 'Phòng đang hiển thị',
      address: 'Quận Hai Bà Trưng',
      basePrice: 3200000,
      area: 25,
      status: 'AVAILABLE',
      expiresAt: '2099-12-31T00:00:00Z',
      landlordName: 'Chủ trọ A',
      landlordEmail: 'landlord-a@example.com',
      coverImage: null,
      reviewCount: 1,
      reportCount: 0,
      bookingCount: 2,
      favoriteCount: 3,
    },
    {
      id: 'room-2',
      title: 'Phòng đang ẩn',
      address: 'Quận Cầu Giấy',
      basePrice: 2500000,
      area: 20,
      status: 'HIDDEN',
      expiresAt: '2026-11-01T00:00:00Z',
      landlordName: 'Chủ trọ B',
      landlordEmail: 'landlord-b@example.com',
      coverImage: null,
      reviewCount: 0,
      reportCount: 1,
      bookingCount: 0,
      favoriteCount: 0,
    },
  ],
  '/admin/bookings': [],
  '/admin/favorites': [],
  '/admin/reviews': [],
  '/admin/reports': [],
  '/admin/landlord-requests': [],
  '/admin/universities': [],
};

beforeEach(() => {
  api.mockReset();
  notify.mockReset();
  logout.mockReset();
  localStorage.clear();
  api.mockImplementation((path, options) =>
    options ? Promise.resolve({}) : Promise.resolve(datasets[path])
  );
});

it('hiển thị dashboard Adminator và chỉ cho admin đặt lại mật khẩu người dùng', async () => {
  render(
    <MemoryRouter initialEntries={['/admin']}>
      <AdminPage />
    </MemoryRouter>
  );

  expect(await screen.findByRole('heading', { name: 'Tổng quan' })).toBeVisible();
  expect(screen.getByText('Hoạt động 7 ngày gần nhất')).toBeVisible();
  await userEvent.click(screen.getByRole('button', { name: /Người dùng/ }));
  expect(await screen.findByText('student@example.com')).toBeVisible();
  expect(screen.queryByText(/0987654321/)).not.toBeInTheDocument();

  await userEvent.click(screen.getByTitle('Đặt lại mật khẩu'));
  fireEvent.change(screen.getByLabelText('Mật khẩu mới'), {
    target: { value: 'NewPassword123!' },
  });
  fireEvent.change(screen.getByLabelText('Xác nhận mật khẩu'), {
    target: { value: 'NewPassword123!' },
  });
  await userEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: /Đặt lại mật khẩu/ })
  );

  await waitFor(() =>
    expect(api).toHaveBeenCalledWith('/admin/users/user-1/password', {
      method: 'PUT',
      body: { password: 'NewPassword123!' },
    })
  );
});

it('lọc, sắp xếp và cung cấp đầy đủ thao tác quản lý phòng', async () => {
  render(
    <MemoryRouter initialEntries={['/admin?view=rooms']}>
      <AdminPage />
    </MemoryRouter>
  );

  expect(await screen.findByRole('heading', { name: 'Phòng trọ', level: 1 })).toBeVisible();
  expect(screen.getByRole('link', { name: /Đăng phòng mới/ })).toHaveAttribute('href', '/post');
  expect(screen.getByLabelText('Ẩn tin Phòng đang hiển thị')).toBeVisible();
  expect(screen.queryByLabelText('Ẩn tin Phòng đang ẩn')).not.toBeInTheDocument();

  await userEvent.selectOptions(screen.getByLabelText('Lọc theo trạng thái phòng'), 'HIDDEN');
  expect(screen.getByText('Phòng đang ẩn')).toBeVisible();
  expect(screen.queryByText('Phòng đang hiển thị')).not.toBeInTheDocument();

  await userEvent.selectOptions(screen.getByLabelText('Sắp xếp phòng trọ'), 'Tin đăng');
  expect(screen.getByRole('button', { name: 'Đổi chiều sắp xếp' })).toBeEnabled();
});
