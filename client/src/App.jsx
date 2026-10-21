import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { AppProvider } from './context/AppContext';
import Layout from './components/Layout';
import { ScrollTop, Empty } from './components/Common';
import ProtectedRoute from './components/ProtectedRoute';
import SearchPage from './pages/SearchPage';
import RoomDetailPage from './pages/RoomDetailPage';
import AuthPage from './pages/AuthPage';
import SavedPage from './pages/SavedPage';
import BookingsPage from './pages/BookingsPage';
import AccountPage from './pages/AccountPage';
import PostRoomPage from './pages/PostRoomPage';
import MyRoomsPage from './pages/MyRoomsPage';
import AdminPage from './pages/AdminPage';
export default function App() {
  return <BrowserRouter><AppProvider><ScrollTop/><a className="skip-link" href="#main-content">Đi đến nội dung</a><Routes><Route element={<Layout/>}><Route index element={<SearchPage/>}/><Route path="rooms/:id" element={<RoomDetailPage/>}/><Route path="login" element={<AuthPage key="login"/>}/><Route path="register" element={<AuthPage key="register" register/>}/><Route element={<ProtectedRoute/>}><Route path="saved" element={<SavedPage/>}/><Route path="bookings" element={<BookingsPage/>}/><Route path="account" element={<AccountPage/>}/></Route><Route element={<ProtectedRoute roles={['LANDLORD','ADMIN']}/>}><Route path="post" element={<PostRoomPage/>}/><Route path="my-rooms" element={<MyRoomsPage/>}/><Route path="rooms/:id/edit" element={<PostRoomPage/>}/></Route><Route element={<ProtectedRoute roles={['ADMIN']}/>}><Route path="admin" element={<AdminPage/>}/></Route><Route path="*" element={<div className="container page-shell"><Empty title="Trang này chưa có ở đây" text="Quay lại để tìm căn phòng tiếp theo của bạn."><Link to="/" className="button primary">Về trang tìm phòng</Link></Empty></div>}/></Route></Routes></AppProvider></BrowserRouter>;
}
