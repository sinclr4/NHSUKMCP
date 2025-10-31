# Clearing Ports (7071, 6277, etc.)

## Problem
A port is already in use and you can't start Azure Functions or MCP Inspector.

## ? Quick Fix (Recommended)

### Option 1: Clear All Processes (Recommended)
```powershell
.\cleanup-all.ps1
```

This script will:
- ? Kill all `func.exe` processes
- ? Check and clear common ports (7071, 6277, 5173)
- ? Optionally kill orphaned Node.js processes
- ? Verify all ports are free

### Option 2: Clear Specific Port
```powershell
# Clear port 7071 (default Azure Functions port)
.\clear-port-7071.ps1

# Clear port 6277 (or any other port)
.\clear-port.ps1 6277

# Clear port 5173 (MCP Inspector UI)
.\clear-port.ps1 5173
```

### Option 3: Manual PowerShell Commands
```powershell
# 1. Find what's using a port (e.g., 6277)
netstat -ano | findstr :6277

# 2. Find the PID (last column) - example output:
#    TCP    [::1]:6277    [::]:0    LISTENING    12132
#      ^^^^^ this is the PID

# 3. Kill the process (replace 12132 with your PID)
Stop-Process -Id 12132 -Force

# 4. Verify port is free
netstat -ano | findstr :6277
# (should return nothing)
```

---

## ?? Common Causes

### 1. Azure Functions Still Running (Port 7071)
**Symptom**: "Port 7071 is already in use"

**Fix**: Kill func.exe process
```powershell
Get-Process -Name "func" | Stop-Process -Force
```

### 2. MCP Inspector Still Running (Port 6277 or 5173)
**Symptom**: "Port 6277 is already in use" or "Port 5173 is already in use"

**Fix**: Kill Node.js processes
```powershell
# Kill specific port
.\clear-port.ps1 6277

# Or kill all node processes
Get-Process -Name "node" | Stop-Process -Force
```

### 3. Multiple Orphaned Processes
**Symptom**: Multiple ports in use, unclear what's running

**Fix**: Complete cleanup
```powershell
.\cleanup-all.ps1
```

---

## ?? Common Ports Reference

| Port | Used By | Clear Command |
|------|---------|---------------|
| 7071 | Azure Functions (default) | `.\clear-port-7071.ps1` |
| 6277 | MCP Inspector server | `.\clear-port.ps1 6277` |
| 5173 | MCP Inspector UI (Vite) | `.\clear-port.ps1 5173` |

---

## ??? Alternative Methods

### Method 1: Task Manager (GUI)
1. Press `Ctrl+Shift+Esc` to open Task Manager
2. Go to "Details" tab
3. Find `func.exe` or `dotnet.exe`
4. Right-click ? "End Task"
5. Confirm when prompted

### Method 2: Command Line (One-liner)
```powershell
# Kill all func.exe processes
taskkill /F /IM func.exe

# Or kill by PID
taskkill /F /PID 11316
```

### Method 3: PowerShell (More Control)
```powershell
# Kill func.exe gracefully
Get-Process -Name "func" -ErrorAction SilentlyContinue | Stop-Process

# If that doesn't work, force kill
Get-Process -Name "func" -ErrorAction SilentlyContinue | Stop-Process -Force
```

---

## ?? If Port Won't Clear

### Run as Administrator
Some processes require admin rights to kill:

1. **PowerShell as Admin**:
   - Right-click PowerShell
   - "Run as Administrator"
 - Run: `.\clear-port-7071.ps1`

2. **Command Prompt as Admin**:
   - Right-click Command Prompt
   - "Run as Administrator"
   - Run: `clear-port-7071.bat`

### Use Resource Monitor
1. Press `Win+R`
2. Type `resmon` and press Enter
3. Go to "Network" tab
4. Look for port 7071 in "Listening Ports"
5. Right-click the process ? "End Process"

---

## ?? After Clearing the Port

Once port 7071 is free:

### Start Functions
```powershell
func start
```

### Verify It's Working
```powershell
.\test-mcp-endpoint.ps1
```

### Launch Inspector
```bash
npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp
```

### Or Use One-Click Launcher
```cmd
launch-inspector.bat
```

---

## ?? Prevent This Issue

### Always Stop Functions Cleanly
When stopping Functions, use `Ctrl+C` in the terminal (not just closing the window)

### Check for Running Processes Before Starting
```powershell
# Add this to your workflow
Get-Process -Name "func" -ErrorAction SilentlyContinue
```

### Use the Launcher Script
The `launch-inspector.bat` script checks if Functions is already running before starting a new instance.

---

## ?? Complete Troubleshooting Checklist

- [ ] Port 7071 is in use
  - [ ] Run `.\clear-port-7071.ps1`
  - [ ] Or manually kill func.exe in Task Manager

- [ ] Port still in use after killing process
  - [ ] Run script as Administrator
  - [ ] Check for other applications using port 7071
  - [ ] Restart your computer (last resort)

- [ ] Want to use different port
  - [ ] Start Functions: `func start --port 7072`
  - [ ] Update Inspector URL: `http://localhost:7072/api/mcp`

- [ ] Process keeps coming back
  - [ ] Check for auto-start services
  - [ ] Check Task Scheduler for automated tasks
  - [ ] Check startup programs

---

## ?? Emergency: Nuclear Option

If nothing else works, restart your computer. This will clear all ports and processes.

```powershell
# Save your work first!
Restart-Computer
```

---

## ?? Scripts Available

| Script | Purpose | Usage |
|--------|---------|-------|
| `clear-port-7071.ps1` | Kill process using port 7071 (PowerShell) | `.\clear-port-7071.ps1` |
| `clear-port.bat` | Kill process using specified port (Batch) | `clear-port.bat 6277` |
| `cleanup-all.ps1` | Kill all processes and clear common ports (PowerShell) | `.\cleanup-all.ps1` |
| `launch-inspector.bat` | Start Functions + Inspector | `launch-inspector.bat` |
| `diagnose-mcp-inspector.ps1` | Full diagnostic | `.\diagnose-mcp-inspector.ps1` |

---

## ?? Pro Tips

### 1. Always Check First
Before starting Functions, check if it's already running:
```powershell
Get-Process -Name "func" -ErrorAction SilentlyContinue
```

### 2. Use Unique Ports for Different Projects
If you have multiple Functions projects:
```powershell
# Project 1
func start --port 7071

# Project 2
func start --port 7072

# Project 3
func start --port 7073
```

### 3. Create a Start Script
Create `start-clean.ps1`:
```powershell
# Kill any existing func processes
Get-Process -Name "func" -ErrorAction SilentlyContinue | Stop-Process -Force

# Wait a moment
Start-Sleep -Seconds 2

# Start Functions fresh
func start
```

---

## ? Summary

**Quick Fix**:
```powershell
# Run this script
.\clear-port-7071.ps1

# Then start Functions
func start
```

**Manual Fix**:
1. Open Task Manager (`Ctrl+Shift+Esc`)
2. Find `func.exe` in Details tab
3. Right-click ? End Task
4. Run `func start`

**Port is now free!** ??
