import { useEffect, useMemo, useState } from 'react';
import {
  Activity,
  ArrowDown,
  ArrowUp,
  ArrowUpDown,
  CalendarDays,
  ChevronLeft,
  ChevronRight,
  Database,
  Filter,
  Flag,
  GraduationCap,
  Heart,
  House,
  KeyRound,
  LayoutDashboard,
  Mail,
  Search,
  ShieldCheck,
  Star,
  UsersRound,
  X,
} from 'lucide-react';
import { Empty } from '../Common';

export const views = [
  { key: 'dashboard', label: 'Tổng quan', icon: LayoutDashboard, group: 'Không gian làm việc' },
  { key: 'rooms', label: 'Phòng trọ', icon: House, group: 'Nội dung' },
  { key: 'users', label: 'Người dùng', icon: UsersRound, group: 'Nội dung' },
  { key: 'bookings', label: 'Lịch hẹn', icon: CalendarDays, group: 'Hoạt động' },
  { key: 'favorites', label: 'Lượt yêu thích', icon: Heart, group: 'Hoạt động' },
  { key: 'reviews', label: 'Đánh giá', icon: Star, group: 'Kiểm duyệt' },
  { key: 'reports', label: 'Báo cáo', icon: Flag, group: 'Kiểm duyệt' },
  { key: 'requests', label: 'Yêu cầu chủ trọ', icon: KeyRound, group: 'Kiểm duyệt' },
  { key: 'universities', label: 'Trường đại học', icon: GraduationCap, group: 'Hệ thống' },
  { key: 'settings', label: 'Cài đặt SMTP', icon: Mail, group: 'Hệ thống' },
];

export const viewLabels = Object.fromEntries(views.map((item) => [item.key, item.label]));

export const statusLabels = {
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

export function date(value, withTime = false) {
  if (!value) return '—';
  return new Date(value).toLocaleString('vi-VN', withTime ? undefined : { dateStyle: 'short' });
}

export function roleLabel(role) {
  return statusLabels[role] || role;
}

export function isExpired(item) {
  return Boolean(item.expiresAt && new Date(item.expiresAt).getTime() <= Date.now());
}

function statusTone(status) {
  if (['AVAILABLE', 'APPROVED', 'CONFIRMED', 'COMPLETED', 'RESOLVED', 'LANDLORD'].includes(status))
    return 'success';
  if (['HIDDEN', 'REJECTED', 'CANCELLED'].includes(status)) return 'danger';
  if (['RENTED', 'ADMIN'].includes(status)) return 'info';
  return 'warning';
}

export function StatusTag({ value, children }) {
  return (
    <span className={'adminator-tag is-' + statusTone(value)}>
      {children || statusLabels[value] || value}
    </span>
  );
}

export function ActivityChart({ activity = [] }) {
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

export function Dashboard({ stats, onOpen }) {
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

export function DataPanel({
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
          {Boolean(filters.length) && (
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
          {Boolean(sortableColumns.length) && (
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
