@echo off
cd ../../

call git submodule update --init --recursive
call dotnet build -c Debug

if errorlevel 1 (
    echo.
    pause
    exit /b %errorlevel%
)

:: Сборка выполнена успешно.
exit /b 0