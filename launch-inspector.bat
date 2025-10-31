@echo off
REM Quick launch script for MCP Inspector
REM This script starts both Functions and Inspector

echo ===================================
echo MCP Inspector Quick Launch
echo ===================================
echo.

REM Check if Functions is already running
tasklist /FI "IMAGENAME eq func.exe" 2>NUL | find /I /N "func.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo [INFO] Azure Functions is already running
) else (
    echo [INFO] Starting Azure Functions in background...
    start /B cmd /c "func start > functions.log 2>&1"
    echo [INFO] Waiting 10 seconds for Functions to start...
    timeout /t 10 /nobreak >nul
)

echo.
echo [INFO] Testing MCP endpoint...
curl.exe -X POST http://localhost:7071/api/mcp -H "Content-Type: application/json" -d "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2024-11-05\"}}" --silent --max-time 5 >nul 2>&1

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Cannot connect to MCP endpoint at http://localhost:7071/api/mcp
    echo [ERROR] Make sure Functions is running on port 7071
    echo.
    echo Press any key to exit...
    pause >nul
    exit /b 1
)

echo [SUCCESS] MCP endpoint is responding
echo.
echo [INFO] Launching MCP Inspector...
echo [INFO] Browser will open at http://localhost:5173
echo [INFO] Connecting to: http://localhost:7071/api/mcp
echo.

npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp

echo.
echo Inspector closed.
echo.
echo Press any key to exit...
pause >nul
