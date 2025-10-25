@echo off
echo ========================================
echo Запуск тестов для CollegeServer
echo ========================================
echo.

echo Проверка установки .NET...
dotnet --version
if %errorlevel% neq 0 (
    echo Ошибка: .NET не установлен или не найден в PATH
    pause
    exit /b 1
)
echo.

echo Восстановление пакетов...
dotnet restore
if %errorlevel% neq 0 (
    echo Ошибка при восстановлении пакетов
    pause
    exit /b 1
)
echo.

echo Сборка проекта...
dotnet build
if %errorlevel% neq 0 (
    echo Ошибка при сборке проекта
    pause
    exit /b 1
)
echo.

echo Запуск тестов...
dotnet test --verbosity normal --logger "console;verbosity=detailed"
if %errorlevel% neq 0 (
    echo Некоторые тесты не прошли
) else (
    echo Все тесты прошли успешно!
)
echo.

echo ========================================
echo Завершение
echo ========================================
pause
