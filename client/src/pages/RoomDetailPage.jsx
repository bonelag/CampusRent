import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useParams, useLocation } from 'react-router-dom';
import {
  ArrowLeft,
  ArrowUpRight,
  MapPin,
  Heart,
  Share2,
  Star,
  Scan,
  UsersRound,
  GraduationCap,
  Check,
  CalendarDays,
  Phone,
  Flag,
  ShieldCheck,
  Zap,
  Droplets,
  Wifi,
  Plus,
  Minus,
  Image,
} from 'lucide-react';
import { api, money, amenities, estimateCost, fallbackImage, mediaUrl } from '../lib/api';
import { useApp } from '../context/AppContext';
import { Loading, ErrorState, Modal } from '../components/Common';
import RoomMap from '../components/RoomMap';
export default function RoomDetailPage() {
  const { id } = useParams(),
    navigate = useNavigate(),
    location = useLocation();
  const { user, saved, toggleSave, notify } = useApp();
  const [room, setRoom] = useState(null),
    [error, setError] = useState(''),
    [reload, setReload] = useState(0),
    [kwh, setKwh] = useState(80),
    [water, setWater] = useState(4),
    [modal, setModal] = useState(''),
    [busy, setBusy] = useState(false),
    [rating, setRating] = useState(5),
    [photo, setPhoto] = useState(0);
  const close = useCallback(() => setModal(''), []);
  useEffect(() => {
    setRoom(null);
    setError('');
    const controller = new AbortController();
    api('/rooms/' + id, { signal: controller.signal })
      .then(setRoom)
      .catch((e) => {
        if (!controller.signal.aborted) setError(e.message);
      });
    return () => controller.abort();
  }, [id, reload]);
  function needAuth(action) {
    if (!user) navigate('/login', { state: { from: location.pathname } });
    else action();
  }
  async function save() {
    needAuth(async () => {
      try {
        await toggleSave(room);
      } catch (e) {
        notify(e.message, 'error');
      }
    });
  }
  async function submit(e, path, body, message) {
    e.preventDefault();
    setBusy(true);
    try {
      await api(path, { method: 'POST', body });
      notify(message);
      setModal('');
      setReload((v) => v + 1);
    } catch (err) {
      notify(err.message, 'error');
    } finally {
      setBusy(false);
    }
  }
  if (error)
    return (
      <div className="container page-shell">
        <Link className="back-link" to="/">
          <ArrowLeft size={16} /> Quay lại tìm phòng
        </Link>
        <ErrorState message={error} retry={() => setReload(reload + 1)} />
      </div>
    );
  if (!room) return <Loading />;
  const total = estimateCost(room, kwh, water),
    isSaved = saved.some((r) => r.id === id),
    images = (room.images.length ? room.images : [fallbackImage]).map(mediaUrl);
  return (
    <div className="container detail-page">
      <div className="detail-top">
        <Link className="back-link" to="/">
          <ArrowLeft size={16} /> Khám phá phòng trọ
        </Link>
        <div>
          <button
            className="text-button"
            onClick={async () => {
              try {
                await navigator.clipboard.writeText(window.location.href);
                notify('Đã sao chép liên kết.');
              } catch {
                notify('Không thể sao chép liên kết trên trình duyệt này.', 'error');
              }
            }}
          >
            <Share2 size={16} /> Chia sẻ
          </button>
          <button className={'text-button ' + (isSaved ? 'teal' : '')} onClick={save}>
            <Heart size={17} fill={isSaved ? 'currentColor' : 'none'} />
            {isSaved ? 'Đã lưu' : 'Lưu phòng'}
          </button>
        </div>
      </div>
      <div className="detail-title">
        <span className="eyebrow">MỘT KHỞI ĐẦU MỚI ĐANG CHỜ BẠN</span>
        <h1>{room.title}</h1>
        <p>
          <MapPin size={16} />
          {room.address}
        </p>
      </div>
      <div className="gallery">
        <button
          className="gallery-main"
          onClick={() => {
            setPhoto(0);
            setModal('gallery');
          }}
          aria-label="Xem ảnh phòng"
        >
          <img
            src={images[0]}
            alt={room.title}
            onError={(e) => {
              e.currentTarget.src = fallbackImage;
            }}
          />
          <span>
            <Image size={16} /> Xem {images.length} ảnh
          </span>
        </button>
        <div className="gallery-secondary">
          <div className="gallery-photo">
            <img
              src={images[1] || images[0]}
              alt="Không gian bên trong phòng"
              onError={(e) => {
                e.currentTarget.src = fallbackImage;
              }}
            />
          </div>
          <div className="gallery-note">
            <GraduationCap size={28} />
            <h3>
              Ít thời gian đi đường.
              <br />
              Nhiều thời gian cho bạn.
            </h3>
            <p>
              {room.university
                ? room.university.name
                : 'Tìm một nơi ở phù hợp để tận hưởng cuộc sống sinh viên.'}
            </p>
            {room.distanceToUniversityKm != null && (
              <span>
                Cách trường {room.distanceToUniversityKm} km <ArrowUpRight size={16} />
              </span>
            )}
          </div>
        </div>
      </div>
      <div className="detail-columns">
        <div className="detail-content">
          <section className="detail-section">
            <div className="detail-specs">
              <span>
                <Scan />
                <strong>{room.area} m²</strong>
                <small>Diện tích phòng</small>
              </span>
              <span>
                <UsersRound />
                <strong>{room.maxPeople} người</strong>
                <small>Số người tối đa</small>
              </span>
              <span>
                <Star />
                <strong>{room.reviewCount ? room.averageRating + ' / 5' : 'Chưa có'}</strong>
                <small>{room.reviewCount} đánh giá</small>
              </span>
            </div>
            <h2>Một chút về căn phòng</h2>
            <p className="description">{room.description}</p>
          </section>
          <section className="detail-section">
            <h2>Đủ đầy những điều bạn cần</h2>
            <div className="amenities-grid">
              {room.amenities.map((a) => (
                <span key={a}>
                  <Check size={16} />
                  {amenities[a] || a}
                </span>
              ))}
            </div>
          </section>
          <section className="detail-section calculator" id="cost">
            <div className="section-title">
              <div>
                <span className="eyebrow">CHI PHÍ MINH BẠCH</span>
                <h2>Mỗi tháng, bạn sẽ cần bao nhiêu?</h2>
              </div>
              <span className="calculator-icon">
                <Zap size={24} />
              </span>
            </div>
            <p>Điều chỉnh theo thói quen của bạn. Mọi khoản phí đều ở đây.</p>
            <div className="usage-inputs">
              <label>
                <span>
                  <Zap size={15} /> Điện dự kiến <strong>{kwh} kWh</strong>
                </span>
                <input
                  aria-label="Điện dự kiến"
                  type="range"
                  min="0"
                  max="300"
                  step="5"
                  value={kwh}
                  onChange={(e) => setKwh(Number(e.target.value))}
                />
                <small>{money(room.electricPrice)} / kWh</small>
              </label>
              <label>
                <span>
                  <Droplets size={15} /> Nước dự kiến <strong>{water} m³</strong>
                </span>
                <input
                  aria-label="Nước dự kiến"
                  type="range"
                  min="0"
                  max="20"
                  value={water}
                  onChange={(e) => setWater(Number(e.target.value))}
                />
                <small>{money(room.waterPrice)} / m³</small>
              </label>
            </div>
            <div className="cost-lines">
              <span>
                Tiền thuê phòng<strong>{money(room.basePrice)}</strong>
              </span>
              <span>
                Tiền điện · {kwh} kWh<strong>{money(room.electricPrice * kwh)}</strong>
              </span>
              <span>
                Tiền nước · {water} m³<strong>{money(room.waterPrice * water)}</strong>
              </span>
              <span>
                Internet<strong>{money(room.internetPrice)}</strong>
              </span>
              <span>
                Phí dịch vụ khác<strong>{money(room.otherFee)}</strong>
              </span>
            </div>
            <div className="cost-total">
              <span>
                Tổng dự kiến / tháng<small>Chưa bao gồm tiền đặt cọc và chi tiêu cá nhân</small>
              </span>
              <strong data-testid="estimated-total">{money(total)}</strong>
            </div>
            <p className="fine-print">
              Đây là dự toán theo đơn giá chủ trọ cung cấp. Hãy xác nhận lại khi xem phòng.
            </p>
          </section>
          <section className="detail-section">
            <h2>Vị trí căn phòng</h2>
            <p className="muted">{room.address}</p>
            <div className="detail-map">
              <RoomMap rooms={[{ ...room, coverImage: images[0] }]} />
            </div>
          </section>
          <section className="detail-section reviews-section">
            <div className="section-title">
              <h2>
                <Star size={21} />{' '}
                {room.reviewCount
                  ? room.averageRating + ' · ' + room.reviewCount + ' đánh giá'
                  : 'Trải nghiệm từ cộng đồng'}
              </h2>
              <button
                className="button outline small"
                onClick={() => needAuth(() => setModal('review'))}
              >
                Viết đánh giá
              </button>
            </div>
            {room.reviews.length === 0 ? (
              <p className="muted">Chưa có đánh giá. Hãy chia sẻ trải nghiệm đầu tiên của bạn.</p>
            ) : (
              room.reviews.map((review) => (
                <article className="review" key={review.id}>
                  <div className="review-heading">
                    <span className="avatar">{review.user.fullName.charAt(0)}</span>
                    <div>
                      <strong>{review.user.fullName}</strong>
                      <small>
                        {new Date(review.createdAt).toLocaleDateString('vi-VN')}
                        {review.user.isEduVerified && (
                          <span className="verified">
                            <ShieldCheck size={12} /> Sinh viên đã xác thực
                          </span>
                        )}
                      </small>
                    </div>
                    <span className="review-stars">
                      {'★'.repeat(review.rating)}
                      {'☆'.repeat(5 - review.rating)}
                    </span>
                    <button
                      className="icon-button"
                      aria-label={'Báo cáo đánh giá của ' + review.user.fullName}
                      onClick={() => needAuth(() => setModal('report-review:' + review.id))}
                    >
                      <Flag size={14} />
                    </button>
                  </div>
                  <p>{review.comment}</p>
                </article>
              ))
            )}
          </section>
        </div>
        <aside className="booking-sidebar">
          <div className="booking-card">
            <span className="available-label">
              <span />
              {room.status === 'AVAILABLE' ? 'Phòng đang sẵn sàng' : 'Tin đã ngừng cho thuê'}
            </span>
            <div className="detail-price">
              {money(room.basePrice)}
              <span>/ tháng</span>
            </div>
            <div className="booking-divider" />
            <div className="landlord">
              <span className="avatar">{room.landlord.fullName.charAt(0)}</span>
              <div>
                <strong>{room.landlord.fullName}</strong>
                <small>Chủ phòng trọ</small>
              </div>
              <ShieldCheck size={20} />
            </div>
            {room.landlord.phoneNumber && (
              <a className="button outline full" href={'tel:' + room.landlord.phoneNumber}>
                <Phone size={17} />
                {room.landlord.phoneNumber}
              </a>
            )}
            <button
              className="button primary full"
              disabled={room.status !== 'AVAILABLE'}
              onClick={() => needAuth(() => setModal('booking'))}
            >
              <CalendarDays size={17} /> Đặt lịch xem phòng <ArrowUpRight size={17} />
            </button>
            <p className="booking-hint">
              Xem phòng trước, quyết định sau.
              <br />
              Đặt lịch hoàn toàn miễn phí.
            </p>
            <div className="booking-safety">
              <ShieldCheck size={18} />
              <p>Trực tiếp xem phòng và kiểm tra hợp đồng trước khi đặt cọc.</p>
            </div>
          </div>
          <button
            className="text-button report-link"
            onClick={() => needAuth(() => setModal('report'))}
          >
            <Flag size={14} /> Báo cáo tin không chính xác
          </button>
        </aside>
      </div>
      {modal === 'gallery' && (
        <Modal title="Không gian căn phòng" onClose={close}>
          <img className="lightbox-image" src={images[photo]} alt={'Ảnh phòng ' + (photo + 1)} />
          <div className="gallery-thumbnails">
            {images.map((img, i) => (
              <button
                key={i}
                onClick={() => setPhoto(i)}
                aria-label={'Ảnh ' + (i + 1)}
                className={photo === i ? 'active' : ''}
              >
                <img src={img} alt="" />
              </button>
            ))}
          </div>
        </Modal>
      )}
      {modal === 'booking' && (
        <Modal title="Hẹn một ngày xem phòng" onClose={close}>
          <p className="muted">Gửi lịch bạn mong muốn. Chủ trọ sẽ xác nhận trong mục Lịch hẹn.</p>
          <form
            className="form"
            onSubmit={(e) => {
              const data = new FormData(e.currentTarget);
              submit(
                e,
                '/rooms/' + id + '/bookings',
                { visitAt: new Date(data.get('visitAt')).toISOString(), note: data.get('note') },
                'Đã gửi yêu cầu xem phòng.'
              );
            }}
          >
            <label>
              Ngày và giờ xem phòng
              <input
                name="visitAt"
                type="datetime-local"
                required
                min={localDate(2)}
                max={localDate(24 * 30)}
              />
            </label>
            <label>
              Lời nhắn cho chủ trọ
              <textarea
                name="note"
                maxLength="1000"
                placeholder="Ví dụ: Mình muốn xem phòng cùng một người bạn…"
              />
            </label>
            <button className="button primary full" disabled={busy}>
              {busy ? 'Đang gửi…' : 'Gửi yêu cầu xem phòng'}
            </button>
          </form>
        </Modal>
      )}
      {modal === 'review' && (
        <Modal title="Chia sẻ trải nghiệm của bạn" onClose={close}>
          <form
            className="form"
            onSubmit={(e) =>
              submit(
                e,
                '/rooms/' + id + '/reviews',
                { rating, comment: new FormData(e.currentTarget).get('comment') },
                'Đã gửi đánh giá. Nội dung vi phạm sẽ chờ kiểm duyệt.'
              )
            }
          >
            <div className="star-picker" role="group" aria-label="Chọn số sao">
              {[1, 2, 3, 4, 5].map((n) => (
                <button
                  type="button"
                  key={n}
                  aria-label={n + ' sao'}
                  aria-pressed={rating === n}
                  onClick={() => setRating(n)}
                >
                  <Star fill={n <= rating ? 'currentColor' : 'none'} />
                </button>
              ))}
            </div>
            <label>
              Đánh giá của bạn
              <textarea
                name="comment"
                required
                minLength="2"
                maxLength="3000"
                placeholder="Điều gì khiến bạn ấn tượng về căn phòng này?"
              />
            </label>
            <button className="button primary full" disabled={busy}>
              {busy ? 'Đang gửi…' : 'Gửi đánh giá'}
            </button>
          </form>
        </Modal>
      )}
      {modal.startsWith('report') && (
        <Modal title="Báo cáo nội dung" onClose={close}>
          <form
            className="form"
            onSubmit={(e) => {
              const data = new FormData(e.currentTarget);
              submit(
                e,
                '/reports',
                {
                  ...(modal.startsWith('report-review:')
                    ? { reviewId: modal.split(':')[1] }
                    : { roomId: id }),
                  reason: data.get('reason'),
                  description: data.get('description'),
                },
                'Đã gửi báo cáo. Cảm ơn bạn đã giúp cộng đồng.'
              );
            }}
          >
            <label>
              Lý do báo cáo
              <select name="reason">
                <option>Thông tin không chính xác</option>
                <option>Ảnh không đúng thực tế</option>
                <option>Nghi ngờ lừa đảo</option>
                <option>Nội dung không phù hợp</option>
              </select>
            </label>
            <label>
              Thông tin bổ sung
              <textarea
                name="description"
                maxLength="3000"
                placeholder="Mô tả để quản trị viên có thể kiểm tra…"
              />
            </label>
            <button className="button primary full" disabled={busy}>
              {busy ? 'Đang gửi…' : 'Gửi báo cáo'}
            </button>
          </form>
        </Modal>
      )}
    </div>
  );
}
function localDate(hours) {
  const date = new Date(Date.now() + hours * 3600000);
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
}
