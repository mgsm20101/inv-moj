@echo off
chcp 65001 >nul
setlocal
rem ── مشغّل WIMS: بيئة الحزمة الذاتية على http://localhost:5000 ──
set ASPNETCORE_ENVIRONMENT=SelfHost
set ASPNETCORE_URLS=http://localhost:5000

cd /d "%~dp0app"

if not exist "WIMS.WebApi.exe" (
  echo [WIMS] لم يُعثر على WIMS.WebApi.exe — تأكد أنك داخل مجلد الحزمة.
  pause
  exit /b 1
)

echo [WIMS] بدء التشغيل على http://localhost:5000 ...
start "WIMS" WIMS.WebApi.exe
timeout /t 4 /nobreak >nul
start "" http://localhost:5000

echo [WIMS] النظام يعمل الآن في نافذة منفصلة عنوانها "WIMS".
echo [WIMS] لإيقاف النظام: أغلِق نافذة "WIMS".
