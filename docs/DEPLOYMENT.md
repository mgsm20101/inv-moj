# دليل النشر — WIMS (نظام إدارة المخازن)

نشر واجهة API على **IIS / Windows Server** مع SQL Server، وواجهة Angular كموقع ثابت خلف نفس الخادم.

## 1. المتطلبات على الخادم
- Windows Server 2019+ مع دور IIS.
- **ASP.NET Core Hosting Bundle 8.x** (يثبّت وحدة ASP.NET Core Module V2 لـ IIS).
- SQL Server 2019+ (أو Express) — قاعدة `WIMS`.
- شهادة TLS مربوطة بالموقع (HTTPS إلزامي في الإنتاج).

## 2. تجهيز النشر (من جهاز البناء)
```bash
# نشر الـ API (Framework-dependent — يعتمد على Hosting Bundle المثبّت):
dotnet publish src/WIMS.WebApi -c Release -o ./publish

# بناء واجهة Angular للإنتاج (من مجلد الواجهة عند توفّرها):
# ng build --configuration production   →  ينسخ dist/ إلى موقع IIS ثابت
```
> ملاحظة بيئة: جهاز التطوير عليه **SDK 10 فقط**؛ الاستهداف `net8.0` مثبّت مركزياً في `Directory.Build.props` ويعمل لتوفّر runtime/ref pack 8.0. خادم الإنتاج يكفيه **Hosting Bundle 8**.

## 3. الأسرار عبر متغيّرات البيئة (لا تُكتب في المستودع)
يفشل الإقلاع في الإنتاج إذا كان `Jwt:SigningKey` فارغاً/افتراضياً/أقصر من 32 بايت (حارس في `Program.cs`).
```
setx ConnectionStrings__Default "Server=.;Database=WIMS;User Id=wims_app;Password=***;TrustServerCertificate=True" /M
setx Jwt__SigningKey "<مفتاح عشوائي ≥ 32 بايت>" /M
setx ASPNETCORE_ENVIRONMENT "Production" /M
```
توليد مفتاح عشوائي: `[Convert]::ToBase64String((1..48 | % {Get-Random -Max 256}))` في PowerShell.

## 4. قاعدة البيانات (Migrations)
تُطبَّق الهجرات تلقائياً عند الإقلاع (`DbSeeder.SeedAsync` → `MigrateAsync`) وتُبذر الصلاحيات والأدوار.
لتطبيقها يدوياً قبل النشر:
```bash
dotnet ef database update -p src/WIMS.Infrastructure -s src/WIMS.WebApi
```
**بعد أول إقلاع: غيّر كلمة مرور `admin` فوراً** (الافتراضية `Admin@12345`).

## 5. IIS
1. أنشئ Application Pool: **No Managed Code** (الـ ANCM يشغّل .NET).
2. أنشئ موقعاً يشير إلى مجلد `publish/` مع ملف `web.config` المرفق (استضافة in-process).
3. اربط HTTPS + الشهادة، وأعد توجيه HTTP→HTTPS.
4. امنح هوية الـ App Pool صلاحية القراءة على المجلد والكتابة على `logs/`.

## 6. التصلّب المُطبَّق (Phase 6)
- **Rate Limiting**: عام 120 طلب/دقيقة/IP، وتسجيل الدخول 10/دقيقة (قابل للضبط في `RateLimiting`).
- **قفل الحساب**: 5 محاولات فاشلة → قفل 15 دقيقة (Identity).
- **ترويسات أمنية**: `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, CSP، و**HSTS** في الإنتاج.
- **CORS**: منشأ الواجهة فقط من `Cors:AllowedOrigins`.
- **Forwarded Headers**: لدعم الوكيل العكسي/TLS termination.
- **Swagger**: مُعطَّل خارج التطوير.

## 7. التحقق بعد النشر (Smoke)
- `POST /api/auth/login` بحساب صحيح → 200 + JWT.
- طلب نقطة محمية بلا رمز → 401؛ الترويسات الأمنية حاضرة في الاستجابة.
- `GET /api/dashboard` بالرمز → 200.
- تكرار الدخول السريع > الحد → 429.
