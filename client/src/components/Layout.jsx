import { useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  BadgeCheck,
  CalendarDays,
  Compass,
  Heart,
  House,
  KeyRound,
  LogOut,
  Menu,
  ShieldCheck,
  UserRound,
  X,
} from 'lucide-react';
import { useApp } from '../context/AppContext';

export function Logo() {
  return (
    <Link className="brand" to="/" aria-label="CampusRent trang chủ">
      <span className="brand-mark">
        <House size={20} />
      </span>
      <span className="brand-wordmark">
        CampusRent
        <small>Hệ thống Trọ Sinh viên</small>
      </span>
    </Link>
  );
}

export default function Layout() {
  const { user, logout, saved } = useApp();
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const authState = {
    from: location.pathname + location.search,
    backgroundLocation: location,
  };
  const profileLabel = user?.isEduVerified
    ? 'Sinh viên đã xác thực'
    : user?.role === 'ADMIN'
      ? 'Quản trị viên'
      : user?.role === 'LANDLORD'
        ? 'Chủ phòng trọ'
        : 'Người tìm trọ';
  const ProfileIcon = user?.isEduVerified
    ? BadgeCheck
    : user?.role === 'ADMIN'
      ? ShieldCheck
      : user?.role === 'LANDLORD'
        ? KeyRound
        : UserRound;
  const initials = user?.fullName.trim().split(/\s+/).at(-1).slice(0, 2).toUpperCase();

  return (
    <>
      <header className="header">
        <div className="header-inner">
          <Logo />
          <nav className="header-nav" aria-label="Điều hướng chính">
            <NavLink to="/" end>
              <Compass size={15} />
              <span>Tìm phòng</span>
            </NavLink>
            {user?.role === 'LANDLORD' && (
              <>
                <NavLink to="/my-rooms">
                  <House size={15} />
                  <span>Phòng của tôi</span>
                </NavLink>
                <NavLink to="/post">
                  <KeyRound size={15} />
                  <span>Đăng tin</span>
                </NavLink>
              </>
            )}
            {user?.role === 'ADMIN' && (
              <NavLink to="/admin">
                <ShieldCheck size={15} />
                <span>Quản trị</span>
              </NavLink>
            )}
          </nav>
          <div className="header-actions">
            {user && (
              <nav className="account-navigation" aria-label="Tiện ích tài khoản">
                <NavLink
                  to="/saved"
                  aria-label={`Đã lưu ${saved.length} phòng`}
                  title="Phòng đã lưu"
                >
                  <Heart size={16} />
                  {saved.length > 0 && <span className="nav-count">{saved.length}</span>}
                </NavLink>
                <NavLink to="/bookings" aria-label="Lịch hẹn của tôi" title="Lịch hẹn của tôi">
                  <CalendarDays size={16} />
                </NavLink>
              </nav>
            )}
            {user ? (
              <>
                <Link className="user-identity" to="/account">
                  <span className="avatar">{initials}</span>
                  <span className="user-meta">
                    <strong>{user.fullName}</strong>
                    <small className={user.isEduVerified ? 'verified' : ''}>
                      <ProfileIcon size={12} /> {profileLabel}
                    </small>
                  </span>
                </Link>
                <button
                  className="icon-button logout-button"
                  aria-label="Đăng xuất"
                  title="Đăng xuất"
                  onClick={() => {
                    logout();
                    navigate('/');
                  }}
                >
                  <LogOut size={16} />
                </button>
              </>
            ) : (
              <>
                <Link to="/login" state={authState} className="text-button login-link">
                  Đăng nhập
                </Link>
                <Link
                  to="/register"
                  state={authState}
                  className="button primary small register-link"
                >
                  Đăng ký
                </Link>
              </>
            )}
            <button
              className="icon-button mobile-menu"
              aria-label={open ? 'Đóng menu' : 'Mở menu'}
              aria-expanded={open}
              onClick={() => setOpen(!open)}
            >
              {open ? <X size={20} /> : <Menu size={20} />}
            </button>
          </div>
        </div>
        <nav
          className={`mobile-navigation ${open ? 'open' : ''}`}
          aria-label="Điều hướng di động"
          onClick={() => setOpen(false)}
        >
          <NavLink to="/" end>
            <Compass size={16} /> Tìm phòng
          </NavLink>
          {user?.role === 'LANDLORD' && (
            <>
              <NavLink to="/my-rooms">
                <House size={16} /> Phòng của tôi
              </NavLink>
              <NavLink to="/post">
                <KeyRound size={16} /> Đăng tin
              </NavLink>
            </>
          )}
          {user?.role === 'ADMIN' && (
            <NavLink to="/admin">
              <ShieldCheck size={16} /> Quản trị
            </NavLink>
          )}
          {user ? (
            <>
              <NavLink to="/saved">
                <Heart size={16} /> Đã lưu {saved.length > 0 && <span>{saved.length}</span>}
              </NavLink>
              <NavLink to="/bookings">
                <CalendarDays size={16} /> Lịch hẹn của tôi
              </NavLink>
              <NavLink to="/account">
                <UserRound size={16} /> Tài khoản
              </NavLink>
            </>
          ) : (
            <>
              <NavLink to="/login" state={authState}>
                Đăng nhập
              </NavLink>
              <NavLink to="/register" state={authState}>
                Đăng ký
              </NavLink>
            </>
          )}
        </nav>
      </header>
      <main id="main-content">
        <Outlet />
      </main>
      <footer className="footer">
        <span>© {new Date().getFullYear()} CampusRent · Hệ thống Trọ Sinh viên</span>
        <span>Thông tin minh bạch · Kết nối trực tiếp chủ trọ</span>
      </footer>
    </>
  );
}
