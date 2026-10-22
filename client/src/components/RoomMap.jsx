import {
  MapContainer,
  TileLayer,
  Marker,
  Popup,
  Circle,
  useMap,
  useMapEvents,
} from 'react-leaflet';
import L from 'leaflet';
import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { shortPrice } from '../lib/api';
import 'leaflet/dist/leaflet.css';

function Fit({ rooms, center }) {
  const map = useMap();
  useEffect(() => {
    map.invalidateSize({ animate: false, pan: false });
    if (center) map.setView(center, 14, { animate: false });
    else if (rooms.length)
      map.fitBounds(
        rooms.map((room) => [room.latitude, room.longitude]),
        { padding: [45, 45], maxZoom: 15, animate: false }
      );
  }, [rooms, center, map]);
  return null;
}

function MapInteraction({ onSelectPoint }) {
  useMapEvents({
    click(event) {
      onSelectPoint?.(event.latlng.lat, event.latlng.lng);
    },
  });
  return null;
}

export default function RoomMap({ rooms, center, radius, onSelectPoint, onSelectRoom }) {
  return (
    <MapContainer
      center={center || [21.022, 105.819]}
      zoom={13}
      scrollWheelZoom={false}
      className="room-map"
      aria-label="Bản đồ phòng trọ"
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <Fit rooms={rooms} center={center} />
      {onSelectPoint && <MapInteraction onSelectPoint={onSelectPoint} />}
      {center && (
        <Circle
          center={center}
          radius={(radius || 1) * 1000}
          pathOptions={{ color: '#4f46e5', fillColor: '#6366f1', fillOpacity: 0.07, weight: 1.5 }}
        />
      )}
      {rooms.map((room) => (
        <Marker
          key={room.id}
          position={[room.latitude, room.longitude]}
          icon={L.divIcon({
            className: 'price-marker',
            html: '<span>' + shortPrice(room.basePrice) + ' tr</span>',
            iconSize: [70, 32],
            iconAnchor: [35, 16],
          })}
        >
          <Popup>
            <strong>{room.title}</strong>
            <p>{room.address}</p>
            {onSelectRoom ? (
              <button className="map-room-link" onClick={() => onSelectRoom(room)}>
                Xem chi tiết →
              </button>
            ) : (
              <Link to={'/rooms/' + room.id}>Xem chi tiết →</Link>
            )}
          </Popup>
        </Marker>
      ))}
    </MapContainer>
  );
}
