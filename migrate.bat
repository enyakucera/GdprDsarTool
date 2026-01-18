@echo off
REM =============================================================================
REM Database Migration Script for Windows
REM =============================================================================
REM This script runs database migrations locally or in CI/CD
REM Usage: migrate.bat [environment]
REM        migrate.bat Development
REM        migrate.bat Production

setlocal

set ENVIRONMENT=%1
if "%ENVIRONMENT%"=="" set ENVIRONMENT=Development

set PROJECT_DIR=src\GdprDsarTool

echo ===================================
echo Database Migration Runner
echo ===================================
echo Environment: %ENVIRONMENT%
echo Project: %PROJECT_DIR%
echo.

cd %PROJECT_DIR%

set ASPNETCORE_ENVIRONMENT=%ENVIRONMENT%

echo Running migrations...
dotnet run --no-build -- --migrate

if %ERRORLEVEL% neq 0 (
    echo.
    echo ===================================
    echo Migration FAILED!
    echo ===================================
    exit /b %ERRORLEVEL%
)

echo.
echo ===================================
echo Migration completed successfully!
echo ===================================
