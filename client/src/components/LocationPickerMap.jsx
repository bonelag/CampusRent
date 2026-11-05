import { useState, useEffect, useRef, useMemo } from 'react';
import { MapContainer, TileLayer, Marker, useMap, useMapEvents } from 'react-leaflet';
import L from 'leaflet';
import {
  MapPin,
  Search,
  LocateFixed,
  LoaderCircle,
  X,
  Check,
  Compass,
  ArrowRight,
} from 'lucide-react';
import { api } from '../lib/api';
import 'leaflet/dist/leaflet.css';

const pickerIcon = L.divIcon({
  className: 'location-picker-pin',
  html: `
    <div class="pin-pulse"></div>
    <div class="pin-body">
      <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
        <path d="M20 10c0 4.993-5.539 10.193-7.399 11.799a1 1 0 0 1-1.202 0C9.539 20.193 4 14.993 4 10a8 8 0 0 1 16 0" fill="#ef4444" stroke="#ffffff"/>
        <circle cx="12" cy="10" r="3" fill="#ffffff"/>
      </svg>
    </div>
  `,
  iconSize: [36, 42],
  iconAnchor: [18, 42],
});

function MapController({ center, zoom = 16 }) {
  const map = useMap();
  useEffect(() => {
    map.invalidateSize();
    if (center && center[0] && center[1]) {
      map.flyTo(center, zoom, { duration: 0.8 });
    }
  }, [center, zoom, map]);
  return null;
}

function MapClickHandler({ onClick }) {
  useMapEvents({
    click(e) {
      onClick(e.latlng.lat, e.latlng.lng);
    },
  });
  return null;
}

export default function LocationPickerMap({
  latitude,
  longitude,
  address,
  onChange,
  universities = [],
}) {
  const currentPos = useMemo(() => {
    return latitude && longitude ? [latitude, longitude] : [21.0055, 105.8433];
  }, [latitude, longitude]);

  const [mapCenter, setMapCenter] = useState(currentPos);
  const [searchText, setSearchText] = useState('');
  const [suggestions, setSuggestions] = useState([]);
  const [suggestLoading, setSuggestLoading] = useState(false);
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const [locating, setLocating] = useState(false);
  const [reverseAddress, setReverseAddress] = useState('');
  const [reverseLoading, setReverseLoading] = useState(false);

  const markerRef = useRef(null);
  const searchBoxRef = useRef(null);

  // Close suggestions when clicking outside
  useEffect(() => {
    function handleClickOutside(e) {
      if (searchBoxRef.current && !searchBoxRef.current.contains(e.target)) {
        setDropdownOpen(false);
      }
    }
    document.addEventListener('pointerdown', handleClickOutside);
    return () => document.removeEventListener('pointerdown', handleClickOutside);
  }, []);

  // Search places debounce
  useEffect(() => {
    if (!searchText.trim() || searchText.trim().length < 2) {
      setSuggestions([]);
      return;
    }
    const timer = setTimeout(async () => {
      setSuggestLoading(true);
      try {
        const results = await api('/places/suggest?q=' + encodeURIComponent(searchText.trim()));
        setSuggestions(results);
        setDropdownOpen(true);
      } catch {
        setSuggestions([]);
      } finally {
        setSuggestLoading(false);
      }
    }, 250);

    return () => clearTimeout(timer);
  }, [searchText]);

  async function handleReverseGeocode(lat, lon) {
    setReverseLoading(true);
    try {
      const res = await api(`/places/reverse?lat=${lat}&lon=${lon}`);
      if (res.address && res.address !== 'Vị trí đã chọn trên bản đồ') {
        setReverseAddress(res.address);
      } else {
        setReverseAddress('');
      }
    } catch {
      setReverseAddress('');
    } finally {
      setReverseLoading(false);
    }
  }

  function handlePointSelect(lat, lon, newAddress = null) {
    const roundedLat = parseFloat(lat.toFixed(6));
    const roundedLon = parseFloat(lon.toFixed(6));
    setMapCenter([roundedLat, roundedLon]);
    onChange({ latitude: roundedLat, longitude: roundedLon, address: newAddress });
    handleReverseGeocode(roundedLat, roundedLon);
  }

  function handleMarkerDragEnd() {
    const marker = markerRef.current;
    if (marker) {
      const pos = marker.getLatLng();
      handlePointSelect(pos.lat, pos.lng);
    }
  }

  function handleSelectSuggestion(s) {
    setDropdownOpen(false);
    setSearchText(s.title);
    if (s.latitude && s.longitude) {
      const fullAddr = s.subtitle ? `${s.title}, ${s.subtitle}` : s.title;
      handlePointSelect(s.latitude, s.longitude, fullAddr);
    }
  }

  function handleLocateMe() {
    if (!navigator.geolocation) {
      alert('Trình duyệt của bạn không hỗ trợ định vị GPS.');
      return;
    }
    setLocating(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setLocating(false);
        handlePointSelect(pos.coords.latitude, pos.coords.longitude);
      },
      () => {
        setLocating(false);
        alert('Không thể lấy vị trí hiện tại. Vui lòng cho phép quyền truy cập vị trí.');
      },
      { timeout: 8000 }
    );
  }

  function applyReverseAddress() {
    if (reverseAddress) {
      onChange({ latitude, longitude, address: reverseAddress });
    }
  }

  return (
    <div className="location-picker-map-wrapper">
      <div className="picker-header">
        <label className="picker-title">
          <MapPin size={17} className="text-primary" />
          <span>Vị trí phòng trọ trên bản đồ</span>
        </label>
        <span className="picker-hint">
          Click hoặc kéo thả ghim đỏ đến đúng vị trí nhà trọ của bạn
        </span>
      </div>

      {/* Map Search & GPS bar */}
      <div className="picker-search-bar" ref={searchBoxRef}>
        <div className="picker-input-group">
          <Search size={16} className="search-icon" />
          <input
            type="text"
            placeholder="Tìm tên đường, ngõ ngách, địa điểm (VD: 84 Chùa Láng, Bách Khoa...)"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            onFocus={() => searchText.trim().length >= 2 && setDropdownOpen(true)}
          />
          {searchText && (
            <button
              type="button"
              className="clear-btn"
              onClick={() => {
                setSearchText('');
                setSuggestions([]);
              }}
            >
              <X size={14} />
            </button>
          )}
          {suggestLoading && <LoaderCircle size={16} className="spin loading-icon" />}
        </div>

        <button
          type="button"
          className="button outline small gps-btn"
          onClick={handleLocateMe}
          disabled={locating}
          title="Lấy vị trí GPS hiện tại của tôi"
        >
          <LocateFixed size={15} />
          <span>{locating ? 'Đang định vị…' : 'Vị trí của tôi'}</span>
        </button>

        {/* Suggestion Dropdown */}
        {dropdownOpen && (
          <div className="picker-suggestions-list" role="listbox">
            {suggestions.length > 0 ? (
              suggestions.map((s) => (
                <button
                  type="button"
                  key={s.id}
                  className="picker-suggestion-item"
                  onClick={() => handleSelectSuggestion(s)}
                >
                  <MapPin size={15} className="item-icon" />
                  <div className="item-text">
                    <strong>{s.title}</strong>
                    {s.subtitle && <small>{s.subtitle}</small>}
                  </div>
                </button>
              ))
            ) : !suggestLoading && searchText.trim().length >= 2 ? (
              <div className="no-suggestion">
                Không tìm thấy địa điểm. Hãy thử nhập tên đường hoặc khu vực gần đó.
              </div>
            ) : null}
          </div>
        )}
      </div>

      {/* Map Container */}
      <div className="picker-map-container">
        <MapContainer
          center={currentPos}
          zoom={15}
          scrollWheelZoom={true}
          className="picker-leaflet-map"
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <MapController center={mapCenter} />
          <MapClickHandler onClick={(lat, lon) => handlePointSelect(lat, lon)} />

          <Marker
            position={currentPos}
            draggable={true}
            eventHandlers={{ dragend: handleMarkerDragEnd }}
            ref={markerRef}
            icon={pickerIcon}
          />
        </MapContainer>
      </div>

      {/* Location Status & Reverse Geocoded Address Box */}
      <div className="picker-footer-info">
        <div className="coords-display">
          <span>
            Tọa độ: <strong>{latitude?.toFixed(6) || '21.005500'}</strong>,{' '}
            <strong>{longitude?.toFixed(6) || '105.843300'}</strong>
          </span>
        </div>

        {reverseAddress && (
          <div className="reverse-address-box">
            <div className="address-content">
              <small>Địa chỉ nhận diện từ bản đồ:</small>
              <strong>{reverseAddress}</strong>
            </div>
            {address !== reverseAddress && (
              <button
                type="button"
                className="button outline small apply-addr-btn"
                onClick={applyReverseAddress}
              >
                <Check size={14} /> Điền vào ô địa chỉ
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
