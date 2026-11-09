const BASE = import.meta.env.VITE_API_BASE_URL || '/api';
export async function api(path, options = {}) {
  const token = sessionStorage.getItem('campusrent-token');
  const headers = {
    ...(options.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }),
    ...(token ? { Authorization: 'Bearer ' + token } : {}),
    ...options.headers,
  };
  let response;
  try {
    response = await fetch(BASE + path, {
      ...options,
      headers,
      body:
        options.body instanceof FormData
          ? options.body
          : options.body
            ? JSON.stringify(options.body)
            : undefined,
    });
  } catch (error) {
    if (error?.name === 'AbortError') throw error;
    throw new Error('Không thể kết nối máy chủ. Vui lòng thử lại.');
  }
  if (response.status === 204) return null;
  const result = await response.json().catch(() => ({}));
  if (!response.ok) {
    if (response.status === 401) {
      sessionStorage.removeItem('campusrent-token');
      window.dispatchEvent(new Event('auth-expired'));
    }
    throw new Error(
      result.message ||
        Object.values(result.errors || {})
          .flat()
          .join(' ') ||
        (response.status === 429
          ? 'Bạn thao tác quá nhanh. Vui lòng thử lại sau một phút.'
          : 'Không thể thực hiện yêu cầu.')
    );
  }
  return result.data;
}

export async function fetchFile(path, { signal } = {}) {
  const origin = new URL(BASE, window.location.origin).origin;
  const url = new URL(path, origin);
  if (
    url.origin !== origin ||
    !/^\/(api\/uploads\/files\/|uploads\/(images|documents)\/)/.test(url.pathname)
  )
    throw new Error('Đường dẫn tệp không hợp lệ.');
  const token = sessionStorage.getItem('campusrent-token');
  let response;
  try {
    response = await fetch(url.href, {
      signal,
      headers: token ? { Authorization: 'Bearer ' + token } : {},
    });
  } catch (error) {
    if (error?.name === 'AbortError') throw error;
    throw new Error('Không thể tải tệp. Vui lòng thử lại.');
  }
  if (!response.ok) {
    if (response.status === 401) {
      sessionStorage.removeItem('campusrent-token');
      window.dispatchEvent(new Event('auth-expired'));
    }
    throw new Error(
      response.status === 403 ? 'Bạn không có quyền xem tệp này.' : 'Không thể tải tệp đính kèm.'
    );
  }
  return response.blob();
}

export async function downloadFile(path, fileName = 'tai-lieu') {
  const blob = await fetchFile(path);
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 60000);
}
export const money = (value) => new Intl.NumberFormat('vi-VN').format(value || 0) + ' ₫';
export const shortPrice = (value) =>
  new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(value / 1000000);
export const amenities = {
  wifi: 'Wi-Fi',
  ac: 'Điều hòa',
  heater: 'Nóng lạnh',
  balcony: 'Ban công',
  parking: 'Chỗ để xe',
  no_curfew: 'Giờ giấc tự do',
  elevator: 'Thang máy',
  cctv: 'Camera an ninh',
  kitchen: 'Bếp riêng',
  refrigerator: 'Tủ lạnh',
  security: 'Bảo vệ',
  washing_machine: 'Máy giặt',
};
export function estimateCost(room, kwh = 80, water = 4) {
  return (
    room.basePrice +
    room.electricPrice * kwh +
    room.waterPrice * water +
    room.internetPrice +
    room.otherFee
  );
}
export const fallbackImage = '/room-placeholder.svg';
export function mediaUrl(value) {
  if (!value) return fallbackImage;
  return value.startsWith('/uploads/')
    ? new URL(value, new URL(BASE, window.location.origin).origin).href
    : value;
}
