# WIMS — نظام إدارة المخازن والمخزون (وزارة العدل)

نظام .NET 8 وفق **Clean Architecture + CQRS**. التنفيذ يتبع [`plan.md`](plan.md) على 7 مراحل.

## الحالة الحالية

| المرحلة | الوصف | الحالة |
|---|---|---|
| 0 | الأساس: مصادقة JWT + RBAC + تدقيق تلقائي | ✅ مكتملة (اختبارات المصادقة/التكامل لسه ناقصة) |
| 1 | الأصناف والمخازن + استيراد Excel | ✅ الجوهر مكتمل ومُختبَر (أصناف + أرصدة افتتاحية). متبقٍّ: استيراد الموظفين (يعتمد على المرحلة 3) |

## المتطلبات

- .NET SDK 8+ (يعمل على SDK 9/10 مع استهداف `net8.0` مركزياً عبر `Directory.Build.props`).
- SQL Server LocalDB أو SQL Server 2019/2022.
- `dotnet-ef` (`dotnet tool install -g dotnet-ef`).

## التشغيل

```bash
# 1) تطبيق الترحيلات وإنشاء قاعدة البيانات
dotnet ef database update -p src/WIMS.Infrastructure -s src/WIMS.WebApi

# 2) تشغيل الـ API (يبذر البيانات الأساسية تلقائياً عند الإقلاع)
dotnet run --project src/WIMS.WebApi
```

- Swagger: `/swagger` (بيئة Development فقط)
- المستخدم الافتراضي: **`admin` / `Admin@12345`** — ⚠️ يجب تغييره فوراً في الإنتاج.

## اختبار سريع (Smoke Test)

```bash
# تسجيل الدخول والحصول على رمز JWT
curl -X POST http://localhost:5021/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@12345"}'

# استدعاء endpoint محمي بالرمز
curl http://localhost:5021/api/me -H "Authorization: Bearer <TOKEN>"
```

## البنية

```
src/
├── WIMS.Domain/          # الكيانات والقواعد (بلا تبعيات)
├── WIMS.Application/      # CQRS + Behaviors + الواجهات
├── WIMS.Infrastructure/   # EF Core + Identity + JWT + الخدمات
├── WIMS.WebApi/          # Controllers + Middleware + DI
└── WIMS.Shared/          # Result<T> + Guard + SimpleMapper
tests/                    # Domain / Application / Integration
```

قاعدة التبعية: `WebApi → Infrastructure → Application → Domain`.

## الاختبارات

```bash
dotnet test
```

## ملاحظات تقنية

- **SimpleMapper**: أداة تعيين داخلية خفيفة في `WIMS.Shared` بديلاً عن AutoMapper.
- **التدقيق (Audit)**: يُكتب تلقائياً لكل أمر (`ICommand`) عبر `AuditBehavior` — لا تدقيق يدوي.
- **RBAC**: صلاحيات دقيقة عبر claims من نوع `permission` وموفّر سياسات ديناميكي.
- الحل بصيغة `WIMS.slnx` (صيغة الحل الجديدة القائمة على XML).
