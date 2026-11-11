param(
    [switch]$UseProductionDatabase,
    [switch]$SkipConfirmation
)

$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot

function Read-DotEnv([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Khong tim thay file cau hinh: $Path"
    }

    $values = @{}
    foreach ($line in [System.IO.File]::ReadAllLines($Path)) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#")) { continue }
        $separator = $trimmed.IndexOf("=")
        if ($separator -lt 1) { continue }
        $key = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim()
        if ($value.Length -ge 2 -and (($value[0] -eq '"' -and $value[-1] -eq '"') -or ($value[0] -eq "'" -and $value[-1] -eq "'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        $values[$key] = $value
    }
    return $values
}

function Stop-WorkspaceBackend {
    $listeners = Get-NetTCPConnection -LocalPort 5059 -State Listen -ErrorAction SilentlyContinue
    foreach ($ownerId in @($listeners | Select-Object -ExpandProperty OwningProcess -Unique)) {
        $process = Get-Process -Id $ownerId -ErrorAction SilentlyContinue
        if (-not $process) { continue }
        $path = $process.Path
        $serverRoot = Join-Path $PSScriptRoot "server"
        if (-not $path -or -not $path.StartsWith($serverRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Cong 5059 dang duoc tien trinh khac su dung (PID $ownerId)."
        }
        Write-Host "Dang dung backend CampusRent cu (PID $ownerId)..." -ForegroundColor Yellow
        Stop-Process -Id $ownerId -Force
        Start-Sleep -Milliseconds 500
    }
}

function Set-FromDotEnv($Settings, [string]$SourceName, [string]$TargetName) {
    $value = $Settings[$SourceName]
    if ([string]::IsNullOrWhiteSpace($value)) {
        Remove-Item "Env:$TargetName" -ErrorAction SilentlyContinue
    } else {
        [Environment]::SetEnvironmentVariable($TargetName, $value, "Process")
    }
}

Write-Host "========================================================" -ForegroundColor Cyan
if ($UseProductionDatabase) {
    Write-Host " CAMPUSRENT LOCAL - PRODUCTION DATABASE (FULL ACCESS)" -ForegroundColor Red
} else {
    Write-Host "        CAMPUSRENT LOCAL - SQLITE DEVELOPMENT" -ForegroundColor Cyan
}
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path "client\node_modules")) {
    Write-Host "[1/3] Dang cai dat thu vien frontend..." -ForegroundColor Yellow
    npm --prefix client install
} else {
    Write-Host "[1/3] Thu vien frontend da san sang." -ForegroundColor Green
}

$environmentNames = @(
    "ASPNETCORE_ENVIRONMENT",
    "Database__Provider",
    "DATABASE_URL",
    "Jwt__Secret",
    "ClientUrl",
    "Admin__Email",
    "Admin__Password",
    "Cloudinary__CloudName",
    "Cloudinary__ApiKey",
    "Cloudinary__ApiSecret",
    "GooglePlaces__ApiKey"
)
$savedEnvironment = @{}
foreach ($name in $environmentNames) {
    $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
}

if ($UseProductionDatabase) {
    $settings = Read-DotEnv (Join-Path $PSScriptRoot ".env")
    if ([string]::IsNullOrWhiteSpace($settings["DATABASE_URL"])) {
        throw "DATABASE_URL khong co trong .env."
    }
    if (-not $SkipConfirmation -and $env:CAMPUSRENT_CONFIRM_PRODUCTION -ne "1") {
        Write-Warning "Backend local se ket noi truc tiep PostgreSQL production voi DAY DU QUYEN DOC/GHI."
        Write-Warning "Dang tin, sua, xoa, dat lich, danh gia va quan tri se thay doi du lieu production that."
        $confirmation = Read-Host "Nhap PRODUCTION de tiep tuc"
        if ($confirmation -cne "PRODUCTION") {
            throw "Da huy khoi dong production database."
        }
    }

    Stop-WorkspaceBackend
    $env:ASPNETCORE_ENVIRONMENT = "Production"
    $env:Database__Provider = "Postgres"
    $env:DATABASE_URL = $settings["DATABASE_URL"]
    $env:ClientUrl = "http://localhost:5173"
    $jwtSecret = $settings["JWT_SECRET"]
    $invalidJwt = [string]::IsNullOrWhiteSpace($jwtSecret) -or
        [Text.Encoding]::UTF8.GetByteCount($jwtSecret) -lt 32 -or
        $jwtSecret.StartsWith("CHANGE_ME") -or
        $jwtSecret.StartsWith("CampusRent_Production_Secret_Key_") -or
        $jwtSecret -eq "CampusRent-local-development-only-secret-2026"
    if ($invalidJwt) {
        $bytes = New-Object byte[] 48
        $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
        try { $generator.GetBytes($bytes) } finally { $generator.Dispose() }
        $env:Jwt__Secret = [Convert]::ToBase64String($bytes)
        Write-Warning "JWT_SECRET trong .env la gia tri mau; da tao secret tam cho phien local nay."
    } else {
        $env:Jwt__Secret = $jwtSecret
    }
    Set-FromDotEnv $settings "ADMIN_EMAIL" "Admin__Email"
    Set-FromDotEnv $settings "ADMIN_PASSWORD" "Admin__Password"
    Set-FromDotEnv $settings "CLOUDINARY_CLOUD_NAME" "Cloudinary__CloudName"
    Set-FromDotEnv $settings "CLOUDINARY_API_KEY" "Cloudinary__ApiKey"
    Set-FromDotEnv $settings "CLOUDINARY_API_SECRET" "Cloudinary__ApiSecret"
    Set-FromDotEnv $settings "GOOGLE_PLACES_API_KEY" "GooglePlaces__ApiKey"
} else {
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:Database__Provider = "Sqlite"
    Remove-Item Env:DATABASE_URL -ErrorAction SilentlyContinue
    Remove-Item Env:Jwt__Secret -ErrorAction SilentlyContinue
    $env:ClientUrl = "http://localhost:5173"
}

try {
    Write-Host "[2/3] Dang khoi dong Backend ASP.NET Core API (port 5059)..." -ForegroundColor Yellow
    Start-Process cmd -ArgumentList "/k cd /d `"$PSScriptRoot`" && dotnet run --project server --urls http://localhost:5059" -WindowStyle Normal
} finally {
    foreach ($name in $environmentNames) {
        $previous = $savedEnvironment[$name]
        if ($null -eq $previous) {
            Remove-Item "Env:$name" -ErrorAction SilentlyContinue
        } else {
            [Environment]::SetEnvironmentVariable($name, $previous, "Process")
        }
    }
}

$frontendRunning = Get-NetTCPConnection -LocalPort 5173 -State Listen -ErrorAction SilentlyContinue
if ($frontendRunning) {
    Write-Host "[3/3] Frontend React dang chay tren port 5173." -ForegroundColor Green
} else {
    Write-Host "[3/3] Dang khoi dong Frontend React (port 5173)..." -ForegroundColor Yellow
    Start-Process cmd -ArgumentList "/k cd /d `"$PSScriptRoot`" && npm --prefix client run dev" -WindowStyle Normal
}

Write-Host ""
Write-Host "Dang mo trinh duyet web..." -ForegroundColor Cyan
Start-Sleep -Seconds 3
Start-Process "http://localhost:5173"

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "                 KHOI CHAY THANH CONG!" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  - Web ung dung:    http://localhost:5173"
Write-Host "  - Backend API:     http://localhost:5059"
Write-Host "  - Health Check:    http://localhost:5059/health"
if ($UseProductionDatabase) {
    Write-Host "  - Database:        PostgreSQL production (FULL ACCESS)" -ForegroundColor Red
    Write-Host "  - Runtime:         ASP.NET Core Production (Swagger tat)" -ForegroundColor Red
    Write-Host "  - Moi thao tac se tac dong truc tiep du lieu production." -ForegroundColor Red
} else {
    Write-Host "  - Swagger API Doc: http://localhost:5059/swagger"
    Write-Host "--------------------------------------------------------"
    Write-Host "  Tai khoan demo (Mat khau: Password@123):"
    Write-Host "    * Sinh vien: nguyenvanan@sis.hust.edu.vn"
    Write-Host "    * Chu tro:   landlord1@gmail.com"
    Write-Host "    * Quan tri:  admin@campusrent.vn"
}
Write-Host "========================================================"
