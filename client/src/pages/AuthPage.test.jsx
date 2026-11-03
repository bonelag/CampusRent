import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi, it, expect, beforeEach } from 'vitest';
import userEvent from '@testing-library/user-event';
import AuthPage from './AuthPage';
const authenticate = vi.fn(),
  notify = vi.fn();
vi.mock('../context/AppContext', () => ({ useApp: () => ({ user: null, authenticate, notify }) }));
beforeEach(() => {
  authenticate.mockReset();
});
it('gửi thông tin đăng nhập và hiển thị lỗi', async () => {
  authenticate.mockRejectedValue(new Error('Email hoặc mật khẩu không chính xác.'));
  render(
    <MemoryRouter>
      <AuthPage />
    </MemoryRouter>
  );
  await userEvent.type(screen.getByLabelText('Email'), 'student@example.com');
  await userEvent.type(screen.getByLabelText('Mật khẩu'), 'incorrect');
  await userEvent.click(screen.getByRole('button', { name: 'Đăng nhập' }));
  expect(authenticate).toHaveBeenCalledWith('login', {
    email: 'student@example.com',
    password: 'incorrect',
  });
  expect(await screen.findByRole('alert')).toHaveTextContent(
    'Email hoặc mật khẩu không chính xác.'
  );
});
it('mặc định đăng ký tài khoản với vai trò người tìm trọ', async () => {
  authenticate.mockResolvedValue({});
  render(
    <MemoryRouter>
      <AuthPage register />
    </MemoryRouter>
  );
  expect(screen.getByLabelText('Họ và tên')).toBeRequired();
  expect(screen.queryByRole('button', { name: /Chủ trọ/ })).not.toBeInTheDocument();
  await userEvent.type(screen.getByLabelText('Họ và tên'), 'Nguyễn Văn An');
  await userEvent.type(screen.getByLabelText('Email'), 'student@example.com');
  await userEvent.type(screen.getByLabelText('Mật khẩu'), 'StudentPassword123!');
  await userEvent.click(screen.getByRole('button', { name: 'Tạo tài khoản' }));
  expect(authenticate).toHaveBeenCalledWith('register', {
    fullName: 'Nguyễn Văn An',
    email: 'student@example.com',
    password: 'StudentPassword123!',
    phoneNumber: '',
    role: 'STUDENT',
  });
});
it('hiện và ẩn mật khẩu theo lựa chọn', async () => {
  render(
    <MemoryRouter>
      <AuthPage />
    </MemoryRouter>
  );
  expect(screen.getByLabelText('Mật khẩu')).toHaveAttribute('type', 'password');
  await userEvent.click(screen.getByRole('button', { name: 'Hiện mật khẩu' }));
  expect(screen.getByLabelText('Mật khẩu')).toHaveAttribute('type', 'text');
});
