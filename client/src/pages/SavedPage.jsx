import { Link } from 'react-router-dom';
import { Heart, ArrowUpRight } from 'lucide-react';
import { useApp } from '../context/AppContext';
import { Empty, PageTitle } from '../components/Common';
import RoomCard from '../components/RoomCard';
export default function SavedPage() {
  const { saved } = useApp();
  return <div className="container page-shell"><PageTitle eyebrow="BỘ SƯU TẬP CỦA BẠN" title="Những căn phòng đã lưu." text={saved.length + ' lựa chọn đang chờ bạn quay lại.'}><Link to="/" className="button outline small">Khám phá thêm <ArrowUpRight size={16}/></Link></PageTitle>{saved.length ? <div className="room-grid saved-grid">{saved.map(room => <RoomCard key={room.id} room={room}/>)}</div> : <Empty title="Một chỗ cho những điều bạn thích" text="Chạm vào trái tim trên căn phòng ưng ý để lưu lại và dễ dàng tìm thấy sau này."><Link className="button primary" to="/">Tìm căn phòng đầu tiên <Heart size={16}/></Link></Empty>}</div>;
}
