@echo off
REM =============================================================================
REM Test Migration Script for Windows
REM =============================================================================
REM Test migrations in a Docker container (simulates K8s environment)

setlocal enabledelayedexpansion

set IMAGE_NAME=gdprdsar-tool:latest

echo ===================================
echo Testing Migration in Container
echo ===================================
echo.

REM Check if image exists
docker images | findstr /C:"%IMAGE_NAME%" >nul
if %ERRORLEVEL% neq 0 (
    echo Image %IMAGE_NAME% not found. Building...
    docker build -t %IMAGE_NAME% .
    echo.
)

echo Testing migration with provided connection string...
set /p CONN_STRING="Enter connection string (or press Enter for default): "

if "%CONN_STRING%"=="" (
    echo No connection string provided. Using default.
    set CONN_STRING=Server=localhost;Database=GdprDsarTool_Test;User ID=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
)

echo.
echo Running migration in container...
echo.

docker run --rm ^
    --network=host ^
    -e "ConnectionStrings__DefaultConnection=%CONN_STRING%" ^
    -e "ASPNETCORE_ENVIRONMENT=Production" ^
    %IMAGE_NAME% ^
    dotnet GdprDsarTool.dll --migrate

set EXIT_CODE=%ERRORLEVEL%

echo.
if %EXIT_CODE% equ 0 (
    echo ===================================
    echo Migration test SUCCESSFUL
    echo ===================================
) else (
    echo ===================================
    echo Migration test FAILED
    echo ===================================
)

exit /b %EXIT_CODE%
