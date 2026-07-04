<#
.SYNOPSIS
  يبني حزمة WIMS التنفيذية الذاتية (جهاز واحد + SQL Server Express).
.DESCRIPTION
  1) يبني واجهة Angular (production)  2) ينسخها إلى wwwroot الخاص بالـ API
  3) ينشر self-contained single-file (win-x64) بلا حاجة لتنصيب .NET runtime
  4) يجمّع مجلد التسليم dist/WIMS-Package مع سكربتات التشغيل والدليل.
.NOTES
  يتطلّب: Node/npm، .NET SDK، واتصال إنترنت أثناء dotnet publish (لجلب runtime pack لـ win-x64).
#>
#requires -Version 5
$ErrorActionPreference = 'Stop'

$root    = $PSScriptRoot
$client  = Join-Path $root 'wims-client'
$webApi  = Join-Path $root 'src\WIMS.WebApi'
$wwwroot = Join-Path $webApi 'wwwroot'
$outDir  = Join-Path $root 'dist\WIMS-Package'
$appDir  = Join-Path $outDir 'app'

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

Write-Host '== 3/4 نشر self-contained (win-x64, single-file) ==' -ForegroundColor Cyan
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
dotnet publish $webApi -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $appDir
if ($LASTEXITCODE -ne 0) { throw "فشل dotnet publish." }

Write-Host '== 4/4 تجميع الحزمة ==' -ForegroundColor Cyan
Copy-Item (Join-Path $root 'Start-WIMS.bat')     $outDir -Force
Copy-Item (Join-Path $root 'setup.ps1')          $outDir -Force
Copy-Item (Join-Path $root 'README-User-AR.md')  $outDir -Force

Write-Host ""
Write-Host "✔ تمّت الحزمة: $outDir" -ForegroundColor Green
Write-Host "  للتسليم: اضغط المجلد كـ ZIP وسلّمه للمستخدم." -ForegroundColor Green
