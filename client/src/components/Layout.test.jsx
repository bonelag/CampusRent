import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import Layout from './Layout';

let user;
const logout = vi.fn();

vi.mock('../context/AppContext', () => ({
  useApp: () => ({ user, logout, saved: [] }),
}));

beforeEach(() => {
  user = { id: 'admin', fullName: 'Quản trị viên', role: 'ADMIN', isEduVerified: false };
  logout.mockReset();
});

it('hiển thị chức năng đăng và quản lý phòng cho quản trị viên', () => {
  render(
    <MemoryRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<div>Trang chủ</div>} />
        </Route>
      </Routes>
    </MemoryRouter>
  );

  expect(screen.getAllByRole('link', { name: /Phòng của tôi/ })).toHaveLength(2);
  expect(screen.getAllByRole('link', { name: /Đăng tin/ })).toHaveLength(2);
  expect(screen.getAllByRole('link', { name: /^Quản trị$/ })).toHaveLength(2);
});

it('dành toàn bộ khung giao diện cho trang quản trị', () => {
  render(
    <MemoryRouter initialEntries={['/admin']}>
      <Routes>
        <Route element={<Layout />}>
          <Route path="admin" element={<div>Bảng điều khiển riêng</div>} />
        </Route>
      </Routes>
    </MemoryRouter>
  );

  expect(screen.getByText('Bảng điều khiển riêng')).toBeVisible();
  expect(screen.queryByRole('banner')).not.toBeInTheDocument();
  expect(screen.queryByRole('contentinfo')).not.toBeInTheDocument();
});
