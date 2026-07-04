<#
.SYNOPSIS
  يبني حزمة WIMS جاهزة للرفع على استضافة MonsterASP.NET (IIS مشتركة + MSSQL).
.DESCRIPTION
  1) يبني واجهة Angular (production)  2) ينسخها إلى wwwroot الخاص بالـ API
  3) ينشر framework-dependent (win-x86 — يطابق مثال MonsterASP.NET الرسمي لبيئتهم)
  4) يضغط الناتج إلى ZIP جاهز للرفع عبر FTP أو File Manager.
.NOTES
  يتطلّب: Node/npm، .NET SDK، واتصال إنترنت أثناء dotnet publish (لجلب حزمة win-x86 المرجعية).
  بعد الرفع: عدّل appsettings.Production.json على الخادم مباشرة (سلسلة الاتصال + Jwt:SigningKey
  + Seed:AdminPassword) — راجع DEPLOY-MonsterASP-AR.md لخطوات لوحة التحكّم كاملة.
#>
#requires -Version 5
$ErrorActionPreference = 'Stop'

$root    = $PSScriptRoot
$client  = Join-Path $root 'wims-client'
$webApi  = Join-Path $root 'src\WIMS.WebApi'
$wwwroot = Join-Path $webApi 'wwwroot'
$outDir  = Join-Path $root 'dist\MonsterASP-Package'
$zipPath = Join-Path $root 'dist\MonsterASP-Package.zip'

Write-Host '== 1/4 بناء واجهة Angular (production) ==' -ForegroundColor Cyan
Push-Location $client
try {
    if (Test-Path (Join-Path $client 'package-lock.json')) { npm ci } else { npm install }
    npx ng build --configuration production
    if ($LASTEXITCODE -ne 0) { throw "فشل بناء Angular." }
} finally { Pop-Location }

Write-Host '== 2/4 نسخ الواجهة إلى wwwroot ==' -ForegroundColor Cyan
if (Test-Path $wwwroot) { Remove-Item $wwwroot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $wwwroot | Out-Null
$browser = Join-Path $client 'dist\wims-client\browser'
if (-not (Test-Path $browser)) { throw "لم يُعثر على مخرجات Angular في $browser" }
Copy-Item (Join-Path $browser '*') $wwwroot -Recurse -Force

Write-Host '== 3/4 نشر framework-dependent (win-x86) ==' -ForegroundColor Cyan
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
dotnet publish $webApi -c Release -r win-x86 --self-contained false -o $outDir
if ($LASTEXITCODE -ne 0) { throw "فشل dotnet publish." }

Write-Host '== 4/4 ضغط الحزمة لسهولة الرفع ==' -ForegroundColor Cyan
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zipPath

Write-Host ""
Write-Host "✔ تمّت الحزمة: $outDir" -ForegroundColor Green
Write-Host "✔ ملف مضغوط جاهز للرفع: $zipPath" -ForegroundColor Green
Write-Host ""
Write-Host "تنبيه: appsettings.Production.json داخل الحزمة قالب فارغ من الأسرار عمداً." -ForegroundColor Yellow
Write-Host "  عدّله على الخادم بعد الرفع (سلسلة الاتصال + Jwt:SigningKey + Seed:AdminPassword)." -ForegroundColor Yellow
Write-Host "  راجع DEPLOY-MonsterASP-AR.md لخطوات لوحة التحكّم والرفع كاملة." -ForegroundColor Yellow
