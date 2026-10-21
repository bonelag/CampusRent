import { MapContainer, TileLayer, Marker, Popup, Circle, useMap } from 'react-leaflet';
import L from 'leaflet';
import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { shortPrice } from '../lib/api';
import 'leaflet/dist/leaflet.css';
function Fit({ rooms, center }) {
  const map = useMap();
  useEffect(() => {
    map.invalidateSize();
    if (center) map.setView(center, 14);
    else if (rooms.length) map.fitBounds(rooms.map(r => [r.latitude, r.longitude]), { padding: [45, 45], maxZoom: 15 });
  }, [rooms, center, map]);
  return null;
}
export default function RoomMap({ rooms, center, radius }) {
  return <MapContainer center={center || [21.022, 105.819]} zoom={13} scrollWheelZoom={false} className="room-map" aria-label="Bản đồ phòng trọ"><TileLayer attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>' url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"/><Fit rooms={rooms} center={center}/>{center && <Circle center={center} radius={(radius || 1) * 1000} pathOptions={{ color: '#158675', fillOpacity: 0.06, weight: 1 }}/>}
  {rooms.map(r => <Marker key={r.id} position={[r.latitude,r.longitude]} icon={L.divIcon({ className: 'price-marker', html: '<span>' + shortPrice(r.basePrice) + ' tr</span>', iconSize: [70,32], iconAnchor: [35,16] })}><Popup><strong>{r.title}</strong><p>{r.address}</p><Link to={'/rooms/' + r.id}>Xem chi tiết →</Link></Popup></Marker>)}</MapContainer>;
}

