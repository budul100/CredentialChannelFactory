@echo off
setlocal

rem Place this file and Publish.ps1 next to the .sln.
rem Trailing "." avoids the "\"" quoting problem of a path ending with a backslash.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Publish.ps1" -Root "%~dp0."
set "EXITCODE=%errorlevel%"

echo.
pause
exit /b %EXITCODE%
