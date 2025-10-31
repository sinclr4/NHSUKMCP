#!/bin/bash

# Test NHS MCP JSON-RPC Implementation
# Usage: ./test-mcp-jsonrpc.sh [base-url]

BASE_URL=${1:-"http://localhost:7071"}
MCP_ENDPOINT="$BASE_URL/mcp"

echo "=================================================="
echo "Testing NHS MCP JSON-RPC Implementation"
echo "Endpoint: $MCP_ENDPOINT"
echo "=================================================="
echo ""

# Test 1: Initialize
echo "Test 1: Initialize"
curl -s -X POST "$MCP_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "clientInfo": {
      "name": "test-client",
        "version": "1.0.0"
   }
    }
  }' | jq '.'
echo ""
echo "=================================================="
echo ""

# Test 2: List Tools
echo "Test 2: List Tools"
curl -s -X POST "$MCP_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
  }' | jq '.'
echo ""
echo "=================================================="
echo ""

# Test 3: Get Organisation Types
echo "Test 3: Get Organisation Types"
curl -s -X POST "$MCP_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
"params": {
    "name": "get_organisation_types",
      "arguments": {}
    }
  }' | jq '.'
echo ""
echo "=================================================="
echo ""

# Test 4: Convert Postcode
echo "Test 4: Convert Postcode to Coordinates"
curl -s -X POST "$MCP_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 4,
    "method": "tools/call",
    "params": {
    "name": "convert_postcode_to_coordinates",
      "arguments": {
  "postcode": "SW1A 1AA"
      }
    }
  }' | jq '.'
echo ""
echo "=================================================="
echo ""

# Test 5: Search by Postcode
echo "Test 5: Search Organisations by Postcode"
curl -s -X POST "$MCP_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 5,
 "method": "tools/call",
"params": {
  "name": "search_organisations_by_postcode",
      "arguments": {
  "organisationType": "PHA",
   "postcode": "SW1A 1AA",
        "maxResults": 3
      }
    }
  }' | jq '.'
echo ""
echo "=================================================="
echo ""

# Test 6: Search by Coordinates
echo "Test 6: Search Organisations by Coordinates"
curl -s -X POST "$MCP_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
  "id": 6,
    "method": "tools/call",
"params": {
      "name": "search_organisations_by_coordinates",
      "arguments": {
     "organisationType": "GPB",
 "latitude": 51.5074,
   "longitude": -0.1278,
        "maxResults": 3
      }
    }
  }' | jq '.'
echo ""
echo "=================================================="
echo ""

# Test 7: Get Health Topic
echo "Test 7: Get Health Topic"
curl -s -X POST "$MCP_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 7,
    "method": "tools/call",
    "params": {
  "name": "get_health_topic",
      "arguments": {
   "topic": "asthma"
   }
    }
  }' | jq '.'
echo ""
echo "=================================================="
echo ""

# Test 8: Ping
echo "Test 8: Ping"
curl -s -X POST "$MCP_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 8,
    "method": "ping",
    "params": {}
  }' | jq '.'
echo ""
echo "=================================================="
echo ""

# Test 9: Error Handling - Invalid Tool
echo "Test 9: Error Handling - Invalid Tool"
curl -s -X POST "$MCP_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
 "id": 9,
    "method": "tools/call",
    "params": {
    "name": "invalid_tool",
"arguments": {}
    }
  }' | jq '.'
echo ""
echo "=================================================="
echo ""

# Test 10: Error Handling - Invalid Postcode
echo "Test 10: Error Handling - Invalid Postcode"
curl -s -X POST "$MCP_ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 10,
    "method": "tools/call",
    "params": {
      "name": "convert_postcode_to_coordinates",
      "arguments": {
        "postcode": "INVALID"
      }
    }
  }' | jq '.'
echo ""
echo "=================================================="
echo ""

echo "All tests completed!"
echo ""
