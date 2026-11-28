import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import RoomDetailPage from './RoomDetailPage';

const { api, notify, toggleSave } = vi.hoisted(() => ({
  api: vi.fn(),
  notify: vi.fn(),
  toggleSave: vi.fn(),
}));

vi.mock('../context/AppContext', () => ({
  useApp: () => ({
    user: { id: 'u1', fullName: 'Người dùng test', role: 'STUDENT' },
    saved: [],
    notify,
    toggleSave,
  }),
}));

vi.mock('../lib/api', async (importOriginal) => {
  const actual = await importOriginal();
  return { ...actual, api };
});

vi.mock('../components/RoomMap', () => ({
  default: () => <div data-testid="room-map">Map</div>,
}));

describe('RoomDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('hiển thị cảnh báo giảm thứ hạng đề xuất và các huy hiệu trọng số đánh giá', async () => {
    const mockRoom = {
      id: 'room-123',
      title: 'Phòng trọ giá rẻ cạnh ĐHBK',
      description: 'Phòng trọ đầy đủ tiện nghi',
      address: 'Số 1 Đại Cồ Việt, Hai Bà Trưng, Hà Nội',
      latitude: 21.005,
      longitude: 105.845,
      basePrice: 3000000,
      electricPrice: 4000,
      waterPrice: 30000,
      internetPrice: 100000,
      otherFee: 50000,
      area: 25,
      maxPeople: 2,
      amenities: ['wifi', 'ac'],
      images: [],
      status: 'AVAILABLE',
      expiresAt: new Date(Date.now() + 86400000).toISOString(),
      createdAt: new Date().toISOString(),
      landlord: { id: 'landlord-1', fullName: 'Nguyễn Văn Chủ', phoneNumber: '0912345678' },
      university: { id: 'uni-1', name: 'Đại học Bách Khoa Hà Nội', shortName: 'HUST' },
      distanceToUniversityKm: 0.5,
      averageRating: 2.3,
      reviewCount: 2,
      isDemoted: true,
      demotionReason: 'Phòng có điểm đánh giá trung bình thấp (< 2.5 sao)',
      reviews: [
        {
          id: 'rev-1',
          rating: 4,
          comment: 'Phòng ở được, vị trí thuận tiện.',
          createdAt: new Date().toISOString(),
          weight: 2.0,
          user: { fullName: 'Trần Sinh Viên', isEduVerified: true },
        },
        {
          id: 'rev-2',
          rating: 1,
          comment: 'Phòng ẩm mốc và ồn ào.',
          createdAt: new Date().toISOString(),
          weight: 1.0,
          user: { fullName: 'Lê Người Dùng', isEduVerified: false },
        },
      ],
    };

    api.mockResolvedValueOnce(mockRoom);

    render(
      <MemoryRouter initialEntries={['/rooms/room-123']}>
        <Routes>
          <Route path="/rooms/:id" element={<RoomDetailPage />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Phòng trọ giá rẻ cạnh ĐHBK')).toBeInTheDocument();
    });

    expect(screen.getByText(/Lưu ý về thứ hạng đề xuất/i)).toBeInTheDocument();
    expect(screen.getByText(/phòng có điểm đánh giá trung bình thấp/i)).toBeInTheDocument();

    expect(
      screen.getByText(/Điểm số có trọng số: Sinh viên xác thực 2.0x · Tài khoản thường 1.0x/i)
    ).toBeInTheDocument();

    expect(screen.getByText(/Sinh viên xác thực · Trọng số 2x/i)).toBeInTheDocument();

    expect(screen.getByText(/Tài khoản thường · Trọng số 1x/i)).toBeInTheDocument();
  });
});
