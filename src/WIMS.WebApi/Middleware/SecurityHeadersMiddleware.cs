namespace WIMS.WebApi.Middleware;

/// <summary>
/// يضيف ترويسات أمنية دفاعية (OWASP Secure Headers) لكل استجابة:
/// منع الـ MIME sniffing، منع التضمين في إطار (clickjacking)، سياسة المُحيل،
/// وسياسة محتوى مبسّطة لواجهة API (لا HTML). HSTS يُضاف في الإنتاج فقط.
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
        // واجهة JSON بحتة — امنع تنفيذ أي محتوى نشط.
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // في الإنتاج فقط (خلف TLS): افرض HTTPS لمدة سنة.
        if (!env.IsDevelopment())
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        await next(context);
    }
}
