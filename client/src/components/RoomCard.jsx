import { Link, useNavigate, useLocation } from 'react-router-dom';
import { Heart, MapPin, Star, Navigation, ShieldAlert, Sparkles } from 'lucide-react';
import { useState } from 'react';
import { useApp } from '../context/AppContext';
import { amenities as amenityLabels, money, fallbackImage, mediaUrl } from '../lib/api';
export default function RoomCard({ room, horizontal = false }) {
  const { saved, user, toggleSave, notify } = useApp();
  const [busy, setBusy] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const isSaved = saved.some((r) => r.id === room.id);
  const detailState = { backgroundLocation: location };
  async function save() {
    if (!user)
      return navigate('/login', {
        state: { from: location.pathname + location.search, backgroundLocation: location },
      });
    setBusy(true);
    try {
      await toggleSave(room);
    } catch (e) {
      notify(e.message, 'error');
    } finally {
      setBusy(false);
    }
  }
  return (
    <article className={'room-card ' + (horizontal ? 'horizontal-card' : '')}>
      <Link
        className="card-image"
        to={'/rooms/' + room.id}
        state={detailState}
        tabIndex={-1}
        aria-hidden="true"
      >
        <img
          src={mediaUrl(room.coverImage)}
          alt=""
          loading="lazy"
          decoding="async"
          onError={(e) => {
            e.currentTarget.onerror = null;
            e.currentTarget.src = fallbackImage;
          }}
        />
        {room.isLowRated ? (
          <span className="card-quality-pill is-low">
            <ShieldAlert size={11} /> Đánh giá thấp
          </span>
        ) : (
          room.reviewCount > 0 &&
          room.averageRating >= 4.3 && (
            <span className="card-quality-pill is-top">
              <Sparkles size={11} /> Uy tín cao
            </span>
          )
        )}
      </Link>
      <div className="card-body">
        <div className="card-topline">
          <span className="distance-badge">
            <Navigation size={10} />
            {room.distanceKm != null ? room.distanceKm + ' km đến tâm tìm kiếm' : 'Còn phòng'}
            <span className="distance-type">{room.distanceKm != null ? ' · chim bay' : ''}</span>
          </span>
          {room.isDemoted && (
            <span
              className="demoted-badge"
              title={room.demotionReason || 'Phòng nhận nhiều đánh giá thấp'}
            >
              Giảm đề xuất
            </span>
          )}
          <span className="rating">
            <Star size={12} fill="currentColor" />
            {room.reviewCount > 0 ? room.averageRating : 'Mới'}
          </span>
          <button
            className={'save-button ' + (isSaved ? 'saved' : '')}
            aria-label={(isSaved ? 'Bỏ lưu ' : 'Lưu ') + room.title}
            aria-pressed={isSaved}
            disabled={busy}
            onClick={save}
          >
            <Heart size={14} fill={isSaved ? 'currentColor' : 'none'} />
          </button>
        </div>
        <h3>
          <Link to={'/rooms/' + room.id} state={detailState}>
            {room.title}
          </Link>
        </h3>
        <p className="card-address">
          <MapPin size={12} />
          {room.address}
        </p>
        {horizontal && room.amenities?.length > 0 && (
          <div className="card-amenities-preview" aria-label="Tiện ích nổi bật">
            {room.amenities.slice(0, 3).map((item) => (
              <span key={item}>{amenityLabels[item] || item}</span>
            ))}
          </div>
        )}
        <div className="card-bottom">
          <span className="card-price">
            <strong>{money(room.basePrice)}</strong>
            <small>/tháng</small>
          </span>
          <span className="room-specs">
            <span>{room.area} m²</span>
            <span>• {room.maxPeople} người</span>
            {room.distanceKm != null && (
              <span className="distance sr-only">{room.distanceKm} km</span>
            )}
          </span>
        </div>
      </div>
    </article>
  );
}
