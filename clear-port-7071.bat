@echo off
REM Quick script to clear port 7071

echo ===================================
echo Port 7071 Cleanup
echo ===================================
echo.

echo [1/3] Checking port 7071...
netstat -ano | findstr :7071 > temp_port_check.txt

if %ERRORLEVEL% NEQ 0 (
    echo [OK] Port 7071 is not in use
    del temp_port_check.txt 2>nul
    echo.
 echo You can now start Functions:
    echo   func start
    goto :end
)

echo [FOUND] Port 7071 is in use by:
type temp_port_check.txt
echo.

echo [2/3] Finding process IDs...
for /f "tokens=5" %%a in (temp_port_check.txt) do (
    set PID=%%a
    goto :kill_process
)

:kill_process
echo [3/3] Killing process with PID: %PID%
taskkill /F /PID %PID% >nul 2>&1

if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] Process killed successfully
    del temp_port_check.txt 2>nul
    echo.
    echo Port 7071 is now free!
 echo You can start Functions:
    echo   func start
) else (
    echo [ERROR] Could not kill process
    del temp_port_check.txt 2>nul
    echo.
    echo You may need to run this as Administrator:
    echo   1. Right-click Command Prompt
    echo   2. Select "Run as Administrator"
    echo   3. Run: clear-port-7071.bat
    echo.
    echo Or manually kill in Task Manager:
    echo   Ctrl+Shift+Esc ^> Find 'func.exe' ^> End Task
)

:end
echo.
pause
