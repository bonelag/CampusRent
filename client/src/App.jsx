import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { AppProvider } from './context/AppContext';
import Layout from './components/Layout';
import { ScrollTop, Empty } from './components/Common';
import SearchPage from './pages/SearchPage';
import RoomDetailPage from './pages/RoomDetailPage';
export default function App() {
  return <BrowserRouter><AppProvider><ScrollTop/><a className="skip-link" href="#main-content">Đi đến nội dung</a><Routes><Route element={<Layout/>}><Route index element={<SearchPage/>}/><Route path="rooms/:id" element={<RoomDetailPage/>}/><Route path="*" element={<Empty title="Trang này chưa có ở đây" text="Quay lại để tìm căn phòng tiếp theo của bạn."><Link to="/" className="button primary">Về trang tìm phòng</Link></Empty>}/></Route></Routes></AppProvider></BrowserRouter>;
}
