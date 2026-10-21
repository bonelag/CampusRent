import { useState } from 'react';
import { Link, NavLink, Outlet, useNavigate, useLocation } from 'react-router-dom';
import {
  House,
  ArrowUpRight,
  Heart,
  CalendarDays,
  Plus,
  Menu,
  X,
  LogOut,
  UserRound,
  ShieldCheck,
} from 'lucide-react';
import { useApp } from '../context/AppContext';
export function Logo() {
  return (
    <Link className="brand" to="/" aria-label="CampusRent trang chủ">
      <span className="brand-mark">
        <House size={23} strokeWidth={2.5} />
      </span>
      <span>
        campus<span className="brand-light">rent</span>
        <span className="brand-dot">.</span>
      </span>
    </Link>
  );
}
export default function Layout() {
  const { user, logout, saved } = useApp();
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  return (
    <>
      <header className="header">
        <div className="header-inner">
          <Logo />
          <nav
            className={open ? 'navigation open' : 'navigation'}
            aria-label="Điều hướng chính"
            onClick={() => setOpen(false)}
          >
            <NavLink to="/" end>
              Tìm phòng trọ
            </NavLink>
            <NavLink to="/saved">
              <Heart size={16} /> Đã lưu{' '}
              {saved.length > 0 && <span className="nav-count">{saved.length}</span>}
            </NavLink>
            <NavLink to="/bookings">
              <CalendarDays size={16} /> Lịch hẹn của tôi
            </NavLink>
            {user?.role === 'ADMIN' && (
              <NavLink to="/admin">
                <ShieldCheck size={16} /> Quản trị
              </NavLink>
            )}
          </nav>
          <div className="header-actions">
            <Link className="button outline small post-link" to="/post">
              <Plus size={16} /> Đăng tin
            </Link>
            {user ? (
              <>
                <Link className="avatar" to="/account" title={user.fullName}>
                  {user.fullName.charAt(0).toUpperCase()}
                </Link>
                <button
                  className="icon-button logout-button"
                  aria-label="Đăng xuất"
                  onClick={() => {
                    logout();
                    navigate('/');
                  }}
                >
                  <LogOut size={18} />
                </button>
              </>
            ) : (
              <Link
                to="/login"
                state={{ from: location.pathname + location.search }}
                className="button dark small"
              >
                Đăng nhập <ArrowUpRight size={15} />
              </Link>
            )}
            <button
              className="icon-button mobile-menu"
              aria-label={open ? 'Đóng menu' : 'Mở menu'}
              aria-expanded={open}
              onClick={() => setOpen(!open)}
            >
              {open ? <X /> : <Menu />}
            </button>
          </div>
        </div>
      </header>
      <main id="main-content">
        <Outlet />
      </main>
      <footer className="footer">
        <div>
          <Logo />
          <p>Một căn phòng tốt. Một khởi đầu mới.</p>
        </div>
        <div className="footer-right">
          <span>Được xây dựng cho cuộc sống sinh viên</span>
          <span>© {new Date().getFullYear()} CampusRent · Hà Nội, Việt Nam</span>
        </div>
      </footer>
    </>
  );
}
