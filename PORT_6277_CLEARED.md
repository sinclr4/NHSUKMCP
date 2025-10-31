# Port 6277 Cleared + Cleanup Tools Created

## ? What Was Done

### 1. Cleared Port 6277
- **Found**: Process ID 12132 (node.exe - MCP Inspector)
- **Action**: Killed the process
- **Status**: ? Port 6277 is now free

### 2. Created New Scripts

#### `cleanup-all.ps1` - Complete Cleanup (Recommended)
```powershell
.\cleanup-all.ps1
```

**Does everything**:
- Kills all `func.exe` processes
- Clears ports 7071, 6277, 5173
- Optionally kills orphaned Node.js processes
- Verifies all ports are free

**Use this when**:
- You have multiple processes running
- Unsure what's blocking ports
- Want a fresh start

#### `clear-port.ps1` - Clear Specific Port
```powershell
.\clear-port.ps1 6277      # Clear port 6277
.\clear-port.ps1 7071# Clear port 7071
.\clear-port.ps1 5173  # Clear port 5173
```

**Does**:
- Finds process using specified port
- Kills that process
- Verifies port is free

**Use this when**:
- You know the exact port number
- Want to be selective about what to kill

#### `clear-port-7071.ps1` - Quick Azure Functions Clear
```powershell
.\clear-port-7071.ps1
```

**Does**:
- Specifically targets port 7071
- Quick shortcut for common case

**Use this when**:
- Azure Functions won't start
- "Port 7071 already in use" error

### 3. Updated Documentation

#### Renamed File
- ? `CLEARING_PORT_7071.md` (old name)
- ? `CLEARING_PORTS.md` (new name - covers all ports)

#### Updated Files
- `CLEARING_PORTS.md` - Now covers ports 7071, 6277, 5173
- `MCP_INSPECTOR_QUICK_HELP.md` - Added port 6277 error
- `README.md` - Updated references

---

## ?? Current Status

### Processes Running
Based on last check, you still have:
- ? **1 func.exe** process (PID 16920) - Azure Functions
- ?? **4 node.exe** processes - Likely MCP Inspector or related

### Ports Status
- ? **Port 6277**: Cleared (was used by node.exe 12132)
- ? **Port 7071**: Check with `netstat -ano | findstr :7071`
- ? **Port 5173**: Check with `netstat -ano | findstr :5173`

---

## ?? What to Do Now

### Option 1: Complete Fresh Start (Recommended)
```powershell
# Clean everything
.\cleanup-all.ps1

# Start Functions fresh
func start

# Test endpoint
.\test-mcp-endpoint.ps1

# Launch Inspector
npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp
```

### Option 2: Use What's Running
If Functions is already running correctly on port 7071:

```powershell
# Test it's working
.\test-mcp-endpoint.ps1

# Launch Inspector
npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp
```

### Option 3: Selective Cleanup
```powershell
# Just clear the ports you need
.\clear-port.ps1 6277     # Clear Inspector port
.\clear-port-7071.ps1     # Clear Functions port

# Then start fresh
func start
```

---

## ?? Port Reference

| Port | Purpose | Clear Command | Check Command |
|------|---------|---------------|---------------|
| 7071 | Azure Functions API | `.\clear-port-7071.ps1` | `netstat -ano \| findstr :7071` |
| 6277 | MCP Inspector server | `.\clear-port.ps1 6277` | `netstat -ano \| findstr :6277` |
| 5173 | MCP Inspector UI (Vite) | `.\clear-port.ps1 5173` | `netstat -ano \| findstr :5173` |

---

## ?? Check What's Running Now

```powershell
# See all Functions and Node processes
Get-Process -Name "func","node" -ErrorAction SilentlyContinue | Select-Object ProcessName, Id, StartTime | Format-Table

# Check specific ports
netstat -ano | findstr ":7071 :6277 :5173"
```

---

## ??? Available Scripts

| Script | Purpose | When to Use |
|--------|---------|-------------|
| `cleanup-all.ps1` | ?? Kill all func.exe and optionally node.exe | Multiple processes running |
| `clear-port.ps1 [port]` | ?? Clear specific port | Know exact port number |
| `clear-port-7071.ps1` | ? Quick clear port 7071 | Functions won't start |
| `diagnose-mcp-inspector.ps1` | ?? Full diagnostic | Something's not working |
| `test-mcp-endpoint.ps1` | ? Test MCP endpoint | Verify endpoint works |
| `launch-inspector.bat` | ?? Start everything | Quick start |

---

## ?? Prevention Tips

### 1. Always Stop Cleanly
- Use `Ctrl+C` to stop Functions (not just closing window)
- Close MCP Inspector properly (not just browser)

### 2. Run Cleanup Before Starting
```powershell
.\cleanup-all.ps1
func start
```

### 3. Check Ports First
```powershell
# Check before starting
netstat -ano | findstr ":7071 :6277"
```

---

## ?? If Problems Persist

### 1. Run as Administrator
Some processes need admin rights to kill:
- Right-click PowerShell
- "Run as Administrator"
- Run `.\cleanup-all.ps1`

### 2. Use Task Manager
- Press `Ctrl+Shift+Esc`
- Go to "Details" tab
- Find and kill `func.exe` or `node.exe`

### 3. Restart Computer
Last resort - clears all processes and ports:
```powershell
Restart-Computer
```

---

## ?? Documentation

| Document | What's In It |
|----------|--------------|
| [CLEARING_PORTS.md](CLEARING_PORTS.md) | Complete port clearing guide |
| [MCP_INSPECTOR_QUICK_HELP.md](MCP_INSPECTOR_QUICK_HELP.md) | Quick troubleshooting |
| [README.md](README.md) | Main documentation |

---

## ? Summary

**Port 6277 is now clear!** 

You have three new powerful scripts:
1. `cleanup-all.ps1` - Complete cleanup
2. `clear-port.ps1` - Clear any port
3. `clear-port-7071.ps1` - Quick Functions port clear

**Next Steps**:
```powershell
# Option A: Fresh start
.\cleanup-all.ps1
func start

# Option B: Test what's running
.\test-mcp-endpoint.ps1

# Option C: Launch everything
.\launch-inspector.bat
```

?? **Ready to continue!**
