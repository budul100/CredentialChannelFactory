@echo off
setlocal

rem Place this file next to the .sln. Requires the cstidy global tool.
rem Trailing "." avoids the "\"" quoting problem of a path ending with a backslash.
set "ROOT=%~dp0."

where cstidy >nul 2>&1
if errorlevel 1 (
    echo.
    echo cstidy was not found on PATH. Install it as global tool first, see the CSTidy README.
    echo.
    pause
    exit /b 1
)

:menu
cls
echo.
echo  CSTidy tools in %ROOT%
echo.
echo   Tidy
echo     [1] Preview changes (diff, writes nothing)
echo     [2] Tidy files
echo.
echo   Clean
echo     [3] Preview artifacts to delete (bin, obj, TestResults, ...)
echo     [4] Delete artifacts
echo.
echo   Repository
echo     [5] Hide dot files
echo     [6] Update git index
echo     [7] Hide dot files + update git index
echo.
echo   Other
echo     [8] License check
echo.
echo   [Q] Quit
echo.
choice /c 12345678Q /n /m "Select: "
goto :opt%errorlevel%

:opt1
cstidy "%ROOT%" --diff
goto :done

:opt2
cstidy "%ROOT%"
goto :done

:opt3
cstidy clean "%ROOT%" --dry-run
goto :done

:opt4
echo.
choice /c YN /n /m "Delete build and test artifacts now? [Y/N] "
if errorlevel 2 goto :menu
cstidy clean "%ROOT%"
goto :done

:opt5
cstidy hide "%ROOT%"
goto :done

:opt6
cstidy git "%ROOT%"
goto :done

:opt7
cstidy refresh "%ROOT%"
goto :done

:opt8
cstidy licenses "%ROOT%"
goto :done

:opt9
endlocal
exit /b 0

:done
echo.
pause
goto :menu
