import { useEffect, useMemo, useRef, useState } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { ChevronLeft, ChevronRight, GraduationCap, List, LoaderCircle, LocateFixed, Map as MapIcon, MapPin, Search, SlidersHorizontal, X } from 'lucide-react';
import { api, amenities } from '../lib/api';
import { useApp } from '../context/AppContext';
import RoomCard from '../components/RoomCard';
import RoomMap from '../components/RoomMap';
import { Empty, ErrorState } from '../components/Common';

export default function SearchPage() {
  const [params, setParams] = useSearchParams();
  const { notify } = useApp();
  const navigate = useNavigate();
  const location = useLocation();
  const [universities, setUniversities] = useState([]);
  const [result, setResult] = useState({ items: [], total: 0 });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [retry, setRetry] = useState(0);
  const [keyword, setKeyword] = useState(params.get('keyword') || '');
  const [locationText, setLocationText] = useState('');
  const [suggestionsOpen, setSuggestionsOpen] = useState(false);
  const [places, setPlaces] = useState([]);
  const [placesLoading, setPlacesLoading] = useState(false);
  const [placesError, setPlacesError] = useState('');
  const [resolvingId, setResolvingId] = useState('');
  const [activeSuggestion, setActiveSuggestion] = useState(-1);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [locating, setLocating] = useState(false);
  const [mobileView, setMobileView] = useState('list');
  const picker = useRef(null);
  const university = universities.find((item) => item.id === params.get('universityId'));
  const center = useMemo(() => university ? [university.latitude, university.longitude] : params.has('centerLat') ? [Number(params.get('centerLat')), Number(params.get('centerLon'))] : null, [university, params]);
  const locationLabel = university?.name || params.get('place') || (center ? 'Vị trí đã chọn trên bản đồ' : 'Tất cả khu vực');
  const matchingUniversities = universities.filter((item) => `${item.name} ${item.shortName} ${item.address}`.toLocaleLowerCase('vi').includes(locationText.toLocaleLowerCase('vi')));
  const suggestions = places.length ? places : matchingUniversities;

  useEffect(() => { api('/universities').then(setUniversities).catch((e) => notify(e.message, 'error')); }, [notify]);
  useEffect(() => { setLocationText(locationLabel === 'Tất cả khu vực' ? '' : locationLabel); }, [locationLabel]);
  useEffect(() => { const close = (event) => { if (!picker.current?.contains(event.target)) setSuggestionsOpen(false); }; document.addEventListener('pointerdown', close); return () => document.removeEventListener('pointerdown', close); }, []);
  useEffect(() => { setKeyword(params.get('keyword') || ''); }, [params]);
  useEffect(() => {
    const controller = new AbortController(); setLoading(true); setError('');
    api('/rooms?' + params.toString(), { signal: controller.signal }).then(setResult).catch((e) => { if (!controller.signal.aborted) setError(e.message); }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [params, retry]);
  useEffect(() => {
    const query = locationText.trim();
    if (!suggestionsOpen || query.length < 2) { setPlaces([]); setPlacesError(''); setPlacesLoading(false); setActiveSuggestion(-1); return undefined; }
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setPlacesLoading(true); setPlacesError('');
      api('/places/suggest?q=' + encodeURIComponent(query), { signal: controller.signal })
        .then((items) => { setPlaces(items); setActiveSuggestion(-1); })
        .catch((e) => { if (!controller.signal.aborted) { setPlaces([]); setPlacesError(e.message); } })
        .finally(() => { if (!controller.signal.aborted) setPlacesLoading(false); });
    }, 300);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [locationText, suggestionsOpen]);

  function change(key, value) { setParams((previous) => { const next = new URLSearchParams(previous); value ? next.set(key, value) : next.delete(key); next.delete('page'); return next; }); }
  function selectUniversity(id) {
    const selected = universities.find((item) => item.id === id);
    setParams((previous) => { const next = new URLSearchParams(previous); if (selected) { next.set('universityId', selected.id); next.set('radius', next.get('radius') || '3'); } else { next.delete('universityId'); next.delete('radius'); } next.delete('place'); next.delete('centerLat'); next.delete('centerLon'); next.delete('page'); return next; });
    setSuggestionsOpen(false); setPlaces([]); setLocationText(selected?.name || '');
  }
  function selectPoint(latitude, longitude, label = '') {
    setParams((previous) => { const next = new URLSearchParams(previous); next.delete('universityId'); label ? next.set('place', label) : next.delete('place'); next.set('centerLat', latitude.toFixed(6)); next.set('centerLon', longitude.toFixed(6)); next.set('radius', next.get('radius') || '3'); next.delete('page'); return next; });
    setSuggestionsOpen(false); setPlaces([]);
  }
  async function selectPlace(place) {
    try {
      let selected = place;
      if (selected.latitude == null || selected.longitude == null) { setResolvingId(selected.id); selected = await api('/places/resolve?id=' + encodeURIComponent(selected.id)); }
      selectPoint(selected.latitude, selected.longitude, selected.title); setLocationText(selected.title);
    } catch (e) { notify(e.message, 'error'); } finally { setResolvingId(''); }
  }
  function locate() {
    if (!navigator.geolocation) return notify('Trình duyệt chưa hỗ trợ định vị.', 'error');
    setLocating(true); navigator.geolocation.getCurrentPosition((position) => { selectPoint(position.coords.latitude, position.coords.longitude); setLocating(false); }, () => { setLocating(false); notify('Chưa lấy được vị trí. Bạn có thể chọn trường đại học.', 'error'); }, { timeout: 10000 });
  }
  function handleLocationKeyDown(event) {
    if (event.key === 'Escape') return setSuggestionsOpen(false);
    if (event.key === 'ArrowDown' && suggestions.length) { event.preventDefault(); return setActiveSuggestion((index) => Math.min(index + 1, suggestions.length - 1)); }
    if (event.key === 'ArrowUp' && suggestions.length) { event.preventDefault(); return setActiveSuggestion((index) => Math.max(index - 1, 0)); }
    if (event.key === 'Enter' && suggestionsOpen && suggestions.length) { event.preventDefault(); const selected = suggestions[activeSuggestion >= 0 ? activeSuggestion : 0]; return selected.source ? selectPlace(selected) : selectUniversity(selected.id); }
  }
  const page = Number(params.get('page') || 1); const pages = Math.ceil(result.total / 12);

  return <div className="container search-page prototype-search"><section className="search-controls"><form className="search-main-row" onSubmit={(event) => { event.preventDefault(); change('keyword', keyword); }}><div className="location-picker" ref={picker}><MapPin size={16}/><input aria-label="Tìm vị trí hoặc trường đại học" aria-autocomplete="list" aria-controls="location-options" aria-expanded={suggestionsOpen} aria-activedescendant={activeSuggestion >= 0 ? `place-option-${activeSuggestion}` : undefined} autoComplete="off" placeholder="Tìm vị trí (Bách Khoa, Cầu Giấy, Thủ Đức…)" value={locationText} onFocus={() => setSuggestionsOpen(true)} onChange={(event) => { setLocationText(event.target.value); setSuggestionsOpen(true); }} onKeyDown={handleLocationKeyDown}/>{locationText && <button type="button" className="clear-location" aria-label="Xóa vị trí" onClick={() => selectUniversity('')}><X size={14}/></button>}{suggestionsOpen && <div className="location-suggestions" id="location-options" role="listbox" aria-label="Gợi ý địa điểm"><button type="button" className="gps-suggestion" onClick={locate} disabled={locating}><LocateFixed size={15}/> {locating ? 'Đang định vị…' : 'Định vị GPS của tôi'}</button>{placesLoading && <p className="suggestion-status"><LoaderCircle className="spin" size={15}/> Đang tìm địa điểm…</p>}{!placesLoading && places.map((place, index) => <button type="button" role="option" id={`place-option-${index}`} aria-selected={activeSuggestion === index} className={activeSuggestion === index ? 'active' : ''} key={place.id} disabled={resolvingId === place.id} onMouseMove={() => setActiveSuggestion(index)} onClick={() => selectPlace(place)}>{resolvingId === place.id ? <LoaderCircle className="spin" size={16}/> : <MapPin size={16}/>}<span><strong>{place.title}</strong><small>{place.subtitle}</small></span></button>)}{!placesLoading && !places.length && matchingUniversities.map((item, index) => <button type="button" role="option" id={`place-option-${index}`} aria-selected={activeSuggestion === index} className={activeSuggestion === index ? 'active' : ''} key={item.id} onMouseMove={() => setActiveSuggestion(index)} onClick={() => selectUniversity(item.id)}><GraduationCap size={16}/><span><strong>{item.name}</strong><small>{item.address}</small></span></button>)}{placesError && <p className="suggestion-status error">{placesError}</p>}{!placesLoading && !placesError && locationText.trim().length >= 2 && !suggestions.length && <p className="suggestion-status">Chưa có gợi ý. Hãy thử nhập thêm tên đường hoặc khu vực.</p>}</div>}</div><div className="keyword-field"><Search size={16}/><input aria-label="Từ khóa tìm phòng" placeholder="Từ khóa (gác lửng, ban công, máy giặt…)" value={keyword} onChange={(event) => setKeyword(event.target.value)}/><button type="submit" aria-label="Tìm phòng ngay" title="Tìm phòng ngay"><Search size={15}/></button></div><div className="radius-filter"><span>Bán kính:</span><div className="radius-options">{['1','3','5'].map((radius) => <button type="button" key={radius} disabled={!center} aria-pressed={params.get('radius') === radius} className={params.get('radius') === radius ? 'selected' : ''} onClick={() => change('radius', radius)}>{radius} km</button>)}</div></div><label className="price-filter"><span>Mức giá:</span><select aria-label="Ngân sách tối đa" value={params.get('maxPrice') || ''} onChange={(event) => change('maxPrice', event.target.value)}><option value="">Tất cả giá</option><option value="2500000">Dưới 2,5 triệu</option><option value="3500000">Dưới 3,5 triệu</option><option value="5000000">Dưới 5 triệu</option><option value="7000000">Dưới 7 triệu</option></select></label></form><div className="quick-locations"><span>Địa điểm nhanh:</span><button className="gps-chip" onClick={locate} disabled={locating}><LocateFixed size={12}/>{locating ? 'Đang định vị…' : 'Định vị GPS của tôi'}</button>{universities.map((item) => <button key={item.id} className={university?.id === item.id ? 'selected' : ''} onClick={() => selectUniversity(item.id)}><MapPin size={11}/>{item.shortName}</button>)}<button className={'filter-options-toggle ' + (filtersOpen ? 'selected' : '')} aria-label={filtersOpen ? 'Đóng bộ lọc' : 'Bộ lọc'} onClick={() => setFiltersOpen(!filtersOpen)}><SlidersHorizontal size={13}/> Lọc thêm</button><span className="search-count">Tìm thấy <strong>{loading ? '…' : result.total}</strong> phòng trọ</span></div>{filtersOpen && <div className="extra-filters"><label>Trường đại học<select aria-label="Trường đại học" value={university?.id || ''} onChange={(event) => selectUniversity(event.target.value)}><option value="">Tất cả trường</option>{universities.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label>Tiện ích<select value={params.get('amenity') || ''} onChange={(event) => change('amenity', event.target.value)}><option value="">Tất cả tiện ích</option>{Object.entries(amenities).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label><label>Sắp xếp<select aria-label="Sắp xếp phòng" value={params.get('sort') || ''} onChange={(event) => change('sort', event.target.value)}><option value="">Mới nhất</option><option value="price_asc">Giá tăng dần</option><option value="price_desc">Giá giảm dần</option><option value="distance">Gần nhất</option></select></label><button className="text-button" onClick={() => { setParams({}); setLocationText(''); setKeyword(''); }}>Xóa bộ lọc</button></div>}</section><div className="search-view-tabs"><button aria-label="Xem danh sách" aria-pressed={mobileView === 'list'} className={mobileView === 'list' ? 'active' : ''} onClick={() => setMobileView('list')}><List size={15}/> Danh sách</button><button aria-label="Xem bản đồ" aria-pressed={mobileView === 'map'} className={mobileView === 'map' ? 'active' : ''} onClick={() => setMobileView('map')}><MapIcon size={15}/> Bản đồ</button></div><div className={'search-split mobile-' + mobileView}><section className="search-list" aria-label="Kết quả tìm phòng">{loading ? <div className="horizontal-skeletons" role="status" aria-label="Đang tìm phòng">{[1,2,3,4].map((item) => <div key={item} className="horizontal-skeleton"><span/><div><i/><i/><i/></div></div>)}</div> : error ? <ErrorState message={error} retry={() => setRetry(retry + 1)}/> : !result.items.length ? <Empty title="Chưa tìm thấy phòng phù hợp" text="Thử tăng bán kính hoặc chọn một vị trí khác trên bản đồ."><button className="button primary small" onClick={() => { setParams({}); setKeyword(''); }}>Xóa bộ lọc</button></Empty> : result.items.map((room) => <RoomCard key={room.id} room={room} horizontal/>)}{pages > 1 && <div className="pagination"><button className="icon-button" aria-label="Trang trước" disabled={page === 1} onClick={() => change('page', String(page - 1))}><ChevronLeft size={16}/></button><span>Trang {page} / {pages}</span><button className="icon-button" aria-label="Trang sau" disabled={page >= pages} onClick={() => setParams((previous) => { const next = new URLSearchParams(previous); next.set('page', String(page + 1)); return next; })}><ChevronRight size={16}/></button></div>}</section><section className="search-map-panel"><div className="map-panel-heading"><span><MapPin size={14}/> Tâm tìm kiếm: <strong>{locationLabel}</strong></span><small>Click bản đồ để đổi vị trí</small></div><RoomMap rooms={result.items} center={center} radius={Number(params.get('radius'))} onSelectPoint={selectPoint} onSelectRoom={(room) => navigate('/rooms/' + room.id, { state: { backgroundLocation: location } })}/></section></div></div>;
}
