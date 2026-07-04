namespace WIMS.Application.Common.Interfaces;

/// <summary>إرسال البريد (SMTP) — يُستخدم لإشعارات الإنذارات الحرِجة. يُنفَّذ في Infrastructure.</summary>
public interface IEmailSender
{
    /// <summary>يرسل رسالة للمستلمين المُهيَّئين. يتجاهل الإرسال بصمت إذا لم يُهيَّأ SMTP.</summary>
    Task SendAsync(string subject, string body, CancellationToken ct = default);
}
