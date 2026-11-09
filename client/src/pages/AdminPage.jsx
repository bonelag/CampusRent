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
  KeyRound,
  FileText,
  Clock,
  AlertCircle,
  ExternalLink,
  Pencil,
  BadgeCheck,
  Search,
} from 'lucide-react';
import { useApp } from '../context/AppContext';
import { api } from '../lib/api';
import { PageTitle, Loading, Empty, ErrorState, Modal } from '../components/Common';
import { DownloadLink, ProtectedImage } from '../components/Attachments';

export default function AdminPage() {
  const { notify } = useApp();
  const [data, setData] = useState(null),
    [error, setError] = useState(''),
    [tab, setTab] = useState('users'),
    [busy, setBusy] = useState(''),
    [target, setTarget] = useState(null),
    [rejectTarget, setRejectTarget] = useState(null),
    [rejectReason, setRejectReason] = useState(''),
    [showUniversity, setShowUniversity] = useState(false);

  // User management state
  const [userSearch, setUserSearch] = useState('');
  const [editingUser, setEditingUser] = useState(null);
  const [userForm, setUserForm] = useState({
    fullName: '',
    phoneNumber: '',
    role: 'STUDENT',
    isEduVerified: false,
  });
  const [savingUser, setSavingUser] = useState(false);

  const close = useCallback(() => {
    setTarget(null);
    setRejectTarget(null);
    setRejectReason('');
    setShowUniversity(false);
    setEditingUser(null);
  }, []);

  const load = useCallback(async () => {
    setError('');
    try {
      const [stats, reports, reviews, landlordRequests, users] = await Promise.all([
        api('/admin/stats'),
        api('/admin/reports'),
        api('/admin/reviews'),
        api('/admin/landlord-requests'),
        api('/admin/users'),
      ]);
      setData({ stats, reports, reviews, landlordRequests, users });
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

  async function approveLandlordRequest(id) {
    setBusy(id);
    try {
      await api('/admin/landlord-requests/' + id + '/approve', { method: 'PATCH' });
      notify('Đã duyệt tài khoản thành Chủ trọ.');
      await load();
    } catch (e) {
      notify(e.message, 'error');
    } finally {
      setBusy('');
    }
  }

  async function handleRejectLandlordRequest(e) {
    e.preventDefault();
    if (!rejectTarget) return;
    setBusy(rejectTarget.id);
    try {
      await api('/admin/landlord-requests/' + rejectTarget.id + '/reject', {
        method: 'PATCH',
        body: { adminNote: rejectReason.trim() },
      });
      notify('Đã từ chối yêu cầu nâng cấp.');
      close();
      await load();
    } catch (e) {
      notify(e.message, 'error');
    } finally {
      setBusy('');
    }
  }

  function openEditUser(u) {
    setEditingUser(u);
    setUserForm({
      fullName: u.fullName,
      phoneNumber: u.phoneNumber || '',
      role: u.role,
      isEduVerified: u.isEduVerified || false,
    });
  }

  async function handleSaveUser(e) {
    e.preventDefault();
    if (!editingUser) return;
    setSavingUser(true);
    try {
      await api('/admin/users/' + editingUser.id, {
        method: 'PUT',
        body: userForm,
      });
      notify('Đã cập nhật thông tin người dùng.');
      setEditingUser(null);
      await load();
    } catch (err) {
      notify(err.message, 'error');
    } finally {
      setSavingUser(false);
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

  const filteredUsers =
    data?.users?.filter((u) => {
      if (!userSearch.trim()) return true;
      const q = userSearch.toLowerCase();
      return (
        u.fullName.toLowerCase().includes(q) ||
        u.email.toLowerCase().includes(q) ||
        (u.phoneNumber && u.phoneNumber.includes(q))
      );
    }) || [];

  return (
    <div className="container page-shell dashboard-page admin-dashboard">
      <PageTitle
        eyebrow="QUẢN TRỊ HỆ THỐNG"
        title="Tổng quan quản trị."
        text="Quản lý người dùng, duyệt yêu cầu làm chủ trọ, kiểm duyệt báo cáo và chống tin ảo."
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
              ['pendingLandlordRequests', 'Yêu cầu làm chủ trọ', 'Cần duyệt quyền', KeyRound],
              ['rooms', 'Tổng bài đăng', 'Toàn hệ thống', House],
              ['availableRooms', 'Phòng đang hiển thị', 'Sinh viên có thể tìm thấy', Check],
              ['pendingReports', 'Báo cáo vi phạm', 'Cần xem xét và xử lý', Flag],
              ['flaggedReviews', 'Đánh giá chờ duyệt', 'Nội dung bị gắn cờ', Star],
            ].map(([key, label, description, Icon]) => (
              <article className={'stat-card stat-' + key} key={key}>
                <span>{label}</span>
                <strong>{data.stats[key] ?? 0}</strong>
                <small>{description}</small>
                <Icon size={20} />
              </article>
            ))}
          </div>

          <section className="dashboard-panel moderation-panel">
            <div className="tabs" role="group" aria-label="Nội dung quản trị">
              <button className={tab === 'users' ? 'active' : ''} onClick={() => setTab('users')}>
                Người dùng <span>{data.users?.length ?? data.stats.users}</span>
              </button>
              <button
                className={tab === 'requests' ? 'active' : ''}
                onClick={() => setTab('requests')}
              >
                Yêu cầu làm chủ trọ <span>{data.stats.pendingLandlordRequests ?? 0}</span>
              </button>
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

            {/* TAB: Quản lý người dùng */}
            {tab === 'users' && (
              <div className="admin-users-tab">
                <div className="admin-search-bar">
                  <Search size={16} />
                  <input
                    type="text"
                    placeholder="Tìm theo họ tên, email hoặc số điện thoại…"
                    value={userSearch}
                    onChange={(e) => setUserSearch(e.target.value)}
                  />
                  {userSearch && (
                    <button
                      type="button"
                      className="clear-search-btn"
                      onClick={() => setUserSearch('')}
                    >
                      <X size={14} />
                    </button>
                  )}
                </div>

                {filteredUsers.length ? (
                  <div className="admin-users-table-container">
                    <table className="admin-users-table">
                      <thead>
                        <tr>
                          <th>Người dùng</th>
                          <th>Số điện thoại</th>
                          <th>Vai trò</th>
                          <th>Xác thực Edu</th>
                          <th>Ngày tạo</th>
                          <th>Thao tác</th>
                        </tr>
                      </thead>
                      <tbody>
                        {filteredUsers.map((u) => (
                          <tr key={u.id}>
                            <td>
                              <div className="user-cell">
                                <span className="avatar small">
                                  {u.fullName?.charAt(0)?.toUpperCase() || 'U'}
                                </span>
                                <div>
                                  <strong>{u.fullName}</strong>
                                  <small className="muted">{u.email}</small>
                                </div>
                              </div>
                            </td>
                            <td>{u.phoneNumber || <span className="muted">—</span>}</td>
                            <td>
                              <span
                                className={
                                  'status-pill ' +
                                  (u.role === 'ADMIN'
                                    ? 'confirmed'
                                    : u.role === 'LANDLORD'
                                      ? 'landlord'
                                      : 'pending')
                                }
                              >
                                {u.role === 'ADMIN'
                                  ? 'Quản trị viên'
                                  : u.role === 'LANDLORD'
                                    ? 'Chủ trọ'
                                    : 'Người tìm trọ'}
                              </span>
                            </td>
                            <td>
                              {u.isEduVerified ? (
                                <span className="edu-tag verified">
                                  <BadgeCheck size={14} /> Đã xác thực
                                </span>
                              ) : (
                                <span className="edu-tag unverified">Chưa xác thực</span>
                              )}
                            </td>
                            <td>
                              <small>{new Date(u.createdAt).toLocaleDateString('vi-VN')}</small>
                            </td>
                            <td>
                              <button
                                type="button"
                                className="button outline small"
                                onClick={() => openEditUser(u)}
                              >
                                <Pencil size={13} /> Sửa
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <Empty
                    title="Không tìm thấy người dùng"
                    text={
                      userSearch
                        ? `Không có kết quả nào khớp với "${userSearch}".`
                        : 'Hệ thống chưa có người dùng nào.'
                    }
                  />
                )}
              </div>
            )}

            {/* TAB: Yêu cầu nâng cấp làm chủ trọ */}
            {tab === 'requests' &&
              (data.landlordRequests?.length ? (
                <div className="moderation-list landlord-requests-list">
                  {data.landlordRequests.map((r) => (
                    <article className="moderation-item landlord-request-card" key={r.id}>
                      <div className="request-card-content">
                        <div className="request-card-header">
                          <span
                            className={
                              'status-pill ' +
                              (r.status === 'PENDING'
                                ? 'pending'
                                : r.status === 'APPROVED'
                                  ? 'confirmed'
                                  : 'cancelled')
                            }
                          >
                            {r.status === 'PENDING'
                              ? 'Chờ duyệt'
                              : r.status === 'APPROVED'
                                ? 'Đã duyệt'
                                : 'Đã từ chối'}
                          </span>
                          <span className="request-timestamp">
                            {new Date(r.createdAt).toLocaleString('vi-VN')}
                          </span>
                        </div>

                        <h3 className="request-title">{r.title}</h3>
                        <p className="request-reason">{r.reason}</p>

                        <div className="request-user-info">
                          <span>
                            <strong>Người gửi:</strong> {r.userName}
                          </span>
                          <span>
                            <strong>Email:</strong> {r.userEmail}
                          </span>
                          {r.userPhoneNumber && (
                            <span>
                              <strong>SĐT:</strong> {r.userPhoneNumber}
                            </span>
                          )}
                        </div>

                        {/* Image attachments */}
                        {r.imageUrls?.length > 0 && (
                          <div className="request-attachments-section">
                            <span className="attachment-label">
                              Ảnh minh họa ({r.imageUrls.length}):
                            </span>
                            <div className="admin-thumb-grid">
                              {r.imageUrls.map((url, i) => (
                                <DownloadLink
                                  key={i}
                                  url={url}
                                  fileName={`anh-xac-minh-${i + 1}.png`}
                                  className="admin-thumb-link"
                                  title="Xem ảnh kích thước đầy đủ"
                                >
                                  <ProtectedImage src={url} alt={`Ảnh ${i + 1}`} />
                                </DownloadLink>
                              ))}
                            </div>
                          </div>
                        )}

                        {/* PDF attachment */}
                        {r.pdfUrl && (
                          <div className="request-attachments-section">
                            <span className="attachment-label">Tài liệu đính kèm (PDF):</span>
                            <DownloadLink
                              url={r.pdfUrl}
                              fileName={r.pdfFileName || 'tai-lieu-xac-minh.pdf'}
                              className="admin-pdf-card"
                            >
                              <FileText size={20} className="pdf-icon" />
                              <div className="admin-pdf-meta">
                                <strong>{r.pdfFileName || 'Giấy tờ xác minh.pdf'}</strong>
                                <small>
                                  Tải tài liệu PDF <ExternalLink size={12} />
                                </small>
                              </div>
                            </DownloadLink>
                          </div>
                        )}

                        {r.adminNote && (
                          <div className="admin-note-box">
                            <strong>Ghi chú từ chối:</strong> {r.adminNote}
                          </div>
                        )}
                      </div>

                      {r.status === 'PENDING' && (
                        <div className="moderation-actions">
                          <button
                            type="button"
                            className="button primary small"
                            disabled={busy === r.id}
                            onClick={() => approveLandlordRequest(r.id)}
                          >
                            <Check size={14} /> Duyệt làm chủ trọ
                          </button>
                          <button
                            type="button"
                            className="button outline small danger"
                            disabled={busy === r.id}
                            onClick={() => setRejectTarget(r)}
                          >
                            <X size={14} /> Từ chối
                          </button>
                        </div>
                      )}
                    </article>
                  ))}
                </div>
              ) : (
                <Empty
                  title="Không có yêu cầu nào"
                  text="Chưa có người tìm trọ nào gửi yêu cầu nâng cấp lên Chủ trọ."
                />
              ))}

            {/* TAB: Báo cáo vi phạm */}
            {tab === 'reports' &&
              (data.reports.length ? (
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
              ))}

            {/* TAB: Đánh giá chờ duyệt */}
            {tab === 'reviews' &&
              (data.reviews.length ? (
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
              ))}
          </section>
        </>
      )}

      {/* Modal chỉnh sửa thông tin người dùng */}
      {editingUser && (
        <Modal title="Chỉnh sửa thông tin người dùng" onClose={close}>
          <form className="form" onSubmit={handleSaveUser}>
            <label>
              Email (không thể thay đổi)
              <input value={editingUser.email} disabled readOnly />
            </label>
            <label>
              Họ và tên <span className="req-star">*</span>
              <input
                required
                maxLength={150}
                value={userForm.fullName}
                onChange={(e) => setUserForm((prev) => ({ ...prev, fullName: e.target.value }))}
              />
            </label>
            <label>
              Số điện thoại
              <input
                type="tel"
                maxLength={20}
                placeholder="VD: 0912345678"
                value={userForm.phoneNumber}
                onChange={(e) => setUserForm((prev) => ({ ...prev, phoneNumber: e.target.value }))}
              />
            </label>
            <label>
              Vai trò tài khoản
              <select
                value={userForm.role}
                onChange={(e) => setUserForm((prev) => ({ ...prev, role: e.target.value }))}
              >
                <option value="STUDENT">STUDENT - Người tìm trọ</option>
                <option value="LANDLORD">LANDLORD - Chủ phòng trọ</option>
                <option value="ADMIN">ADMIN - Quản trị viên</option>
              </select>
            </label>
            <label className="admin-checkbox-row">
              <input
                type="checkbox"
                checked={userForm.isEduVerified}
                onChange={(e) =>
                  setUserForm((prev) => ({ ...prev, isEduVerified: e.target.checked }))
                }
              />
              <span>Xác thực sinh viên uy tín (Gắn huy hiệu Edu Verified)</span>
            </label>
            <div className="modal-actions">
              <button type="button" className="button outline small" onClick={close}>
                Hủy bỏ
              </button>
              <button type="submit" className="button primary small" disabled={savingUser}>
                {savingUser ? 'Đang lưu…' : 'Lưu thay đổi'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Modal từ chối yêu cầu làm chủ trọ */}
      {rejectTarget && (
        <Modal title="Từ chối yêu cầu nâng cấp làm Chủ trọ" onClose={close}>
          <form className="form" onSubmit={handleRejectLandlordRequest}>
            <p className="muted">
              Người yêu cầu: <strong>{rejectTarget.userName}</strong> ({rejectTarget.userEmail})
              <br />
              Tiêu đề: <em>"{rejectTarget.title}"</em>
            </p>
            <label>
              Lý do từ chối (tùy chọn)
              <textarea
                rows={3}
                placeholder="Nhập lý do để người dùng biết cần bổ sung thông tin gì..."
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
              />
            </label>
            <div className="modal-actions">
              <button type="button" className="button outline small" onClick={close}>
                Hủy bỏ
              </button>
              <button
                type="submit"
                className="button danger small"
                disabled={busy === rejectTarget.id}
              >
                Xác nhận từ chối
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Modal xử lý báo cáo vi phạm */}
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

      {/* Modal thêm trường đại học */}
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
              <input name="address" required maxLength="255" />
            </label>
            <div className="form-row">
              <label>
                Vĩ độ (Latitude)
                <input name="latitude" type="number" step="any" required />
              </label>
              <label>
                Kinh độ (Longitude)
                <input name="longitude" type="number" step="any" required />
              </label>
            </div>
            <button className="button primary" disabled={busy === 'university'}>
              Thêm trường
            </button>
          </form>
        </Modal>
      )}
    </div>
  );
}
