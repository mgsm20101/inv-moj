# خطة النسخ الاحتياطي والتعافي من الكوارث (DR) — WIMS

الهدف (NFR-BKP): **RPO ≤ 24 ساعة، RTO ≤ 4 ساعات**. النظام سجل جرد رسمي لوزارة العدل — الدفتر (`StockTransactions`) و`AuditLogs` غير قابلة للحذف (append-only / soft-delete)، لذا الاستعادة يجب أن تحافظ على تكامل التسلسل.

## 1. جدول النسخ
| النوع | التكرار | الاستبقاء |
|---|---|---|
| Full backup | يومي 02:00 | 30 يوماً |
| Differential | كل 6 ساعات | 7 أيام |
| Transaction Log | كل 15 دقيقة | 3 أيام |

يتطلب Log backups أن تكون قاعدة `WIMS` في **Recovery Model = FULL**.

## 2. أوامر النسخ (SQL Agent Jobs)
```sql
-- كامل يومي
BACKUP DATABASE [WIMS] TO DISK = N'D:\Backups\WIMS\WIMS_FULL.bak'
  WITH INIT, COMPRESSION, CHECKSUM, STATS = 10;

-- سجل كل 15 دقيقة
BACKUP LOG [WIMS] TO DISK = N'D:\Backups\WIMS\WIMS_LOG.trn'
  WITH COMPRESSION, CHECKSUM;
```
انسخ ملفات `.bak/.trn` إلى موقع خارج الخادم (مشاركة شبكية/تخزين بارد) يومياً.

## 3. الاستعادة (Point-in-Time)
```sql
RESTORE DATABASE [WIMS] FROM DISK = N'...\WIMS_FULL.bak' WITH NORECOVERY, REPLACE;
RESTORE DATABASE [WIMS] FROM DISK = N'...\WIMS_DIFF.bak' WITH NORECOVERY;
RESTORE LOG [WIMS] FROM DISK = N'...\WIMS_LOG.trn'
  WITH RECOVERY, STOPAT = '2026-07-01T13:45:00';
```
بعد الاستعادة: أعد تشغيل الـ API — `MigrateAsync` تتأكد من المخطط، والبذر Idempotent.

## 4. عناصر خارج قاعدة البيانات
- **مفتاح `Jwt:SigningKey`** ومتغيّرات البيئة: خزّنها في خزنة الأسرار المؤسسية — فقدانها يُبطل كل الرموز الصادرة (يتطلب إعادة تسجيل دخول فقط، لا فقدان بيانات).
- **ملفات السجل `logs/`** وملفات Excel المستوردة: تُنسخ ضمن نسخة نظام الملفات.

## 5. اختبار التعافي (إلزامي دورياً)
- **ربع سنوي**: استعادة آخر نسخة على خادم اختبار والتحقق من:
  - عدد صفوف `StockTransactions` وتسلسل `TransactionNo` متصل بلا فجوات.
  - مطابقة `StockBalance` لآخر لقطة `QtyOnHandAfter` في الدفتر لعيّنة أصناف.
  - نجاح تسجيل الدخول واستخراج تقرير رصيد المخزون.
- وثّق زمن الاستعادة الفعلي وقارنه بـ RTO.

## 6. المسؤوليات
| الدور | المسؤولية |
|---|---|
| DBA | مراقبة نجاح الـ Jobs يومياً، النقل الخارجي، اختبار الاستعادة الربع سنوي |
| مسؤول النظام | نسخ الأسرار/الإعدادات، توثيق التغييرات |
| مالك المنتج | اعتماد نتائج اختبار DR |
