# دليل استخدام Impeccable لواجهة WIMS (Angular + Claude Code)

**المصدر:** [impeccable.style](https://impeccable.style/) · [github.com/pbakaus/impeccable](https://github.com/pbakaus/impeccable)
**السياق:** واجهة WIMS العربية RTL — قطاع التطوير التقني ومركز المعلومات القضائي، وزارة العدل

> **Impeccable** = skill واحد + 23 command + كاشف (detector) ضد "الـ AI slop". بيدّي الـ agent **مفردات مصمّم** (تباين، هرمية، انضباط) بدل ما يخمّن، وبيقرأ سياق مشروعك من ملفَّي `PRODUCT.md` و`DESIGN.md` قبل أي تعديل. يشتغل مع Claude Code (اللي بتستخدمه) وCursor وCopilot وGemini/Codex CLI.

---

## 1. التثبيت (على جهاز التطوير)

> **ملاحظة بيئة:** ثبّتها على **جهاز التطوير اللي عليه إنترنت** — مش على سيرفر الجهة المعزول. تحتاج **Node 24+**.

**المسار الأساسي (Claude Code):**
```bash
# من جذر مشروع wims-client
npx impeccable install        # يكتشف الـ harness ويكتب ملفات الـ skill (.claude/skills/)
```
ثم أعِد تحميل Claude Code واكتب `/` — لازم يظهر `/impeccable`.

**بدائل:**
```bash
# كـ Plugin على Claude Code
/plugin marketplace add pbakaus/impeccable      # ثم /plugin وثبّته من القائمة

# مثبّت skills عام (بناء واحد مشترك، غير مخصّص للـ harness)
npx skills add pbakaus/impeccable
```

**التحديث لاحقاً:**
```bash
npx impeccable check     # يقولك لو نسختك قديمة
npx impeccable update
```

اختيارياً يقدر المثبّت يضيف **Design Hook** تلقائي (على Claude Code وCopilot وCodex وCursor) يشغّل مبادئ الديزاين تلقائياً.

---

## 2. طريقة الاستدعاء

```
/impeccable <command> <target>
```
أمثلة: `/impeccable polish شاشة الأصناف` · `/impeccable audit شاشة الصرف`. واكتب `/impeccable` لوحده تشوف كل الأوامر.
- **الأمر الحر:** أي وصف بعد `/impeccable` يطبّق المبادئ على المهمة (`/impeccable حسّن هذا الـ hero`).
- **اختصار:** `/impeccable pin audit` يعمل لك `/audit` مستقل.

---

## 3. الإعداد لمشروع WIMS (مرة واحدة — أهم خطوة)

الديزاين بدون سياق = ناتج عام. شغّل:
```
/impeccable init
```
هيسألك أسئلة قصيرة ويكتب **`PRODUCT.md`** في جذر المشروع. إجاباتك المقترحة لـ WIMS:

| السؤال | الإجابة المقترحة لـ WIMS |
|---|---|
| **Register (نوع السطح)** | **Product** — أداة عمل/لوحة، التصميم يخدم إنجاز المهمة (مش سطح تسويقي) |
| **لمن؟ (محدّد)** | أمناء المخازن والمعتمدون بالقطاع؛ يقرأون بيانات كثيفة (أذون/أرصدة/عُهد) بسرعة ودقّة على شاشات مكتبية |
| **صوت العلامة (3 كلمات حقيقية)** | «رسمي، دقيق، هادئ» (لا صخب ولا تسويق) |
| **مراجع بصرية (أسماء لا صفات)** | دفاتر السجلات الحكومية وكارت الصنف الورقي · الأختام الرسمية · مستندات النسخ العربي (Amiri) |
| **مراجع مضادة (Anti-references)** | تدرّجات لونية · glassmorphism · ألوان الـ AI الزاعقة · الخلفية الكريمية · شعارات «عزّز إنتاجيتك» |

> افتح `PRODUCT.md` وعدّل أي حاجة مش مظبوطة — الملف بتاعك.

### 3.1 التقاط النظام البصري
في آخر `init` هيعرض يشغّل `document` — **وافق**:
```
/impeccable document      # يكتب DESIGN.md (صيغة Google Stitch)
```
`DESIGN.md` يحمل الألوان والطباعة والمكوّنات، ويُقرأ قبل كل أمر. **غذِّه برموز اتجاه «الدفتر الرسمي»** من دليل التصميم:
- ألوان: حبر `#17263B` · ورق `#F4F6F7` · أخضر العدالة `#1E6A54` · **براس الختم `#A6742B`** (التوقيع).
- طباعة: `IBM Plex Sans Arabic` (واجهة) + `Amiri` (توقيع/مستندات) + أرقام tabular.

> **القاعدة:** `PRODUCT.md` = الاستراتيجية (مين/إيه/ليه) · `DESIGN.md` = البصريات. الاتنين مصدر الحقيقة اللي يخلّي كل الأوامر (وكل الـ agents) متسقة.

---

## 4. كتالوج الأوامر الـ 23 (مجمّعة)

| المجموعة | الأوامر | الغرض |
|---|---|---|
| **Create** | `craft` · `shape` · `impeccable` | بناء feature/صفحة جديدة (`craft` = تشكيل ثم بناء)، `shape` = الاتجاه/الهوية، `impeccable` = حرّ |
| **Evaluate** | `audit` · `critique` | فحص قبل التسليم / مراجعة تصميم كاملة بتقييم |
| **Refine** | `typeset` · `colorize` · `layout` · `animate` · `bolder` · `quieter` · `overdrive` · `delight` | صقل تخصّصي: طباعة، لون، تخطيط، حركة، جرأة/تهدئة، لمسات |
| **Simplify** | `distill` · `clarify` · `adapt` | تقطير الكثافة، توضيح، تكييف |
| **Harden** | `polish` · `optimize` · `harden` · `onboard` | تلميع شامل، أداء، تصليب، onboarding |
| **System** | `init` · `document` · `extract` · `live` | السياق، DESIGN.md، الاستخراج، Live Mode |

---

## 5. ترتيب الاستخدام الموصى به لـ WIMS

### أ) مرة واحدة للمشروع
1. `npx impeccable install`
2. `/impeccable init` → راجع `PRODUCT.md`
3. `/impeccable document` → غذِّ `DESIGN.md` برموز الدفتر/الختم

### ب) لكل شاشة/feature جديدة (مع مراحل خطة الواجهة)
4. **بناء:** `/impeccable craft شاشة كتالوج الأصناف` — أو ابنِ الشاشة ثم:
5. **تلميع شامل:** `/impeccable polish شاشة الأصناف` (محاذاة، مسافات، طباعة، لون، حالات، حركة، copy)
6. **صقل تخصّصي (بالترتيب المناسب للسجل الرسمي):**
   - `/impeccable typeset` — الطباعة العربية والسُّلّم.
   - `/impeccable colorize` — ربط اللون برموز DESIGN.md.
   - `/impeccable layout` — تخطيط الجداول الكثيفة.
   - `/impeccable animate` — حركة **وظيفية** محدودة (دمغة الختم، انتقالات).
   - **`/impeccable quieter`** — تهدئة الضجيج ← الأنسب للجهة الرسمية. **تجنّب** `bolder` / `overdrive` / `delight` الزائدة هنا.

### ج) تبسيط الشاشات الكثيفة
7. `/impeccable distill` أو `/impeccable clarify` لأذون الحركة والتقارير المزدحمة.

### د) قبل تسليم كل شاشة
8. `/impeccable critique شاشة الصرف` — مراجعة كاملة بتقييم واختبارات persona وكشف slop.
9. `/impeccable audit شاشة الصرف` — إتاحة (a11y)، أداء، theming، responsive، anti-patterns — وراجع يدوياً صحّة **RTL**.

### هـ) بوابة الجودة (CI)
10. `npx impeccable detect src/` — كاشف حتمي (45 قاعدة) بأكواد خروج تصلح لفحص الـ PR. + فعّل الـ Design Hook.

### و) التكرار البصري (اختياري — BETA)
11. **Live Mode:** يفتح منتقي عناصر على سيرفر التطوير، يولّد 3 بدائل إنتاجية للعنصر، والقبول يُكتب في المصدر عبر HMR.

---

## 6. توصيات خاصة بـ WIMS

- **Register = Product دائماً** — يضبط الكثافة والحركة لصالح إنجاز المهمة لا الانطباع.
- **فضّل `quieter` على `bolder`/`overdrive`** — السجل رسمي؛ اصرف الجرأة في الختم فقط.
- **`DESIGN.md` هو الحَكَم:** خليه يحمل رموز الدفتر/الختم، فيلتزم بيها كل أمر تلقائياً ويمنع انحراف الهوية.
- **RTL:** الأداة تساعد في الطباعة والتخطيط، لكن **راجع الانعكاس والأسهم والتقويم يدوياً** بعد كل تعديل، واعمل `audit` للإتاحة.
- **الـ detector في الـ PR checks:** يمنع تسريب "الـ slop" (تدرّجات نص، حدود شريطية، ألوان AI) قبل الدمج.

---

## 7. تحذيرات عملية

- Node **24+** مطلوب؛ التثبيت على جهاز عليه إنترنت (مش السيرفر المعزول).
- الأداة **رأيها قوي لكنها ليست معصومة** — راجع كل diff، ولو عدّلت حاجة مناسبة لـ RTL/السجل الرسمي، **اطلب التراجع (revert)** عن التعديل المحدّد وكمّل.
- الأوامر تظهر بعد **إعادة تحميل** الـ harness؛ لو مش ظاهرة تأكّد إنها اتكتبت في `.claude/skills/`.
- خلّي `PRODUCT.md` و`DESIGN.md` **محدّثين** مع تطوّر المشروع — ده اللي يخلّي كل جلسات Claude Code متسقة.

---

## 8. ربطها بمراحل خطة الواجهة

| المرحلة | الشاشات | تسلسل Impeccable |
|---|---|---|
| 0 الأساس | الدخول + Shell | `craft` الدخول (لحظة البطل) → `polish` → `critique` |
| 1 الأصناف | الكتالوج + رفع Excel | `craft` → `layout`+`typeset` → `distill` → `audit` |
| 2 الحركات | الأذون | `polish` → `quieter` → `animate` (الختم) → `critique` |
| 3 العُهد/الموافقات | Approval Inbox | `layout` → `clarify` → `audit` |
| 4 الجرد/الإنذارات | الجرد + التنبيهات | `distill` → `colorize` → `audit` |
| 5 التقارير/Dashboard | التقارير + المؤشرات | `craft` → `typeset` → `critique` |
| 6 الإدارة/النشر | المستخدمون/الصلاحيات | `audit` شامل + `detect` في CI |

---

**الخلاصة (لو هتفتكر سطر واحد):**
```
npx impeccable install → /impeccable init → /impeccable document → لكل شاشة: craft/polish ثم quieter ثم critique + audit → detect في CI
```
