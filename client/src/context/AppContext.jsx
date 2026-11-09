import { createContext, useContext, useEffect, useState, useCallback } from 'react';
import { api } from '../lib/api';
const Context = createContext(null);
export function AppProvider({ children }) {
  const [user, setUser] = useState(null);
  const [authLoading, setAuthLoading] = useState(!!sessionStorage.getItem('campusrent-token'));
  const [saved, setSaved] = useState([]);
  const [toast, setToast] = useState(null);
  const notify = useCallback(
    (message, type = 'success') => setToast({ message, type, id: Date.now() }),
    []
  );
  useEffect(() => {
    if (toast) {
      const timer = setTimeout(() => setToast(null), 4500);
      return () => clearTimeout(timer);
    }
  }, [toast]);
  const refreshSaved = useCallback(async () => {
    if (sessionStorage.getItem('campusrent-token')) setSaved(await api('/favorites'));
    else setSaved([]);
  }, []);
  const refreshUser = useCallback(async () => {
    if (!sessionStorage.getItem('campusrent-token')) return null;
    const next = await api('/auth/me');
    setUser(next);
    return next;
  }, []);
  useEffect(() => {
    const refresh = () => refreshUser().catch(() => {});
    window.addEventListener('focus', refresh);
    return () => window.removeEventListener('focus', refresh);
  }, [refreshUser]);
  useEffect(() => {
    const clear = () => {
      setUser(null);
      setSaved([]);
    };
    window.addEventListener('auth-expired', clear);
    if (sessionStorage.getItem('campusrent-token'))
      api('/auth/me')
        .then(async (u) => {
          setUser(u);
          await refreshSaved();
        })
        .catch(() => {})
        .finally(() => setAuthLoading(false));
    return () => window.removeEventListener('auth-expired', clear);
  }, [refreshSaved]);
  async function authenticate(path, body) {
    const result = await api('/auth/' + path, { method: 'POST', body });
    sessionStorage.setItem('campusrent-token', result.token);
    setUser(result.user);
    await refreshSaved().catch(() => setSaved([]));
    return result.user;
  }
  async function changePassword(body) {
    const result = await api('/auth/password', { method: 'PUT', body });
    sessionStorage.setItem('campusrent-token', result.token);
    setUser(result.user);
  }
  function logout() {
    sessionStorage.removeItem('campusrent-token');
    setUser(null);
    setSaved([]);
    notify('Bạn đã đăng xuất.');
  }
  async function toggleSave(room) {
    const exists = saved.some((r) => r.id === room.id);
    await api('/favorites/' + room.id, { method: exists ? 'DELETE' : 'PUT' });
    await refreshSaved();
    notify(exists ? 'Đã bỏ lưu phòng.' : 'Đã lưu vào danh sách yêu thích.');
  }
  return (
    <Context.Provider
      value={{
        user,
        setUser,
        authLoading,
        saved,
        refreshSaved,
        refreshUser,
        changePassword,
        authenticate,
        logout,
        toggleSave,
        notify,
      }}
    >
      {children}
      {toast && (
        <div
          className={'toast ' + toast.type}
          role={toast.type === 'error' ? 'alert' : 'status'}
          key={toast.id}
        >
          {toast.message}
          <button aria-label="Đóng thông báo" onClick={() => setToast(null)}>
            ×
          </button>
        </div>
      )}
    </Context.Provider>
  );
}
export const useApp = () => useContext(Context);
