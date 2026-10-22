import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Check, Eye, EyeOff, List, Pencil, PlusCircle, RefreshCw } from 'lucide-react';
import { api, fallbackImage, money } from '../lib/api';
import { useApp } from '../context/AppContext';
import { Empty, ErrorState, Loading, Modal, PageTitle } from '../components/Common';

export default function MyRoomsPage() {
  const { notify } = useApp();
  const [rooms, setRooms] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState('');
  const [archive, setArchive] = useState(null);
  const close = useCallback(() => setArchive(null), []);
  const load = useCallback(async () => {
    setError('');
    try {
      setRooms(await api('/rooms/mine'));
    } catch (requestError) {
      setError(requestError.message);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function status(room, nextStatus, renew = false) {
    setBusy(room.id);
    try {
      await api('/rooms/' + room.id + '/status', {
        method: 'PATCH',
        body: { status: nextStatus, renew },
      });
      await load();
      setArchive(null);
      notify('Đã cập nhật tin đăng.');
    } catch (requestError) {
      notify(requestError.message, 'error');
    } finally {
      setBusy('');
    }
  }

  const roomStats = rooms
    ? {
        available: rooms.filter((room) => room.status === 'AVAILABLE').length,
        rented: rooms.filter((room) => room.status === 'RENTED').length,
        hidden: rooms.filter((room) => room.status === 'HIDDEN').length,
      }
    : null;

  return (
    <div className="container page-shell dashboard-page landlord-dashboard">
      <PageTitle
        eyebrow="KÊNH DÀNH CHO CHỦ NHÀ"
        title="Kênh Quản lý Phòng trọ của Chủ nhà"
        text="Đăng tin trọ mới, quản lý trạng thái còn phòng và gia hạn trong một nơi."
      >
        <Link className="button primary" to="/post">
          <PlusCircle size={17} /> Đăng phòng trọ mới
        </Link>
      </PageTitle>
      {error ? (
        <ErrorState message={error} retry={load} />
      ) : !rooms ? (
        <Loading />
      ) : !rooms.length ? (
        <Empty
          title="Căn phòng đầu tiên đang chờ"
          text="Thêm thông tin, ảnh và chi phí để kết nối với người thuê phù hợp."
        >
          <Link className="button primary" to="/post">
            Bắt đầu đăng tin
          </Link>
        </Empty>
      ) : (
        <>
          <section className="landlord-kpis" aria-label="Tổng quan tin đăng">
            <article className="dashboard-stat accent-indigo">
              <span>Tin đang hiển thị</span>
              <strong>{roomStats.available}</strong>
              <small>Sinh viên có thể tìm thấy</small>
            </article>
            <article className="dashboard-stat">
              <span>Phòng đã cho thuê</span>
              <strong>{roomStats.rented}</strong>
              <small>Tạm ngừng nhận liên hệ</small>
            </article>
            <article className="dashboard-stat accent-amber">
              <span>Tin đang ẩn</span>
              <strong>{roomStats.hidden}</strong>
              <small>Có thể đăng lại bằng gia hạn</small>
            </article>
            <article className="dashboard-stat accent-emerald">
              <span>Thời hạn tin đăng</span>
              <strong>
                30 <em>ngày</em>
              </strong>
              <small>Tự động ẩn nếu không gia hạn</small>
            </article>
          </section>
          <section className="dashboard-panel landlord-list-panel">
            <header className="dashboard-panel-heading">
              <h2>
                <List size={17} /> Danh sách phòng trọ đã đăng
              </h2>
              <span>{rooms.length} phòng</span>
            </header>
            <div className="managed-rooms">
              {rooms.map((room) => (
                <article className="managed-room" key={room.id}>
                  <img
                    src={room.coverImage || fallbackImage}
                    alt=""
                    onError={(event) => {
                      event.currentTarget.onerror = null;
                      event.currentTarget.src = fallbackImage;
                    }}
                  />
                  <div className="managed-room-body">
                    <div className="managed-room-topline">
                      <span
                        className={
                          'status-pill ' +
                          (room.status === 'AVAILABLE'
                            ? 'confirmed'
                            : room.status === 'RENTED'
                              ? 'pending'
                              : 'cancelled')
                        }
                      >
                        {room.status === 'AVAILABLE'
                          ? 'Đang mở cho thuê'
                          : room.status === 'RENTED'
                            ? 'Đã có người thuê'
                            : 'Đã ẩn'}
                      </span>
                      <Link
                        className="managed-preview"
                        to={'/rooms/' + room.id}
                        aria-label={'Xem tin ' + room.title}
                        title="Xem tin"
                      >
                        <Eye size={14} />
                      </Link>
                    </div>
                    <h2>{room.title}</h2>
                    <p>{room.address}</p>
                    <strong>
                      {money(room.basePrice)} <small>/ tháng · {room.area} m²</small>
                    </strong>
                    <div className="managed-actions">
                      {room.status === 'AVAILABLE' && (
                        <button
                          className="button outline small"
                          disabled={busy === room.id}
                          onClick={() => status(room, 'RENTED')}
                        >
                          <Check size={13} /> Đánh dấu: Đã thuê
                        </button>
                      )}
                      <Link className="button outline small" to={'/rooms/' + room.id + '/edit'}>
                        <Pencil size={13} /> Chỉnh sửa
                      </Link>
                      <button
                        className="button soft-primary small"
                        disabled={busy === room.id}
                        onClick={() => status(room, 'AVAILABLE', true)}
                      >
                        <RefreshCw size={13} /> Gia hạn +30 ngày
                      </button>
                      {room.status === 'AVAILABLE' && (
                        <button
                          className="icon-button managed-hide"
                          aria-label="Ẩn tin"
                          title="Ẩn tin"
                          onClick={() => setArchive(room)}
                        >
                          <EyeOff size={14} />
                        </button>
                      )}
                    </div>
                  </div>
                </article>
              ))}
            </div>
          </section>
        </>
      )}
      {archive && (
        <Modal title="Ẩn tin đăng?" onClose={close}>
          <p className="muted">
            Tin “{archive.title}” sẽ ngừng xuất hiện trong kết quả tìm kiếm. Bạn có thể đăng lại
            bằng cách gia hạn.
          </p>
          <div className="modal-actions">
            <button className="button outline" onClick={close}>
              Giữ hiển thị
            </button>
            <button
              className="button danger"
              disabled={!!busy}
              onClick={() => status(archive, 'HIDDEN')}
            >
              Ẩn tin
            </button>
          </div>
        </Modal>
      )}
    </div>
  );
}
