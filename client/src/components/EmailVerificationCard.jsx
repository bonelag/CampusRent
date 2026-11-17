import { useEffect, useState } from 'react';
import { MailCheck, Send, ShieldAlert, CheckCircle2, LoaderCircle } from 'lucide-react';
import { api } from '../lib/api';
import { useApp } from '../context/AppContext';

export default function EmailVerificationCard({ compact = false, onVerified }) {
  const { user, setUser, notify } = useApp();
  const [sending, setSending] = useState(false);
  const [verifying, setVerifying] = useState(false);
  const [code, setCode] = useState('');
  const [error, setError] = useState('');
  const [sentInfo, setSentInfo] = useState(null);
  const [cooldown, setCooldown] = useState(0);

  useEffect(() => {
    if (cooldown <= 0) return undefined;
    const timer = window.setTimeout(() => setCooldown((value) => Math.max(0, value - 1)), 1000);
    return () => window.clearTimeout(timer);
  }, [cooldown]);

  if (!user) return null;

  if (user.isEmailVerified) {
    if (compact) return null;
    return (
      <div className="verification-box is-verified">
        <CheckCircle2 size={20} />
        <div>
          <strong>Email đã được xác minh ({user.email})</strong>
          <p>
            {user.isEduVerified
              ? 'Tài khoản đã kích hoạt huy hiệu Sinh viên (.edu) với trọng số đánh giá cao hơn.'
              : 'Tài khoản đủ điều kiện xác thực để gửi yêu cầu nâng cấp hoặc đăng bài.'}
          </p>
        </div>
      </div>
    );
  }

  async function sendCode() {
    setSending(true);
    setError('');
    try {
      const res = await api('/auth/send-verification', { method: 'POST' });
      setSentInfo(res);
      setCooldown(60);
      notify(
        res?.simulated
          ? 'Đã tạo mã xác minh (chế độ thử nghiệm chưa bật SMTP).'
          : `Đã gửi mã xác minh tới ${user.email}.`
      );
    } catch (err) {
      setError(err.message);
    } finally {
      setSending(false);
    }
  }

  async function verifyCode(e) {
    e.preventDefault();
    if (!code.trim()) return;
    setVerifying(true);
    setError('');
    try {
      const updatedUser = await api('/auth/verify-email', {
        method: 'POST',
        body: { code: code.trim() },
      });
      setUser(updatedUser);
      setCode('');
      setSentInfo(null);
      notify('Xác minh email thành công!');
      onVerified?.(updatedUser);
    } catch (err) {
      setError(err.message);
    } finally {
      setVerifying(false);
    }
  }

  return (
    <div className="verification-box is-pending">
      <div className="verification-head">
        <ShieldAlert size={20} />
        <div>
          <strong>Xác minh email để mở khóa đăng bài</strong>
          <p>
            Tài khoản <code>{user.email}</code> chưa xác minh. Sau khi tạo tài khoản, bạn cần nhập
            mã xác minh gửi về email trước khi đăng tin phòng trọ.
          </p>
        </div>
      </div>

      <div className="verification-actions-row">
        <button
          type="button"
          className="button outline small"
          disabled={sending || cooldown > 0}
          onClick={sendCode}
        >
          {sending ? <LoaderCircle size={14} className="spin" /> : <Send size={14} />}
          {cooldown > 0 ? `Gửi lại sau ${cooldown}s` : sentInfo ? 'Gửi lại mã' : 'Gửi mã xác minh'}
        </button>
        {sentInfo?.previewCode && (
          <span className="verification-dev-pill">
            Mã thử nghiệm: <strong>{sentInfo.previewCode}</strong>
          </span>
        )}
      </div>

      <form className="verification-form" onSubmit={verifyCode}>
        <label className="sr-only" htmlFor="email-verify-code">
          Mã xác minh email
        </label>
        <input
          id="email-verify-code"
          name="verificationCode"
          inputMode="numeric"
          autoComplete="one-time-code"
          maxLength={6}
          placeholder="Nhập mã 6 chữ số"
          value={code}
          onChange={(e) => setCode(e.target.value)}
          required
        />
        <button type="submit" className="button primary small" disabled={verifying || !code.trim()}>
          <MailCheck size={15} />
          {verifying ? 'Đang kiểm tra…' : 'Xác nhận mã'}
        </button>
      </form>

      {error && (
        <div className="form-error" role="alert">
          {error}
        </div>
      )}
    </div>
  );
}
