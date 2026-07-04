using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WIMS.Application.Common.Interfaces;

namespace WIMS.Infrastructure.Services;

/// <summary>إعدادات SMTP وقائمة مستلمي الإنذارات (من appsettings قسم "Smtp").</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "wims@moj.local";
    public string FromName { get; set; } = "نظام WIMS";

    /// <summary>مستلمو إشعارات الإنذارات الحرِجة.</summary>
    public List<string> AlertRecipients { get; set; } = [];
}

/// <summary>
/// مُرسِل بريد عبر SMTP. يتجاهل الإرسال بأمان (يُسجّل فقط) إذا لم يُهيَّأ Host
/// أو لم يوجد مستلمون — لكيلا يفشل النظام في بيئة بلا SMTP.
/// </summary>
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _opts = options.Value;

    public async Task SendAsync(string subject, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opts.Host) || _opts.AlertRecipients.Count == 0)
        {
            logger.LogInformation("SMTP غير مُهيَّأ أو بلا مستلمين — تخطّي إرسال البريد: {Subject}", subject);
            return;
        }

        try
        {
            using var client = new SmtpClient(_opts.Host, _opts.Port) { EnableSsl = _opts.EnableSsl };
            if (!string.IsNullOrWhiteSpace(_opts.User))
                client.Credentials = new NetworkCredential(_opts.User, _opts.Password);

            using var message = new MailMessage
            {
                From = new MailAddress(_opts.From, _opts.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };
            foreach (var to in _opts.AlertRecipients)
                message.To.Add(to);

            await client.SendMailAsync(message, ct);
            logger.LogInformation("أُرسِل بريد الإنذار إلى {Count} مستلم.", _opts.AlertRecipients.Count);
        }
        catch (Exception ex)
        {
            // فشل البريد لا يُسقط عملية الفحص.
            logger.LogWarning(ex, "تعذّر إرسال بريد الإنذار: {Subject}", subject);
        }
    }
}
