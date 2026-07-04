WIMS — الخطوط المستضافة ذاتياً (Self-hosted fonts)
==================================================

الغرض
-----
يعمل التطبيق بلا أي اتصال خارجي (offline-safe). لا يحمّل index.html أي
خطوط من Google Fonts. بدلاً من ذلك تُعرّف الخطوط عبر @font-face في
src/styles/_base.scss وتشير إلى الملفات الموجودة في هذا المجلد (public/fonts).

مجلد public/ يُنسخ كما هو إلى جذر الموقع عند البناء، فتُقدَّم هذه الملفات
على المسار /fonts/<filename>.woff2 (كما في تعريفات @font-face).

إن غابت هذه الملفات، يعود المتصفح تلقائياً وبلا أخطاء إلى خطوط النظام
(system-ui / Segoe UI) المعرّفة في متغيّري --font-ui و --font-doc.
لذا التطبيق يعمل حتى بلا هذه الملفات — لكن إسقاطها يمنح المظهر الرسمي المقصود.

الملفات المطلوبة (بالأسماء نفسها تماماً)
---------------------------------------
واجهة الاستخدام — IBM Plex Sans Arabic:
  IBMPlexSansArabic-Regular.woff2     (وزن 400)
  IBMPlexSansArabic-Medium.woff2      (وزن 500)
  IBMPlexSansArabic-SemiBold.woff2    (وزن 600)
  IBMPlexSansArabic-Bold.woff2        (وزن 700)

خط التوقيع/المستندات — Amiri:
  Amiri-Regular.woff2                 (وزن 400)
  Amiri-Bold.woff2                    (وزن 700)

من أين تحصل عليها
------------------
IBM Plex Sans Arabic:
  - Fontsource: https://fontsource.org/fonts/ibm-plex-sans-arabic
    npm i @fontsource/ibm-plex-sans-arabic
    ثم انسخ ملفات .woff2 من node_modules/@fontsource/ibm-plex-sans-arabic/files/
    وأعد تسميتها للأسماء أعلاه.
  - أو من Google Fonts: https://fonts.google.com/specimen/IBM+Plex+Sans+Arabic
    (نزّل TTF ثم حوّله إلى woff2 عبر أداة مثل woff2_compress أو fonttools).

Amiri:
  - Fontsource: https://fontsource.org/fonts/amiri
    npm i @fontsource/amiri
  - أو Google Fonts: https://fonts.google.com/specimen/Amiri
  - المصدر الرسمي: https://github.com/alif-type/amiri/releases

ملاحظات
-------
- استخدم صيغة woff2 فقط (الأصغر والأوسع دعماً). لا حاجة لصيغ أخرى.
- الأسماء حسّاسة لحالة الأحرف على خوادم Linux — التزم بالأسماء أعلاه بالضبط.
- لا تُضِف روابط خطوط خارجية في index.html؛ ذلك يكسر ضمان العمل بلا إنترنت.
