#!/usr/bin/env node
/**
 * Example MCP Client for NHS Organizations Server
 * 
 * Install dependencies:
 *   npm install @modelcontextprotocol/sdk
 * 
 * Run:
 *   node example-client.js
 */

import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StdioClientTransport } from '@modelcontextprotocol/sdk/client/stdio.js';

async function main() {
  // Create client
  const client = new Client({
    name: 'nhs-orgs-client',
    version: '1.0.0'
  }, {
    capabilities: {}
  });

  // Connect to server via stdio
  const transport = new StdioClientTransport({
    command: '/Users/robsinclair/NHSOrgsMCP/bin/Release/net9.0/NHSOrgsMCP',
    env: {
      AZURE_SEARCH_ENDPOINT: 'https://nhsuksearchintuks.search.windows.net',
      AZURE_SEARCH_API_KEY: 'your-key-here',
      AZURE_SEARCH_POSTCODE_INDEX: 'postcodesandplaces-1-0-b-int',
      AZURE_SEARCH_SERVICE_INDEX: 'service-search-internal-3-11'
    }
  });

  await client.connect(transport);
  console.log('Connected to NHS Organizations MCP Server');

  // List available tools
  const tools = await client.listTools();
  console.log('\nAvailable tools:');
  tools.tools.forEach(tool => {
    console.log(`- ${tool.name}: ${tool.description}`);
  });

  // Example: Get organization types
  console.log('\n--- Getting Organization Types ---');
  const typesResult = await client.callTool({
    name: 'get_organization_types',
    arguments: {}
  });
  console.log(JSON.parse(typesResult.content[0].text));

  // Example: Convert postcode
  console.log('\n--- Converting Postcode SW1A 1AA ---');
  const postcodeResult = await client.callTool({
    name: 'convert_postcode_to_coordinates',
    arguments: { postcode: 'SW1A 1AA' }
  });
  console.log(JSON.parse(postcodeResult.content[0].text));

  // Example: Search pharmacies by postcode
  console.log('\n--- Searching Pharmacies near SW1A 1AA ---');
  const searchResult = await client.callTool({
    name: 'search_organizations_by_postcode',
    arguments: {
      organizationType: 'PHA',
      postcode: 'SW1A 1AA',
      maxResults: 3
    }
  });
  console.log(JSON.parse(searchResult.content[0].text));

  // Close connection
  await client.close();
  console.log('\nDisconnected');
}

main().catch(console.error);
