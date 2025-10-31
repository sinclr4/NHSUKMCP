# NHS Organisations MCP Server - Distribution Guide

This guide explains how to package and distribute your MCP server so others can easily install and use it.

## Distribution Methods

### 1. Python Package (PyPI) - RECOMMENDED

The Python version is the easiest to distribute and install.

#### Step 1: Prepare the Package

```bash
cd /Users/robsinclair/NHSUKMCP-Python

# Install build tools
pip install build twine

# Build the package
python -m build
```

This creates:
- `dist/nhs-organizations-mcp-1.0.0.tar.gz` (source distribution)
- `dist/nhs_organizations_mcp-1.0.0-py3-none-any.whl` (wheel)

#### Step 2: Test Locally

```bash
# Install in editable mode for testing
pip install -e .

# Test the command
nhs-orgs-mcp --help
```

#### Step 3: Publish to PyPI

```bash
# Upload to PyPI (you need a PyPI account)
python -m twine upload dist/*

# Or test on TestPyPI first
python -m twine upload --repository testpypi dist/*
```

#### Step 4: Users Install With

```bash
pip install nhs-organizations-mcp
```

#### Step 5: Users Configure Claude Desktop

Users add this to `~/Library/Application Support/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "nhs-organizations": {
      "command": "nhs-orgs-mcp",
      "env": {
        "AZURE_SEARCH_ENDPOINT": "https://nhsuksearchintuks.search.windows.net",
        "AZURE_SEARCH_API_KEY": "their-api-key",
        "AZURE_SEARCH_POSTCODE_INDEX": "postcodesandplaces-1-0-b-int",
        "AZURE_SEARCH_SERVICE_INDEX": "service-search-internal-3-11"
      }
    }
  }
}
```

---

### 2. npm Package (Node.js)

Good for JavaScript/TypeScript developers.

#### Step 1: Build Platform-Specific Binaries

```bash
cd /Users/robsinclair/NHSUKMCP

# Build for macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
mkdir -p bin/osx-x64
cp bin/Release/net9.0/osx-x64/publish/NHSUKMCP bin/osx-x64/

# Build for macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
mkdir -p bin/osx-arm64
cp bin/Release/net9.0/osx-arm64/publish/NHSUKMCP bin/osx-arm64/

# Build for Linux
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
mkdir -p bin/linux-x64
cp bin/Release/net9.0/linux-x64/publish/NHSUKMCP bin/linux-x64/

# Build for Windows
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
mkdir -p bin/win-x64
cp bin/Release/net9.0/win-x64/publish/NHSUKMCP.exe bin/win-x64/
```

#### Step 2: Publish to npm

```bash
# Login to npm
npm login

# Publish
npm publish --access public
```

#### Step 3: Users Install With

```bash
npm install -g @nhs/organizations-mcp-server

# Or use without installing
npx -y @nhs/organizations-mcp-server
```

#### Step 4: Users Configure Claude Desktop

```json
{
  "mcpServers": {
    "nhs-organizations": {
      "command": "npx",
      "args": ["-y", "@nhs/organizations-mcp-server"],
      "env": {
        "AZURE_SEARCH_ENDPOINT": "https://nhsuksearchintuks.search.windows.net",
        "AZURE_SEARCH_API_KEY": "their-api-key",
        "AZURE_SEARCH_POSTCODE_INDEX": "postcodesandplaces-1-0-b-int",
        "AZURE_SEARCH_SERVICE_INDEX": "service-search-internal-3-11"
      }
    }
  }
}
```

---

### 3. GitHub Releases (Binary Distribution)

For users who don't want to use package managers.

#### Step 1: Build Binaries

Same as npm package method - build for all platforms.

#### Step 2: Create Release on GitHub

1. Create a git repository
2. Tag a version: `git tag v1.0.0`
3. Push: `git push origin v1.0.0`
4. Create GitHub release
5. Upload binaries as assets:
   - `nhs-orgs-mcp-macos-intel.zip`
   - `nhs-orgs-mcp-macos-arm64.zip`
   - `nhs-orgs-mcp-linux-x64.zip`
   - `nhs-orgs-mcp-windows-x64.zip`

#### Step 3: Users Download and Install

```bash
# macOS example
curl -L https://github.com/yourusername/nhs-orgs-mcp/releases/download/v1.0.0/nhs-orgs-mcp-macos-arm64.zip -o nhs-orgs-mcp.zip
unzip nhs-orgs-mcp.zip
chmod +x nhs-orgs-mcp
mv nhs-orgs-mcp /usr/local/bin/
```

#### Step 4: Users Configure Claude Desktop

```json
{
  "mcpServers": {
    "nhs-organizations": {
      "command": "/usr/local/bin/nhs-orgs-mcp",
      "env": {
        "AZURE_SEARCH_ENDPOINT": "https://nhsuksearchintuks.search.windows.net",
        "AZURE_SEARCH_API_KEY": "their-api-key",
        "AZURE_SEARCH_POSTCODE_INDEX": "postcodesandplaces-1-0-b-int",
        "AZURE_SEARCH_SERVICE_INDEX": "service-search-internal-3-11"
      }
    }
  }
}
```

---

### 4. Docker Image

For containerized deployments.

#### Step 1: Build and Push

```bash
cd /Users/robsinclair/NHSUKMCP

# Build
docker build -t yourusername/nhs-orgs-mcp:latest .

# Push to Docker Hub
docker login
docker push yourusername/nhs-orgs-mcp:latest
```

#### Step 2: Users Run With Docker

```bash
docker run -it --rm \
  -e AZURE_SEARCH_ENDPOINT="https://nhsuksearchintuks.search.windows.net" \
  -e AZURE_SEARCH_API_KEY="their-api-key" \
  -e AZURE_SEARCH_POSTCODE_INDEX="postcodesandplaces-1-0-b-int" \
  -e AZURE_SEARCH_SERVICE_INDEX="service-search-internal-3-11" \
  yourusername/nhs-orgs-mcp:latest
```

---

## Comparison

| Method | Pros | Cons | Best For |
|--------|------|------|----------|
| **PyPI (pip)** | ✅ Easy install<br>✅ Cross-platform<br>✅ Python ecosystem | ❌ Requires Python | Python developers, recommended |
| **npm** | ✅ Easy install<br>✅ Works with npx<br>✅ Node ecosystem | ❌ Large binary size<br>❌ Platform-specific builds | JavaScript developers |
| **GitHub Releases** | ✅ No package manager<br>✅ Direct download | ❌ Manual installation<br>❌ No updates | One-off installs |
| **Docker** | ✅ Isolated environment<br>✅ Reproducible | ❌ Overhead<br>❌ Complex for Claude | Server deployments |

---

## Recommended Approach

**For Claude Desktop users**: Publish to **PyPI** (Python)
- Simplest installation: `pip install nhs-organizations-mcp`
- Cross-platform without building binaries
- Easy updates: `pip install --upgrade nhs-organizations-mcp`

**For Azure/HTTP API users**: Use the **Docker image** deployed to Azure Container Apps (already done!)

---

## Quick Start for PyPI Distribution

```bash
# 1. Navigate to Python project
cd /Users/robsinclair/NHSUKMCP-Python

# 2. Install build tools
pip install build twine

# 3. Build package
python -m build

# 4. Upload to PyPI (need account at pypi.org)
python -m twine upload dist/*

# That's it! Users can now:
pip install nhs-organizations-mcp
```

---

## File Checklist for Distribution

### Python Package (PyPI)
- ✅ `pyproject.toml` - Package metadata
- ✅ `README.md` - Installation instructions
- ✅ `LICENSE` - MIT or appropriate license
- ✅ `nhs_orgs_mcp/` - Source code
- ✅ `nhs_orgs_mcp/__init__.py` - Package init
- ✅ `nhs_orgs_mcp/server.py` - Main server with `main()` function

### npm Package
- ✅ `package.json` - Package metadata
- ✅ `README.md` - Installation instructions
- ✅ `LICENSE` - MIT or appropriate license
- ✅ `bin/server.js` - Launcher script
- ✅ `bin/osx-x64/NHSUKMCP` - macOS Intel binary
- ✅ `bin/osx-arm64/NHSUKMCP` - macOS ARM binary
- ✅ `bin/linux-x64/NHSUKMCP` - Linux binary
- ✅ `bin/win-x64/NHSUKMCP.exe` - Windows binary

---

## Next Steps

1. **Choose your distribution method** (Recommend: PyPI)
2. **Create accounts** (PyPI account, npm account, etc.)
3. **Add LICENSE file** (MIT recommended)
4. **Update README** with installation instructions
5. **Build and publish** following the steps above
6. **Test installation** on a clean machine
7. **Share with users!**
