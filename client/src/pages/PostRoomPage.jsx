import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  ArrowLeft,
  ArrowUpRight,
  Camera,
  Check,
  MapPin,
  ImagePlus,
  X,
  House,
  Wallet,
  ListChecks,
} from 'lucide-react';
import { api, amenities, estimateCost, money } from '../lib/api';
import { useApp } from '../context/AppContext';
import { PageTitle, Loading, ErrorState } from '../components/Common';
import LocationPickerMap from '../components/LocationPickerMap';
const defaults = {
  title: '',
  description: '',
  address: '',
  universityId: '',
  latitude: 21.0055,
  longitude: 105.8433,
  basePrice: 3000000,
  electricPrice: 3800,
  waterPrice: 30000,
  internetPrice: 100000,
  otherFee: 50000,
  area: 25,
  maxPeople: 2,
  amenities: [],
  imageUrls: [],
};
export default function PostRoomPage() {
  const { id } = useParams(),
    navigate = useNavigate(),
    { notify } = useApp();
  const [form, setForm] = useState(defaults),
    [universities, setUniversities] = useState([]),
    [loading, setLoading] = useState(!!id),
    [loadError, setLoadError] = useState(''),
    [error, setError] = useState(''),
    [busy, setBusy] = useState(false),
    [uploading, setUploading] = useState(false),
    [url, setUrl] = useState('');
  useEffect(() => {
    api('/universities')
      .then(setUniversities)
      .catch((e) => setLoadError(e.message));
    if (id)
      api('/rooms/' + id)
        .then((r) => setForm({ ...r, imageUrls: r.images, universityId: r.universityId || '' }))
        .catch((e) => setLoadError(e.message))
        .finally(() => setLoading(false));
  }, [id]);
  function change(key, value) {
    setForm((p) => ({ ...p, [key]: value }));
  }
  function school(value) {
    const u = universities.find((u) => u.id === value);
    setForm((p) => ({
      ...p,
      universityId: value,
      ...(u ? { latitude: u.latitude, longitude: u.longitude } : {}),
    }));
  }
  function addImage() {
    if (!url.trim()) return;
    try {
      const parsed = new URL(url);
      if (parsed.protocol !== 'https:') throw new Error();
      if (form.imageUrls.length >= 10) return setError('Tối đa 10 ảnh.');
      change('imageUrls', [...form.imageUrls, url.trim()]);
      setUrl('');
      setError('');
    } catch {
      setError('Nhập đường dẫn ảnh HTTPS hợp lệ.');
    }
  }
  async function upload(e) {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size > 5 * 1024 * 1024) return setError('Ảnh cần nhỏ hơn 5 MB.');
    setUploading(true);
    setError('');
    try {
      const body = new FormData();
      body.append('file', file);
      const result = await api('/images', { method: 'POST', body });
      setForm((p) => ({ ...p, imageUrls: [...p.imageUrls, result.url].slice(0, 10) }));
    } catch (e) {
      setError(e.message);
    } finally {
      setUploading(false);
      e.target.value = '';
    }
  }
  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setError('');
    if (!form.imageUrls.length) {
      setError('Thêm ít nhất một ảnh phòng trước khi đăng.');
      setBusy(false);
      return;
    }
    try {
      const result = await api(id ? '/rooms/' + id : '/rooms', {
        method: id ? 'PUT' : 'POST',
        body: { ...form, universityId: form.universityId || null },
      });
      notify(id ? 'Đã cập nhật tin đăng.' : 'Tin đăng của bạn đã sẵn sàng!');
      navigate('/rooms/' + result.id);
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }
  const input = (key, label, props = {}) => (
    <label>
      {label}
      <input
        name={key}
        value={form[key]}
        onChange={(e) =>
          change(key, props.type === 'number' ? Number(e.target.value) : e.target.value)
        }
        {...props}
      />
    </label>
  );
  if (loading) return <Loading />;
  if (loadError)
    return (
      <div className="container page-shell">
        <ErrorState message={loadError} retry={() => window.location.reload()} />
      </div>
    );
  return (
    <div className="container page-shell post-page">
      <Link className="back-link" to="/my-rooms">
        <ArrowLeft size={15} /> Quản lý tin đăng
      </Link>
      <PageTitle
        eyebrow="KẾT NỐI MỘT CĂN PHÒNG, MỞ RA MỘT HÀNH TRÌNH"
        title={id ? 'Làm mới căn phòng của bạn.' : 'Chia sẻ một không gian tốt.'}
        text="Thông tin đầy đủ và rõ ràng giúp bạn tìm được người thuê phù hợp hơn."
      />
      <form className="post-layout" onSubmit={submit}>
        <div className="post-fields">
          <section className="panel form">
            <div className="form-section-title">
              <span>01</span>
              <div>
                <h2>Giới thiệu căn phòng</h2>
                <p>Những thông tin đầu tiên để người thuê hiểu về nơi ở.</p>
              </div>
              <House size={23} />
            </div>
            {input('title', 'Tiêu đề tin đăng', {
              required: true,
              maxLength: 255,
              placeholder: 'Ví dụ: Studio có ban công, cách Bách Khoa 500m',
            })}
            {input('address', 'Địa chỉ đầy đủ', {
              required: true,
              maxLength: 300,
              placeholder: 'Số nhà, ngõ, đường, quận, thành phố',
            })}
            <label>
              Trường đại học gần nhất
              <select
                name="universityId"
                value={form.universityId}
                onChange={(e) => school(e.target.value)}
              >
                <option value="">Chọn trường gần phòng</option>
                {universities.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.name}
                  </option>
                ))}
              </select>
            </label>

            <LocationPickerMap
              latitude={form.latitude}
              longitude={form.longitude}
              address={form.address}
              universities={universities}
              onChange={({ latitude, longitude, address: newAddress }) => {
                setForm((prev) => ({
                  ...prev,
                  latitude,
                  longitude,
                  ...(newAddress ? { address: newAddress } : {}),
                }));
              }}
            />

            <div className="form-row coords-manual-row">
              {input('latitude', 'Vĩ độ (Latitude)', {
                type: 'number',
                required: true,
                min: -90,
                max: 90,
                step: 'any',
              })}
              {input('longitude', 'Kinh độ (Longitude)', {
                type: 'number',
                required: true,
                min: -180,
                max: 180,
                step: 'any',
              })}
            </div>
            <div className="form-row">
              {input('area', 'Diện tích (m²)', {
                type: 'number',
                required: true,
                min: 1,
                max: 10000,
                step: 'any',
              })}
              {input('maxPeople', 'Số người tối đa', {
                type: 'number',
                required: true,
                min: 1,
                max: 100,
              })}
            </div>
            <label>
              Mô tả chi tiết
              <textarea
                name="description"
                required
                maxLength="10000"
                rows="5"
                value={form.description}
                onChange={(e) => change('description', e.target.value)}
                placeholder="Ánh sáng, không gian, khu vực xung quanh, quy định sinh hoạt…"
              />
            </label>
          </section>
          <section className="panel form">
            <div className="form-section-title">
              <span>02</span>
              <div>
                <h2>Chi phí rõ ràng</h2>
                <p>Công khai các khoản phí để người thuê yên tâm lựa chọn.</p>
              </div>
              <Wallet size={23} />
            </div>
            {input('basePrice', 'Tiền thuê phòng (₫ / tháng)', {
              type: 'number',
              required: true,
              min: 0,
              max: 1000000000,
            })}
            <div className="form-row">
              {input('electricPrice', 'Giá điện (₫ / kWh)', {
                type: 'number',
                required: true,
                min: 0,
                max: 1000000,
              })}
              {input('waterPrice', 'Giá nước (₫ / m³)', {
                type: 'number',
                required: true,
                min: 0,
                max: 1000000,
              })}
            </div>
            <div className="form-row">
              {input('internetPrice', 'Internet (₫ / tháng)', {
                type: 'number',
                required: true,
                min: 0,
                max: 10000000,
              })}
              {input('otherFee', 'Phí dịch vụ khác (₫ / tháng)', {
                type: 'number',
                required: true,
                min: 0,
                max: 10000000,
              })}
            </div>
          </section>
          <section className="panel form">
            <div className="form-section-title">
              <span>03</span>
              <div>
                <h2>Tiện ích & hình ảnh</h2>
                <p>Ảnh thật và tiện ích đúng giúp căn phòng nổi bật hơn.</p>
              </div>
              <Camera size={23} />
            </div>
            <div className="amenity-picker">
              {Object.entries(amenities).map(([key, label]) => (
                <label key={key}>
                  <input
                    type="checkbox"
                    checked={form.amenities.includes(key)}
                    onChange={() =>
                      change(
                        'amenities',
                        form.amenities.includes(key)
                          ? form.amenities.filter((a) => a !== key)
                          : [...form.amenities, key]
                      )
                    }
                  />
                  {label}
                </label>
              ))}
            </div>
            <div className="upload-zone">
              <ImagePlus size={28} />
              <h3>{uploading ? 'Đang tải ảnh…' : 'Cho người thuê thấy không gian của bạn'}</h3>
              <p>JPG, PNG hoặc WebP · Tối đa 5 MB / ảnh · Tối đa 10 ảnh</p>
              <label className="button outline small">
                Chọn ảnh từ máy
                <input
                  aria-label="Tải ảnh phòng"
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  disabled={uploading || form.imageUrls.length >= 10}
                  onChange={upload}
                />
              </label>
            </div>
            <div className="image-url-row">
              <input
                aria-label="Đường dẫn ảnh HTTPS"
                type="url"
                value={url}
                onChange={(e) => setUrl(e.target.value)}
                placeholder="Hoặc dán đường dẫn ảnh HTTPS"
              />
              <button
                className="button outline small"
                type="button"
                onClick={addImage}
                disabled={form.imageUrls.length >= 10}
              >
                Thêm ảnh
              </button>
            </div>
            <div className="post-images">
              {form.imageUrls.map((image, i) => (
                <div key={i}>
                  <img src={image} alt={'Ảnh phòng ' + (i + 1)} />
                  {i === 0 && <span>Ảnh bìa</span>}
                  <button
                    type="button"
                    className="icon-button"
                    aria-label={'Xóa ảnh ' + (i + 1)}
                    onClick={() =>
                      change(
                        'imageUrls',
                        form.imageUrls.filter((_, n) => n !== i)
                      )
                    }
                  >
                    <X size={15} />
                  </button>
                </div>
              ))}
            </div>
          </section>
        </div>
        <aside className="post-summary">
          <div className="panel">
            <span className="eyebrow">MỘT CHÚT TỔNG QUAN</span>
            <h2>Tin đăng của bạn</h2>
            <div className="summary-lines">
              <span>
                Giá thuê<strong>{money(form.basePrice)}</strong>
              </span>
              <span>
                Diện tích<strong>{form.area} m²</strong>
              </span>
              <span>
                Hình ảnh<strong>{form.imageUrls.length} / 10</strong>
              </span>
              <span>
                Tiện ích<strong>{form.amenities.length} tiện ích</strong>
              </span>
            </div>
            <div className="summary-total">
              <small>Dự toán với 80 kWh điện & 4 m³ nước</small>
              <strong>
                {money(estimateCost(form))}
                <span>/ tháng</span>
              </strong>
            </div>
            <p className="muted">
              Hãy kiểm tra thông tin trước khi đăng. Tin mới có thời hạn hiển thị 30 ngày.
            </p>
            {error && (
              <div role="alert" className="form-error">
                {error}
              </div>
            )}
            <button className="button primary full" disabled={busy || uploading}>
              {busy ? 'Đang lưu…' : id ? 'Lưu thay đổi' : 'Đăng tin phòng trọ'}
              <ArrowUpRight size={16} />
            </button>
            <Link className="text-button" to="/my-rooms">
              Quay lại quản lý tin
            </Link>
          </div>
        </aside>
      </form>
    </div>
  );
}
