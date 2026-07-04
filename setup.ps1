<#
.SYNOPSIS
  تهيئة WIMS لمرة واحدة على جهاز المستخدم: ضبط SQL Server Express، توليد مفتاح JWT، وكلمة مرور admin.
.DESCRIPTION
  يُشغَّل مرة واحدة من داخل مجلد الحزمة بعد فكّ الضغط. يعدّل app\appsettings.SelfHost.json محلياً.
  آمن للتكرار (Idempotent): لا يُعيد توليد مفتاح JWT موجود.
#>
#requires -Version 5
$ErrorActionPreference = 'Stop'

$cfgPath = Join-Path $PSScriptRoot 'app\appsettings.SelfHost.json'
if (-not (Test-Path $cfgPath)) {
    throw "لم يُعثر على $cfgPath — شغّل هذا السكربت من داخل مجلد الحزمة (بجوار Start-WIMS.bat)."
}

$cfg = Get-Content $cfgPath -Raw -Encoding UTF8 | ConvertFrom-Json

Write-Host "== تهيئة WIMS ==" -ForegroundColor Cyan

# ── 1) SQL Server Express instance ──
$inst = Read-Host "اسم SQL Server instance [افتراضي: .\SQLEXPRESS]"
if ([string]::IsNullOrWhiteSpace($inst)) { $inst = '.\SQLEXPRESS' }
$cfg.ConnectionStrings.Default =
    "Server=$inst;Database=WIMS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
Write-Host "  ✔ سلسلة الاتصال: $inst / قاعدة WIMS" -ForegroundColor Green

# ── 2) مفتاح توقيع JWT (توليد إن غاب) ──
if ([string]::IsNullOrWhiteSpace($cfg.Jwt.SigningKey)) {
    $bytes = New-Object 'System.Byte[]' 48
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $cfg.Jwt.SigningKey = [Convert]::ToBase64String($bytes)
    Write-Host "  ✔ تم توليد مفتاح JWT عشوائي." -ForegroundColor Green
} else {
    Write-Host "  • مفتاح JWT موجود — تُرك كما هو." -ForegroundColor Yellow
}

# ── 3) كلمة مرور admin ──
$sec = Read-Host "كلمة مرور admin (اتركها فارغة لاستخدام Admin@12345 مؤقتاً) — 8+ أحرف، حرف كبير/صغير/رقم/رمز" -AsSecureString
$plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec))
if (-not [string]::IsNullOrWhiteSpace($plain)) {
    $cfg.Seed.AdminPassword = $plain
    Write-Host "  ✔ ضُبطت كلمة مرور admin." -ForegroundColor Green
} else {
    Write-Host "  • لم تُضبط — سيُستخدم Admin@12345 (غيّرها فوراً بعد الدخول)." -ForegroundColor Yellow
}

$cfg | ConvertTo-Json -Depth 10 | Set-Content $cfgPath -Encoding UTF8

Write-Host ""
Write-Host "✔ تمّت التهيئة. شغّل الآن: Start-WIMS.bat" -ForegroundColor Green
