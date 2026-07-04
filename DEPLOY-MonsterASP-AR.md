# نشر WIMS على MonsterASP.NET

⚠️ **تنبيه مهم:** هذه استضافة عامة مشتركة — مناسبة للتجربة والعرض التوضيحي (Demo)، وليست بديلاً عن استضافة مُصرَّح بها رسمياً لبيانات وزارة العدل الحقيقية في الإنتاج. للإنتاج الفعلي استخدم باقة `build-package.ps1` (SelfHost) على جهاز داخلي، أو استضافة حكومية معتمدة.

## المتطلبات قبل البدء

- حساب على [MonsterASP.NET](https://www.monsterasp.net/) (خطة مجانية أو مدفوعة).
- Node.js/npm و .NET SDK مثبَّتَين على جهازك (نفس متطلبات باقي المشروع).
- اتصال إنترنت أثناء التشغيل (لتنزيل حزمة نشر `win-x86` من NuGet).
- عميل FTP مجاني مثل [FileZilla](https://filezilla-project.org/) — أو استخدم File Manager الموجود داخل لوحة تحكّم MonsterASP نفسها.

## الخطوة 1 — إنشاء الموقع وقاعدة البيانات في لوحة التحكّم

1. سجّل الدخول للوحة تحكّم MonsterASP وأنشئ موقعاً جديداً (Website) — لاحظ اسم الدومين الفرعي الذي يعطيك إياه (مثال: `wims-demo.monsterasp.net`).
2. من نفس اللوحة، أنشئ **قاعدة بيانات MSSQL** جديدة. بعد الإنشاء ستحصل على سلسلة اتصال بالشكل:
   ```
   Server=dbXXX.public.databaseasp.net;Database=dbXXXX;User Id=dbXXXX;Password=XXXXXXXX;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True
   ```
   احتفظ بها — ستحتاجها في الخطوة 4.
3. فعّل بيانات **FTP** أو **WebDeploy** من نفس اللوحة (Control Panel → FTP Accounts أو WebDeploy) واحصل على: عنوان الخادم، اسم المستخدم، كلمة المرور.

## الخطوة 2 — بناء حزمة النشر محلياً

من جذر المشروع (`D:\inventory`) شغّل:

```powershell
.\publish-monsterasp.ps1
```

هذا السكربت:
1. يبني واجهة Angular بوضع production.
2. ينسخها داخل `src/WIMS.WebApi/wwwroot` (تُخدَم من نفس origin الـ API — لا حاجة لاستضافة منفصلة للواجهة ولا لإعداد CORS).
3. ينشر الـ API بصيغة framework-dependent مستهدفاً `win-x86` (يطابق مثال MonsterASP الرسمي لبيئة استضافتهم).
4. ينتج `dist\MonsterASP-Package\` ومجلداً مضغوطاً `dist\MonsterASP-Package.zip` جاهزَين للرفع.

## الخطوة 3 — رفع الملفات

اختر إحدى الطرق التالية:

### أ) FTP (المُوصى بها — مضمونة الآن) بـ FileZilla

اتصل ببيانات FTP من الخطوة 1، وارفع **محتوى** مجلد `dist\MonsterASP-Package\` (وليس المجلد نفسه) إلى المجلد الجذر لموقعك على الخادم (عادة `wwwroot` أو الجذر مباشرة حسب تسمية اللوحة).

### ب) File Manager

لو اللوحة تدعم رفع وفكّ ضغط ZIP مباشرة، ارفع `dist\MonsterASP-Package.zip` وفكّ ضغطه في مجلد الموقع.

### ج) WebDeploy من سطر الأوامر — ⚠️ معطوبة حالياً على SDK 10.0.202، لا تستخدمها

> **حالة معروفة:** `dotnet publish ... /p:DeployOnBuild=true /p:PublishProfile=MonsterASP` يفشل بخطأ
> `MSB4006: There is a circular dependency in the target dependency graph involving target "Publish"`
> على .NET SDK 10.0.202 (وربما إصدارات قريبة). تم تكرار الخطأ فعلياً ومحاولة `dotnet msbuild -t:Publish`
> كبديل ولم يُجدِ — نفس الخطأ. هذا خلل معروف في تعارض بين مسار `dotnet publish` الحديث ومسار
> WebDeploy القديم (`Microsoft.NET.Sdk.Publish`)، وليس خطأً في إعداد المشروع. استخدم الطريقة (أ) أو (ب) بدلاً منه.
>
> لو أصررت على WebDeploy لاحقاً (مثلاً بعد تحديث SDK يُصلح المشكلة، أو لو ثبّتّ Visual Studio ونشرت من خلاله
> بدل CLI)، ملف [`MonsterASP.pubxml`](src/WIMS.WebApi/Properties/PublishProfiles/MonsterASP.pubxml) جاهز
> ومُعدّ ببيانات موقعك (بدون كلمة المرور) لهذا الغرض بالضبط.

### د) عبر GitHub Actions (بديل مُوصى به لتفادي خلل SDK محلياً)

بما أن Runner جيت‌هَب بيستخدم .NET SDK 8 محدَّد بدقّة (مش SDK 10 اللي فيه خلل MSB4006)، وبما أن الخطوة دي بتفصل النشر (`dotnet publish` عادي بلا `DeployOnBuild`) عن خطوة الرفع (أكشن مستقل بيستخدم بروتوكول MSDeploy)، فبيتفادى نفس المشكلة تماماً.

**المواصفات الفنية المطلوبة** (جاهزة بالفعل في [`.github/workflows/deploy-monsterasp.yml`](.github/workflows/deploy-monsterasp.yml)):

| المتطلّب | القيمة |
|---|---|
| Runner | `windows-latest` |
| إصدار .NET SDK | `8.0.x` (عبر `actions/setup-dotnet@v4`) |
| Node.js | `20` (عبر `actions/setup-node@v4`، لبناء Angular) |
| أمر النشر | `dotnet publish ... --runtime win-x86 --self-contained false --output ./publish` (بلا WebDeploy) |
| أكشن الرفع | [`rasmusbuchholdt/simply-web-deploy@2.2.0`](https://github.com/rasmusbuchholdt/simply-web-deploy) |
| التشغيل | يدوي فقط (`workflow_dispatch`) — لا نشر تلقائي عند كل push |

**خطوات التفعيل (تعملها إنت بنفسك في GitHub، مش أنا):**

1. ادفع (push) هذا المستودع إلى مستودع GitHub خاص (Private) بحسابك — كرّر: **خاص**، لأن المستودع فيه بنية النظام الداخلية.
2. من صفحة المستودع على GitHub: **Settings → Secrets and variables → Actions → New repository secret**، وأضِف 4 أسرار (القيم من نفس ملف الـ `.publishSettings` اللي عندك):

   | اسم الـ Secret | القيمة (من ملف publishSettings بتاعك) |
   |---|---|
   | `WEBSITE_NAME` | `site77694` |
   | `SERVER_COMPUTER_NAME` | `https://site77694.siteasp.net:8172` |
   | `SERVER_USERNAME` | `site77694` |
   | `SERVER_PASSWORD` | كلمة مرور WebDeploy الحقيقية |

3. من تبويب **Actions** في المستودع، افتح workflow "نشر WIMS على MonsterASP.NET" واضغط **Run workflow** يدوياً.

> هذه الخطوات (الدفع لـ GitHub، وإضافة الأسرار) لازم تعملها إنت بنفسك — دخول حسابك على GitHub وإدخال كلمات مرور مش حاجة أقدر أو المفروض أعملها بدالك.

## الخطوة 4 — تعبئة الإعدادات الحسّاسة على الخادم مباشرة

**هذه أهم خطوة — النظام لن يعمل بدونها عمداً (حارس أمني في الكود).**

افتح `appsettings.Production.json` من داخل مجلد الموقع على الخادم (عبر File Manager أو FTP، عدّله في مكانه ثم أعد رفعه)، واملأ:

```json
{
  "ConnectionStrings": {
    "Default": "الصق هنا سلسلة الاتصال الحقيقية من الخطوة 1"
  },
  "Jwt": {
    "SigningKey": "مفتاح عشوائي طويل (32 حرفاً على الأقل) — ولّده مثلاً بأمر PowerShell التالي"
  },
  "Seed": {
    "AdminPassword": "كلمة مرور قوية لحساب admin — غيّرها فوراً بعد أول دخول أيضاً"
  }
}
```

لتوليد مفتاح JWT عشوائي محلياً:
```powershell
-join ((48..57)+(65..90)+(97..122)|Get-Random -Count 48|%{[char]$_})
```

> لا تكتب هذه القيم الحقيقية أبداً داخل المستودع (Git) — عدّلها فقط على الخادم بعد الرفع.

## الخطوة 5 — التحقق

1. افتح رابط موقعك (`https://wims-demo.monsterasp.net` أو نطاقك المخصّص) — يجب أن تظهر شاشة تسجيل الدخول.
2. سجّل الدخول بـ `admin` وكلمة المرور التي وضعتها في `Seed:AdminPassword`.
3. **غيّر كلمة مرور admin فوراً** من قائمة المستخدم أعلى الشاشة (تغيير كلمة المرور) حتى لو كانت قوية أصلاً.
4. جرّب إنشاء صنف أو سند بسيط للتأكّد أن الاتصال بقاعدة البيانات يعمل فعلياً.

## حلّ المشاكل الشائعة

| العرض | السبب المرجّح | الحل |
|---|---|---|
| خطأ `500.30 - ANCM In-Process Start Failure` | غالباً `Jwt:SigningKey` لسه فاضي، أو سلسلة الاتصال غلط | راجع الخطوة 4، وفعّل `stdoutLogEnabled="true"` في `web.config` مؤقتاً لقراءة تفاصيل الخطأ من ملف `logs/stdout*.log` |
| الموقع يفتح لكن الواجهة (Angular) بيضاء | فشل بناء `ng build` أو لم تُنسخ الملفات لـ `wwwroot` قبل النشر | أعد تشغيل `publish-monsterasp.ps1` وتأكّد من عدم وجود أخطاء في مرحلة `ng build` |
| خطأ اتصال بقاعدة البيانات | سلسلة الاتصال غير مطابقة لما أعطته اللوحة، أو نسيت `Encrypt=True;TrustServerCertificate=True` | انسخ السلسلة كما هي من لوحة التحكّم دون تعديل يدوي |
| 403 عند أي طلب API | لو فعّلت نطاقاً مخصّصاً، حدّث `AllowedHosts` في `appsettings.Production.json` ليطابقه | عدّل القيمة وأعد رفع الملف فقط (لا حاجة لإعادة نشر كامل) |
| `MSB4006: circular dependency ... target "Publish"` عند النشر | خلل معروف في SDK 10.0.202 عند خلط `dotnet publish` مع WebDeploy (`DeployOnBuild=true`) | لا تستخدم WebDeploy من سطر الأوامر حالياً — استخدم FTP أو File Manager (الطريقة أ/ب أعلاه) |

## ملاحظة أمنية ختامية

هذا الدليل لتجربة النظام أونلاين بسرعة ومجاناً فقط. أي بيانات حقيقية لوزارة العدل يجب أن تبقى على البنية التحتية الرسمية المعتمدة (باقة SelfHost المحلية، أو استضافة حكومية مصرَّح بها) — لا على استضافة عامة مشتركة مجانية.
