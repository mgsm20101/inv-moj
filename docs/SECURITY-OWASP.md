# مراجعة أمنية — OWASP Top 10 (2021) لنظام WIMS

مراجعة المرحلة 6 لواجهة API. الحالة: ✅ مُعالَج · ⚠️ مُخفَّف/يتطلب إعداد تشغيلي · ➖ غير منطبق.

| # | الفئة | الحالة | المعالجة في WIMS |
|---|---|---|---|
| A01 | **Broken Access Control** | ✅ | صلاحيات دقيقة عبر claims «permission» وموفّر سياسات ديناميكي؛ كل نقطة محمية بـ `[Authorize(Policy=...)]`؛ فصل الواجبات (SoD) مفروض في الاعتماد (المُنشئ ≠ المعتمِد، لا خطوتين متتاليتين). الاستعلامات لا تُدقَّق لكنها تُصرَّح. |
| A02 | **Cryptographic Failures** | ✅ | كلمات المرور بـ Identity (PBKDF2)؛ JWT موقّع HMAC-SHA256؛ **حارس إقلاع** يمنع مفتاح توقيع افتراضي/ضعيف في الإنتاج؛ TLS + HSTS إلزامي إنتاجاً. |
| A03 | **Injection** | ✅ | EF Core مُعامَل بالكامل (لا تسلسل SQL)؛ التحقق عبر FluentValidation؛ استيراد Excel يطبّع ويتحقق صفّياً. |
| A04 | **Insecure Design** | ✅ | Clean Architecture + Result بدل الاستثناءات للأخطاء المتوقّعة؛ الدفتر append-only، الحذف الناعم فقط، RowVersion يمنع الصرف المزدوج، تجميد الجرد يمنع الحركة. |
| A05 | **Security Misconfiguration** | ✅ | Swagger مُعطَّل خارج التطوير؛ ترويسات أمنية (nosniff/DENY/CSP/Referrer)؛ إزالة `X-Powered-By`؛ رسائل خطأ عربية دون تسريب تفاصيل (ExceptionHandlingMiddleware)؛ الأسرار عبر متغيّرات البيئة. |
| A06 | **Vulnerable Components** | ⚠️ | حزم مثبّتة وحديثة (EF/Identity 8.0.11، QuestPDF، ClosedXML). **إجراء تشغيلي:** `dotnet list package --vulnerable` دورياً في الـ CI. |
| A07 | **Identification & Auth Failures** | ✅ | قفل الحساب (5 محاولات/15 دقيقة)؛ **Rate Limiting** على الدخول (10/دقيقة/IP) والعام (120)؛ سياسة كلمة مرور قوية؛ انتهاء صلاحية الرمز (60 دقيقة إنتاجاً). |
| A08 | **Software & Data Integrity** | ✅ | تدقيق تلقائي لكل أمر (`AuditBehavior`→`AuditLogs`) غير قابل للحذف؛ لقطات `QtyOnHandAfter/WacAfter` في الدفتر للمطابقة. |
| A09 | **Logging & Monitoring Failures** | ✅ | Serilog (ملف متدحرج 60 يوماً + جدول SQL إنتاجاً) + `AuditLog` تجاري؛ تسجيل طلبات HTTP. |
| A10 | **SSRF** | ➖ | لا يجلب النظام موارد من عناوين يتحكم بها المستخدم. البريد (SMTP) إلى مضيف مُهيّأ إدارياً فقط. |

## بنود تشغيلية متبقّية (المرحلة 6)
- فحص الحزم المعرّضة في الـ CI (A06).
- تغيير كلمات مرور الحسابات المبذورة (`admin`/`approver`/`finance`) بعد النشر.
- مراجعة صلاحيات هوية App Pool (أقل امتياز) وحساب SQL للتطبيق (لا `sysadmin`).
