import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import {
  Activity,
  ArrowLeft,
  ArrowDown,
  ArrowUp,
  ArrowUpDown,
  BadgeCheck,
  CalendarDays,
  Check,
  ChevronLeft,
  ChevronRight,
  Database,
  ExternalLink,
  Eye,
  EyeOff,
  FileText,
  Filter,
  Flag,
  GraduationCap,
  Heart,
  House,
  KeyRound,
  LayoutDashboard,
  LockKeyhole,
  LogOut,
  Menu,
  Moon,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  ShieldCheck,
  Star,
  Sun,
  Trash2,
  UsersRound,
  X,
} from 'lucide-react';
import { useApp } from '../context/AppContext';
import { api, mediaUrl, money } from '../lib/api';
import { Empty, ErrorState, Loading, Modal } from '../components/Common';
import { DownloadLink, ProtectedImage } from '../components/Attachments';

const views = [
  { key: 'dashboard', label: 'Tổng quan', icon: LayoutDashboard, group: 'Không gian làm việc' },
  { key: 'rooms', label: 'Phòng trọ', icon: House, group: 'Nội dung' },
  { key: 'users', label: 'Người dùng', icon: UsersRound, group: 'Nội dung' },
  { key: 'bookings', label: 'Lịch hẹn', icon: CalendarDays, group: 'Hoạt động' },
  { key: 'favorites', label: 'Lượt yêu thích', icon: Heart, group: 'Hoạt động' },
  { key: 'reviews', label: 'Đánh giá', icon: Star, group: 'Kiểm duyệt' },
  { key: 'reports', label: 'Báo cáo', icon: Flag, group: 'Kiểm duyệt' },
  { key: 'requests', label: 'Yêu cầu chủ trọ', icon: KeyRound, group: 'Kiểm duyệt' },
  { key: 'universities', label: 'Trường đại học', icon: GraduationCap, group: 'Hệ thống' },
];

const viewLabels = Object.fromEntries(views.map((item) => [item.key, item.label]));
const statusLabels = {
  ADMIN: 'Quản trị viên',
  LANDLORD: 'Chủ trọ',
  STUDENT: 'Người tìm trọ',
  AVAILABLE: 'Đang hiển thị',
  RENTED: 'Đã thuê',
  HIDDEN: 'Đang ẩn',
  PENDING: 'Đang chờ',
  CONFIRMED: 'Đã xác nhận',
  COMPLETED: 'Đã hoàn thành',
  CANCELLED: 'Đã hủy',
  APPROVED: 'Đã duyệt',
  REJECTED: 'Đã từ chối',
  RESOLVED: 'Đã xử lý',
  DISMISSED: 'Đã bỏ qua',
};
const date = (value, withTime = false) =>
  value
    ? new Date(value).toLocaleString('vi-VN', withTime ? undefined : { dateStyle: 'short' })
    : '—';
const roleLabel = (role) => statusLabels[role] || role;
const isExpired = (item) => item.expiresAt && new Date(item.expiresAt).getTime() <= Date.now();
const statusTone = (status) => {
  if (['AVAILABLE', 'APPROVED', 'CONFIRMED', 'COMPLETED', 'RESOLVED', 'LANDLORD'].includes(status))
    return 'success';
  if (['HIDDEN', 'REJECTED', 'CANCELLED'].includes(status)) return 'danger';
  if (['RENTED', 'ADMIN'].includes(status)) return 'info';
  return 'warning';
};

function StatusTag({ value, children }) {
  return (
    <span className={'adminator-tag is-' + statusTone(value)}>
      {children || statusLabels[value] || value}
    </span>
  );
}

function ActivityChart({ activity = [] }) {
  const series = [
    ['users', 'Người dùng', 'var(--ad-primary)'],
    ['rooms', 'Phòng mới', 'var(--ad-success)'],
    ['bookings', 'Lịch hẹn', 'var(--ad-purple)'],
  ];
  const maximum = Math.max(1, ...activity.flatMap((item) => series.map(([key]) => item[key] || 0)));
  const points = (key) =>
    activity
      .map((item, index) => {
        const x = 48 + index * (624 / Math.max(activity.length - 1, 1));
        const y = 188 - ((item[key] || 0) / maximum) * 142;
        return x + ',' + y;
      })
      .join(' ');

  return (
    <div className="adminator-chart">
      <div className="adminator-chart-legend">
        {series.map(([key, label, color]) => (
          <span key={key}>
            <i style={{ background: color }} /> {label}
          </span>
        ))}
      </div>
      <svg viewBox="0 0 720 230" role="img" aria-label="Hoạt động hệ thống trong bảy ngày">
        {[46, 81, 116, 151, 186].map((y) => (
          <line key={y} x1="48" x2="672" y1={y} y2={y} className="adminator-chart-grid" />
        ))}
        {series.map(([key, , color]) => (
          <polyline
            key={key}
            points={points(key)}
            fill="none"
            stroke={color}
            strokeWidth="3"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        ))}
        {activity.map((item, index) => (
          <text key={item.date} x={48 + index * (624 / Math.max(activity.length - 1, 1))} y="218">
            {item.date}
          </text>
        ))}
      </svg>
    </div>
  );
}

function Dashboard({ stats, onOpen }) {
  const totalRooms = Math.max(stats.rooms || 0, 1);
  const available = ((stats.availableRooms || 0) / totalRooms) * 100;
  const rented = ((stats.rentedRooms || 0) / totalRooms) * 100;
  const donut = {
    background: `conic-gradient(var(--ad-success) 0 ${available}%, var(--ad-info) ${available}% ${available + rented}%, var(--ad-danger) ${available + rented}% 100%)`,
  };
  const cards = [
    {
      label: 'Người dùng',
      value: stats.users,
      note: `${stats.landlords || 0} chủ trọ · ${stats.students || 0} người tìm · ${stats.administrators || 0} admin`,
      icon: UsersRound,
      tone: 'primary',
    },
    {
      label: 'Phòng trọ',
      value: stats.rooms,
      note: `${stats.availableRooms || 0} đang hiển thị`,
      icon: House,
      tone: 'success',
    },
    {
      label: 'Lịch hẹn',
      value: stats.bookings,
      note: `${stats.pendingBookings || 0} đang chờ xác nhận`,
      icon: CalendarDays,
      tone: 'purple',
    },
    {
      label: 'Cần xử lý',
      value: stats.moderationBacklog,
      note: 'Báo cáo · đánh giá · yêu cầu · lịch hẹn',
      icon: ShieldCheck,
      tone: 'danger',
    },
  ];

  return (
    <>
      <section className="adminator-kpi-grid">
        {cards.map(({ label, value, note, icon: Icon, tone }) => (
          <article className={'adminator-kpi-card tone-' + tone} key={label}>
            <div className="adminator-kpi-top">
              <span className={'adminator-kpi-icon tone-' + tone}>
                <Icon size={17} />
              </span>
              <span className="adminator-kpi-pill">Tổng</span>
            </div>
            <strong>{value || 0}</strong>
            <span>{label}</span>
            <small>{note}</small>
          </article>
        ))}
      </section>

      <section className="adminator-dashboard-grid">
        <article className="adminator-card adminator-activity-card">
          <header className="adminator-card-head">
            <div>
              <span className="adminator-eyebrow">Hiệu suất</span>
              <h2>Hoạt động 7 ngày gần nhất</h2>
            </div>
            <Activity size={18} />
          </header>
          <ActivityChart activity={stats.activity} />
        </article>

        <article className="adminator-card">
          <header className="adminator-card-head">
            <div>
              <span className="adminator-eyebrow">Nguồn cung</span>
              <h2>Trạng thái phòng</h2>
            </div>
            <Database size={18} />
          </header>
          <div className="adminator-room-distribution">
            <div className="adminator-donut" style={donut}>
              <span>{stats.rooms || 0}</span>
              <small>Tổng phòng</small>
            </div>
            <div className="adminator-distribution-list">
              <span>
                <i className="is-success" /> Đang hiển thị{' '}
                <strong>{stats.availableRooms || 0}</strong>
              </span>
              <span>
                <i className="is-info" /> Đã thuê <strong>{stats.rentedRooms || 0}</strong>
              </span>
              <span>
                <i className="is-danger" /> Đang ẩn/hết hạn{' '}
                <strong>{stats.hiddenRooms || 0}</strong>
              </span>
            </div>
          </div>
        </article>

        <article className="adminator-card adminator-task-card">
          <header className="adminator-card-head">
            <div>
              <span className="adminator-eyebrow">Hàng đợi</span>
              <h2>Công việc cần xử lý</h2>
            </div>
          </header>
          {[
            ['reports', 'Báo cáo vi phạm', stats.pendingReports, Flag, 'danger'],
            ['reviews', 'Đánh giá bị gắn cờ', stats.flaggedReviews, Star, 'warning'],
            ['requests', 'Yêu cầu làm chủ trọ', stats.pendingLandlordRequests, KeyRound, 'primary'],
            ['bookings', 'Lịch hẹn đang chờ', stats.pendingBookings, CalendarDays, 'purple'],
          ].map(([key, label, count, Icon, tone]) => (
            <button key={key} onClick={() => onOpen(key)} className="adminator-task-row">
              <span className={'adminator-task-icon tone-' + tone}>
                <Icon size={16} />
              </span>
              <span>{label}</span>
              <strong>{count || 0}</strong>
              <ChevronRight size={15} />
            </button>
          ))}
        </article>

        <article className="adminator-card adminator-metrics-card">
          <header className="adminator-card-head">
            <div>
              <span className="adminator-eyebrow">Toàn hệ thống</span>
              <h2>Chỉ số bổ sung</h2>
            </div>
          </header>
          <div className="adminator-mini-metrics">
            {[
              ['Lượt yêu thích', stats.favorites, Heart],
              ['Đánh giá', stats.reviews, Star],
              ['Báo cáo', stats.reports, Flag],
              ['Trường đại học', stats.universities, GraduationCap],
            ].map(([label, value, Icon]) => (
              <div key={label}>
                <Icon size={17} />
                <strong>{value || 0}</strong>
                <span>{label}</span>
              </div>
            ))}
          </div>
        </article>
      </section>
    </>
  );
}

function DataPanel({
  title,
  description,
  rows,
  columns,
  query,
  setQuery,
  page,
  setPage,
  action,
  filters = [],
  filterLabel = 'Lọc dữ liệu',
}) {
  const pageSize = 8;
  const [activeFilter, setActiveFilter] = useState('all');
  const [sort, setSort] = useState(null);
  const filtered = useMemo(() => {
    const term = query.trim().toLocaleLowerCase('vi');
    const selected = filters.find((item) => item.value === activeFilter);
    return rows.filter(
      (row) =>
        (!selected || selected.matches(row)) &&
        (!term || JSON.stringify(row).toLocaleLowerCase('vi').includes(term))
    );
  }, [activeFilter, filters, query, rows]);
  const ordered = useMemo(() => {
    if (!sort) return filtered;
    const column = columns.find((item) => (item.key || item.label) === sort.key);
    if (!column?.sortValue) return filtered;
    const direction = sort.direction === 'ascending' ? 1 : -1;
    return [...filtered].sort((left, right) => {
      const leftValue = column.sortValue(left);
      const rightValue = column.sortValue(right);
      if (typeof leftValue === 'number' && typeof rightValue === 'number')
        return (leftValue - rightValue) * direction;
      return (
        String(leftValue ?? '').localeCompare(String(rightValue ?? ''), 'vi', {
          numeric: true,
          sensitivity: 'base',
        }) * direction
      );
    });
  }, [columns, filtered, sort]);
  const pages = Math.max(1, Math.ceil(ordered.length / pageSize));
  const currentPage = Math.min(page, pages);
  const pageRows = ordered.slice((currentPage - 1) * pageSize, currentPage * pageSize);
  const sortableColumns = columns.filter((column) => column.sortValue);

  useEffect(() => {
    if (page > pages) setPage(pages);
  }, [page, pages, setPage]);

  useEffect(() => {
    setActiveFilter('all');
    setSort(null);
  }, [title]);

  function toggleSort(column) {
    if (!column.sortValue) return;
    const key = column.key || column.label;
    setSort((current) => ({
      key,
      direction:
        current?.key === key && current.direction === 'ascending' ? 'descending' : 'ascending',
    }));
    setPage(1);
  }

  return (
    <section className="adminator-card adminator-data-card">
      <header className="adminator-card-head adminator-data-heading">
        <div>
          <span className="adminator-eyebrow">Quản lý dữ liệu</span>
          <h2>{title}</h2>
          <p>{description}</p>
        </div>
        {action}
      </header>
      <div className="adminator-data-toolbar">
        <div className="adminator-toolbar-controls">
          <label className="adminator-search">
            <Search size={15} />
            <input
              aria-label={'Tìm trong ' + title.toLocaleLowerCase('vi')}
              value={query}
              onChange={(event) => {
                setQuery(event.target.value);
                setPage(1);
              }}
              placeholder={'Tìm trong ' + title.toLocaleLowerCase('vi') + '…'}
            />
            {query && (
              <button
                type="button"
                aria-label="Xóa tìm kiếm"
                onClick={() => {
                  setQuery('');
                  setPage(1);
                }}
              >
                <X size={14} />
              </button>
            )}
          </label>
          {!!filters.length && (
            <label className="adminator-filter">
              <Filter size={14} />
              <span className="sr-only">{filterLabel}</span>
              <select
                aria-label={filterLabel}
                value={activeFilter}
                onChange={(event) => {
                  setActiveFilter(event.target.value);
                  setPage(1);
                }}
              >
                <option value="all">Tất cả</option>
                {filters.map((item) => (
                  <option key={item.value} value={item.value}>
                    {item.label}
                  </option>
                ))}
              </select>
            </label>
          )}
          {!!sortableColumns.length && (
            <div className="adminator-mobile-sort">
              <ArrowUpDown size={14} />
              <select
                aria-label={'Sắp xếp ' + title.toLocaleLowerCase('vi')}
                value={sort?.key || ''}
                onChange={(event) => {
                  setSort(
                    event.target.value ? { key: event.target.value, direction: 'ascending' } : null
                  );
                  setPage(1);
                }}
              >
                <option value="">Sắp xếp mặc định</option>
                {sortableColumns.map((column) => (
                  <option key={column.key || column.label} value={column.key || column.label}>
                    {column.label}
                  </option>
                ))}
              </select>
              <button
                type="button"
                aria-label="Đổi chiều sắp xếp"
                title="Đổi chiều sắp xếp"
                disabled={!sort}
                onClick={() => {
                  setSort((current) =>
                    current
                      ? {
                          ...current,
                          direction: current.direction === 'ascending' ? 'descending' : 'ascending',
                        }
                      : current
                  );
                  setPage(1);
                }}
              >
                {sort?.direction === 'descending' ? <ArrowDown size={14} /> : <ArrowUp size={14} />}
              </button>
            </div>
          )}
        </div>
        <span>
          {ordered.length}
          {ordered.length !== rows.length ? ` / ${rows.length}` : ''} bản ghi
        </span>
      </div>
      {pageRows.length ? (
        <>
          <div className="adminator-table-wrap">
            <table className="adminator-table">
              <caption className="sr-only">
                {title}: {description}
              </caption>
              <thead>
                <tr>
                  {columns.map((column) => {
                    const key = column.key || column.label;
                    const direction = sort?.key === key ? sort.direction : null;
                    return (
                      <th key={key} aria-sort={column.sortValue ? direction || 'none' : undefined}>
                        {column.sortValue ? (
                          <button type="button" onClick={() => toggleSort(column)}>
                            {column.label}
                            {direction === 'ascending' ? (
                              <ArrowUp size={12} />
                            ) : direction === 'descending' ? (
                              <ArrowDown size={12} />
                            ) : (
                              <ArrowUpDown size={12} />
                            )}
                          </button>
                        ) : (
                          column.label
                        )}
                      </th>
                    );
                  })}
                </tr>
              </thead>
              <tbody>
                {pageRows.map((row) => (
                  <tr key={row.id || row.userId + row.roomId}>
                    {columns.map((column) => (
                      <td key={column.key || column.label} data-label={column.label}>
                        {column.render(row)}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <footer className="adminator-data-foot">
            <span>
              Hiển thị {(currentPage - 1) * pageSize + 1}–
              {Math.min(currentPage * pageSize, ordered.length)} / {ordered.length}
            </span>
            <div>
              <button
                disabled={currentPage <= 1}
                onClick={() => setPage((value) => Math.max(1, value - 1))}
                aria-label="Trang trước"
              >
                <ChevronLeft size={15} />
              </button>
              <span>
                {currentPage} / {pages}
              </span>
              <button
                disabled={currentPage >= pages}
                onClick={() => setPage((value) => Math.min(pages, value + 1))}
                aria-label="Trang sau"
              >
                <ChevronRight size={15} />
              </button>
            </div>
          </footer>
        </>
      ) : (
        <Empty
          title={rows.length ? 'Không có dữ liệu phù hợp' : 'Chưa có dữ liệu'}
          text={
            rows.length ? 'Thử thay đổi từ khóa hoặc bộ lọc.' : 'Dữ liệu mới sẽ xuất hiện tại đây.'
          }
        />
      )}
    </section>
  );
}

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

  const load = useCallback(async () => {
    const version = ++loadVersion.current;
    setError('');
    setRefreshing(true);
    try {
      const [stats, users, rooms, bookings, favorites, reviews, reports, requests, universities] =
        await Promise.all([
          api('/admin/stats'),
          api('/admin/users'),
          api('/admin/rooms'),
          api('/admin/bookings'),
          api('/admin/favorites'),
          api('/admin/reviews'),
          api('/admin/reports'),
          api('/admin/landlord-requests'),
          api('/admin/universities'),
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
      });
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

  useEffect(() => {
    const refreshOnFocus = () => load();
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
              sortValue: (item) => Number(item.isEduVerified),
              render: (item) =>
                item.isEduVerified ? (
                  <span className="adminator-verified">
                    <BadgeCheck size={14} /> Edu verified
                  </span>
                ) : (
                  <span className="adminator-subtle">Chưa xác thực</span>
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
