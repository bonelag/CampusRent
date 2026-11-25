@echo off
chcp 65001 >nul
title CampusRent - Production Database
cd /d "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run.ps1" -UseProductionDatabase -SkipConfirmation %*
if errorlevel 1 (
    echo.
    echo Khong the khoi dong CampusRent voi database production.
    echo.
    pause
    exit /b 1
)
exit /b 0
