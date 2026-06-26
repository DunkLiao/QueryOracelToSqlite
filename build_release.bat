@echo off
setlocal

set "ROOT=%~dp0"
set "PROJECT=%ROOT%src\OracleToSqlite.App\OracleToSqlite.App.csproj"
set "PUBLISH_DIR=%ROOT%src\OracleToSqlite.App\bin\Release\net8.0-windows\win-x64\publish"

echo.
echo Building OracleToSqlite.App release executable...
echo Project: "%PROJECT%"
echo.

dotnet restore "%PROJECT%"
if errorlevel 1 goto :fail

dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 goto :fail

echo.
echo Build completed.
echo Executable:
echo "%PUBLISH_DIR%\OracleToSqlite.App.exe"
echo.
start "" "%PUBLISH_DIR%"
pause
exit /b 0

:fail
echo.
echo Build failed. Please review the error messages above.
pause
exit /b 1
