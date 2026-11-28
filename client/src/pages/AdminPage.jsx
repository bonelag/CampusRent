import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import {
  ArrowLeft,
  Check,
  ExternalLink,
  Eye,
  EyeOff,
  FileText,
  House,
  LockKeyhole,
  LogOut,
  Mail,
  Menu,
  Moon,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  Send,
  Sun,
  Trash2,
  X,
} from 'lucide-react';
import { useApp } from '../context/AppContext';
import { api, mediaUrl, money } from '../lib/api';
import { ErrorState, Loading, Modal } from '../components/Common';
import { DownloadLink, ProtectedImage } from '../components/Attachments';
import {
  Dashboard,
  DataPanel,
  StatusTag,
  date,
  isExpired,
  roleLabel,
  statusLabels,
  viewLabels,
  views,
} from '../components/admin/AdminWidgets';

export default function AdminPage() {
  const { user, logout, notify } = useApp();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const requestedView = searchParams.get('view') || 'dashboard';
  const view = viewLabels[requestedView] ? requestedView : 'dashboard';
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  const [refreshing, setRefreshing] = useState(false);
  const [lastUpdated, setLastUpdated] = useState(null);
  const [busy, setBusy] = useState('');
  const loadVersion = useRef(0);
  const mutationInFlight = useRef(false);
  const [query, setQuery] = useState('');
  const [page, setPage] = useState(1);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [dark, setDark] = useState(() => localStorage.getItem('campusrent-admin-theme') === 'dark');
  const [modal, setModal] = useState(null);
  const [password, setPassword] = useState('');
  const [passwordConfirm, setPasswordConfirm] = useState('');
  const [rejectReason, setRejectReason] = useState('');
  const [universityForm, setUniversityForm] = useState({
    name: '',
    shortName: '',
    address: '',
    latitude: '',
    longitude: '',
  });
  const [smtpForm, setSmtpForm] = useState({
    enabled: false,
    host: 'smtp.gmail.com',
    port: 587,
    useSsl: true,
    username: '',
    password: '',
    hasPassword: false,
    fromEmail: '',
    fromName: 'CampusRent',
  });
  const [testEmail, setTestEmail] = useState('');

  const load = useCallback(async () => {
    const version = ++loadVersion.current;
    setError('');
    setRefreshing(true);
    try {
      const [
        stats,
        users,
        rooms,
        bookings,
        favorites,
        reviews,
        reports,
        requests,
        universities,
        smtp,
      ] = await Promise.all([
        api('/admin/stats'),
        api('/admin/users'),
        api('/admin/rooms'),
        api('/admin/bookings'),
        api('/admin/favorites'),
        api('/admin/reviews'),
        api('/admin/reports'),
        api('/admin/landlord-requests'),
        api('/admin/universities'),
        api('/admin/smtp').catch(() => null),
      ]);
      if (version !== loadVersion.current) return;
      setData({
        stats,
        users,
        rooms,
        bookings,
        favorites,
        reviews,
        reports,
        requests,
        universities,
        smtp,
      });
      if (smtp) {
        setSmtpForm((prev) => ({
          ...prev,
          enabled: Boolean(smtp.enabled),
          host: smtp.host || 'smtp.gmail.com',
          port: Number(smtp.port) || 587,
          useSsl: smtp.useSsl !== false,
          username: smtp.username || '',
          password: '',
          hasPassword: Boolean(smtp.hasPassword),
          fromEmail: smtp.fromEmail || '',
          fromName: smtp.fromName || 'CampusRent',
        }));
      }
      setLastUpdated(new Date());
    } catch (reason) {
      if (version === loadVersion.current) setError(reason.message);
    } finally {
      if (version === loadVersion.current) setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const lastUpdatedRef = useRef(null);

  useEffect(() => {
    lastUpdatedRef.current = lastUpdated;
  }, [lastUpdated]);

  useEffect(() => {
    const refreshOnFocus = () => {
      if (lastUpdatedRef.current && Date.now() - lastUpdatedRef.current.getTime() < 30000) {
        return;
      }
      load();
    };
    window.addEventListener('focus', refreshOnFocus);
    return () => window.removeEventListener('focus', refreshOnFocus);
  }, [load]);

  useEffect(() => {
    localStorage.setItem('campusrent-admin-theme', dark ? 'dark' : 'light');
  }, [dark]);

  useEffect(() => {
    setQuery('');
    setPage(1);
    setSidebarOpen(false);
  }, [view]);

  function openView(next) {
    setSearchParams(next === 'dashboard' ? {} : { view: next });
  }

  function closeModal() {
    setModal(null);
    setPassword('');
    setPasswordConfirm('');
    setRejectReason('');
  }

  async function mutate(key, path, options, message) {
    if (mutationInFlight.current) return;
    mutationInFlight.current = true;
    setBusy(key);
    try {
      await api(path, options);
      notify(message);
      closeModal();
      await load();
    } catch (reason) {
      notify(reason.message, 'error');
    } finally {
      mutationInFlight.current = false;
      setBusy('');
    }
  }

  async function resetPassword(event) {
    event.preventDefault();
    if (password !== passwordConfirm) return notify('Mật khẩu xác nhận chưa khớp.', 'error');
    await mutate(
      'password-' + modal.item.id,
      '/admin/users/' + modal.item.id + '/password',
      { method: 'PUT', body: { password } },
      'Đã đặt lại mật khẩu và vô hiệu hóa phiên cũ.'
    );
  }

  function editUniversity(item = null) {
    setUniversityForm({
      name: item?.name || '',
      shortName: item?.shortName || '',
      address: item?.address || '',
      latitude: item?.latitude ?? '',
      longitude: item?.longitude ?? '',
    });
    setModal({ type: 'university', item });
  }

  async function saveUniversity(event) {
    event.preventDefault();
    const item = modal.item;
    await mutate(
      'university-' + (item?.id || 'new'),
      item ? '/admin/universities/' + item.id : '/admin/universities',
      {
        method: item ? 'PUT' : 'POST',
        body: {
          ...universityForm,
          latitude: Number(universityForm.latitude),
          longitude: Number(universityForm.longitude),
        },
      },
      item ? 'Đã cập nhật trường đại học.' : 'Đã thêm trường đại học.'
    );
  }

  async function saveSmtp(event) {
    event.preventDefault();
    await mutate(
      'smtp-save',
      '/admin/smtp',
      {
        method: 'PUT',
        body: {
          enabled: smtpForm.enabled,
          host: smtpForm.host.trim(),
          port: Number(smtpForm.port),
          useSsl: smtpForm.useSsl,
          username: smtpForm.username.trim(),
          password: smtpForm.password.trim() || null,
          fromEmail: smtpForm.fromEmail.trim(),
          fromName: smtpForm.fromName.trim() || 'CampusRent',
        },
      },
      'Đã lưu cấu hình Gmail SMTP.'
    );
  }

  async function sendSmtpTest(event) {
    event.preventDefault();
    if (mutationInFlight.current) return;
    mutationInFlight.current = true;
    setBusy('smtp-test');
    try {
      const res = await api('/admin/smtp/test', {
        method: 'POST',
        body: { toEmail: testEmail.trim() || user?.email || '' },
      });
      notify(`Đã gửi email kiểm tra tới ${res?.toEmail || testEmail || user?.email}.`);
    } catch (reason) {
      notify(reason.message, 'error');
    } finally {
      mutationInFlight.current = false;
      setBusy('');
    }
  }

  const navBadges = data
    ? {
        bookings: data.stats.pendingBookings,
        reviews: data.stats.flaggedReviews,
        reports: data.stats.pendingReports,
        requests: data.stats.pendingLandlordRequests,
      }
    : {};

  const renderCurrentView = () => {
    if (view === 'dashboard') return <Dashboard stats={data.stats} onOpen={openView} />;

    if (view === 'users')
      return (
        <DataPanel
          title="Người dùng"
          description="Chỉ hiển thị thông tin định danh cần thiết. Admin không được sửa hồ sơ cá nhân, số điện thoại hoặc địa chỉ."
          rows={data.users}
          query={query}
          setQuery={setQuery}
          page={page}
          setPage={setPage}
          filterLabel="Lọc theo vai trò"
          filters={[
            {
              value: 'STUDENT',
              label: 'Người tìm trọ',
              matches: (item) => item.role === 'STUDENT',
            },
            { value: 'LANDLORD', label: 'Chủ trọ', matches: (item) => item.role === 'LANDLORD' },
            { value: 'ADMIN', label: 'Quản trị viên', matches: (item) => item.role === 'ADMIN' },
          ]}
          columns={[
            {
              label: 'Người dùng',
              sortValue: (item) => item.fullName,
              render: (item) => (
                <div className="adminator-user-cell">
                  <span>{item.fullName?.slice(0, 1).toUpperCase()}</span>
                  <div>
                    <strong>{item.fullName}</strong>
                    <small>{item.email}</small>
                  </div>
                </div>
              ),
            },
            {
              label: 'Vai trò',
              sortValue: (item) => item.role,
              render: (item) => <StatusTag value={item.role}>{roleLabel(item.role)}</StatusTag>,
            },
            {
              label: 'Hoạt động',
              sortValue: (item) => item.roomsCount + item.reviewsCount,
              render: (item) => (
                <span className="adminator-subtle">
                  {item.roomsCount} phòng · {item.reviewsCount} đánh giá
                </span>
              ),
            },
            {
              label: 'Xác thực',
              sortValue: (item) => Number(item.isEduVerified) * 2 + Number(item.isEmailVerified),
              render: (item) => (
                <div className="adminator-verify-badges">
                  <label className="adminator-verify-toggle">
                    <input
                      type="checkbox"
                      checked={Boolean(item.isEmailVerified)}
                      aria-label={'Xác minh email thường cho ' + item.fullName}
                      disabled={Boolean(busy)}
                      onChange={(e) =>
                        mutate(
                          'verify-email-' + item.id,
                          '/admin/users/' + item.id + '/verification',
                          {
                            method: 'PATCH',
                            body: {
                              isEmailVerified: e.target.checked,
                              isEduVerified: Boolean(item.isEduVerified),
                            },
                          },
                          e.target.checked
                            ? `Đã xác minh email cho ${item.fullName}.`
                            : `Đã bỏ xác minh email cho ${item.fullName}.`
                        )
                      }
                    />
                    <span>{item.isEmailVerified ? 'Email đã xác minh' : 'Email thường'}</span>
                  </label>
                  <label className="adminator-verify-toggle is-edu">
                    <input
                      type="checkbox"
                      checked={Boolean(item.isEduVerified)}
                      aria-label={'Xác minh email .edu cho ' + item.fullName}
                      disabled={Boolean(busy)}
                      onChange={(e) =>
                        mutate(
                          'verify-edu-' + item.id,
                          '/admin/users/' + item.id + '/verification',
                          {
                            method: 'PATCH',
                            body: {
                              isEmailVerified: e.target.checked
                                ? true
                                : Boolean(item.isEmailVerified),
                              isEduVerified: e.target.checked,
                            },
                          },
                          e.target.checked
                            ? `Đã cấp xác minh Sinh viên .edu cho ${item.fullName}.`
                            : `Đã bỏ xác minh Sinh viên .edu cho ${item.fullName}.`
                        )
                      }
                    />
                    <span>Sinh viên .edu</span>
                  </label>
                </div>
              ),
            },
            {
              label: 'Tham gia',
              sortValue: (item) => new Date(item.createdAt).getTime(),
              render: (item) => date(item.createdAt),
            },
            {
              label: 'Thao tác',
              render: (item) => (
                <button
                  className="adminator-icon-action"
                  title="Đặt lại mật khẩu"
                  aria-label={'Đặt lại mật khẩu cho ' + item.fullName}
                  disabled={Boolean(busy)}
                  onClick={() => setModal({ type: 'password', item })}
                >
                  <LockKeyhole size={15} />
                </button>
              ),
            },
          ]}
        />
      );

    if (view === 'rooms')
      return (
        <DataPanel
          title="Phòng trọ"
          description="Theo dõi toàn bộ tin, chỉnh sửa nội dung, gia hạn hoặc thay đổi trạng thái hiển thị."
          rows={data.rooms}
          query={query}
          setQuery={setQuery}
          page={page}
          setPage={setPage}
          action={
            <Link className="adminator-primary-button" to="/post">
              <Plus size={15} /> Đăng phòng mới
            </Link>
          }
          filterLabel="Lọc theo trạng thái phòng"
          filters={[
            {
              value: 'AVAILABLE',
              label: 'Đang hiển thị',
              matches: (item) => item.status === 'AVAILABLE' && !isExpired(item),
            },
            { value: 'RENTED', label: 'Đã thuê', matches: (item) => item.status === 'RENTED' },
            { value: 'HIDDEN', label: 'Đang ẩn', matches: (item) => item.status === 'HIDDEN' },
            {
              value: 'EXPIRED',
              label: 'Đã hết hạn',
              matches: (item) => item.status === 'AVAILABLE' && isExpired(item),
            },
          ]}
          columns={[
            {
              label: 'Tin đăng',
              sortValue: (item) => item.title,
              render: (item) => (
                <div className="adminator-room-cell">
                  <img src={mediaUrl(item.coverImage)} alt="" />
                  <div>
                    <strong>{item.title}</strong>
                    <small>{item.address}</small>
                  </div>
                </div>
              ),
            },
            {
              label: 'Chủ trọ',
              sortValue: (item) => item.landlordName,
              render: (item) => (
                <span>
                  {item.landlordName}
                  <small className="adminator-block">{item.landlordEmail}</small>
                </span>
              ),
            },
            {
              label: 'Giá / diện tích',
              sortValue: (item) => item.basePrice,
              render: (item) => (
                <span>
                  <strong>{money(item.basePrice)}</strong>
                  <small className="adminator-block">{item.area} m²</small>
                </span>
              ),
            },
            {
              label: 'Trạng thái',
              sortValue: (item) => (isExpired(item) ? 'EXPIRED' : item.status),
              render: (item) => (
                <span className="adminator-status-control">
                  <select
                    className="adminator-inline-select"
                    aria-label={'Trạng thái phòng ' + item.title}
                    value={item.status}
                    disabled={Boolean(busy)}
                    onChange={(event) => {
                      const nextStatus = event.target.value;
                      const renew = nextStatus === 'AVAILABLE' && isExpired(item);
                      mutate(
                        item.id,
                        '/admin/rooms/' + item.id + '/status',
                        { method: 'PATCH', body: { status: nextStatus, renew } },
                        renew
                          ? 'Đã mở lại và gia hạn phòng thêm 30 ngày.'
                          : 'Đã cập nhật trạng thái phòng.'
                      );
                    }}
                  >
                    <option value="AVAILABLE">Đang hiển thị</option>
                    <option value="RENTED">Đã thuê</option>
                    <option value="HIDDEN">Đang ẩn</option>
                  </select>
                  {item.status === 'AVAILABLE' && isExpired(item) && (
                    <small className="adminator-expired">Đã hết hạn</small>
                  )}
                </span>
              ),
            },
            {
              label: 'Hết hạn',
              sortValue: (item) => new Date(item.expiresAt).getTime(),
              render: (item) => (
                <span className={isExpired(item) ? 'adminator-expired-date' : 'adminator-subtle'}>
                  {date(item.expiresAt)}
                </span>
              ),
            },
            {
              label: 'Tương tác',
              sortValue: (item) => item.favoriteCount + item.bookingCount + item.reviewCount,
              render: (item) => (
                <span className="adminator-subtle">
                  {item.favoriteCount} lưu · {item.bookingCount} lịch · {item.reviewCount} đánh giá
                </span>
              ),
            },
            {
              label: 'Thao tác',
              render: (item) => (
                <div className="adminator-row-actions">
                  <Link
                    className="adminator-icon-action"
                    title="Xem tin"
                    aria-label={'Xem tin ' + item.title}
                    to={'/rooms/' + item.id}
                  >
                    <Eye size={15} />
                  </Link>
                  <Link
                    className="adminator-icon-action"
                    title="Sửa phòng"
                    aria-label={'Sửa phòng ' + item.title}
                    to={'/rooms/' + item.id + '/edit'}
                  >
                    <Pencil size={15} />
                  </Link>
                  <button
                    className="adminator-icon-action"
                    title="Gia hạn 30 ngày"
                    aria-label={'Gia hạn 30 ngày cho ' + item.title}
                    disabled={Boolean(busy)}
                    onClick={() =>
                      mutate(
                        item.id,
                        '/admin/rooms/' + item.id + '/status',
                        { method: 'PATCH', body: { status: item.status, renew: true } },
                        'Đã gia hạn phòng thêm 30 ngày.'
                      )
                    }
                  >
                    <RefreshCw size={15} />
                  </button>
                  {item.status !== 'HIDDEN' && (
                    <button
                      className="adminator-icon-action danger"
                      title="Ẩn tin"
                      aria-label={'Ẩn tin ' + item.title}
                      disabled={Boolean(busy)}
                      onClick={() =>
                        setModal({
                          type: 'confirm',
                          title: 'Ẩn tin đăng',
                          text: 'Tin sẽ ngừng xuất hiện với người dùng nhưng lịch sử vẫn được giữ lại.',
                          action: () =>
                            mutate(
                              item.id,
                              '/admin/rooms/' + item.id,
                              { method: 'DELETE' },
                              'Đã ẩn tin đăng.'
                            ),
                        })
                      }
                    >
                      <EyeOff size={15} />
                    </button>
                  )}
                </div>
              ),
            },
          ]}
        />
      );

    if (view === 'bookings')
      return (
        <DataPanel
          title="Lịch hẹn"
          description="Quản lý trạng thái và loại bỏ lịch hẹn không còn hợp lệ."
          rows={data.bookings}
          query={query}
          setQuery={setQuery}
          page={page}
          setPage={setPage}
          filterLabel="Lọc theo trạng thái lịch hẹn"
          filters={[
            { value: 'PENDING', label: 'Đang chờ', matches: (item) => item.status === 'PENDING' },
            {
              value: 'CONFIRMED',
              label: 'Đã xác nhận',
              matches: (item) => item.status === 'CONFIRMED',
            },
            {
              value: 'COMPLETED',
              label: 'Đã hoàn thành',
              matches: (item) => item.status === 'COMPLETED',
            },
            { value: 'CANCELLED', label: 'Đã hủy', matches: (item) => item.status === 'CANCELLED' },
          ]}
          columns={[
            {
              label: 'Phòng',
              sortValue: (item) => item.roomTitle,
              render: (item) => (
                <span>
                  <strong>{item.roomTitle}</strong>
                  <small className="adminator-block">{item.landlordName}</small>
                </span>
              ),
            },
            {
              label: 'Người đặt',
              sortValue: (item) => item.studentName,
              render: (item) => (
                <span>
                  {item.studentName}
                  <small className="adminator-block">{item.studentEmail}</small>
                </span>
              ),
            },
            {
              label: 'Thời gian xem',
              sortValue: (item) => new Date(item.visitAt).getTime(),
              render: (item) => date(item.visitAt, true),
            },
            {
              label: 'Trạng thái',
              sortValue: (item) => item.status,
              render: (item) => (
                <select
                  className="adminator-inline-select"
                  aria-label={'Trạng thái lịch hẹn tại ' + item.roomTitle}
                  value={item.status}
                  disabled={Boolean(busy)}
                  onChange={(event) =>
                    mutate(
                      item.id,
                      '/admin/bookings/' + item.id,
                      { method: 'PATCH', body: { status: event.target.value } },
                      'Đã cập nhật lịch hẹn.'
                    )
                  }
                >
                  <option value="PENDING">Chờ xác nhận</option>
                  <option value="CONFIRMED">Đã xác nhận</option>
                  <option value="COMPLETED">Đã hoàn thành</option>
                  <option value="CANCELLED">Đã hủy</option>
                </select>
              ),
            },
            { label: 'Ghi chú', render: (item) => item.note || '—' },
            {
              label: 'Thao tác',
              render: (item) => (
                <button
                  className="adminator-icon-action danger"
                  title="Xóa lịch hẹn"
                  aria-label={'Xóa lịch hẹn tại ' + item.roomTitle}
                  disabled={Boolean(busy)}
                  onClick={() =>
                    setModal({
                      type: 'confirm',
                      title: 'Xóa lịch hẹn',
                      text: 'Lịch hẹn sẽ bị xóa vĩnh viễn khỏi hệ thống.',
                      action: () =>
                        mutate(
                          item.id,
                          '/admin/bookings/' + item.id,
                          { method: 'DELETE' },
                          'Đã xóa lịch hẹn.'
                        ),
                    })
                  }
                >
                  <Trash2 size={15} />
                </button>
              ),
            },
          ]}
        />
      );

    if (view === 'favorites')
      return (
        <DataPanel
          title="Lượt yêu thích"
          description="Theo dõi và gỡ các liên kết lưu phòng giữa người dùng và tin đăng."
          rows={data.favorites}
          query={query}
          setQuery={setQuery}
          page={page}
          setPage={setPage}
          filterLabel="Lọc theo trạng thái phòng"
          filters={[
            {
              value: 'AVAILABLE',
              label: 'Đang hiển thị',
              matches: (item) => item.roomStatus === 'AVAILABLE',
            },
            { value: 'RENTED', label: 'Đã thuê', matches: (item) => item.roomStatus === 'RENTED' },
            { value: 'HIDDEN', label: 'Đang ẩn', matches: (item) => item.roomStatus === 'HIDDEN' },
          ]}
          columns={[
            {
              label: 'Người dùng',
              sortValue: (item) => item.userName,
              render: (item) => (
                <span>
                  <strong>{item.userName}</strong>
                  <small className="adminator-block">{item.userEmail}</small>
                </span>
              ),
            },
            {
              label: 'Phòng đã lưu',
              sortValue: (item) => item.roomTitle,
              render: (item) => (
                <div className="adminator-room-cell compact">
                  <img src={mediaUrl(item.coverImage)} alt="" />
                  <strong>{item.roomTitle}</strong>
                </div>
              ),
            },
            {
              label: 'Trạng thái phòng',
              sortValue: (item) => item.roomStatus,
              render: (item) => <StatusTag value={item.roomStatus} />,
            },
            {
              label: 'Thao tác',
              render: (item) => (
                <button
                  className="adminator-icon-action danger"
                  title="Gỡ lượt lưu"
                  aria-label={'Gỡ ' + item.roomTitle + ' khỏi danh sách của ' + item.userName}
                  disabled={Boolean(busy)}
                  onClick={() =>
                    setModal({
                      type: 'confirm',
                      title: 'Gỡ lượt yêu thích',
                      text: 'Phòng sẽ bị xóa khỏi danh sách đã lưu của người dùng này.',
                      action: () =>
                        mutate(
                          item.userId + item.roomId,
                          '/admin/favorites/' + item.userId + '/' + item.roomId,
                          { method: 'DELETE' },
                          'Đã gỡ lượt yêu thích.'
                        ),
                    })
                  }
                >
                  <Trash2 size={15} />
                </button>
              ),
            },
          ]}
        />
      );

    if (view === 'reviews')
      return (
        <DataPanel
          title="Đánh giá"
          description="Xem toàn bộ đánh giá, ẩn nội dung vi phạm hoặc khôi phục nội dung hợp lệ."
          rows={data.reviews}
          query={query}
          setQuery={setQuery}
          page={page}
          setPage={setPage}
          filterLabel="Lọc theo trạng thái kiểm duyệt"
          filters={[
            { value: 'VISIBLE', label: 'Đang hiển thị', matches: (item) => !item.isFlagged },
            { value: 'FLAGGED', label: 'Đang ẩn', matches: (item) => item.isFlagged },
            { value: 'REPORTED', label: 'Có báo cáo', matches: (item) => item.reportCount > 0 },
          ]}
          columns={[
            {
              label: 'Nội dung',
              sortValue: (item) => item.roomTitle,
              render: (item) => (
                <span className="adminator-review-cell">
                  <strong>{item.roomTitle}</strong>
                  <small>“{item.comment}”</small>
                </span>
              ),
            },
            {
              label: 'Tác giả',
              sortValue: (item) => item.userName,
              render: (item) => (
                <span>
                  {item.userName}
                  <small className="adminator-block">{item.userEmail}</small>
                </span>
              ),
            },
            {
              label: 'Điểm',
              sortValue: (item) => item.rating,
              render: (item) => <span className="adminator-stars">{'★'.repeat(item.rating)}</span>,
            },
            {
              label: 'Kiểm duyệt',
              sortValue: (item) => Number(item.isFlagged),
              render: (item) => (
                <StatusTag value={item.isFlagged ? 'PENDING' : 'APPROVED'}>
                  {item.isFlagged ? 'Đang ẩn' : 'Đang hiển thị'}
                </StatusTag>
              ),
            },
            {
              label: 'Ngày tạo',
              sortValue: (item) => new Date(item.createdAt).getTime(),
              render: (item) => date(item.createdAt, true),
            },
            {
              label: 'Thao tác',
              render: (item) => (
                <div className="adminator-row-actions">
                  <button
                    className="adminator-icon-action"
                    title={item.isFlagged ? 'Cho phép hiển thị' : 'Ẩn đánh giá'}
                    aria-label={
                      (item.isFlagged ? 'Cho phép hiển thị' : 'Ẩn đánh giá') +
                      ' của ' +
                      item.userName
                    }
                    disabled={Boolean(busy)}
                    onClick={() =>
                      mutate(
                        item.id,
                        '/admin/reviews/' + item.id,
                        {
                          method: 'PATCH',
                          body: {
                            isFlagged: !item.isFlagged,
                            flagReason: item.isFlagged ? null : 'Quản trị viên ẩn nội dung',
                          },
                        },
                        item.isFlagged ? 'Đã khôi phục đánh giá.' : 'Đã ẩn đánh giá.'
                      )
                    }
                  >
                    {item.isFlagged ? <Check size={15} /> : <ShieldCheck size={15} />}
                  </button>
                  <button
                    className="adminator-icon-action danger"
                    title="Xóa đánh giá"
                    aria-label={'Xóa đánh giá của ' + item.userName}
                    disabled={Boolean(busy)}
                    onClick={() =>
                      setModal({
                        type: 'confirm',
                        title: 'Xóa đánh giá',
                        text: 'Đánh giá và các báo cáo liên quan sẽ bị xóa vĩnh viễn.',
                        action: () =>
                          mutate(
                            item.id,
                            '/admin/reviews/' + item.id,
                            { method: 'DELETE' },
                            'Đã xóa đánh giá.'
                          ),
                      })
                    }
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
              ),
            },
          ]}
        />
      );

    if (view === 'reports')
      return (
        <DataPanel
          title="Báo cáo vi phạm"
          description="Điều tra báo cáo, ẩn nội dung vi phạm, bỏ qua hoặc xóa bản ghi."
          rows={data.reports}
          query={query}
          setQuery={setQuery}
          page={page}
          setPage={setPage}
          filterLabel="Lọc theo trạng thái báo cáo"
          filters={[
            { value: 'PENDING', label: 'Đang chờ', matches: (item) => item.status === 'PENDING' },
            { value: 'RESOLVED', label: 'Đã xử lý', matches: (item) => item.status === 'RESOLVED' },
            {
              value: 'DISMISSED',
              label: 'Đã bỏ qua',
              matches: (item) => item.status === 'DISMISSED',
            },
          ]}
          columns={[
            {
              label: 'Báo cáo',
              sortValue: (item) => item.reason,
              render: (item) => (
                <span className="adminator-review-cell">
                  <strong>{item.reason}</strong>
                  <small>{item.roomTitle || item.reviewComment}</small>
                </span>
              ),
            },
            {
              label: 'Người gửi',
              sortValue: (item) => item.reporterName,
              render: (item) => (
                <span>
                  {item.reporterName}
                  <small className="adminator-block">{item.reporterEmail}</small>
                </span>
              ),
            },
            {
              label: 'Trạng thái',
              sortValue: (item) => item.status,
              render: (item) => <StatusTag value={item.status} />,
            },
            {
              label: 'Ngày gửi',
              sortValue: (item) => new Date(item.createdAt).getTime(),
              render: (item) => date(item.createdAt, true),
            },
            {
              label: 'Thao tác',
              render: (item) => (
                <div className="adminator-row-actions">
                  {item.status === 'PENDING' && (
                    <>
                      <button
                        className="adminator-icon-action"
                        title="Xử lý báo cáo"
                        aria-label={'Xử lý báo cáo ' + item.reason}
                        disabled={Boolean(busy)}
                        onClick={() => setModal({ type: 'report', item })}
                      >
                        <ShieldCheck size={15} />
                      </button>
                      <button
                        className="adminator-icon-action"
                        title="Bỏ qua"
                        aria-label={'Bỏ qua báo cáo ' + item.reason}
                        disabled={Boolean(busy)}
                        onClick={() =>
                          mutate(
                            item.id,
                            '/admin/reports/' + item.id,
                            { method: 'PATCH', body: { status: 'DISMISSED' } },
                            'Đã bỏ qua báo cáo.'
                          )
                        }
                      >
                        <X size={15} />
                      </button>
                    </>
                  )}
                  <button
                    className="adminator-icon-action danger"
                    title="Xóa báo cáo"
                    aria-label={'Xóa báo cáo ' + item.reason}
                    disabled={Boolean(busy)}
                    onClick={() =>
                      setModal({
                        type: 'confirm',
                        title: 'Xóa báo cáo',
                        text: 'Bản ghi báo cáo sẽ bị xóa vĩnh viễn.',
                        action: () =>
                          mutate(
                            item.id,
                            '/admin/reports/' + item.id,
                            { method: 'DELETE' },
                            'Đã xóa báo cáo.'
                          ),
                      })
                    }
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
              ),
            },
          ]}
        />
      );

    if (view === 'requests')
      return (
        <DataPanel
          title="Yêu cầu làm chủ trọ"
          description="Duyệt hồ sơ nâng cấp; thay đổi role chỉ diễn ra thông qua quy trình này."
          rows={data.requests}
          query={query}
          setQuery={setQuery}
          page={page}
          setPage={setPage}
          filterLabel="Lọc theo trạng thái hồ sơ"
          filters={[
            { value: 'PENDING', label: 'Đang chờ', matches: (item) => item.status === 'PENDING' },
            { value: 'APPROVED', label: 'Đã duyệt', matches: (item) => item.status === 'APPROVED' },
            {
              value: 'REJECTED',
              label: 'Đã từ chối',
              matches: (item) => item.status === 'REJECTED',
            },
          ]}
          columns={[
            {
              label: 'Người gửi',
              sortValue: (item) => item.userName,
              render: (item) => (
                <span>
                  <strong>{item.userName}</strong>
                  <small className="adminator-block">{item.userEmail}</small>
                </span>
              ),
            },
            {
              label: 'Hồ sơ',
              sortValue: (item) => item.title,
              render: (item) => (
                <span className="adminator-review-cell">
                  <strong>{item.title}</strong>
                  <small>{item.reason}</small>
                </span>
              ),
            },
            {
              label: 'Tệp',
              render: (item) => (
                <div className="adminator-attachments">
                  {item.imageUrls?.slice(0, 2).map((url, index) => (
                    <DownloadLink
                      key={url}
                      url={url}
                      fileName={'xac-minh-' + (index + 1) + '.png'}
                      aria-label={'Mở ảnh xác minh ' + (index + 1) + ' của ' + item.userName}
                    >
                      <ProtectedImage src={url} alt={'Ảnh xác minh ' + (index + 1)} />
                    </DownloadLink>
                  ))}
                  {item.pdfUrl && (
                    <DownloadLink
                      url={item.pdfUrl}
                      fileName={item.pdfFileName || 'xac-minh.pdf'}
                      aria-label={'Tải hồ sơ PDF của ' + item.userName}
                    >
                      <FileText size={18} />
                    </DownloadLink>
                  )}
                  {!item.imageUrls?.length && !item.pdfUrl && (
                    <span className="adminator-subtle">Không có</span>
                  )}
                </div>
              ),
            },
            {
              label: 'Trạng thái',
              sortValue: (item) => item.status,
              render: (item) => <StatusTag value={item.status} />,
            },
            {
              label: 'Ngày gửi',
              sortValue: (item) => new Date(item.createdAt).getTime(),
              render: (item) => date(item.createdAt, true),
            },
            {
              label: 'Thao tác',
              render: (item) => (
                <div className="adminator-row-actions">
                  {item.status === 'PENDING' && (
                    <>
                      <button
                        className="adminator-icon-action"
                        title="Duyệt hồ sơ"
                        aria-label={'Duyệt hồ sơ của ' + item.userName}
                        disabled={Boolean(busy)}
                        onClick={() =>
                          mutate(
                            item.id,
                            '/admin/landlord-requests/' + item.id + '/approve',
                            { method: 'PATCH' },
                            'Đã duyệt tài khoản chủ trọ.'
                          )
                        }
                      >
                        <Check size={15} />
                      </button>
                      <button
                        className="adminator-icon-action"
                        title="Từ chối"
                        aria-label={'Từ chối hồ sơ của ' + item.userName}
                        disabled={Boolean(busy)}
                        onClick={() => setModal({ type: 'reject', item })}
                      >
                        <X size={15} />
                      </button>
                    </>
                  )}
                  <button
                    className="adminator-icon-action danger"
                    title="Xóa yêu cầu"
                    aria-label={'Xóa hồ sơ của ' + item.userName}
                    disabled={Boolean(busy)}
                    onClick={() =>
                      setModal({
                        type: 'confirm',
                        title: 'Xóa yêu cầu',
                        text: 'Hồ sơ nâng cấp sẽ bị xóa vĩnh viễn.',
                        action: () =>
                          mutate(
                            item.id,
                            '/admin/landlord-requests/' + item.id,
                            { method: 'DELETE' },
                            'Đã xóa yêu cầu.'
                          ),
                      })
                    }
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
              ),
            },
          ]}
        />
      );

    if (view === 'settings')
      return (
        <section className="adminator-settings-grid">
          <article className="adminator-card">
            <header className="adminator-card-head">
              <div>
                <span className="adminator-eyebrow">Cấu hình hệ thống</span>
                <h2>Cài đặt Gmail SMTP (Gửi mã xác minh)</h2>
              </div>
              <button
                type="button"
                className="button outline small"
                onClick={() =>
                  setSmtpForm((prev) => ({
                    ...prev,
                    host: 'smtp.gmail.com',
                    port: 587,
                    useSsl: true,
                    fromName: prev.fromName || 'CampusRent',
                  }))
                }
              >
                Điền nhanh Gmail SMTP
              </button>
            </header>
            <form className="form adminator-smtp-form" onSubmit={saveSmtp}>
              <label className="adminator-checkbox-row">
                <input
                  type="checkbox"
                  checked={smtpForm.enabled}
                  onChange={(e) => setSmtpForm((prev) => ({ ...prev, enabled: e.target.checked }))}
                />
                <span>Kích hoạt gửi email xác minh qua máy chủ SMTP</span>
              </label>

              <div className="form-row">
                <label>
                  Máy chủ SMTP (Host)
                  <input
                    required
                    value={smtpForm.host}
                    placeholder="smtp.gmail.com"
                    onChange={(e) => setSmtpForm((prev) => ({ ...prev, host: e.target.value }))}
                  />
                </label>
                <label>
                  Cổng (Port)
                  <input
                    type="number"
                    required
                    min={1}
                    max={65535}
                    value={smtpForm.port}
                    onChange={(e) => setSmtpForm((prev) => ({ ...prev, port: e.target.value }))}
                  />
                </label>
              </div>

              <label className="adminator-checkbox-row">
                <input
                  type="checkbox"
                  checked={smtpForm.useSsl}
                  onChange={(e) => setSmtpForm((prev) => ({ ...prev, useSsl: e.target.checked }))}
                />
                <span>Sử dụng kết nối bảo mật SSL / STARTTLS (Khuyến nghị với Gmail cổng 587)</span>
              </label>

              <div className="form-row">
                <label>
                  Tài khoản Gmail / SMTP Username
                  <input
                    value={smtpForm.username}
                    placeholder="your-app@gmail.com"
                    onChange={(e) => setSmtpForm((prev) => ({ ...prev, username: e.target.value }))}
                  />
                </label>
                <label>
                  Mật khẩu ứng dụng Gmail (App Password)
                  <input
                    type="password"
                    autoComplete="new-password"
                    value={smtpForm.password}
                    placeholder={
                      smtpForm.hasPassword
                        ? 'Đã lưu mật khẩu (để trống nếu giữ nguyên)'
                        : 'Nhập App Password 16 ký tự'
                    }
                    onChange={(e) => setSmtpForm((prev) => ({ ...prev, password: e.target.value }))}
                  />
                </label>
              </div>

              <div className="form-row">
                <label>
                  Email hiển thị (From Email)
                  <input
                    type="email"
                    value={smtpForm.fromEmail}
                    placeholder="noreply@campusrent.vn"
                    onChange={(e) =>
                      setSmtpForm((prev) => ({ ...prev, fromEmail: e.target.value }))
                    }
                  />
                </label>
                <label>
                  Tên người gửi (From Name)
                  <input
                    value={smtpForm.fromName}
                    placeholder="CampusRent"
                    onChange={(e) => setSmtpForm((prev) => ({ ...prev, fromName: e.target.value }))}
                  />
                </label>
              </div>

              <div className="modal-actions">
                <button
                  type="submit"
                  className="adminator-primary-button"
                  disabled={busy === 'smtp-save'}
                >
                  <Check size={15} /> {busy === 'smtp-save' ? 'Đang lưu…' : 'Lưu cấu hình SMTP'}
                </button>
              </div>
            </form>
          </article>

          <article className="adminator-card">
            <header className="adminator-card-head">
              <div>
                <span className="adminator-eyebrow">Kiểm tra kết nối</span>
                <h2>Gửi email thử nghiệm</h2>
              </div>
              <Send size={18} />
            </header>
            <p className="muted">
              Sau khi lưu cấu hình Gmail SMTP và App Password, gửi một email kiểm tra để xác nhận
              máy chủ gửi mã OTP hoạt động thông suốt trước khi mở cho sinh viên đăng ký.
            </p>
            <form className="form" onSubmit={sendSmtpTest}>
              <label>
                Email nhận thư thử
                <input
                  type="email"
                  value={testEmail}
                  placeholder={user?.email || 'admin@campusrent.vn'}
                  onChange={(e) => setTestEmail(e.target.value)}
                />
              </label>
              <button
                type="submit"
                className="button outline small"
                disabled={busy === 'smtp-test'}
              >
                <Send size={14} /> {busy === 'smtp-test' ? 'Đang gửi…' : 'Gửi email kiểm tra'}
              </button>
            </form>
            <div className="adminator-smtp-guide">
              <strong>Hướng dẫn lấy App Password Gmail:</strong>
              <ol>
                <li>Mở Tài khoản Google → Bảo mật → Bật Xác minh 2 bước.</li>
                <li>Tìm mục Mật khẩu ứng dụng (App passwords), tạo mật khẩu mới cho CampusRent.</li>
                <li>Dán chuỗi 16 ký tự vào ô Mật khẩu ứng dụng và nhấn Lưu cấu hình SMTP.</li>
              </ol>
            </div>
          </article>
        </section>
      );

    return (
      <DataPanel
        title="Trường đại học"
        description="Quản lý dữ liệu trường dùng cho tìm kiếm theo vị trí và bán kính."
        rows={data.universities}
        query={query}
        setQuery={setQuery}
        page={page}
        setPage={setPage}
        action={
          <button
            type="button"
            className="adminator-primary-button"
            disabled={Boolean(busy)}
            onClick={() => editUniversity()}
          >
            <Plus size={15} /> Thêm trường
          </button>
        }
        columns={[
          {
            label: 'Trường',
            sortValue: (item) => item.name,
            render: (item) => (
              <span>
                <strong>{item.name}</strong>
                <small className="adminator-block">{item.shortName}</small>
              </span>
            ),
          },
          { label: 'Địa chỉ', sortValue: (item) => item.address, render: (item) => item.address },
          {
            label: 'Tọa độ',
            render: (item) => (
              <span className="adminator-mono">
                {item.latitude.toFixed(4)}, {item.longitude.toFixed(4)}
              </span>
            ),
          },
          {
            label: 'Số phòng',
            sortValue: (item) => item.roomsCount,
            render: (item) => item.roomsCount,
          },
          {
            label: 'Ngày tạo',
            sortValue: (item) => new Date(item.createdAt).getTime(),
            render: (item) => date(item.createdAt),
          },
          {
            label: 'Thao tác',
            render: (item) => (
              <div className="adminator-row-actions">
                <button
                  className="adminator-icon-action"
                  title="Sửa trường"
                  aria-label={'Sửa ' + item.name}
                  disabled={Boolean(busy)}
                  onClick={() => editUniversity(item)}
                >
                  <Pencil size={15} />
                </button>
                <button
                  className="adminator-icon-action danger"
                  title="Xóa trường"
                  aria-label={'Xóa ' + item.name}
                  disabled={Boolean(busy)}
                  onClick={() =>
                    setModal({
                      type: 'confirm',
                      title: 'Xóa trường đại học',
                      text: 'Các phòng đang liên kết sẽ được giữ lại nhưng không còn gắn với trường này.',
                      action: () =>
                        mutate(
                          item.id,
                          '/admin/universities/' + item.id,
                          { method: 'DELETE' },
                          'Đã xóa trường đại học.'
                        ),
                    })
                  }
                >
                  <Trash2 size={15} />
                </button>
              </div>
            ),
          },
        ]}
      />
    );
  };

  const initials = user?.fullName
    ?.trim()
    .split(/\s+/)
    .map((part) => part[0])
    .slice(-2)
    .join('')
    .toUpperCase();
  const groups = [...new Set(views.map((item) => item.group))];
  const updatedLabel = lastUpdated
    ? lastUpdated.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
    : null;

  return (
    <div
      className={'adminator-shell ' + (dark ? 'is-dark ' : '') + (sidebarOpen ? 'has-drawer ' : '')}
    >
      <aside className="adminator-sidebar">
        <Link to="/" className="adminator-brand">
          <span>
            <House size={18} />
          </span>
          <div>
            <strong>CampusRent</strong>
            <small>ADMIN CONSOLE</small>
          </div>
        </Link>
        <nav aria-label="Điều hướng quản trị">
          {groups.map((group) => (
            <div className="adminator-nav-group" key={group}>
              <span>{group}</span>
              {views
                .filter((item) => item.group === group)
                .map(({ key, label, icon: Icon }) => (
                  <button
                    type="button"
                    key={key}
                    className={view === key ? 'active' : ''}
                    onClick={() => openView(key)}
                  >
                    <Icon size={17} />
                    <span>{label}</span>
                    {!!navBadges[key] && <em>{navBadges[key]}</em>}
                  </button>
                ))}
            </div>
          ))}
        </nav>
        <div className="adminator-sidebar-user">
          <span>{initials || 'AD'}</span>
          <div>
            <strong>{user?.fullName}</strong>
            <small>Quản trị viên</small>
          </div>
        </div>
      </aside>

      <button
        type="button"
        className="adminator-backdrop"
        aria-label="Đóng menu"
        onClick={() => setSidebarOpen(false)}
      />

      <div className="adminator-main" aria-busy={refreshing || Boolean(busy)}>
        <header className="adminator-topbar">
          <div className="adminator-topbar-left">
            <button
              type="button"
              className="adminator-mobile-menu"
              aria-label="Mở menu quản trị"
              onClick={() => setSidebarOpen(true)}
            >
              <Menu size={20} />
            </button>
            <span>Workspace</span>
            <i>/</i>
            <strong>{viewLabels[view]}</strong>
          </div>
          <div className="adminator-topbar-actions">
            {view !== 'dashboard' && (
              <label className="adminator-command-search">
                <Search size={15} />
                <input
                  aria-label={'Tìm ' + viewLabels[view].toLocaleLowerCase('vi')}
                  value={query}
                  onChange={(event) => {
                    setQuery(event.target.value);
                    setPage(1);
                  }}
                  placeholder={'Tìm ' + viewLabels[view].toLocaleLowerCase('vi') + '…'}
                />
              </label>
            )}
            <button
              type="button"
              title="Tải lại dữ liệu"
              aria-label="Tải lại dữ liệu"
              disabled={refreshing}
              onClick={load}
            >
              <RefreshCw className={refreshing ? 'spin' : ''} size={17} />
            </button>
            <button
              type="button"
              title={dark ? 'Dùng giao diện sáng' : 'Dùng giao diện tối'}
              aria-label={dark ? 'Dùng giao diện sáng' : 'Dùng giao diện tối'}
              onClick={() => setDark(!dark)}
            >
              {dark ? <Sun size={17} /> : <Moon size={17} />}
            </button>
            <Link aria-label="Về website" title="Về website" to="/">
              <ArrowLeft size={17} />
            </Link>
            <button
              type="button"
              title="Đăng xuất"
              aria-label="Đăng xuất"
              onClick={() => {
                logout();
                navigate('/');
              }}
            >
              <LogOut size={17} />
            </button>
            <span className="adminator-topbar-avatar">{initials || 'AD'}</span>
          </div>
        </header>

        <main className="adminator-content">
          <section className="adminator-hero">
            <div>
              <span className="adminator-eyebrow">CampusRent operations</span>
              <h1>{viewLabels[view]}</h1>
              <p>
                {view === 'dashboard'
                  ? 'Theo dõi sức khỏe hệ thống và xử lý các hàng đợi quan trọng.'
                  : 'Quản lý dữ liệu trực tiếp với phân quyền quản trị viên.'}
              </p>
            </div>
            <span className="adminator-live-badge" role="status">
              <i />{' '}
              {refreshing
                ? 'Đang đồng bộ…'
                : updatedLabel
                  ? `Cập nhật ${updatedLabel}`
                  : 'Chờ đồng bộ'}
            </span>
          </section>

          {error && data && (
            <div className="adminator-inline-error" role="alert">
              <span>Chưa thể đồng bộ dữ liệu mới: {error}</span>
              <button type="button" onClick={load}>
                Thử lại
              </button>
            </div>
          )}

          {error && !data ? (
            <ErrorState message={error} retry={load} />
          ) : !data ? (
            <Loading />
          ) : (
            renderCurrentView()
          )}
        </main>

        <footer className="adminator-footer">
          <span>CampusRent Admin Console</span>
          <span>
            UI adapted from{' '}
            <a
              href="https://github.com/puikinsh/Adminator-admin-dashboard"
              target="_blank"
              rel="noreferrer"
            >
              Adminator <ExternalLink size={11} />
            </a>
          </span>
        </footer>
      </div>

      {modal?.type === 'password' && (
        <Modal title="Đặt lại mật khẩu" onClose={closeModal}>
          <form className="form" onSubmit={resetPassword}>
            <p className="muted">
              Tài khoản: <strong>{modal.item.fullName}</strong> ({modal.item.email}). Thao tác này
              sẽ vô hiệu hóa mọi token đã cấp trước đó.
            </p>
            <label>
              Mật khẩu mới
              <input
                type="password"
                minLength={8}
                maxLength={72}
                required
                autoComplete="new-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
              />
            </label>
            <label>
              Xác nhận mật khẩu
              <input
                type="password"
                minLength={8}
                maxLength={72}
                required
                autoComplete="new-password"
                value={passwordConfirm}
                onChange={(event) => setPasswordConfirm(event.target.value)}
              />
            </label>
            <div className="modal-actions">
              <button type="button" className="button outline small" onClick={closeModal}>
                Hủy
              </button>
              <button className="button primary small" disabled={busy.startsWith('password-')}>
                <LockKeyhole size={14} /> Đặt lại mật khẩu
              </button>
            </div>
          </form>
        </Modal>
      )}

      {modal?.type === 'university' && (
        <Modal
          title={modal.item ? 'Chỉnh sửa trường đại học' : 'Thêm trường đại học'}
          onClose={closeModal}
        >
          <form className="form" onSubmit={saveUniversity}>
            <label>
              Tên trường
              <input
                required
                maxLength={200}
                value={universityForm.name}
                onChange={(event) =>
                  setUniversityForm((value) => ({ ...value, name: event.target.value }))
                }
              />
            </label>
            <label>
              Mã viết tắt
              <input
                required
                maxLength={50}
                value={universityForm.shortName}
                onChange={(event) =>
                  setUniversityForm((value) => ({ ...value, shortName: event.target.value }))
                }
              />
            </label>
            <label>
              Địa chỉ
              <input
                required
                maxLength={300}
                value={universityForm.address}
                onChange={(event) =>
                  setUniversityForm((value) => ({ ...value, address: event.target.value }))
                }
              />
            </label>
            <div className="form-row">
              <label>
                Vĩ độ
                <input
                  required
                  type="number"
                  step="any"
                  min={-90}
                  max={90}
                  value={universityForm.latitude}
                  onChange={(event) =>
                    setUniversityForm((value) => ({ ...value, latitude: event.target.value }))
                  }
                />
              </label>
              <label>
                Kinh độ
                <input
                  required
                  type="number"
                  step="any"
                  min={-180}
                  max={180}
                  value={universityForm.longitude}
                  onChange={(event) =>
                    setUniversityForm((value) => ({ ...value, longitude: event.target.value }))
                  }
                />
              </label>
            </div>
            <div className="modal-actions">
              <button type="button" className="button outline small" onClick={closeModal}>
                Hủy
              </button>
              <button className="button primary small" disabled={busy.startsWith('university-')}>
                {modal.item ? 'Lưu thay đổi' : 'Thêm trường'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {modal?.type === 'reject' && (
        <Modal title="Từ chối yêu cầu chủ trọ" onClose={closeModal}>
          <form
            className="form"
            onSubmit={(event) => {
              event.preventDefault();
              mutate(
                modal.item.id,
                '/admin/landlord-requests/' + modal.item.id + '/reject',
                { method: 'PATCH', body: { adminNote: rejectReason } },
                'Đã từ chối yêu cầu.'
              );
            }}
          >
            <p className="muted">
              Hồ sơ của <strong>{modal.item.userName}</strong>: {modal.item.title}
            </p>
            <label>
              Ghi chú cho người dùng
              <textarea
                rows={4}
                maxLength={1000}
                value={rejectReason}
                onChange={(event) => setRejectReason(event.target.value)}
              />
            </label>
            <div className="modal-actions">
              <button type="button" className="button outline small" onClick={closeModal}>
                Hủy
              </button>
              <button className="button danger small" disabled={Boolean(busy)}>
                Xác nhận từ chối
              </button>
            </div>
          </form>
        </Modal>
      )}

      {modal?.type === 'report' && (
        <Modal title="Xử lý báo cáo" onClose={closeModal}>
          <p className="muted">
            {modal.item.reason}: {modal.item.roomTitle || modal.item.reviewComment}
          </p>
          <div className="modal-actions">
            <button
              className="button outline small"
              disabled={Boolean(busy)}
              onClick={() =>
                mutate(
                  modal.item.id,
                  '/admin/reports/' + modal.item.id,
                  { method: 'PATCH', body: { status: 'RESOLVED' } },
                  'Đã đánh dấu báo cáo được xử lý.'
                )
              }
            >
              Chỉ đánh dấu đã xử lý
            </button>
            <button
              className="button danger small"
              disabled={Boolean(busy)}
              onClick={() =>
                mutate(
                  modal.item.id,
                  '/admin/reports/' + modal.item.id,
                  { method: 'PATCH', body: { status: 'RESOLVED', deleteTarget: true } },
                  'Đã xử lý và ẩn nội dung vi phạm.'
                )
              }
            >
              Xử lý và ẩn nội dung
            </button>
          </div>
        </Modal>
      )}

      {modal?.type === 'confirm' && (
        <Modal title={modal.title} onClose={closeModal}>
          <p className="muted">{modal.text}</p>
          <div className="modal-actions">
            <button className="button outline small" onClick={closeModal}>
              Hủy
            </button>
            <button className="button danger small" disabled={Boolean(busy)} onClick={modal.action}>
              <Trash2 size={14} /> Xác nhận
            </button>
          </div>
        </Modal>
      )}
    </div>
  );
}
