import { describe, it, expect, vi, afterEach } from 'vitest';
import { api, estimateCost, money, shortPrice, fetchFile } from './api';
afterEach(() => vi.unstubAllGlobals());
describe('Chi phí và định dạng', () => {
  const room = {
    basePrice: 3200000,
    electricPrice: 3800,
    waterPrice: 30000,
    internetPrice: 100000,
    otherFee: 50000,
  };
  it('tính đúng các khoản theo mặc định', () => expect(estimateCost(room)).toBe(3774000));
  it('cập nhật theo mức tiêu thụ', () => expect(estimateCost(room, 0, 0)).toBe(3350000));
  it('định dạng tiền Việt', () => {
    expect(money(3200000)).toContain('3.200.000');
    expect(shortPrice(3200000)).toBe('3,2');
  });
});
describe('Tệp xác minh', () => {
  it('tải tệp riêng bằng JWT', async () => {
    sessionStorage.setItem('campusrent-token', 'proof-token');
    const blob = new Blob(['document']);
    const fetch = vi.fn().mockResolvedValue({ ok: true, status: 200, blob: async () => blob });
    vi.stubGlobal('fetch', fetch);
    expect(await fetchFile('/api/uploads/files/owner/document.pdf')).toBe(blob);
    expect(fetch.mock.calls[0][1].headers.Authorization).toBe('Bearer proof-token');
  });
  it('không gửi JWT đến website khác qua liên kết tệp', async () => {
    const fetch = vi.fn();
    vi.stubGlobal('fetch', fetch);
    await expect(
      fetchFile('https://outside.example.com/uploads/documents/file.pdf')
    ).rejects.toThrow('Đường dẫn tệp không hợp lệ');
    expect(fetch).not.toHaveBeenCalled();
  });
});
describe('API client', () => {
  it('gửi JWT và đọc envelope', async () => {
    sessionStorage.setItem('campusrent-token', 'test');
    const fetch = vi
      .fn()
      .mockResolvedValue({ ok: true, status: 200, json: async () => ({ data: { id: 1 } }) });
    vi.stubGlobal('fetch', fetch);
    expect(await api('/auth/me')).toEqual({ id: 1 });
    expect(fetch.mock.calls[0][1].headers.Authorization).toBe('Bearer test');
  });
  it('hiện lỗi validation từ máy chủ', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: false,
        status: 400,
        json: async () => ({ errors: { Email: ['Email không hợp lệ'] } }),
      })
    );
    await expect(api('/auth/register')).rejects.toThrow('Email không hợp lệ');
  });
  it('xóa token hết hạn', async () => {
    sessionStorage.setItem('campusrent-token', 'expired');
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({ ok: false, status: 401, json: async () => ({}) })
    );
    await expect(api('/auth/me')).rejects.toThrow();
    expect(sessionStorage.getItem('campusrent-token')).toBeNull();
  });
  it('xử lý response 204', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, status: 204 }));
    expect(await api('/favorites/id', { method: 'DELETE' })).toBeNull();
  });
  it('báo lỗi kết nối', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline')));
    await expect(api('/rooms')).rejects.toThrow('Không thể kết nối');
  });
});
