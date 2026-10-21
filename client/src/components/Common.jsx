import { useEffect, useRef } from 'react';
import { X, SearchX, ArrowRight, LoaderCircle } from 'lucide-react';
import { Link, useLocation } from 'react-router-dom';
export function Loading({ label = 'Đang tải dữ liệu…' }) {
  return (
    <div className="loading" role="status">
      <LoaderCircle className="spin" />
      <span>{label}</span>
    </div>
  );
}
export function Empty({
  title = 'Chưa tìm thấy phòng phù hợp',
  text = 'Thử thay đổi bộ lọc để có thêm lựa chọn nhé.',
  children,
}) {
  return (
    <div className="empty">
      <span className="empty-icon">
        <SearchX size={30} />
      </span>
      <h2>{title}</h2>
      <p>{text}</p>
      {children}
    </div>
  );
}
export function ErrorState({ message, retry }) {
  return (
    <div className="error-state" role="alert">
      <h3>Chưa thể tải thông tin</h3>
      <p>{message}</p>
      {retry && (
        <button className="button outline" onClick={retry}>
          Thử lại
        </button>
      )}
    </div>
  );
}
export function Modal({ title, children, onClose }) {
  const ref = useRef(null);
  useEffect(() => {
    const before = document.activeElement;
    const overflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    ref.current?.focus();
    function key(e) {
      if (e.key === 'Escape') onClose();
      if (e.key === 'Tab') {
        const items = ref.current?.querySelectorAll(
          'a[href], button, input, select, textarea, [tabindex="0"]'
        );
        if (!items?.length) return;
        const first = items[0],
          last = items[items.length - 1];
        if (e.shiftKey && document.activeElement === first) {
          e.preventDefault();
          last.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    }
    document.addEventListener('keydown', key);
    return () => {
      document.body.style.overflow = overflow;
      document.removeEventListener('keydown', key);
      before?.focus();
    };
  }, [onClose]);
  return (
    <div
      className="modal-backdrop"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <section
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
        tabIndex={-1}
        ref={ref}
      >
        <div className="modal-heading">
          <h2 id="modal-title">{title}</h2>
          <button className="icon-button" aria-label="Đóng" onClick={onClose}>
            <X />
          </button>
        </div>
        {children}
      </section>
    </div>
  );
}
export function PageTitle({ eyebrow, title, text, children }) {
  return (
    <div className="page-title">
      <div>
        {eyebrow && <span className="eyebrow">{eyebrow}</span>}
        <h1>{title}</h1>
        {text && <p>{text}</p>}
      </div>
      {children}
    </div>
  );
}
export function ScrollTop() {
  const { pathname } = useLocation();
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);
  return null;
}
export function LoginPrompt() {
  const location = useLocation();
  return (
    <div className="login-prompt">
      <h3>Lưu lại những điều bạn quan tâm</h3>
      <p>Đăng nhập để lưu phòng, đặt lịch và chia sẻ trải nghiệm của bạn.</p>
      <Link to="/login" state={{ from: location.pathname }} className="button primary">
        Đăng nhập để tiếp tục <ArrowRight size={16} />
      </Link>
    </div>
  );
}
