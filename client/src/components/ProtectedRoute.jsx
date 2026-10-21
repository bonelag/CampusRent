import { Navigate, Outlet, useLocation, Link } from 'react-router-dom';
import { useApp } from '../context/AppContext';
import { Empty, Loading } from './Common';
export default function ProtectedRoute({ roles }) {
  const { user, authLoading } = useApp(),
    location = useLocation();
  if (authLoading) return <Loading />;
  if (!user)
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  if (roles && !roles.includes(user.role))
    return (
      <div className="container page-shell">
        <Empty
          title="Trang dành cho chủ trọ"
          text="Tài khoản của bạn chưa có quyền truy cập chức năng này."
        >
          <Link className="button primary" to="/">
            Tiếp tục tìm phòng
          </Link>
        </Empty>
      </div>
    );
  return <Outlet />;
}
