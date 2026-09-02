@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0srdk.ps1" %*
exit /b %ERRORLEVEL%
