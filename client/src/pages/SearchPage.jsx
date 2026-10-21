import { useEffect, useState, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  Heart,
  ArrowUpRight,
  Search,
  MapPin,
  GraduationCap,
  SlidersHorizontal,
  LayoutGrid,
  Map,
  X,
  LocateFixed,
  ShieldCheck,
  Wallet,
  ArrowRight,
  ChevronLeft,
  ChevronRight,
  Sparkles,
  Check,
} from 'lucide-react';
import { api, amenities, money } from '../lib/api';
import { useApp } from '../context/AppContext';
import RoomCard from '../components/RoomCard';
import RoomMap from '../components/RoomMap';
import { Loading, Empty, ErrorState } from '../components/Common';
export default function SearchPage() {
  const [params, setParams] = useSearchParams();
  const { notify } = useApp();
  const [universities, setUniversities] = useState([]),
    [result, setResult] = useState({ items: [], total: 0 }),
    [loading, setLoading] = useState(true),
    [error, setError] = useState('');
  const [keyword, setKeyword] = useState(params.get('keyword') || ''),
    [showMap, setShowMap] = useState(false),
    [filtersOpen, setFiltersOpen] = useState(false),
    [retry, setRetry] = useState(0),
    [locating, setLocating] = useState(false);
  const university = universities.find((u) => u.id === params.get('universityId'));
  const center = useMemo(
    () =>
      university
        ? [university.latitude, university.longitude]
        : params.has('centerLat')
          ? [Number(params.get('centerLat')), Number(params.get('centerLon'))]
          : null,
    [university, params]
  );
  useEffect(() => {
    api('/universities')
      .then(setUniversities)
      .catch((e) => notify(e.message, 'error'));
  }, [notify]);
  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError('');
    api('/rooms?' + params.toString(), { signal: controller.signal })
      .then(setResult)
      .catch((e) => {
        if (!controller.signal.aborted) setError(e.message);
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [params, retry]);
  function change(key, value) {
    setParams((previous) => {
      const p = new URLSearchParams(previous);
      value ? p.set(key, value) : p.delete(key);
      p.delete('page');
      if (key === 'universityId') {
        p.delete('centerLat');
        p.delete('centerLon');
        if (!value) p.delete('radius');
      }
      return p;
    });
  }
  function locate() {
    if (!navigator.geolocation) return notify('Trình duyệt chưa hỗ trợ định vị.', 'error');
    setLocating(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setParams({
          centerLat: pos.coords.latitude.toString(),
          centerLon: pos.coords.longitude.toString(),
          radius: '3',
        });
        setLocating(false);
      },
      () => {
        setLocating(false);
        notify('Chưa lấy được vị trí. Bạn có thể chọn trường đại học.', 'error');
      },
      { timeout: 10000 }
    );
  }
  const page = Number(params.get('page') || 1),
    pages = Math.ceil(result.total / 12);
  return (
    <div className="search-page container">
      <section className="hero">
        <div className="hero-copy">
          <span className="hero-eyebrow">
            <span /> TỔ ẤM NHỎ, HÀNH TRÌNH LỚN
          </span>
          <h1>
            Gần trường hơn.
            <br />
            <span>Gần ước mơ hơn.</span>
          </h1>
          <p>
            Tìm một căn phòng vừa ý, một mức giá vừa túi.
            <br />
            Để những năm tháng sinh viên thật đáng nhớ.
          </p>
          <div className="hero-pills">
            <span>
              <ShieldCheck size={15} /> Thông tin minh bạch
            </span>
            <span>
              <GraduationCap size={16} /> Dành cho sinh viên
            </span>
          </div>
        </div>
        <div className="hero-photo" />
        <div className="hero-note">
          <span className="hero-note-icon">
            <HouseIcon />
          </span>
          <div>
            Một góc nhỏ của riêng mình<span>Chặng đường mới bắt đầu từ đây.</span>
          </div>
          <Sparkles size={17} />
        </div>
        <span className="hero-number">01 / YOUR NEXT CHAPTER</span>
      </section>
      <form
        className="search-bar"
        onSubmit={(e) => {
          e.preventDefault();
          change('keyword', keyword);
        }}
      >
        <label className="search-school">
          <GraduationCap size={21} />
          <span>
            <span className="input-eyebrow">BẠN HỌC Ở ĐÂU?</span>
            <select
              aria-label="Trường đại học"
              value={params.get('universityId') || ''}
              onChange={(e) => change('universityId', e.target.value)}
            >
              <option value="">Chọn trường đại học</option>
              {universities.map((u) => (
                <option key={u.id} value={u.id}>
                  {u.name}
                </option>
              ))}
            </select>
          </span>
        </label>
        <label className="search-keyword">
          <MapPin size={20} />
          <span>
            <span className="input-eyebrow">KHU VỰC MONG MUỐN</span>
            <input
              aria-label="Từ khóa tìm phòng"
              placeholder="Tên đường, khu vực hoặc phòng trọ…"
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
            />
          </span>
        </label>
        <button className="button primary search-submit">
          <Search size={18} /> Tìm phòng ngay
        </button>
      </form>
      <div className="explore-intro">
        <div>
          <span className="eyebrow">KHÔNG GIAN HỢP GU, CHI PHÍ VỪA ĐỦ</span>
          <h2>
            Khám phá căn phòng tiếp theo<span className="heading-dot">.</span>
          </h2>
        </div>
        <button className="text-button locate-button" onClick={locate} disabled={locating}>
          <LocateFixed size={16} />
          {locating ? 'Đang định vị…' : 'Tìm quanh đây'}
          <ArrowUpRight size={15} />
        </button>
      </div>
      <div className="explore-layout">
        <aside className={'filters ' + (filtersOpen ? 'visible' : '')}>
          <div className="filter-heading">
            <h3>
              <SlidersHorizontal size={17} /> Bộ lọc tìm kiếm
            </h3>
            <button
              className="text-button reset"
              onClick={() => {
                setParams({});
                setKeyword('');
              }}
            >
              Đặt lại
            </button>
            <button
              className="icon-button close-filters"
              aria-label="Đóng bộ lọc"
              onClick={() => setFiltersOpen(false)}
            >
              <X size={18} />
            </button>
          </div>
          <div className="filter-section">
            <h4>
              Khoảng giá <small>/ tháng</small>
            </h4>
            <label className="field-label" htmlFor="max-price">
              Ngân sách tối đa
            </label>
            <select
              id="max-price"
              value={params.get('maxPrice') || ''}
              onChange={(e) => change('maxPrice', e.target.value)}
            >
              <option value="">Tất cả mức giá</option>
              <option value="2500000">Dưới 2,5 triệu</option>
              <option value="3500000">Dưới 3,5 triệu</option>
              <option value="5000000">Dưới 5 triệu</option>
              <option value="7000000">Dưới 7 triệu</option>
            </select>
            <div className="budget-line">
              <span>0 ₫</span>
              <span>
                {params.get('maxPrice') ? money(Number(params.get('maxPrice'))) : '7.000.000+ ₫'}
              </span>
            </div>
            <div className="budget-track">
              <span
                style={{
                  width: params.has('maxPrice')
                    ? Number(params.get('maxPrice')) / 70000 + '%'
                    : '100%',
                }}
              />
            </div>
          </div>
          <div className="filter-section">
            <h4>Khoảng cách đến trường</h4>
            <div className="radius-options">
              {['1', '3', '5'].map((r) => (
                <button
                  key={r}
                  className={params.get('radius') === r ? 'selected' : ''}
                  disabled={!center}
                  onClick={() => change('radius', params.get('radius') === r ? '' : r)}
                >
                  {r} km
                </button>
              ))}
            </div>
            <p className="filter-hint">
              {center ? 'Khoảng cách theo đường chim bay.' : 'Chọn trường để tìm phòng gần bạn.'}
            </p>
          </div>
          <div className="filter-section">
            <h4>Tiện ích bạn cần</h4>
            {['ac', 'wifi', 'kitchen', 'parking', 'no_curfew', 'balcony'].map((a) => (
              <label className="check-label" key={a}>
                <input
                  type="checkbox"
                  checked={params.get('amenity') === a}
                  onChange={() => change('amenity', params.get('amenity') === a ? '' : a)}
                />
                <span>{amenities[a]}</span>
              </label>
            ))}
          </div>
          <div className="filter-help">
            <span className="tip-icon">
              <Wallet size={21} />
            </span>
            <h4>Thuê trọ, không lo lố ví.</h4>
            <p>Dự toán điện, nước và sinh hoạt phí ngay trong chi tiết mỗi phòng.</p>
            <span className="tip-link">
              Biết rõ trước khi thuê <ArrowRight size={14} />
            </span>
          </div>
        </aside>
        <section className="results">
          <div className="results-toolbar">
            <div className="result-count">
              <strong>{loading ? '…' : result.total}</strong> phòng phù hợp{' '}
              <span className="result-area">
                {university ? '· ' + university.shortName : '· Hà Nội'}
              </span>
            </div>
            <div className="results-actions">
              <button
                className="button outline small filter-toggle"
                onClick={() => setFiltersOpen(true)}
              >
                <SlidersHorizontal size={15} /> Bộ lọc
              </button>
              <label className="sort-select">
                <span>Sắp xếp:</span>
                <select
                  aria-label="Sắp xếp phòng"
                  value={params.get('sort') || ''}
                  onChange={(e) => change('sort', e.target.value)}
                >
                  <option value="">Mới nhất</option>
                  <option value="price_asc">Giá tăng dần</option>
                  <option value="price_desc">Giá giảm dần</option>
                  <option value="distance">Gần nhất</option>
                </select>
              </label>
              <div className="view-toggle">
                <button
                  aria-label="Xem danh sách"
                  aria-pressed={!showMap}
                  className={!showMap ? 'active' : ''}
                  onClick={() => setShowMap(false)}
                >
                  <LayoutGrid size={17} />
                </button>
                <button
                  aria-label="Xem bản đồ"
                  aria-pressed={showMap}
                  className={showMap ? 'active' : ''}
                  onClick={() => setShowMap(true)}
                >
                  <Map size={17} />
                </button>
              </div>
            </div>
          </div>
          {params.size > 0 && (
            <div className="active-filters">
              {university && (
                <button onClick={() => change('universityId', '')}>
                  {university.shortName}
                  <X size={12} />
                </button>
              )}
              {params.get('keyword') && (
                <button
                  onClick={() => {
                    change('keyword', '');
                    setKeyword('');
                  }}
                >
                  {params.get('keyword')}
                  <X size={12} />
                </button>
              )}
              {params.get('maxPrice') && (
                <button onClick={() => change('maxPrice', '')}>
                  Dưới {money(Number(params.get('maxPrice')))}
                  <X size={12} />
                </button>
              )}
              {params.get('radius') && (
                <button onClick={() => change('radius', '')}>
                  {params.get('radius')} km
                  <X size={12} />
                </button>
              )}
              {params.get('amenity') && (
                <button onClick={() => change('amenity', '')}>
                  {amenities[params.get('amenity')]}
                  <X size={12} />
                </button>
              )}
            </div>
          )}
          {loading ? (
            <div className="room-grid skeleton-grid">
              {[1, 2, 3, 4, 5, 6].map((n) => (
                <div className="skeleton-card" key={n}>
                  <div />
                  <span />
                  <span />
                  <span />
                </div>
              ))}
            </div>
          ) : error ? (
            <ErrorState message={error} retry={() => setRetry(retry + 1)} />
          ) : !result.items.length ? (
            <Empty>
              <button
                className="button primary"
                onClick={() => {
                  setParams({});
                  setKeyword('');
                }}
              >
                Xóa bộ lọc
              </button>
            </Empty>
          ) : (
            <div className={showMap ? 'map-results' : ''}>
              <div className="room-grid">
                {result.items.map((room) => (
                  <RoomCard key={room.id} room={room} />
                ))}
              </div>
              {showMap && (
                <div className="map-panel">
                  <div className="map-panel-label">
                    <MapPin size={14} /> Một bước gần hơn với nơi ở mới
                  </div>
                  <RoomMap
                    rooms={result.items}
                    center={center}
                    radius={Number(params.get('radius'))}
                  />
                </div>
              )}
            </div>
          )}
          {pages > 1 && (
            <div className="pagination">
              <button
                className="icon-button"
                disabled={page === 1}
                aria-label="Trang trước"
                onClick={() =>
                  setParams((p) => {
                    p.set('page', String(page - 1));
                    return p;
                  })
                }
              >
                <ChevronLeft size={18} />
              </button>
              <span>
                Trang {page} / {pages}
              </span>
              <button
                className="icon-button"
                disabled={page >= pages}
                aria-label="Trang sau"
                onClick={() =>
                  setParams((p) => {
                    p.set('page', String(page + 1));
                    return p;
                  })
                }
              >
                <ChevronRight size={18} />
              </button>
            </div>
          )}
          <div className="results-end">
            <span />
            <p>Một nơi để ở. Nhiều điều để bắt đầu.</p>
            <span />
          </div>
        </section>
      </div>
      <section className="trust-strip">
        <div>
          <ShieldCheck />
          <span>
            <strong>Thông tin rõ ràng</strong>
            <small>Giá thuê và phụ phí được công khai</small>
          </span>
        </div>
        <div>
          <GraduationCap />
          <span>
            <strong>Gần giảng đường hơn</strong>
            <small>Tìm phòng theo trường đại học của bạn</small>
          </span>
        </div>
        <div>
          <Heart />
          <span>
            <strong>Trải nghiệm thật</strong>
            <small>Đánh giá từ cộng đồng người thuê</small>
          </span>
        </div>
      </section>
    </div>
  );
}
function HouseIcon() {
  return (
    <svg
      width="23"
      height="23"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
    >
      <path d="M3 10 12 3l9 7v10a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1Z" />
      <path d="M9 21v-8h6v8" />
    </svg>
  );
}
