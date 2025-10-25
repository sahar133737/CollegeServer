# PowerShell скрипт для запуска тестов CollegeServer

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Запуск тестов для CollegeServer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Проверка установки .NET
Write-Host "Проверка установки .NET..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host ".NET версия: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "Ошибка: .NET не установлен или не найден в PATH" -ForegroundColor Red
    Read-Host "Нажмите Enter для выхода"
    exit 1
}
Write-Host ""

# Восстановление пакетов
Write-Host "Восстановление пакетов..." -ForegroundColor Yellow
try {
    dotnet restore
    Write-Host "Пакеты восстановлены успешно" -ForegroundColor Green
} catch {
    Write-Host "Ошибка при восстановлении пакетов" -ForegroundColor Red
    Read-Host "Нажмите Enter для выхода"
    exit 1
}
Write-Host ""

# Сборка проекта
Write-Host "Сборка проекта..." -ForegroundColor Yellow
try {
    dotnet build
    Write-Host "Проект собран успешно" -ForegroundColor Green
} catch {
    Write-Host "Ошибка при сборке проекта" -ForegroundColor Red
    Read-Host "Нажмите Enter для выхода"
    exit 1
}
Write-Host ""

# Запуск тестов
Write-Host "Запуск тестов..." -ForegroundColor Yellow
try {
    $testResult = dotnet test --verbosity normal --logger "console;verbosity=detailed"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Все тесты прошли успешно!" -ForegroundColor Green
    } else {
        Write-Host "Некоторые тесты не прошли" -ForegroundColor Red
    }
} catch {
    Write-Host "Ошибка при запуске тестов" -ForegroundColor Red
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Завершение" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Read-Host "Нажмите Enter для выхода"
