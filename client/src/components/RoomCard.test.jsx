import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi, it, expect } from 'vitest';
import userEvent from '@testing-library/user-event';
import RoomCard from './RoomCard';
const toggleSave = vi.fn().mockResolvedValue(undefined);
vi.mock('../context/AppContext', () => ({
  useApp: () => ({ user: { id: 'u' }, saved: [], toggleSave, notify: vi.fn() }),
}));
const room = {
  id: 'room1',
  title: 'Studio gần Bách Khoa',
  address: 'Hà Nội',
  basePrice: 3200000,
  area: 25,
  maxPeople: 2,
  amenities: ['wifi', 'ac'],
  status: 'AVAILABLE',
  reviewCount: 1,
  averageRating: 5,
  distanceKm: 0.5,
};
it('hiển thị thông tin và liên kết đúng', () => {
  render(
    <MemoryRouter>
      <RoomCard room={room} />
    </MemoryRouter>
  );
  expect(screen.getByText('Studio gần Bách Khoa').closest('a')).toHaveAttribute(
    'href',
    '/rooms/room1'
  );
  expect(screen.getByText('25 m²')).toBeVisible();
  expect(screen.getByText('0.5 km')).toBeVisible();
});
it('nút lưu gọi API qua context', async () => {
  render(
    <MemoryRouter>
      <RoomCard room={room} />
    </MemoryRouter>
  );
  await userEvent.click(screen.getByRole('button', { name: 'Lưu Studio gần Bách Khoa' }));
  expect(toggleSave).toHaveBeenCalledWith(room);
});
