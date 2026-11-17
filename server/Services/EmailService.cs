using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusRent.Server.Services;

public sealed record EmailDeliveryResult(bool Sent, bool IsSimulated = false, string? Error = null);

public interface IEmailService
{
    Task<SmtpSetting> GetEffectiveSettingsAsync(CancellationToken ct = default);
    string ProtectSecret(string plainText);
    string UnprotectSecret(string cipherOrPlain);
    Task<EmailDeliveryResult> SendVerificationCodeAsync(string toEmail, string fullName, string code, CancellationToken ct = default);
    Task<EmailDeliveryResult> SendTestEmailAsync(string toEmail, SmtpSetting? settingsOverride = null, CancellationToken ct = default);
    Task<EmailDeliveryResult> SendBookingReminderAsync(string toEmail, string landlordName, string studentName, string roomTitle, DateTime visitAt, CancellationToken ct = default);
}

public class EmailService(
    CampusRentDbContext db,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<EmailService> logger) : IEmailService
{
    private const string EncPrefix = "enc:v1:";

    private byte[] EncryptionKey()
    {
        var secret = configuration["Jwt:Secret"] ?? "CampusRent-local-development-only-secret-2026";
        return SHA256.HashData(Encoding.UTF8.GetBytes($"smtp-secret:{secret}"));
    }

    public string ProtectSecret(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        if (plainText.StartsWith(EncPrefix, StringComparison.Ordinal)) return plainText;
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var cipher = new byte[plainBytes.Length];
        using var aes = new AesGcm(EncryptionKey(), AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plainBytes, cipher, tag);
        var packed = new byte[nonce.Length + tag.Length + cipher.Length];
        Buffer.BlockCopy(nonce, 0, packed, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, packed, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipher, 0, packed, nonce.Length + tag.Length, cipher.Length);
        return EncPrefix + Convert.ToBase64String(packed);
    }

    public string UnprotectSecret(string cipherOrPlain)
    {
        if (string.IsNullOrEmpty(cipherOrPlain)) return string.Empty;
        if (!cipherOrPlain.StartsWith(EncPrefix, StringComparison.Ordinal)) return cipherOrPlain;
        try
        {
            var packed = Convert.FromBase64String(cipherOrPlain[EncPrefix.Length..]);
            var nonceSize = AesGcm.NonceByteSizes.MaxSize;
            var tagSize = AesGcm.TagByteSizes.MaxSize;
            if (packed.Length < nonceSize + tagSize) return string.Empty;
            var nonce = packed.AsSpan(0, nonceSize);
            var tag = packed.AsSpan(nonceSize, tagSize);
            var cipher = packed.AsSpan(nonceSize + tagSize);
            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(EncryptionKey(), tagSize);
            aes.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
        catch (CryptographicException)
        {
            return string.Empty;
        }
    }

    public async Task<SmtpSetting> GetEffectiveSettingsAsync(CancellationToken ct = default)
    {
        var saved = await db.SmtpSettings.AsNoTracking().SingleOrDefaultAsync(s => s.Id == 1, ct);
        if (saved != null) return saved;

        var host = configuration["Smtp:Host"];
        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];
        return new SmtpSetting
        {
            Id = 1,
            Enabled = configuration.GetValue("Smtp:Enabled", !string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password)),
            Host = string.IsNullOrWhiteSpace(host) ? "smtp.gmail.com" : host.Trim(),
            Port = configuration.GetValue("Smtp:Port", 587),
            UseSsl = configuration.GetValue("Smtp:UseSsl", true),
            Username = username?.Trim() ?? string.Empty,
            Password = password ?? string.Empty,
            FromEmail = configuration["Smtp:FromEmail"]?.Trim() ?? username?.Trim() ?? string.Empty,
            FromName = configuration["Smtp:FromName"]?.Trim() ?? "CampusRent",
            UpdatedAt = DateTime.UtcNow
        };
    }

    public async Task<EmailDeliveryResult> SendVerificationCodeAsync(string toEmail, string fullName, string code, CancellationToken ct = default)
    {
        var setting = await GetEffectiveSettingsAsync(ct);
        var isDevelopmentOrTest = environment.IsDevelopment() || environment.IsEnvironment("Testing");

        if (!setting.Enabled || string.IsNullOrWhiteSpace(setting.Host) || string.IsNullOrWhiteSpace(setting.Username))
        {
            if (isDevelopmentOrTest)
            {
                logger.LogInformation("[DEV SMTP] Mã xác minh cho {Email}: {Code}", toEmail, code);
                return new EmailDeliveryResult(false, IsSimulated: true);
            }
            return new EmailDeliveryResult(false, Error: "Hệ thống chưa cấu hình máy chủ gửi email SMTP. Vui lòng liên hệ quản trị viên.");
        }

        var subject = "[CampusRent] Mã xác minh địa chỉ email của bạn";
        var encodedName = WebUtility.HtmlEncode(fullName);
        var htmlBody = $"""
            <div style="font-family:Arial,sans-serif;max-width:520px;margin:0 auto;padding:24px;border:1px solid #e5e7eb;border-radius:12px;">
              <h2 style="color:#0f5132;margin-top:0;">Xác minh email tài khoản CampusRent</h2>
              <p>Xin chào <strong>{encodedName}</strong>,</p>
              <p>Mã xác minh email của bạn là:</p>
              <div style="font-size:28px;font-weight:700;letter-spacing:6px;padding:14px 20px;background:#f3f4f6;border-radius:8px;text-align:center;color:#111827;margin:16px 0;">
                {code}
              </div>
              <p>Mã có hiệu lực trong <strong>15 phút</strong>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
              <p style="color:#6b7280;font-size:13px;margin-top:24px;">Email được gửi tự động từ hệ thống CampusRent.</p>
            </div>
            """;

        return await SendInternalAsync(setting, toEmail, subject, htmlBody, ct);
    }

    public async Task<EmailDeliveryResult> SendTestEmailAsync(string toEmail, SmtpSetting? settingsOverride = null, CancellationToken ct = default)
    {
        var setting = settingsOverride ?? await GetEffectiveSettingsAsync(ct);
        if (string.IsNullOrWhiteSpace(setting.Host) || string.IsNullOrWhiteSpace(setting.Username))
            return new EmailDeliveryResult(false, Error: "Vui lòng nhập đầy đủ SMTP Host và tài khoản email trước khi gửi thử.");

        var subject = "[CampusRent] Kiểm tra kết nối Gmail SMTP thành công";
        var htmlBody = $"""
            <div style="font-family:Arial,sans-serif;max-width:520px;margin:0 auto;padding:24px;border:1px solid #e5e7eb;border-radius:12px;">
              <h2 style="color:#0f5132;margin-top:0;">Kết nối SMTP hoạt động bình thường</h2>
              <p>Đây là email kiểm tra được gửi từ trang quản trị <strong>CampusRent</strong>.</p>
              <p>Máy chủ: <code>{WebUtility.HtmlEncode(setting.Host)}:{setting.Port}</code> (SSL/TLS: {(setting.UseSsl ? "Bật" : "Tắt")})</p>
              <p>Thời gian (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z</p>
            </div>
            """;

        return await SendInternalAsync(setting, toEmail, subject, htmlBody, ct);
    }

    public async Task<EmailDeliveryResult> SendBookingReminderAsync(string toEmail, string landlordName, string studentName, string roomTitle, DateTime visitAt, CancellationToken ct = default)
    {
        var setting = await GetEffectiveSettingsAsync(ct);
        var isDevelopmentOrTest = environment.IsDevelopment() || environment.IsEnvironment("Testing");

        if (!setting.Enabled || string.IsNullOrWhiteSpace(setting.Host) || string.IsNullOrWhiteSpace(setting.Username))
        {
            if (isDevelopmentOrTest)
            {
                logger.LogInformation("[DEV SMTP] Nhắc lịch hẹn xem phòng {Room} gửi tới chủ trọ {Email} (khách {Student}, lúc {VisitAt:yyyy-MM-dd HH:mm})", roomTitle, toEmail, studentName, visitAt);
                return new EmailDeliveryResult(false, IsSimulated: true);
            }
            return new EmailDeliveryResult(false, Error: "Chưa bật cấu hình SMTP để gửi email nhắc lịch hẹn.");
        }

        var localVisit = visitAt.AddHours(7);
        var subject = $"[CampusRent] Nhắc lịch hẹn xem phòng hôm nay: {roomTitle}";
        var htmlBody = $"""
            <div style="font-family:Arial,sans-serif;max-width:540px;margin:0 auto;padding:24px;border:1px solid #e5e7eb;border-radius:12px;">
              <h2 style="color:#4338ca;margin-top:0;">Nhắc lịch hẹn xem phòng hôm nay</h2>
              <p>Xin chào <strong>{WebUtility.HtmlEncode(landlordName)}</strong>,</p>
              <p>Hôm nay bạn có lịch hẹn xem phòng đã xác nhận trên <strong>CampusRent</strong>:</p>
              <ul style="padding-left:18px;line-height:1.6;">
                <li><strong>Phòng trọ:</strong> {WebUtility.HtmlEncode(roomTitle)}</li>
                <li><strong>Người đặt lịch:</strong> {WebUtility.HtmlEncode(studentName)}</li>
                <li><strong>Thời gian (GMT+7):</strong> {localVisit:dd/MM/yyyy HH:mm}</li>
              </ul>
              <p style="padding:12px 14px;background:#fffbeb;border:1px solid #fde68a;border-radius:8px;color:#92400e;">
                Lưu ý: Sau <strong>3 ngày kể từ ngày hẹn</strong>, nếu bạn không xác nhận <em>"Khách chưa chốt"</em> trong mục Lịch hẹn, hệ thống sẽ mặc định phòng đã được chốt và tự động ẩn bài đăng.
              </p>
              <p style="color:#6b7280;font-size:13px;margin-top:20px;">Email được gửi tự động từ hệ thống CampusRent.</p>
            </div>
            """;

        return await SendInternalAsync(setting, toEmail, subject, htmlBody, ct);
    }

    private async Task<EmailDeliveryResult> SendInternalAsync(SmtpSetting setting, string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var plainPassword = UnprotectSecret(setting.Password);
        if (setting.Host.Contains("gmail", StringComparison.OrdinalIgnoreCase))
            plainPassword = plainPassword.Replace(" ", string.Empty);
        if (string.IsNullOrWhiteSpace(plainPassword))
            return new EmailDeliveryResult(false, Error: "Chưa cấu hình mật khẩu ứng dụng (App Password) cho tài khoản SMTP.");

        var fromEmail = string.IsNullOrWhiteSpace(setting.FromEmail) ? setting.Username : setting.FromEmail;
        var fromName = string.IsNullOrWhiteSpace(setting.FromName) ? "CampusRent" : setting.FromName;

        try
        {
            using var client = new SmtpClient(setting.Host.Trim(), setting.Port)
            {
                EnableSsl = setting.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(setting.Username.Trim(), plainPassword),
                Timeout = 15000
            };
            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail.Trim(), fromName.Trim(), Encoding.UTF8),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(toEmail.Trim());
            await client.SendMailAsync(message, ct);
            return new EmailDeliveryResult(true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SMTP send failed to {Email}", toEmail);
            return new EmailDeliveryResult(false, Error: $"Không thể gửi email qua SMTP: {ex.Message}");
        }
    }
}
