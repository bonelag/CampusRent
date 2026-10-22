import { useCallback, useEffect, useState } from 'react';
import {
  ShieldCheck,
  UsersRound,
  House,
  Flag,
  Star,
  CalendarDays,
  Check,
  X,
  GraduationCap,
} from 'lucide-react';
import { useApp } from '../context/AppContext';
import { api } from '../lib/api';
import { PageTitle, Loading, Empty, ErrorState, Modal } from '../components/Common';
export default function AdminPage() {
  const { notify } = useApp();
  const [data, setData] = useState(null),
    [error, setError] = useState(''),
    [tab, setTab] = useState('reports'),
    [busy, setBusy] = useState(''),
    [target, setTarget] = useState(null),
    [showUniversity, setShowUniversity] = useState(false);
  const close = useCallback(() => {
    setTarget(null);
    setShowUniversity(false);
  }, []);
  const load = useCallback(async () => {
    setError('');
    try {
      const [stats, reports, reviews] = await Promise.all([
        api('/admin/stats'),
        api('/admin/reports'),
        api('/admin/reviews'),
      ]);
      setData({ stats, reports, reviews });
    } catch (e) {
      setError(e.message);
    }
  }, []);
  useEffect(() => {
    load();
  }, [load]);
  async function resolve(id, status, deleteTarget = false) {
    setBusy(id);
    try {
      await api('/admin/reports/' + id, { method: 'PATCH', body: { status, deleteTarget } });
      notify('Đã xử lý báo cáo.');
      setTarget(null);
      await load();
    } catch (e) {
      notify(e.message, 'error');
    } finally {
      setBusy('');
    }
  }
  async function approve(id) {
    setBusy(id);
    try {
      await api('/admin/reviews/' + id + '/approve', { method: 'PATCH' });
      notify('Đã duyệt đánh giá.');
      await load();
    } catch (e) {
      notify(e.message, 'error');
    } finally {
      setBusy('');
    }
  }
  async function university(e) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    setBusy('university');
    try {
      await api('/universities', {
        method: 'POST',
        body: {
          name: f.get('name'),
          shortName: f.get('shortName'),
          address: f.get('address'),
          latitude: Number(f.get('latitude')),
          longitude: Number(f.get('longitude')),
        },
      });
      notify('Đã thêm trường đại học.');
      close();
    } catch (e) {
      notify(e.message, 'error');
    } finally {
      setBusy('');
    }
  }
  return (
    <div className="container page-shell dashboard-page admin-dashboard">
      <PageTitle
        eyebrow="QUẢN TRỊ HỆ THỐNG"
        title="Tổng quan quản trị."
        text="Kiểm duyệt báo cáo vi phạm, quản lý nội dung cộng đồng và chống tin ảo."
      >
        <div className="admin-heading-actions">
          <span className="admin-access-badge">
            <ShieldCheck size={14} /> Admin Role: Full Access
          </span>
          <button className="button outline small" onClick={() => setShowUniversity(true)}>
            <GraduationCap size={16} /> Thêm trường
          </button>
        </div>
      </PageTitle>
      {error ? (
        <ErrorState message={error} retry={load} />
      ) : !data ? (
        <Loading />
      ) : (
        <>
          <div className="stats-grid admin-kpis">
            {[
              ['users', 'Người dùng', 'Toàn hệ thống', UsersRound],
              ['rooms', 'Tổng bài đăng', 'Toàn hệ thống', House],
              ['availableRooms', 'Phòng đang hiển thị', 'Sinh viên có thể tìm thấy', Check],
              ['pendingReports', 'Báo cáo vi phạm', 'Cần xem xét và xử lý', Flag],
              ['flaggedReviews', 'Đánh giá chờ duyệt', 'Nội dung bị gắn cờ', Star],
              ['bookings', 'Lịch xem phòng', 'Tổng lượt đặt lịch', CalendarDays],
            ].map(([key, label, description, Icon]) => (
              <article className={'stat-card stat-' + key} key={key}>
                <span>{label}</span>
                <strong>{data.stats[key]}</strong>
                <small>{description}</small>
                <Icon size={20} />
              </article>
            ))}
          </div>
          <section className="dashboard-panel moderation-panel">
            <div className="tabs" role="group" aria-label="Nội dung kiểm duyệt">
              <button
                className={tab === 'reports' ? 'active' : ''}
                onClick={() => setTab('reports')}
              >
                Báo cáo vi phạm <span>{data.stats.pendingReports}</span>
              </button>
              <button
                className={tab === 'reviews' ? 'active' : ''}
                onClick={() => setTab('reviews')}
              >
                Đánh giá chờ duyệt <span>{data.stats.flaggedReviews}</span>
              </button>
            </div>
            {tab === 'reports' ? (
              data.reports.length ? (
                <div className="moderation-list">
                  {data.reports.map((r) => (
                    <article className="moderation-item" key={r.id}>
                      <div>
                        <span
                          className={
                            'status-pill ' + (r.status === 'PENDING' ? 'pending' : 'confirmed')
                          }
                        >
                          {r.status === 'PENDING'
                            ? 'Chờ xử lý'
                            : r.status === 'RESOLVED'
                              ? 'Đã xử lý'
                              : 'Đã bỏ qua'}
                        </span>
                        <h3>{r.reason}</h3>
                        <p>{r.roomTitle || r.reviewComment}</p>
                        {r.description && <p className="muted">{r.description}</p>}
                        <small>
                          Gửi bởi {r.reporterName} · {new Date(r.createdAt).toLocaleString('vi-VN')}
                        </small>
                      </div>
                      {r.status === 'PENDING' && (
                        <div className="moderation-actions">
                          <button
                            className="button primary small"
                            disabled={busy === r.id}
                            onClick={() => setTarget(r)}
                          >
                            Xử lý báo cáo
                          </button>
                          <button
                            className="button outline small"
                            disabled={busy === r.id}
                            onClick={() => resolve(r.id, 'DISMISSED')}
                          >
                            Bỏ qua
                          </button>
                        </div>
                      )}
                    </article>
                  ))}
                </div>
              ) : (
                <Empty
                  title="Cộng đồng đang yên tâm"
                  text="Chưa có báo cáo vi phạm nào được gửi đến."
                />
              )
            ) : data.reviews.length ? (
              <div className="moderation-list">
                {data.reviews.map((r) => (
                  <article className="moderation-item" key={r.id}>
                    <div>
                      <span className="status-pill pending">Cần kiểm duyệt</span>
                      <h3>{r.roomTitle}</h3>
                      <p>“{r.comment}”</p>
                      <small>
                        {r.userName} · {r.rating} sao · Từ khóa: {r.flagReason}
                      </small>
                    </div>
                    <button
                      className="button primary small"
                      disabled={busy === r.id}
                      onClick={() => approve(r.id)}
                    >
                      <Check size={14} /> Cho phép hiển thị
                    </button>
                  </article>
                ))}
              </div>
            ) : (
              <Empty title="Đã kiểm duyệt xong" text="Chưa có đánh giá nào đang chờ duyệt." />
            )}
          </section>
        </>
      )}
      {target && (
        <Modal title="Xử lý báo cáo vi phạm" onClose={close}>
          <p className="muted">
            Nội dung bị báo cáo: {target.roomTitle || target.reviewComment}. Bạn có thể ẩn nội dung
            vi phạm hoặc chỉ đánh dấu báo cáo đã xử lý.
          </p>
          <div className="modal-actions">
            <button
              className="button outline small"
              disabled={!!busy}
              onClick={() => resolve(target.id, 'RESOLVED')}
            >
              Đánh dấu đã xử lý
            </button>
            <button
              className="button danger small"
              disabled={!!busy}
              onClick={() => resolve(target.id, 'RESOLVED', true)}
            >
              Ẩn nội dung vi phạm
            </button>
          </div>
        </Modal>
      )}
      {showUniversity && (
        <Modal title="Thêm trường đại học" onClose={close}>
          <form className="form" onSubmit={university}>
            <label>
              Tên trường
              <input name="name" required maxLength="200" />
            </label>
            <label>
              Mã viết tắt
              <input name="shortName" required maxLength="50" />
            </label>
            <label>
              Địa chỉ
              <input name="address" required maxLength="300" />
            </label>
            <div className="form-row">
              <label>
                Vĩ độ
                <input name="latitude" type="number" min="-90" max="90" step="any" required />
              </label>
              <label>
                Kinh độ
                <input name="longitude" type="number" min="-180" max="180" step="any" required />
              </label>
            </div>
            <button className="button primary full" disabled={!!busy}>
              Thêm trường đại học
            </button>
          </form>
        </Modal>
      )}
    </div>
  );
}
