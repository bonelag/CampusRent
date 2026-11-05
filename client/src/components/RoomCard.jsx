import { Link, useNavigate, useLocation } from 'react-router-dom';
import { Heart, MapPin, Star, Navigation } from 'lucide-react';
import { useState } from 'react';
import { useApp } from '../context/AppContext';
import { money, fallbackImage } from '../lib/api';
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
          src={room.coverImage || fallbackImage}
          alt=""
          loading="lazy"
          onError={(e) => {
            e.currentTarget.onerror = null;
            e.currentTarget.src = fallbackImage;
          }}
        />
      </Link>
      <div className="card-body">
        <div className="card-topline">
          <span className="distance-badge">
            <Navigation size={10} />
            {room.distanceKm != null ? room.distanceKm + ' km đến tâm tìm kiếm' : 'Còn phòng'}
            <span className="distance-type">{room.distanceKm != null ? ' · chim bay' : ''}</span>
          </span>
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
