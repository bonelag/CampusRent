import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { AppProvider } from './context/AppContext';
import Layout from './components/Layout';
import { ScrollTop, Empty } from './components/Common';
import ProtectedRoute from './components/ProtectedRoute';
import SearchPage from './pages/SearchPage';
import { lazy } from 'react';
const RoomDetailPage = lazy(() => import('./pages/RoomDetailPage'));
const AuthPage = lazy(() => import('./pages/AuthPage'));
const SavedPage = lazy(() => import('./pages/SavedPage'));
const BookingsPage = lazy(() => import('./pages/BookingsPage'));
const AccountPage = lazy(() => import('./pages/AccountPage'));
const PostRoomPage = lazy(() => import('./pages/PostRoomPage'));
const MyRoomsPage = lazy(() => import('./pages/MyRoomsPage'));
const AdminPage = lazy(() => import('./pages/AdminPage'));
export default function App() {
  return (
    <BrowserRouter>
      <AppProvider>
        <ScrollTop />
        <a className="skip-link" href="#main-content">
          Đi đến nội dung
        </a>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<SearchPage />} />
            <Route path="rooms/:id" element={<RoomDetailPage />} />
            <Route path="login" element={<AuthPage key="login" />} />
            <Route path="register" element={<AuthPage key="register" register />} />
            <Route element={<ProtectedRoute />}>
              <Route path="saved" element={<SavedPage />} />
              <Route path="bookings" element={<BookingsPage />} />
              <Route path="account" element={<AccountPage />} />
            </Route>
            <Route element={<ProtectedRoute roles={['LANDLORD', 'ADMIN']} />}>
              <Route path="post" element={<PostRoomPage />} />
              <Route path="my-rooms" element={<MyRoomsPage />} />
              <Route path="rooms/:id/edit" element={<PostRoomPage />} />
            </Route>
            <Route element={<ProtectedRoute roles={['ADMIN']} />}>
              <Route path="admin" element={<AdminPage />} />
            </Route>
            <Route
              path="*"
              element={
                <div className="container page-shell">
                  <Empty
                    title="Trang này chưa có ở đây"
                    text="Quay lại để tìm căn phòng tiếp theo của bạn."
                  >
                    <Link to="/" className="button primary">
                      Về trang tìm phòng
                    </Link>
                  </Empty>
                </div>
              }
            />
          </Route>
        </Routes>
      </AppProvider>
    </BrowserRouter>
  );
}
