# خطة تنفيذ نظام إدارة المخازن والمخزون (WIMS)

**الجهة:** قطاع التطوير التقني ومركز المعلومات القضائي — وزارة العدل
**المُعِدّ:** محمد جمال صديق · **الإصدار:** 1.0 · **مرجع المتطلبات:** SRS v1.1

> خطة تنفيذ مباشرة للبدء فوراً. كل مرحلة مستقلّة قابلة للتسليم (Milestone)، بمهام مرقّمة ومربوطة بأرقام متطلبات الـ SRS. مصممة لتُنفَّذ عبر Claude Code بنمط **Plan → Execute** (Opus للتخطيط، Sonnet للتنفيذ).

---

## 0. حالة التنفيذ (Progress) — آخر تحديث: 2026-07-01

| المرحلة | الحالة | ملاحظات |
|---|---|---|
| 0 — الأساس | ✅ مكتملة ومُختبَرة | مصادقة JWT + RBAC + تدقيق تلقائي؛ تحقّق end-to-end |
| 1 — الأصناف + استيراد Excel | ✅ الجوهر مكتمل ومُختبَر | أصناف/تصنيفات/وحدات/مخازن + توليد كود `GG-CCCC-SSSS` + استيراد Excel (أصناف + أرصدة افتتاحية). متبقٍّ: استيراد الموظفين (يعتمد على المرحلة 3) |
| 2 — حركات المخزون | ✅ مكتملة ومُختبَرة | GRN/صرف/تحويل/مرتجع/تسوية + دفتر ثابت + تكلفة طبقية بالدُفعة (FEFO/FIFO) + SoD؛ سيناريو DoD تحقّق 100% |
| 3 — العُهد والموافقات | ✅ مكتملة ومُختبَرة | موظفون (+استيراد)، عُهد + توفير تلقائي من الصرف المستديم، محرّك موافقات متعدد المستويات، كشف عهدة، حراسات BR-EMP-03/BR-CUS-03؛ سيناريو DoD تحقّق |
| 4 — الجرد والإنذارات | ✅ مكتملة ومُختبَرة | دورة جرد كاملة (تخطيط→تجميد يمنع الحركة→عدّ→فروقات آلية→SoD→ترحيل تسويات) + محرّك إنذارات خلفي (نقطة الطلب/الحد الأدنى/الصلاحية/الركود) مع إزالة تكرار وإغلاق تلقائي و SMTP؛ سيناريو DoD تحقّق 100% |
| 5 — التقارير والتصدير | ✅ مكتملة ومُختبَرة | 6 تقارير (رصيد/كارت صنف/راكد/تحت الحد/عُهد/محضر جرد) بنموذج موحّد + تصدير PDF (QuestPDF RTL) و Excel (ClosedXML) و JSON + لوحة مؤشرات؛ DoD تحقّق end-to-end (محضر الجرد طابق فروقات المرحلة 4: صافي كمية 2/قيمة 19.75). الباركود مؤجّل (اختياري) |
| 6 — التصلّب والنشر | ✅ الجوهر التقني مكتمل ومُختبَر | تصلّب أمني OWASP (Rate Limiting + ترويسات أمنية + CORS + Forwarded Headers + حارس مفتاح إنتاج) + إعدادات إنتاج (appsettings.Production/web.config) + توثيق نشر/نسخ احتياطي-DR/OWASP + اختبارات فعلية (23 اختباراً: تكاملية end-to-end للمصادقة/الصلاحيات/التصدير/الحد + ثبات الصلاحيات). متبقٍّ تشغيلي: بناء Angular ونشر IIS الفعلي + ترحيل الأرصدة الفعلية + التدريب (خارج نطاق المستودع) |

**قرارات تنفيذية مثبّتة أثناء البناء:** الحل بصيغة `WIMS.slnx` (SDK 10)؛ استهداف `net8.0` مركزياً عبر `Directory.Build.props`؛ SimpleMapper داخلي في `WIMS.Shared`؛ التدقيق عبر `AuditBehavior` (كل `ICommand`)؛ RBAC عبر claims «permission» وموفّر سياسات ديناميكي.

---

## 1. القرارات المعمارية المثبّتة (Locked Decisions)

| القرار | الاختيار | السبب |
|---|---|---|
| المنصة | .NET 8 (LTS) | التوافق مع الخبرة والبنية الحالية |
| النمط المعماري | Clean Architecture + CQRS (MediatR) | فصل المسؤوليات وقابلية الاختبار |
| الـ Mapping | **SimpleMapper** (بديل AutoMapper) | التفضيل المعتمد ومكتبتك الخاصة |
| قاعدة البيانات | SQL Server + EF Core 8 | بيئة On-Premises للجهة |
| المصادقة | داخلية (ASP.NET Core Identity + JWT + RBAC) | **لا يوجد Active Directory** |
| الواجهة الأمامية | Angular 17+ (Standalone) — RTL كامل | التوافق مع خبرتك |
| التحقق | FluentValidation | تحقق مركزي على مستوى Application |
| التسجيل (Logging) | Serilog (Rolling File + SQL Sink) | تدقيق ومتابعة |
| استيراد Excel | ClosedXML | **بيانات الأصناف والموظفين من Excel** |
| تصدير التقارير | QuestPDF (PDF) + ClosedXML (Excel) | تسليم للإدارة المالية للجهة الأم |
| سجل التدقيق | **قابل للتعديل بصلاحية** + تتبّع مَن عدّل ومتى | حسب `FR-AUD-01` |
| الباركود | **اختياري** — الإدخال اليدوي كامل | حسب `FR-COD-02` |
| النشر | Windows Server 2022 + IIS (On-Premises) | بيئة الجهة |

---

## 2. المتطلبات المسبقة (Prerequisites)

- [ ] تثبيت **.NET 8 SDK** و**Node.js 20 LTS** و**Angular CLI**.
- [ ] **SQL Server 2019/2022** (Developer محلياً، Standard/Enterprise للإنتاج) + **SSMS**.
- [ ] **Git** + مستودع (repo) داخلي للجهة.
- [ ] **Visual Studio 2022** أو **VS Code** + Claude Code.
- [ ] الحصول على النماذج الورقية الرسمية الحالية (أذون، محاضر، كارت صنف) لمطابقة الطباعة.
- [ ] اعتماد دليل ترميز الأصناف الموحّد + قوائم (تصنيفات/وحدات/مخازن) — من تبويب «القوائم» في ملف Excel.

---

## 3. هيكل الحل (Solution Structure)

```
WIMS/
├── WIMS.sln
├── src/
│   ├── WIMS.Domain/            # Entities, Enums, Domain Events, Interfaces (لا تبعيات)
│   ├── WIMS.Application/       # CQRS Commands/Queries, DTOs, Validators, Interfaces
│   ├── WIMS.Infrastructure/    # EF Core, Repositories, Auth, Excel, Pdf, Serilog
│   ├── WIMS.WebApi/            # Controllers/Endpoints, Middleware, DI, Swagger
│   └── WIMS.Shared/            # SimpleMapper, Result<T>, Guards, Extensions
├── client/                     # Angular RTL (SPA)
└── tests/
    ├── WIMS.Domain.Tests/
    ├── WIMS.Application.Tests/
    └── WIMS.IntegrationTests/
```

**قاعدة التبعية (Dependency Rule):** `WebApi → Infrastructure → Application → Domain`. الطبقة الداخلية لا تعرف الخارجية إطلاقاً.

---

## 4. أوامر الإعداد الأولي (Bootstrap — جاهزة للنسخ)

```bash
# 1) الحل والمشاريع
dotnet new sln -n WIMS
dotnet new classlib -n WIMS.Domain        -o src/WIMS.Domain
dotnet new classlib -n WIMS.Application    -o src/WIMS.Application
dotnet new classlib -n WIMS.Infrastructure -o src/WIMS.Infrastructure
dotnet new classlib -n WIMS.Shared         -o src/WIMS.Shared
dotnet new webapi   -n WIMS.WebApi         -o src/WIMS.WebApi --use-controllers

# 2) مشاريع الاختبار
dotnet new xunit -n WIMS.Domain.Tests      -o tests/WIMS.Domain.Tests
dotnet new xunit -n WIMS.Application.Tests  -o tests/WIMS.Application.Tests
dotnet new xunit -n WIMS.IntegrationTests   -o tests/WIMS.IntegrationTests

# 3) ربط المشاريع بالحل
dotnet sln add (Get-ChildItem -r *.csproj)   # PowerShell
# أو Bash: dotnet sln add $(find . -name "*.csproj")

# 4) مراجع التبعية بين الطبقات
dotnet add src/WIMS.Application/WIMS.Application.csproj    reference src/WIMS.Domain/WIMS.Domain.csproj src/WIMS.Shared/WIMS.Shared.csproj
dotnet add src/WIMS.Infrastructure/WIMS.Infrastructure.csproj reference src/WIMS.Application/WIMS.Application.csproj
dotnet add src/WIMS.WebApi/WIMS.WebApi.csproj             reference src/WIMS.Infrastructure/WIMS.Infrastructure.csproj

# 5) الحزم الأساسية
dotnet add src/WIMS.Application    package MediatR
dotnet add src/WIMS.Application    package FluentValidation.DependencyInjectionExtensions
dotnet add src/WIMS.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/WIMS.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/WIMS.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add src/WIMS.Infrastructure package System.IdentityModel.Tokens.Jwt
dotnet add src/WIMS.Infrastructure package ClosedXML
dotnet add src/WIMS.Infrastructure package QuestPDF
dotnet add src/WIMS.Infrastructure package Serilog.AspNetCore
dotnet add src/WIMS.WebApi         package Swashbuckle.AspNetCore

# 6) الواجهة الأمامية Angular RTL
ng new wims-client --routing --style=scss --standalone
# ثم في styles.scss: html[dir="rtl"] + تحميل خط عربي، وضبط <html dir="rtl" lang="ar">
```

> **SimpleMapper:** أضِف مكتبتك كـ project reference أو NuGet داخلي في `WIMS.Shared`، وسجّلها في DI بدل AutoMapper.

---

## 5. مراحل التنفيذ (Execution Phases)

### المرحلة 0 — الأساس والهيكل (Foundation)
**الهدف:** حلّ يعمل من طرف لطرف بمصادقة وصلاحيات وتدقيق.

- [x] إعداد الطبقات والمراجع وحِزم NuGet (القسم 4).
- [x] `WIMS.Shared`: `Result<T>`, `Error`, `Guard`, تسجيل **SimpleMapper** في DI.
- [x] `Domain`: `BaseEntity` (Id, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, IsDeleted).
- [x] `Application`: إعداد MediatR + Pipeline Behaviors (`ValidationBehavior`, `LoggingBehavior`, `AuditBehavior`).
- [x] `Infrastructure`: `AppDbContext` + الاتصال بـ SQL Server + Serilog.
- [x] **المصادقة الداخلية:** Identity + JWT + سياسة كلمة مرور + قفل بعد محاولات فاشلة + انتهاء صلاحية الجلسة.
- [x] **RBAC:** كيانات `Role` و`Permission` و`RolePermission` + Policy-based Authorization.
- [x] **سجل التدقيق (Editable):** كيان `AuditLog` (User, Timestamp, Entity, Before, After) + قابلية تعديله بصلاحية `Audit.Edit` مع تسجيل مَن عدّل السجل ومتى (تعديل موثّق لا استبدال صامت).
- [x] Middleware للأخطاء الموحّد (ProblemDetails بالعربية) + Swagger.
- [x] **DoD:** تسجيل دخول ينتج JWT، endpoint محمي يعمل، أي عملية تُكتب في `AuditLog`. ✅
- **يغطّي:** `FR-USR-01..04`, `FR-AUD-01..03`, `NFR-SEC-01..03`.

---

### المرحلة 1 — الأصناف والمخازن + استيراد Excel
**الهدف:** كتالوج الأصناف والمخازن جاهز، والرفع من Excel يعمل.

- [x] كيانات: `Item`, `ItemCategory` (شجري), `UnitOfMeasure`, `Warehouse`, `WarehouseLocation`, `StockBalance`.
- [x] Enum `ItemType` = { مستهلك, مستديم, خطر, قابل للتلف }.
- [x] CQRS للأصناف: `Create/Update/Deactivate ItemCommand`, `GetItems/GetItemByIdQuery` + Validators.
- [x] CQRS للمخازن والمواقع + منع الصرف من مخزن مغلق/مجمّد.
- [x] حدود المخزون لكل صنف/مخزن (Min/Max/ReorderPoint).
- [x] **خدمة استيراد Excel** (`IExcelReader` + ClosedXML): قراءة تبويب «الأصناف»، تحقق من الأعمدة الإلزامية والتكرار، تقرير أخطاء صفّي، ثم Insert بمعاملة واحدة. + الأرصدة الافتتاحية. ⏳ «الموظفون» مؤجّل للمرحلة 3.
- [x] Endpoints: `POST /api/import/items`, `POST /api/import/opening-balances` (رفع ملف + معاينة أخطاء قبل الاعتماد). ⏳ `import/employees`.
- [x] **DoD:** رفع نموذج Excel المعتمد يُنشئ الأصناف والأرصدة الافتتاحية بنجاح ويظهر تقرير أخطاء واضح. ✅
- **يغطّي:** `FR-ITM-01..10`, `FR-WH-01..05`, `FR-INT-02`, `FR-INT-03`.

---

### المرحلة 2 — حركات المخزون (Transactions)
**الهدف:** استلام/صرف/تحويل/مرتجع/تسوية مع تحديث آمن للأرصدة.

- [x] كيانات: `Voucher` + `VoucherLine`, `StockTransaction` (دفتر ثابت append-only), `Supplier`.
- [x] Enum `VoucherType` (+ Reversal) · `VoucherStatus` · `AdjustmentType` · `TransferStatus` · `StockTxnType`.
- [x] **إذن الإضافة (GRN):** استلام كميات + مطابق/مرفوض + دُفعة/صلاحية/سيريال + WAC عند الإدخال.
- [x] **إذن الصرف:** منع تجاوز الرصيد + تخصيص FEFO/FIFO بالدُفعات + ربط بمركز التكلفة + حماية تزامن (RowVersion).
- [x] **التحويل:** حالات (صادر/قيد النقل/وارد) + تأكيد استلام قبل زيادة رصيد الهدف.
- [x] **المرتجع والتسوية:** بحركة موثّقة + منع أي حذف (حركة عكسية Reversal فقط).
- [x] **كل حركة معاملة ذرية (Transaction)** تُحدّث `StockBalance` بعد الاعتماد فقط + فصل واجبات (SoD).
- [x] **DoD:** سيناريو كامل (استلام → صرف → تحويل → مرتجع) ينتج أرصدة صحيحة 100%. ✅ (تكلفة طبقية بالدُفعة)
- ⏳ متبقٍّ (تحسينات): حركة Reversal الكاملة، الحجز (Reservation) الاختياري.
- **يغطّي:** `FR-RCV-01..07`, `FR-ISS-01..07`, `FR-TRF-01..02`, `FR-RET-01..02`, `FR-ADJ-01..02`.

---

### المرحلة 3 — العُهد ودورة الموافقات
**الهدف:** إدارة العُهد + سير اعتماد متعدد المستويات.

- [x] كيانات: `Employee` (+استيراد Excel), `Custody` + `CustodyItem`, `ApprovalWorkflow` + `ApprovalStep` + `ApprovalRequest` + `ApprovalAction`.
- [x] عهدة شخصية + كشف عهدة + براءة ذمة (BR-CUS-05). ⏳ نقل عهدة بتوقيع الطرفين (تحسين مؤجّل).
- [x] منع نقل/إنهاء خدمة موظف لديه عهدة قائمة (BR-EMP-03 — منع صارم 409).
- [x] **Approval Engine:** مسارات حسب النوع والقيمة + اعتماد/رفض/إرجاع + فصل الواجبات (لا خطوتين متتاليتين لنفس المستخدم). ⏳ التفويض البديل (Delegation) مؤجّل.
  - **قرار مُحدَّث (2026-07-04):** أُزيل قيد «من ينشئ لا يعتمد» بقرار عمل صريح — يُسمح الآن للمنشئ باعتماد سنده الخاص لكل أنواع السندات (لا استثناء). قيد «لا خطوتين متتاليتين لنفس المستخدم» في مسارات الاعتماد متعددة الخطوات يبقى نافذاً.
- [x] تحويل الأصناف المستديمة المصروفة تلقائياً لعهدة المستلِم (ذرّياً عند الاعتماد النهائي).
- [x] **DoD:** حركة صرف تمرّ بمسار اعتماد خطوتين موثّق كل خطوة؛ كشف عهدة صحيح. ✅
- **يغطّي:** `FR-CST-01..06`, `FR-APR-01..05`, `FR-ISS-07`.

---

### المرحلة 4 — الجرد والإنذارات
**الهدف:** جرد دوري/سنوي + إنذارات الحدود والصلاحية والركود.

- [x] كيانات: `StockCount` + `StockCountLine` (نوع، نطاق، حالة، لقطة رصيد دفتري + تكلفة).
- [x] أمر جرد + **تجميد الحركة (Freeze)**: يمنع ترحيل أي حركة على المخزن المُجمَّد (فحص مركزي في `VoucherPostingService.PostAsync` + تأكيد التحويل) → 409.
- [x] إدخال الرصيد الفعلي + احتساب الفروقات آلياً (الأسطر غير المعدودة = مطابِقة للدفتري) + اعتماد المحضر (SoD: المعتمِد ≠ من جمّد/أنشأ) وترحيل التسويات عبر سندَي تسوية (زيادة/عجز) يمرّان بمحرّك الترحيل نفسه.
- [x] **محرّك الإنذارات (`AlertsBackgroundService`):** نقطة إعادة الطلب/الحد الأدنى (رصيد إجمالي) + قرب/انتهاء الصلاحية (بالدُفعة) + الأصناف الراكدة + إشعار داخل النظام (كيان `Alert`) و SMTP للحرِج؛ إزالة تكرار عبر `DedupKey` وإغلاق تلقائي عند زوال السبب.
- [x] **DoD مُحقَّق:** جرد كامل أنتج فروقات (+5/−3) وسندَي تسوية `ADJ-2026-000001/2` وصحّح الأرصدة؛ الإنذار يُطلق عند بلوغ الحد (Critical) ويُزال تكراره ويُغلق تلقائياً.
- **يغطّي:** `FR-STK-01..06`, `FR-REO-01..05`. Migration `Phase4_StockCountAlerts`.
- **قرار مثبّت:** التجميد على مستوى **المخزن** (لا الصنف) لبساطة الاتّساق؛ جرد نشِط واحد لكل مخزن. تسويات الجرد تُبنى كسندات `Adjustment` معتمدة آلياً (تفويضها من اعتماد المحضر) وتُرحَّل بعد ضبط حالة المحضر إلى Approved (كي تتجاوز حارس التجميد). عتبات الإنذار من `appsettings:Alerts` (فاصل الفحص/أيام الصلاحية/أيام الركود).

---

### المرحلة 5 — التقارير والتصدير والباركود
**الهدف:** التقارير الرسمية + التصدير للإدارة المالية + الباركود الاختياري.

- [x] تقارير: رصيد المخزون، كارت الصنف، الأصناف الراكدة، تحت الحد الأدنى، العُهد، محضر الجرد — كلها كـ `IQuery<Result<ReportDocument>>` في `Features/Reports/ReportQueries.cs` تبني نموذجاً جدولياً موحّداً (`ReportDocument`) محايداً للصيغة.
- [x] **تصدير:** PDF (QuestPDF RTL بالنموذج الرسمي: ترويسة وزارة العدل + جدول + إجماليات + ترقيم صفحات) و Excel (ClosedXML، ورقة RTL مع تجميد الترويسة) و JSON للعرض؛ عبر `IReportExporter` (Infrastructure `Services/Reporting/`). المُصدِّر يختار الصيغة من `?format=pdf|excel|json`.
- [x] لوحة مؤشرات (`GetDashboardQuery` → `DashboardDto`): إجمالي/نشِط الأصناف، قيمة المخزون، تحت الحد، قرب/انتهاء الصلاحية، سندات معلّقة، جرد مفتوح، إنذارات مفتوحة/حرِجة + أعلى 5 عجوزات + آخر الإنذارات الحرِجة.
- [ ] **الباركود (اختياري — مؤجّل):** لم يُنفَّذ (اختياري ولا يوقف التشغيل)؛ يُضاف لاحقاً كـ endpoint يولّد Code128/QR SVG للملصقات.
- [x] **DoD مُحقَّق:** كل تقرير صدّر PDF (`%PDF-`) و Excel (`PK`, RTL) بنجاح؛ الإجماليات طابقت لوحة المؤشرات؛ **محضر الجرد طابق تماماً فروقات المرحلة 4** (صافي كمية 2 / قيمة 19.75)؛ مسار 404 للصنف/المحضر غير الموجود.
- **يغطّي:** `FR-RPT-01..07`, `FR-INT-01`. (الباركود `FR-COD-01..03` مؤجّل اختيارياً.) **لا Migration** — التقارير قراءة فقط؛ صلاحيتا `Reports.View`/`Dashboard.View` تُبذران آلياً عند الإقلاع.
- **قرار مثبّت:** نموذج تقرير موحّد واحد (`ReportDocument`: عنوان/فرعي/ترويسة/أعمدة/صفوف نصية مُنسَّقة مسبقاً/إجماليات) يخدم كل التقارير ومُصدِّرَي PDF/Excel معاً — الخلايا تُنسَّق في المعالِج ليبقى المُصدِّر عرضياً بحتاً. خط PDF = "Arial" (متوفّر على Windows/IIS) لدعم العربية.

---

### المرحلة 6 — التصلّب والاختبار والنشر
**الهدف:** جاهزية الإنتاج على IIS.

- [x] اختبارات فعلية: **23 اختباراً** (كانت 14). Domain: ثبات مصدر الصلاحيات الوحيد (`PermissionKeysTests`: لا تكرار + كل ثابت مبذور والعكس). Integration (`WebApplicationFactory` على قاعدة `WIMS_Test`): دخول صحيح→JWT، كلمة خاطئة→401، نقطة محمية بلا رمز→401 ومعها→200، تصدير تقرير PDF (`%PDF-`)، حضور الترويسات الأمنية، وتجاوز حدّ الدخول→429.
- [x] **مراجعة أمنية (OWASP Top 10)** موثّقة في [`docs/SECURITY-OWASP.md`](docs/SECURITY-OWASP.md) + **Rate Limiting** (عام 120/دقيقة، دخول 10/دقيقة، قابل للضبط) + ترويسات أمنية (`nosniff`/`DENY`/CSP/Referrer/HSTS إنتاجاً) + CORS للمنشأ + Forwarded Headers + **حارس إقلاع يمنع مفتاح JWT ضعيف/افتراضي في الإنتاج**.
- [x] **النسخ الاحتياطي وDR** موثّق في [`docs/BACKUP-DR.md`](docs/BACKUP-DR.md) (Full/Diff/Log، RPO≤24س/RTO≤4س، Point-in-Time، اختبار استعادة ربع سنوي يتحقق من اتصال تسلسل الدفتر).
- [x] **إعدادات ودليل نشر IIS:** `appsettings.Production.json` (أسرار عبر متغيّرات بيئة) + `web.config` (ANCM in-process، حدّ حجم الطلب، إزالة `X-Powered-By`) + [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md).
- [ ] **بناء Angular للإنتاج + النشر الفعلي على IIS + شهادة TLS** — خارج نطاق هذا المستودع (لا واجهة Angular هنا)؛ الخطوات موثّقة في دليل النشر.
- [ ] **ترحيل الأرصدة الافتتاحية الفعلية + تدريب المستخدمين** — مهام تشغيلية عند الإطلاق (آلية الاستيراد جاهزة من المرحلة 1).
- [x] **DoD (الجزء التقني):** بناء نظيف 0/0، **23 اختباراً أخضر**، تصلّب OWASP مُطبَّق ومُختبَر، توثيق نشر/DR/أمن كامل. متبقٍّ تشغيلي (Angular/تدريب/تطبيق DR الفعلي) موثّق.
- **يغطّي:** `NFR-BKP/MAINT/COMPL/LOC` + التصلّب الأمني. **لا Migration** (تغييرات إعداد/برمجة فقط).
- **قرار مثبّت (فخّ اختبار مهم):** `Program` يقرأ الإعدادات **مباشرةً وقت التهيئة** (الحارس/المُحدِّد/سلسلة الاتصال قبل `Build()`)، فتجاوز `WebApplicationFactory.ConfigureAppConfiguration` يصل **متأخّراً**. الحل: ضبط **متغيّرات البيئة** عبر `[ModuleInitializer]` (يلتقطها `CreateBuilder` فوراً) في `TestEnvironment`.

---

## 6. الكيانات الأساسية (Domain Entities — مرجع سريع)

| الكيان | أهم الحقول |
|---|---|
| `Item` | Code, NameAr, NameEn, CategoryId, UnitId, Type, IsActive |
| `ItemCategory` | Name, ParentId (شجري) |
| `Warehouse` | Code, Name, Type, KeeperEmployeeId, IsFrozen |
| `WarehouseLocation` | WarehouseId, Code (رف/صندوق) |
| `StockBalance` | ItemId, WarehouseId, Qty, AvgCost |
| `Voucher` / `VoucherLine` | Type, Status, Date, ApprovedBy / ItemId, Qty, Cost, Batch, Serial |
| `StockTransaction` | ItemId, WarehouseId, Type, Qty, Value, RefVoucherId |
| `Custody` / `CustodyItem` | EmployeeId, Date, Status / ItemId, Qty, Serial |
| `StockCount` / `StockCountLine` | Scope, Committee, Status / ItemId, BookQty, PhysicalQty, Diff |
| `Employee` | Code, Name, Department, CostCenter, Email |
| `User` / `Role` / `Permission` | Username, Hash / Name / Key, Scope |
| `AuditLog` | UserId, Timestamp, Entity, Action, Before, After, EditedBy, EditedAt |

---

## 7. كيفية التنفيذ مع Claude Code (Plan → Execute)

1. ابدأ كل مرحلة بجلسة **تخطيط (Opus)**: الصق قسم المرحلة + الكيانات المعنية واطلب خطة ملفات تفصيلية.
2. نفّذ بجلسة **تنفيذ (Sonnet)** مهمة‑بمهمة (Checkbox واحد في المرة) مع مراجعة الـ diff.
3. بعد كل مرحلة: شغّل `dotnet test` وطابق **DoD** قبل الانتقال.
4. استخدم مهارة **code-review-dotnet-angular** بعد كل Feature كبيرة.
5. حدّث `AuditLog` كسلوك افتراضي عبر `AuditBehavior` وليس يدوياً في كل Handler.

---

## 8. ترتيب البدء الفوري (Today)

1. [ ] نفّذ أوامر Bootstrap (القسم 4) وارفع الهيكل على الـ repo.
2. [ ] أنشئ `AppDbContext` + أول Migration + قاعدة بيانات فارغة.
3. [ ] نفّذ المصادقة الداخلية + أول مستخدم Admin (Seed).
4. [ ] نفّذ `AuditBehavior` + كيان `AuditLog`.
5. [ ] ابدأ المرحلة 1 (الأصناف) + اربطها بنموذج Excel المعتمد.

> **الحرِج أولاً:** الأساس (المرحلة 0) ثم الأصناف/الاستيراد (المرحلة 1) ثم الحركات (المرحلة 2) — هذه العمود الفقري؛ ما بعدها يُبنى فوقه.
