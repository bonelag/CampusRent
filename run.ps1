$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "        DANG KHOI CHAY HE THONG CAMPUSRENT LOCAL        " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path "client\node_modules")) {
    Write-Host "[1/3] Dang cai dat thu vien frontend..." -ForegroundColor Yellow
    npm --prefix client install
} else {
    Write-Host "[1/3] Thu vien frontend da san sang." -ForegroundColor Green
}

Write-Host "[2/3] Dang khoi dong Backend ASP.NET Core API (port 5059)..." -ForegroundColor Yellow
Start-Process cmd -ArgumentList "/k cd /d `"$PSScriptRoot`" && dotnet run --project server --urls http://localhost:5059"

Write-Host "[3/3] Dang khoi dong Frontend React (port 5173)..." -ForegroundColor Yellow
Start-Process cmd -ArgumentList "/k cd /d `"$PSScriptRoot`" && npm --prefix client run dev"

Write-Host ""
Write-Host "Dang mo trinh duyet web..." -ForegroundColor Cyan
Start-Sleep -Seconds 3
Start-Process "http://localhost:5173"

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "                 KHOI CHAY THANH CONG!                  " -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  - Web ung dung:    http://localhost:5173"
Write-Host "  - Backend API:     http://localhost:5059"
Write-Host "  - Swagger API Doc: http://localhost:5059/swagger"
Write-Host "  - Health Check:    http://localhost:5059/health"
Write-Host "--------------------------------------------------------"
Write-Host "  Tai khoan demo (Mat khau: Password@123):"
Write-Host "    * Sinh vien: nguyenvanan@sis.hust.edu.vn"
Write-Host "    * Chu tro:   landlord1@gmail.com"
Write-Host "    * Quan tri:  admin@campusrent.vn"
Write-Host "========================================================"
