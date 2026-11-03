@echo off
chcp 65001 >nul
title CampusRent - Local Launcher
cd /d "%~dp0"

echo ========================================================
echo        DANG KHOI CHAY HE THONG CAMPUSRENT LOCAL        
echo ========================================================
echo.

if not exist "client\node_modules" (
    echo [1/3] Dang cai dat thu vien frontend...
    call npm --prefix client install
) else (
    echo [1/3] Thu vien frontend da san sang.
)

echo [2/3] Dang khoi dong Backend ASP.NET Core API (port 5059)...
start "CampusRent Backend API" cmd /k "cd /d "%~dp0" & dotnet run --project server --urls http://localhost:5059"

echo [3/3] Dang khoi dong Frontend React (port 5173)...
start "CampusRent Frontend" cmd /k "cd /d "%~dp0" & npm --prefix client run dev"

echo.
echo Dang mo trinh duyet web...
timeout /t 3 /nobreak >nul
start http://localhost:5173

echo.
echo ========================================================
echo                 KHOI CHAY THANH CONG!
echo ========================================================
echo   - Web ung dung:    http://localhost:5173
echo   - Backend API:     http://localhost:5059
echo   - Swagger API Doc: http://localhost:5059/swagger
echo   - Health Check:    http://localhost:5059/health
echo --------------------------------------------------------
echo   Tai khoan demo (Mat khau: Password@123):
echo     * Sinh vien: nguyenvanan@sis.hust.edu.vn
echo     * Chu tro:   landlord1@gmail.com
echo     * Quan tri:  admin@campusrent.vn
echo ========================================================
echo.
pause
