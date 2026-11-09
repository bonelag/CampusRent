import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Heart,
  CalendarDays,
  House,
  ArrowUpRight,
  LogOut,
  KeyRound,
  FileText,
  Upload,
  X,
  Clock,
  AlertCircle,
  CheckCircle2,
  ImagePlus,
} from 'lucide-react';
import { useApp } from '../context/AppContext';
import { api } from '../lib/api';
import { PageTitle, Modal } from '../components/Common';
import { DownloadLink, ProtectedImage } from '../components/Attachments';

export default function AccountPage() {
  const { user, setUser, saved, notify, logout, refreshUser, changePassword } = useApp();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [passwordOpen, setPasswordOpen] = useState(false);
  const [passwordBusy, setPasswordBusy] = useState(false);
  const [passwordError, setPasswordError] = useState('');
  const closePassword = useCallback(() => setPasswordOpen(false), []);

  // Landlord upgrade state
  const [requests, setRequests] = useState([]);
  const [loadingReqs, setLoadingReqs] = useState(false);
  const [reqTitle, setReqTitle] = useState('');
  const [reqReason, setReqReason] = useState('');
  const [imageUrls, setImageUrls] = useState([]);
  const [pdfFile, setPdfFile] = useState(null); // { url, fileName, size }
  const [uploadingImage, setUploadingImage] = useState(false);
  const [uploadingPdf, setUploadingPdf] = useState(false);
  const [reqError, setReqError] = useState('');
  const [submittingReq, setSubmittingReq] = useState(false);

  const loadRequests = useCallback(async () => {
    setLoadingReqs(true);
    try {
      const data = await api('/landlord-requests/my');
      setRequests(data);
    } catch (err) {
      setReqError(err.message);
    } finally {
      setLoadingReqs(false);
    }
  }, []);

  useEffect(() => {
    if (user?.role === 'STUDENT') loadRequests();
  }, [user?.role, loadRequests]);
  useEffect(() => {
    if (!requests.some((request) => request.status === 'PENDING')) return;
    const timer = window.setInterval(async () => {
      await loadRequests();
      refreshUser().catch(() => {});
    }, 15000);
    return () => window.clearInterval(timer);
  }, [requests, loadRequests, refreshUser]);

  async function submitPassword(event) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    if (data.get('newPassword') !== data.get('confirmPassword')) {
      setPasswordError('Hai lần nhập mật khẩu mới chưa khớp.');
      return;
    }
    setPasswordBusy(true);
    setPasswordError('');
    try {
      await changePassword({
        currentPassword: data.get('currentPassword'),
        newPassword: data.get('newPassword'),
      });
      setPasswordOpen(false);
      notify('Đã đổi mật khẩu. Các phiên đăng nhập khác đã hết hiệu lực.');
    } catch (err) {
      setPasswordError(err.message);
    } finally {
      setPasswordBusy(false);
    }
  }

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setError('');
    const data = new FormData(e.currentTarget);
    try {
      setUser(
        await api('/auth/me', {
          method: 'PUT',
          body: { fullName: data.get('fullName'), phoneNumber: data.get('phoneNumber') },
        })
      );
      notify('Đã cập nhật thông tin của bạn.');
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  async function handleImageUpload(e) {
    const files = Array.from(e.target.files || []);
    if (!files.length) return;
    if (imageUrls.length + files.length > 10) {
      setReqError('Chỉ được đính kèm tối đa 10 ảnh.');
      return;
    }

    setUploadingImage(true);
    setReqError('');
    try {
      for (const file of files) {
        if (file.size > 10 * 1024 * 1024) {
          throw new Error(`Ảnh "${file.name}" vượt quá giới hạn 10 MB.`);
        }
        const formData = new FormData();
        formData.append('file', file);
        const res = await api('/uploads/image', { method: 'POST', body: formData });
        setImageUrls((prev) => [...prev, res.url]);
      }
    } catch (err) {
      setReqError(err.message);
    } finally {
      setUploadingImage(false);
      e.target.value = '';
    }
  }

  function removeImage(idx) {
    setImageUrls((prev) => prev.filter((_, i) => i !== idx));
  }

  async function handlePdfUpload(e) {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!file.name.toLowerCase().endsWith('.pdf') && file.type !== 'application/pdf') {
      setReqError('Chỉ chấp nhận file định dạng PDF (.pdf).');
      e.target.value = '';
      return;
    }

    if (file.size > 15 * 1024 * 1024) {
      setReqError('File PDF không được vượt quá 15 MB.');
      e.target.value = '';
      return;
    }

    setUploadingPdf(true);
    setReqError('');
    try {
      const formData = new FormData();
      formData.append('file', file);
      const res = await api('/uploads/pdf', { method: 'POST', body: formData });
      setPdfFile({ url: res.url, fileName: res.fileName || file.name, size: file.size });
    } catch (err) {
      setReqError(err.message);
    } finally {
      setUploadingPdf(false);
      e.target.value = '';
    }
  }

  async function handleUpgradeSubmit(e) {
    e.preventDefault();
    if (!reqTitle.trim()) {
      setReqError('Vui lòng nhập tiêu đề yêu cầu.');
      return;
    }
    if (!reqReason.trim()) {
      setReqError('Vui lòng nhập lý do nâng cấp tài khoản.');
      return;
    }

    setSubmittingReq(true);
    setReqError('');
    try {
      await api('/landlord-requests', {
        method: 'POST',
        body: {
          title: reqTitle.trim(),
          reason: reqReason.trim(),
          imageUrls,
          pdfUrl: pdfFile?.url || null,
          pdfFileName: pdfFile?.fileName || null,
        },
      });
      notify('Yêu cầu nâng cấp thành Chủ trọ đã được gửi tới quản trị viên.');
      setReqTitle('');
      setReqReason('');
      setImageUrls([]);
      setPdfFile(null);
      await loadRequests();
    } catch (err) {
      setReqError(err.message);
    } finally {
      setSubmittingReq(false);
    }
  }

  async function cancelRequest(id) {
    if (!window.confirm('Bạn có chắc chắn muốn hủy yêu cầu nâng cấp này?')) return;
    try {
      await api('/landlord-requests/' + id, { method: 'DELETE' });
      notify('Đã hủy yêu cầu nâng cấp.');
      await loadRequests();
    } catch (err) {
      notify(err.message, 'error');
    }
  }

  const pendingRequest = requests.find((r) => r.status === 'PENDING');
  const rejectedRequest = requests.find((r) => r.status === 'REJECTED');

  return (
    <div className="container page-shell">
      <PageTitle
        eyebrow="GÓC NHỎ CỦA BẠN"
        title={'Xin chào, ' + user.fullName.split(' ').at(-1) + '.'}
        text="Quản lý thông tin và những kế hoạch cho nơi ở tiếp theo."
      />
      <div className="account-layout">
        <aside className="profile-card">
          <span className="avatar large">{user.fullName.charAt(0)}</span>
          <h2>{user.fullName}</h2>
          <p>{user.email}</p>
          <span className="status-pill confirmed">
            {user.role === 'ADMIN'
              ? 'Quản trị viên'
              : user.role === 'LANDLORD'
                ? 'Chủ trọ'
                : 'Người tìm trọ'}
          </span>
          <small>Tham gia từ {new Date(user.createdAt).toLocaleDateString('vi-VN')}</small>
          <button className="text-button" onClick={logout}>
            <LogOut size={15} /> Đăng xuất
          </button>
        </aside>
        <div>
          <div className="account-shortcuts">
            <Link to="/saved">
              <Heart />
              <strong>{saved.length} phòng đã lưu</strong>
              <ArrowUpRight size={17} />
            </Link>
            <Link to="/bookings">
              <CalendarDays />
              <strong>Lịch hẹn của tôi</strong>
              <ArrowUpRight size={17} />
            </Link>
            {['LANDLORD', 'ADMIN'].includes(user.role) && (
              <Link to="/my-rooms">
                <House />
                <strong>Quản lý tin đăng</strong>
                <ArrowUpRight size={17} />
              </Link>
            )}
          </div>

          <section className="panel">
            <h2>Thông tin cá nhân</h2>
            <p className="muted">Giúp chủ trọ và người thuê liên hệ với bạn thuận tiện hơn.</p>
            <form className="form" onSubmit={submit}>
              <div className="form-row">
                <label>
                  Họ và tên
                  <input name="fullName" required maxLength="150" defaultValue={user.fullName} />
                </label>
                <label>
                  Số điện thoại
                  <input
                    name="phoneNumber"
                    type="tel"
                    maxLength="20"
                    defaultValue={user.phoneNumber || ''}
                  />
                </label>
              </div>
              <label>
                Email
                <input value={user.email} disabled readOnly />
              </label>
              {error && (
                <div role="alert" className="form-error">
                  {error}
                </div>
              )}
              <button className="button primary align-start" disabled={busy}>
                {busy ? 'Đang lưu…' : 'Lưu thay đổi'}
              </button>
            </form>
          </section>

          <section className="panel">
            <h2>Bảo mật tài khoản</h2>
            <p className="muted">
              Đổi mật khẩu để kết thúc các phiên đăng nhập trên thiết bị khác.
            </p>
            <button
              className="button outline small"
              onClick={() => {
                setPasswordError('');
                setPasswordOpen(true);
              }}
            >
              Đổi mật khẩu
            </button>
          </section>

          {/* Section nâng cấp thành chủ trọ dành cho người tìm trọ (STUDENT) */}
          {user.role === 'STUDENT' && (
            <section className="panel upgrade-section">
              <div className="section-header-row">
                <div>
                  <h2 className="section-title-with-icon">
                    <KeyRound size={20} /> Nâng cấp tài khoản thành Chủ trọ
                  </h2>
                  <p className="muted">
                    Bạn có phòng trọ cho sinh viên thuê? Gửi yêu cầu kèm giấy tờ xác minh để ban
                    quản trị duyệt quyền đăng tin.
                  </p>
                </div>
              </div>

              {pendingRequest ? (
                <div className="upgrade-pending-card">
                  <div className="pending-status-header">
                    <span className="status-pill pending">
                      <Clock size={14} /> Chờ quản trị viên xét duyệt
                    </span>
                    <span className="timestamp">
                      Gửi lúc {new Date(pendingRequest.createdAt).toLocaleString('vi-VN')}
                    </span>
                  </div>

                  <h3 className="pending-title">{pendingRequest.title}</h3>
                  <p className="pending-reason">{pendingRequest.reason}</p>

                  {/* Attached images preview */}
                  {pendingRequest.imageUrls?.length > 0 && (
                    <div className="attachment-group">
                      <strong>Ảnh đính kèm ({pendingRequest.imageUrls.length}):</strong>
                      <div className="attachment-image-grid">
                        {pendingRequest.imageUrls.map((url, i) => (
                          <DownloadLink
                            key={i}
                            url={url}
                            fileName={`anh-xac-minh-${i + 1}.png`}
                            className="attachment-thumb-link"
                          >
                            <ProtectedImage src={url} alt={`Ảnh đính kèm ${i + 1}`} />
                          </DownloadLink>
                        ))}
                      </div>
                    </div>
                  )}

                  {/* Attached PDF document */}
                  {pendingRequest.pdfUrl && (
                    <div className="attachment-group">
                      <strong>Tài liệu đính kèm (PDF):</strong>
                      <DownloadLink
                        url={pendingRequest.pdfUrl}
                        fileName={pendingRequest.pdfFileName || 'tai-lieu-xac-minh.pdf'}
                        className="pdf-download-badge"
                      >
                        <FileText size={18} />
                        <div className="pdf-info">
                          <span>{pendingRequest.pdfFileName || 'Tài liệu xác thực.pdf'}</span>
                          <small>Tải tài liệu PDF</small>
                        </div>
                      </DownloadLink>
                    </div>
                  )}

                  <div className="pending-actions">
                    <button
                      type="button"
                      className="button outline small danger"
                      onClick={() => cancelRequest(pendingRequest.id)}
                    >
                      Hủy yêu cầu này
                    </button>
                  </div>
                </div>
              ) : (
                <>
                  {rejectedRequest && (
                    <div className="alert-box alert-rejected">
                      <AlertCircle size={18} />
                      <div>
                        <strong>Yêu cầu nâng cấp trước đó đã bị từ chối</strong>
                        {rejectedRequest.adminNote && <p>{rejectedRequest.adminNote}</p>}
                        <small>Bạn có thể bổ sung thông tin và gửi lại yêu cầu bên dưới.</small>
                      </div>
                    </div>
                  )}

                  <form className="form upgrade-form" onSubmit={handleUpgradeSubmit}>
                    <label>
                      Tiêu đề yêu cầu <span className="req-star">*</span>
                      <input
                        required
                        maxLength={200}
                        placeholder="VD: Đăng ký cho thuê dãy trọ 8 phòng tại Bách Khoa"
                        value={reqTitle}
                        onChange={(e) => setReqTitle(e.target.value)}
                      />
                    </label>

                    <label>
                      Lý do & Giới thiệu cơ sở phòng trọ <span className="req-star">*</span>
                      <textarea
                        required
                        rows={4}
                        maxLength={2000}
                        placeholder="VD: Tôi có dãy trọ tại ngõ 15 Tạ Quang Bửu, gồm 8 phòng cho sinh viên BKHN/NEU thuê. Xin cấp quyền đăng tin trên hệ thống..."
                        value={reqReason}
                        onChange={(e) => setReqReason(e.target.value)}
                      />
                    </label>

                    {/* Image attachments */}
                    <div className="upload-field-block">
                      <label className="upload-block-label">
                        Đính kèm ảnh minh họa (tùy chọn, tối đa 10 ảnh)
                      </label>
                      <p className="upload-subtext">Hỗ trợ JPG, PNG, WebP (tối đa 10 MB/ảnh).</p>

                      <div className="image-previews-container">
                        {imageUrls.map((url, idx) => (
                          <div className="image-preview-item" key={idx}>
                            <ProtectedImage src={url} alt={`Ảnh ${idx + 1}`} />
                            <button
                              type="button"
                              className="remove-thumb-btn"
                              aria-label="Xóa ảnh"
                              onClick={() => removeImage(idx)}
                            >
                              <X size={14} />
                            </button>
                          </div>
                        ))}

                        {imageUrls.length < 10 && (
                          <label className="image-upload-trigger">
                            <input
                              type="file"
                              accept="image/jpeg,image/png,image/webp"
                              multiple
                              style={{ display: 'none' }}
                              onChange={handleImageUpload}
                              disabled={uploadingImage}
                            />
                            <ImagePlus size={20} />
                            <span>{uploadingImage ? 'Đang tải…' : 'Thêm ảnh'}</span>
                          </label>
                        )}
                      </div>
                    </div>

                    {/* PDF only attachment */}
                    <div className="upload-field-block">
                      <label className="upload-block-label">
                        Đính kèm giấy tờ / chứng từ xác minh (Chỉ file PDF)
                      </label>
                      <p className="upload-subtext">
                        Định dạng chấp nhận: <strong>.pdf</strong> (tối đa 15 MB). Ví dụ: Giấy đăng
                        ký kinh doanh, giấy tờ sở hữu/hợp đồng nhà trọ.
                      </p>

                      {pdfFile ? (
                        <div className="pdf-selected-chip">
                          <FileText size={20} className="pdf-icon" />
                          <div className="pdf-details">
                            <strong>{pdfFile.fileName}</strong>
                            {pdfFile.size && (
                              <small>{(pdfFile.size / 1024 / 1024).toFixed(2)} MB</small>
                            )}
                          </div>
                          <button
                            type="button"
                            className="remove-pdf-btn"
                            aria-label="Xóa file PDF"
                            onClick={() => setPdfFile(null)}
                          >
                            <X size={16} />
                          </button>
                        </div>
                      ) : (
                        <label className="pdf-upload-button">
                          <input
                            type="file"
                            accept=".pdf,application/pdf"
                            style={{ display: 'none' }}
                            onChange={handlePdfUpload}
                            disabled={uploadingPdf}
                          />
                          <Upload size={16} />
                          <span>
                            {uploadingPdf ? 'Đang tải file PDF…' : 'Chọn file PDF để đính kèm'}
                          </span>
                        </label>
                      )}
                    </div>

                    {reqError && (
                      <div role="alert" className="form-error">
                        {reqError}
                      </div>
                    )}

                    <button
                      type="submit"
                      className="button primary align-start"
                      disabled={submittingReq || uploadingImage || uploadingPdf}
                    >
                      {submittingReq ? 'Đang gửi yêu cầu…' : 'Gửi yêu cầu lên Quản trị viên'}
                    </button>
                  </form>
                </>
              )}
            </section>
          )}
        </div>
      </div>
      {passwordOpen && (
        <Modal title="Đổi mật khẩu" onClose={closePassword}>
          <form className="form" onSubmit={submitPassword}>
            <label>
              Mật khẩu hiện tại
              <input
                name="currentPassword"
                type="password"
                required
                maxLength={72}
                autoComplete="current-password"
              />
            </label>
            <label>
              Mật khẩu mới
              <input
                name="newPassword"
                type="password"
                required
                minLength={8}
                maxLength={72}
                autoComplete="new-password"
              />
            </label>
            <label>
              Nhập lại mật khẩu mới
              <input
                name="confirmPassword"
                type="password"
                required
                minLength={8}
                maxLength={72}
                autoComplete="new-password"
              />
            </label>
            {passwordError && (
              <div className="form-error" role="alert">
                {passwordError}
              </div>
            )}
            <button className="button primary full" disabled={passwordBusy}>
              {passwordBusy ? 'Đang lưu…' : 'Lưu mật khẩu mới'}
            </button>
          </form>
        </Modal>
      )}
    </div>
  );
}
