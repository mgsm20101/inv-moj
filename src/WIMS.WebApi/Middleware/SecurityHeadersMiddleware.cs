namespace WIMS.WebApi.Middleware;

/// <summary>
/// يضيف ترويسات أمنية دفاعية (OWASP Secure Headers) لكل استجابة:
/// منع الـ MIME sniffing، منع التضمين في إطار (clickjacking)، سياسة المُحيل،
/// وسياسة محتوى تسمح فقط بموارد من نفس المصدر (تخدم واجهة Angular من wwwroot
/// وواجهة الـ API معاً). HSTS يُضاف في الإنتاج فقط.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        // موارد من نفس المصدر فقط (لا CDN خارجي) — style-src يسمح بـ unsafe-inline
        // لأن Angular يُضمِّن CSS حرِج inline افتراضياً عند البناء production.
        headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; font-src 'self' data:; connect-src 'self'; " +
            "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

        // في الإنتاج فقط (خلف TLS): افرض HTTPS لمدة سنة.
        if (!env.IsDevelopment())
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        await next(context);
    }
}
