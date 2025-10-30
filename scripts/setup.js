#!/usr/bin/env node

/**
 * Post-install script for NHS Organizations MCP Server
 * Downloads and extracts the appropriate platform-specific binary
 */

import { platform, arch } from 'os';
import { existsSync, mkdirSync, chmodSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

console.log('Setting up NHS Organizations MCP Server...');

const platformName = platform();
const archName = arch();

console.log(`Platform: ${platformName}`);
console.log(`Architecture: ${archName}`);

// For now, users need to build or download the binary themselves
console.log(`
To complete installation:

1. Build the .NET binary:
   cd /Users/robsinclair/NHSOrgsMCP
   dotnet publish -c Release -r <runtime-identifier>

   Runtime identifiers:
   - osx-x64 (macOS Intel)
   - osx-arm64 (macOS Apple Silicon)
   - linux-x64 (Linux)
   - win-x64 (Windows)

2. Copy the binary to node_modules/@nhs/organizations-mcp-server/bin/<platform>/

3. Or, use npx to run directly:
   npx -y @nhs/organizations-mcp-server

For Claude Desktop configuration, see README.md
`);

console.log('Setup complete!');
